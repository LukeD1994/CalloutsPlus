namespace CalloutsPlus.Callouts
{
    using System;
    using System.Collections.Generic;

    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.API;
    using LCPD_First_Response.LCPDFR.Callouts;

    /// <summary>
    /// The shootout callout.
    /// </summary>
    [CalloutInfo("Manhunt", ECalloutProbability.Low)]
    internal class Manhunt : Callout
    {
        private ECalloutType calloutType;
        private string[] criminalModels = { "M_Y_THIEF", "M_Y_THIEF", "M_Y_GRUS_LO_01", "M_Y_GRU2_LO_01", "M_Y_GMAF_LO_01", "M_Y_GMAF_HI_01", "M_Y_GTRI_LO_01", "M_Y_GTRI_LO_02", "M_Y_GALB_LO_01", "M_Y_GALB_LO_02" };
        private LHandle pursuit;
        private LPed suspect;
        private Vector3 spawnPosition;

        public Manhunt()
        {
            this.calloutType = ECalloutType.FootChase; //(ECalloutType)Common.GetRandomEnumValue(typeof(ECalloutType));

            this.spawnPosition = World.GetNextPositionOnStreet(LPlayer.LocalPlayer.Ped.Position.Around(400.0f));

            while (this.spawnPosition.DistanceTo(LPlayer.LocalPlayer.Ped.Position) < 100.0f)
            {
                this.spawnPosition = World.GetNextPositionOnStreet(LPlayer.LocalPlayer.Ped.Position.Around(400.0f));
            }

            if (this.spawnPosition == Vector3.Zero)
            {
                // It obviously failed, set the position to be the player's position and the distance check will catch it.
                this.spawnPosition = LPlayer.LocalPlayer.Ped.Position;
            }

            this.ShowCalloutAreaBlipBeforeAccepting(this.spawnPosition, 50f);
            this.AddMinimumDistanceCheck(80f, this.spawnPosition);

            if (this.calloutType == ECalloutType.FootChase)
            {
                this.CalloutMessage = string.Format("Attention all units, we have a reports of a criminal evading police on foot, please respond.");
                Functions.PlaySoundUsingPosition("ATTENTION_ALL_UNITS INS_WE_HAVER_A_REPORT_OF_ERRR SUSPECT ON_FOOT IN_OR_ON_LOCATION", this.spawnPosition);
            }
            else if (this.calloutType == ECalloutType.Manhunt)
            {
                this.CalloutMessage = string.Format("Attention all units, request a perimeter search for a HVT");
                Functions.PlaySoundUsingPosition("ATTENTION_ALL_UNITS INS_WE_HAVE_A_REPORT_OF_ERRR CRIM_A_SUSPECT_RESISTING_ARREST IN_OR_ON_POSITION", this.spawnPosition);

            }
        }

        private enum ECalloutType
        {
            FootChase,
            Manhunt,
        }

        public override bool OnCalloutAccepted()
        {
            base.OnCalloutAccepted();

            this.pursuit = Functions.CreatePursuit();

            this.suspect = new LPed(World.GetNextPositionOnStreet(this.spawnPosition), Common.GetRandomCollectionValue<string>(this.criminalModels), LPed.EPedGroup.Criminal);

            this.suspect.BlockPermanentEvents = true;
            this.suspect.Task.AlwaysKeepTask = true;

            Functions.AddToScriptDeletionList(this.suspect, this);
            Functions.AddPedToPursuit(this.pursuit, this.suspect);

            if (!Functions.DoesPedHaveAnOwner(this.suspect))
            {
                Functions.SetPedIsOwnedByScript(this.suspect, this, true);
            }

            if (this.calloutType == ECalloutType.FootChase)
            {
                Functions.SetPursuitCalledIn(this.pursuit, true);
                Functions.SetPursuitIsActiveForPlayer(this.pursuit, true);
                Functions.PrintText(Functions.GetStringFromLanguageFile("CALLOUT_ROBBERY_CATCH_UP"), 25000);

                this.suspect.WantedByPolice = true;

                LPed cop = new LPed(this.spawnPosition, "M_Y_COP");
                Functions.AddToScriptDeletionList(cop, this);

                if (cop.Exists())
                {
                    cop.Task.AimAt(suspect, 4000);
                    cop.Task.AlwaysKeepTask = true;
                }
                //Functions.RequestPoliceBackupAtPosition(this.suspect.Position);
            }
            else
            {
                Functions.SetPursuitCalledIn(this.pursuit, true);
                Functions.SetPursuitIsActiveForPlayer(this.pursuit, true);
                Functions.RequestPoliceBackupAtPosition(this.spawnPosition);
                this.suspect.DeleteBlip();
                this.suspect.WantedByPolice = true;
                this.suspect.RangeToDetectEnemies = 10f;
                this.suspect.EquipWeapon();
            }

            return true;
        }

        public override void Process()
        {
            base.Process();

            if (suspect.HasBeenArrested == true)
            {
                this.SetCalloutFinished(true, true, true);
                this.End();
            }

            if (!Functions.IsPursuitStillRunning(this.pursuit))
            {
                this.SetCalloutFinished(true, true, true);
                this.End();
            }
        }


        public override void End()
        {
            base.End();

            if (this.pursuit != null)
            {
                Functions.ForceEndPursuit(this.pursuit);
            }

        }

        public override void PedLeftScript(LPed ped)
        {
            base.PedLeftScript(ped);

            Functions.RemoveFromDeletionList(ped, this);
            Functions.SetPedIsOwnedByScript(ped, this, false);
        }
    }

}
