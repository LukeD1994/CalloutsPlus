using System.Windows.Forms;

namespace CalloutsPlus.Callouts
{
    using GTA;
    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.API;
    using LCPD_First_Response.LCPDFR.Callouts;
    using System;
    using System.Collections.Generic;

    [CalloutInfo("Murder", ECalloutProbability.VeryLow)]
    internal class Murder : Callout
    {
        private string[] criminalModels = { "M_Y_THIEF", "M_Y_THIEF", "M_Y_GRUS_LO_01", "M_Y_GRU2_LO_01", "M_Y_GMAF_LO_01", "M_Y_GMAF_HI_01", "M_Y_GTRI_LO_01", "M_Y_GTRI_LO_02", "M_Y_GALB_LO_01", "M_Y_GALB_LO_02" };
        private LHandle pursuit;
        ///private LPed[] peds;
        private LPed victim;
        private SpawnPoint spawnPoint;
        private Blip blip;
        private Boolean alive;

        public Murder()
        {
           this.CalloutMessage = string.Format("Report of a severely injured victim, possibly deceased, please advise.");
           Functions.PlaySoundUsingPosition("THIS_IS_CONTROL INS_WE_HAVE_A_REPORT_OF_ERRR CRIM_A_CIVILIAN_FATALITY IN_OR_ON_POSITION", this.spawnPoint.Position);
        }

        [Flags]
        internal enum EPedState
        {
            None = 0x0,
            WaitingForPlayer = 0x1,
            PlayerIsClose = 0x2,
            PlayerOnScene = 0x4,
        }

        public override bool OnBeforeCalloutDisplayed()
        {
            this.spawnPoint = Callout.GetSpawnPointInRange(LPlayer.LocalPlayer.Ped.Position, 100, 400);

            if (this.spawnPoint == SpawnPoint.Zero)
            {
                return false;
            }

            this.ShowCalloutAreaBlipBeforeAccepting(this.spawnPoint.Position, 20f);
            this.AddMinimumDistanceCheck(80f, this.spawnPoint.Position);

            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted()
        {
            base.OnCalloutAccepted();

            this.pursuit = Functions.CreatePursuit();
            Functions.SetPursuitCopsCanJoin(this.pursuit, false);
            Functions.SetPursuitDontEnableCopBlips(this.pursuit, true);

            this.blip = Functions.CreateBlipForArea(this.spawnPoint.Position, 20f);
            this.blip.Display = BlipDisplay.ArrowAndMap;
            this.blip.RouteActive = true;

            this.victim = new LPed(this.spawnPoint.Position, Common.GetRandomCollectionValue<string>(this.criminalModels), LPed.EPedGroup.MissionPed);
            if (victim.Exists())
            {
                if (victim.EnsurePedIsNotInBuilding(victim.Position))
                {
                    Functions.AddToScriptDeletionList(victim, this);
                    Functions.SetPedIsOwnedByScript(victim, this, true);
                    Functions.AddPedToPursuit(this.pursuit, victim);
                    //this.victim.Die();

                    this.victim.AttachBlip();
                    int random = Common.GetRandomValue(0, 100);
                    if (random <= 14)
                    {
                        this.victim.Health = 10;
                        //victim.FreezePosition = true;
                        //victim.ForceRagdoll(-1, false);
                        victim.Die();
                        
                        victim.Task.AlwaysKeepTask = true;
                        this.victim.HasBeenDamagedBy(Weapon.Melee_Knife);
                        alive = true;
                    }
                    else
                    {
                        //this.victim.Health = 0;
                        this.victim.HasBeenDamagedBy(Weapon.Melee_Knife);
                        this.victim.Die();
                        alive = false;
                    }
                }
                else
                {
                    Log.Debug("OnCalloutAccepted: Failed to place ped properly outside of building", this);
                    victim.Delete();
                }
            }
            this.RegisterStateCallback(EPedState.WaitingForPlayer, this.WaitingForPlayer);
            this.RegisterStateCallback(EPedState.PlayerIsClose, this.PlayerIsClose);
            this.RegisterStateCallback(EPedState.PlayerOnScene, this.PlayerOnScene);
            this.RegisterStateCallback(EPedState.None, this.CalloutOver);
            this.State = EPedState.WaitingForPlayer;
            Functions.PrintText(Functions.GetStringFromLanguageFile("CALLOUT_GET_TO_CRIME_SCENE"), 8000);
            return true;

        }

        public override void Process()
        {
            base.Process();

        }

        public override void End()
        {
            base.End();

            this.State = EPedState.None;

            this.victim.DeleteBlip();
            this.victim.Delete();


            this.SetCalloutFinished(true, true, true);

            if (this.pursuit != null)
            {
                Functions.ForceEndPursuit(this.pursuit);
            }

            if (this.blip != null && this.blip.Exists())
            {
                this.blip.Delete();
            }


        }

        private void WaitingForPlayer()
        {
            if (LPlayer.LocalPlayer.Ped.Position.DistanceTo(this.spawnPoint.Position) > 50)
            {
                return;
            }

            
            Functions.PrintText("Confirm the status of the victim.", 4000);
            Functions.AddTextToTextwall("Control I'm on scene now stand by.", "Officer " + LPlayer.LocalPlayer.Username);
            this.State = EPedState.PlayerIsClose;
        }

        private void PlayerIsClose()
        {
            if (LPlayer.LocalPlayer.Ped.Position.DistanceTo(this.victim.Position) < 5)
            {
                Functions.PrintHelp("You can check the status of the victim by pressing E while next to them.");
                this.State = EPedState.PlayerOnScene;
            }
        }

        private void PlayerOnScene()
        {
            if (Functions.IsKeyDown(Keys.E))
            {
                if (victim != null && LPlayer.LocalPlayer.Ped.Position.DistanceTo(victim.Position) < 3.0f)
                {
                    LPlayer.LocalPlayer.Ped.Task.TurnTo(victim);
                    DelayedCaller.Call(delegate
                    {
                        LPlayer.LocalPlayer.Ped.Animation.Play(new AnimationSet("medic"), "medic_cpr_in", 4.0f);

                        DelayedCaller.Call(delegate
                        {
                            LPlayer.LocalPlayer.Ped.Animation.Play(new AnimationSet("medic"), "medic_cpr_loop", 4.0f);

                            DelayedCaller.Call(delegate
                            {
                                LPlayer.LocalPlayer.Ped.Animation.Play(new AnimationSet("medic"), "medic_cpr_out", 4.0f);
                                if (alive == false)
                                {
                                    Functions.PrintText("The victim is deceased, secure the crime scene.", 4000);
                                    Functions.PrintHelp("Use " + CalloutsPlusMain.RequestParamedicModifierKey + " + " + CalloutsPlusMain.RequestParamedicKey + " to call for a paramedic who will clear the victim away. Be sure to secure the scene first!");
                                    this.blip.Delete();
                                    this.victim.Detach();
                                    this.State = EPedState.None;

                                }
                                else
                                {
                                    Functions.PrintText("The victim is still alive, call for a paramedic!", 4000);
                                    this.State = EPedState.None;
                                }

                            }, this, 6000);
                        }, this, 2000);
                    }, this, 2000);
                }
            }
        }

        private void CalloutOver()
        {
            if (!victim.Exists())
            {
                this.End();
            }
        }


        public override void PedLeftScript(LPed ped)
        {
            base.PedLeftScript(victim);

            // Free ped
            Functions.RemoveFromDeletionList(victim, this);
            Functions.SetPedIsOwnedByScript(victim, this, false);
            this.End();
        }


    }
}
