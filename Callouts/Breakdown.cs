namespace CalloutsPlus.Callouts
{
    using System;
    using System.Collections.Generic;

    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.API;
    using LCPD_First_Response.LCPDFR.Callouts;
    using System.Windows.Forms;

    /// <summary>
    /// The shootout callout.
    /// </summary>
    [CalloutInfo("Breakdown", ECalloutProbability.Medium)]
    internal class Breakdown : Callout
    {
        private string[] victimModels = { "M_Y_THIEF", "M_Y_THIEF", "M_Y_GRUS_LO_01", "M_Y_GRU2_LO_01" };
        private string[] vehicleModels = new string[] { "FUTO", "SENTINAL", "SABREGT", "URANUS", "SOLAIR", "RUINER", "DUKES", "FELTZER" };
        private LHandle pursuit;
        private LVehicle vehicle1;
        private LPed driver;
        private Vector3 spawnPosition;
        private Blip blip;
        private PlayerFunctions BreakdownObj = new PlayerFunctions();

        public Breakdown()
        {
            this.CalloutMessage = string.Format("This is control, we have a report of a broken down vehicle blocking the highway, please respond.");
            Functions.PlaySoundUsingPosition("THIS_IS_CONTROL INS_WE_HAVE_A_REPORT_OF_ERRR CRIM_A_TRAFFIC_HAZARD IN_OR_ON_POSITION", this.spawnPosition);

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
        }

        public override bool OnCalloutAccepted()
        {
            base.OnCalloutAccepted();

            this.pursuit = Functions.CreatePursuit();
            Functions.SetPursuitCopsCanJoin(this.pursuit, false);
            Functions.SetPursuitDontEnableCopBlips(this.pursuit, true);

            this.blip = Functions.CreateBlipForArea(this.spawnPosition, 20f);
            this.blip.Display = BlipDisplay.ArrowAndMap;
            this.blip.RouteActive = true;

            this.vehicle1 = new LVehicle(World.GetNextPositionOnStreet(this.spawnPosition), Common.GetRandomCollectionValue<string>(this.vehicleModels));
            if (this.vehicle1.Exists())
            { 
                // Ensure vehicle is freed on end
                Functions.AddToScriptDeletionList(this.vehicle1, this);
                this.vehicle1.PlaceOnNextStreetProperly();
                this.vehicle1.AttachBlip();
                this.vehicle1.EngineHealth = 50;
                driver = this.vehicle1.CreatePedOnSeat(VehicleSeat.Driver);
                Functions.SetPedIsOwnedByScript(driver, this, true);
                DelayedCaller.Call(delegate
                {
                    driver.Task.CruiseWithVehicle(vehicle1, 20, true);
                }, this, 1000);
                DelayedCaller.Call(delegate
                {
                    this.vehicle1.HazardLightsOn = true;
                    this.vehicle1.EngineHealth = 0;
                }, this, 4000);
                Functions.PrintText(Functions.GetStringFromLanguageFile("CALLOUT_GET_TO_CRIME_SCENE"), 8000);
            }
            return true;
        }

        public override void Process()
        {
            base.Process();
            if (CalloutsPlusMain.RepairEngineModifierKey == Keys.None) //Checks to see if the modifier is None
            {
                if (Functions.IsKeyDown(CalloutsPlusMain.RepairEngineKey))
                {
                    BreakdownObj.RepairVehicle(vehicle1);
                }
            }
            else
            {
                if (Functions.IsKeyStillDown(CalloutsPlusMain.RepairEngineModifierKey) && Functions.IsKeyDown(CalloutsPlusMain.RepairEngineKey))
                {
                    BreakdownObj.RepairVehicle(vehicle1);
                }
            }

            if (this.vehicle1.Exists())
            {
                if (this.vehicle1.EngineHealth > 60)
                {
                    Functions.PrintHelp("The vehicle's engine is fixed, order the pedestrian to get back in his vehicle or deal with him however you choose.");
                    Functions.SetPedIsOwnedByScript(driver, this, false);
                    this.driver.Task.StandStill(-1);
                    this.End();
                }
            }
            else
            {
                this.End();
            }
            

        }

        public override void End()
        {
            base.End();
            //this.vehicle1.Delete();
            if (this.vehicle1.Exists())
            {
                this.vehicle1.NoLongerNeeded();
            }
            if (this.blip.Exists())
            {
                this.blip.Delete();
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
