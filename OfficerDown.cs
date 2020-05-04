using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalloutsPlus.Callouts
{
    using GTA;
    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.API;
    using LCPD_First_Response.LCPDFR.Callouts;
    using System;
    using System.Collections.Generic;

    [CalloutInfo("officerdown", ECalloutProbability.Low)]
    internal class OfficerDown : Callout
    {
        private LPed officer, suspect;
        private LVehicle getawayCar, copCar;
        private Vector3 spawnPosition;
        private LHandle pursuit;
        private Blip blip, LastKnownBlip;

        private string[] criminalModels = { "M_Y_THIEF", "M_Y_THIEF", "M_Y_GRUS_LO_01", "M_Y_GRU2_LO_01", "M_Y_GMAF_LO_01", "M_Y_GMAF_HI_01", "M_Y_GTRI_LO_01", "M_Y_GTRI_LO_02", "M_Y_GALB_LO_01", "M_Y_GALB_LO_02" };
        private string[] vehicleModels = new string[] { "ADMIRAL", "FUTO", "SENTINAL", "SABREGT", "URANUS", "SOLAIR", "RUINER", "DUKES", "FELTZER", "ORACLE", "CAVALCADE", "BOBCAT" };

        public OfficerDown()
        {
            CalloutMessage = string.Format("All units we have an officer down, available units please respond.");
            Functions.PlaySoundUsingPosition("THIS_IS_CONTROL INS_WE_HAVE_A_REPORT_OF_ERRR CRIM_AN_OFFICER_DOWN IN_OR_ON_POSITION", spawnPosition);

            spawnPosition = World.GetNextPositionOnStreet(LPlayer.LocalPlayer.Ped.Position.Around(400.0f));

            while (spawnPosition.DistanceTo(LPlayer.LocalPlayer.Ped.Position) < 100.0f)
            {
                spawnPosition = World.GetNextPositionOnStreet(LPlayer.LocalPlayer.Ped.Position.Around(400.0f));
            }

            if (spawnPosition == Vector3.Zero)
            {
                // It obviously failed, set the position to be the player's position and the distance check will catch it.
                spawnPosition = LPlayer.LocalPlayer.Ped.Position;
            }

            ShowCalloutAreaBlipBeforeAccepting(spawnPosition, 50f);
            AddMinimumDistanceCheck(80f, spawnPosition);

        }


        [Flags]
        internal enum EPedState
        {
            None = 0x0,
            WaitingForPlayer = 0x1,
            PlayerOnScene = 0x2,
            PlayerSearching = 0x4,
            Over = 0x5,
        }

        public override bool OnCalloutAccepted()
        {
            base.OnCalloutAccepted();

            copCar = new LVehicle(World.GetNextPositionOnStreet(spawnPosition), "POLICE");

            officer = new LPed(spawnPosition.Around(2f), "M_Y_COP");
            if (copCar != null && copCar.Exists())
            {
                copCar.PlaceOnNextStreetProperly();
                copCar.SirenActive = true;
            }

            suspect = new LPed(spawnPosition, Common.GetRandomCollectionValue<string>(criminalModels));
            getawayCar = new LVehicle(World.GetNextPositionOnStreet(spawnPosition), Common.GetRandomCollectionValue<string>(vehicleModels));

            if (getawayCar != null && getawayCar.Exists())
            {
                getawayCar.PlaceOnNextStreetProperly();
                if (suspect.Exists())
                {
                    suspect.WarpIntoVehicle(getawayCar, VehicleSeat.Driver);

                    officer.Task.Die();
                    suspect.Task.CruiseWithVehicle(getawayCar, 15, true);
                    blip = Functions.CreateBlipForArea(spawnPosition, 20f);
                    blip.Display = BlipDisplay.ArrowAndMap;
                    blip.RouteActive = true;


                    Functions.PrintText(Functions.GetStringFromLanguageFile("CALLOUT_GET_TO_CRIME_SCENE"), 8000);
                    RegisterStateCallback(EPedState.WaitingForPlayer, WaitingForPlayer);
                    RegisterStateCallback(EPedState.PlayerOnScene, PlayerOnScene);
                    RegisterStateCallback(EPedState.Over, CalloutOver);
                    RegisterStateCallback(EPedState.PlayerSearching, PlayerSearching);
                    State = EPedState.WaitingForPlayer;
                }
                else
                {
                    Functions.AddTextToTextwall("Disregard previous, situation is code 4.", "CONTROL");
                    End();
                }
            }
            else
            {
                Functions.AddTextToTextwall("Disregard previous, situation is code 4.", "CONTROL");
                End();
            }


            return true;
        }

        private void WaitingForPlayer()
        {
            if (LPlayer.LocalPlayer.Ped.Position.DistanceTo(officer.Position) < 20f)
            {
                Functions.AddTextToTextwall("Control I'm on scene now stand-by", "Officer " + LPlayer.LocalPlayer.Username);
                Functions.PrintText("Check the condition of the officer by pressing E while next to them", 4000);
                State = EPedState.PlayerOnScene;
            }
        }

        private void PlayerOnScene()
        {
            if (LPlayer.LocalPlayer.Ped.Position.DistanceTo(officer.Position) <= 3f)
            {
                if (Functions.IsKeyDown(System.Windows.Forms.Keys.E))
                {
                    LPlayer.LocalPlayer.Ped.Task.TurnTo(officer);
                    DelayedCaller.Call(delegate
                    {
                        LPlayer.LocalPlayer.Ped.Animation.Play(new AnimationSet("medic"), "medic_cpr_in", 4.0f);
                        DelayedCaller.Call(delegate
                        {
                            LPlayer.LocalPlayer.Ped.Animation.Play(new AnimationSet("medic"), "medic_cpr_loop", 4.0f);
                            DelayedCaller.Call(delegate
                            {
                                LPlayer.LocalPlayer.Ped.Animation.Play(new AnimationSet("medic"), "medic_cpr_out", 4.0f);
                                Functions.PrintHelp("The officer is injured but stable and will be dealt with soon, locate the suspect.");
                                Vector3 LastKnown = new Vector3();
                                if (getawayCar.Exists())
                                {
                                    LastKnown = getawayCar.Position;
                                }
                                else
                                {
                                    End();
                                }
                                Functions.PrintText("The suspect is in a " + getawayCar.Color + " " + getawayCar.Name + ". A blip has been placed at their last known location.", 4000);
                                if (officer.Exists())
                                {
                                    officer.NoLongerNeeded();
                                }
                                if (copCar.Exists())
                                {
                                    copCar.NoLongerNeeded();
                                }
                                if (blip.Exists())
                                {
                                    blip.Delete();
                                }
                                
                                LastKnownBlip = Functions.CreateBlipForArea(LastKnown, 40f);
                                if (LastKnownBlip.Exists())
                                {
                                    LastKnownBlip.Display = BlipDisplay.ArrowAndMap;
                                    LastKnownBlip.RouteActive = true;
                                }
                                State = EPedState.PlayerSearching;
                            }, this, 6000);
                        }, this, 2000);
                    }, this, 2000);
                }
            }
        }
        private void PlayerSearching()
        {
            if (LastKnownBlip.Exists() && LPlayer.LocalPlayer.Ped.Position.DistanceTo(LastKnownBlip.Position) < 50f)
            {
                //LastKnownBlip.Display = BlipDisplay.Hidden;
                LastKnownBlip.Delete();
            }
            //Has the player spotted the suspect (in front is set to false meaning position can be anywhere)
            if (LPlayer.LocalPlayer.Ped.HasSpottedPed(suspect, false))
            {
                pursuit = Functions.CreatePursuit();
                suspect.AttachBlip().Color = BlipColor.Red;
                Functions.AddTextToTextwall("Control I've located the suspect, in pursuit.", "Officer " + LPlayer.LocalPlayer.Username);
                Functions.AddPedToPursuit(pursuit, suspect);
                Functions.SetPursuitCalledIn(pursuit, true);

                Functions.SetPursuitCopsCanJoin(pursuit, true);
                Functions.SetPursuitIsActiveForPlayer(pursuit, true);
                State = EPedState.None;
            }

            if (LPlayer.LocalPlayer.Ped.Position.DistanceTo(getawayCar.Position) > 600f)
            {
                Functions.AddTextToTextwall("Control, suspect was never found, resuming patrol.", "Officer " + LPlayer.LocalPlayer.Username);
                Functions.AddTextToTextwall("Affirmative, we'll send details to ANPR database.", "CONTROL");
                End();
            }
        }

        private void CalloutOver()
        {
            if (suspect.HasBeenArrested)
            {
                Functions.PrintText("All arrested!", 4000);
                SetCalloutFinished(true, true, true);

                End();
            }
        }

        public override void End()
        {
            base.End();
            if (pursuit != null)
            {
                Functions.ForceEndPursuit(pursuit);
            }
            if (copCar != null && copCar.Exists())
            {
                copCar.Delete();
            }
            if (getawayCar != null && getawayCar.Exists())
            {
                getawayCar.NoLongerNeeded();
            }
            if (blip != null && blip.Exists())
            {
                blip.Delete();
            }
            if (LastKnownBlip != null && LastKnownBlip.Exists())
            {
                LastKnownBlip.Delete();
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