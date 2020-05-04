namespace CalloutsPlus.Callouts
{
    using System.Linq;

    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.LCPDFR.API;
    using LCPD_First_Response.LCPDFR.Callouts;
    using LCPD_First_Response.Engine.Timers;
    using System;

    [CalloutInfo("RTC", ECalloutProbability.Medium)]
    internal class RTC : Callout
    {
        private string[] victimModels = { "M_Y_THIEF", "M_Y_GRUS_LO_01", "M_Y_GRU2_LO_01", "M_Y_DOWNTOWN_03", "M_Y_DOWNTOWN_01"};
        private string[] vehicleModels = new string[] { "ADMIRAL", "FUTO", "SENTINAL", "SABREGT", "URANUS", "SOLAIR", "RUINER", "DUKES", "FELTZER", "ORACLE", "CAVALCADE", "BOBCAT"};
        private LHandle pursuit;
        private LPed vic1, vic2;
        private LVehicle vehicle1, vehicle2;
        private Vector3 spawnPosition;
        private Blip blip;

        public RTC()
        {
            this.CalloutMessage = string.Format("Reports of a Road Traffic Collision, any units available please respond.");
            Functions.PlaySoundUsingPosition("THIS_IS_CONTROL INS_WE_HAVE_A_REPORT_OF_ERRR CRIM_A_TRAFFIC_HAZARD IN_OR_ON_POSITION", this.spawnPosition);

            this.spawnPosition = World.GetNextPositionOnStreet(LPlayer.LocalPlayer.Ped.Position.Around(100.0f));

            while (this.spawnPosition.DistanceTo(LPlayer.LocalPlayer.Ped.Position) < 100.0f)
            {
                this.spawnPosition = World.GetNextPositionOnStreet(LPlayer.LocalPlayer.Ped.Position.Around(100.0f));
            }

            if (this.spawnPosition == Vector3.Zero)
            {
                // It obviously failed, set the position to be the player's position and the distance check will catch it.
                this.spawnPosition = LPlayer.LocalPlayer.Ped.Position;
            }

            this.ShowCalloutAreaBlipBeforeAccepting(this.spawnPosition, 50f);
            this.AddMinimumDistanceCheck(80f, this.spawnPosition);

        }

        [Flags]
        internal enum EPedState
        {
            None = 0x0,
            WaitingForPlayer = 0x1,
            PlayerOnScene = 0x2,
        }
                
        public override bool OnCalloutAccepted()
        {
            base.OnCalloutAccepted();

            this.pursuit = Functions.CreatePursuit();
            Functions.SetPursuitCopsCanJoin(this.pursuit, false);
            Functions.SetPursuitDontEnableCopBlips(this.pursuit, true);


            this.vehicle1 = new LVehicle(World.GetNextPositionOnStreet(this.spawnPosition), Common.GetRandomCollectionValue<string>(this.vehicleModels));
            if (vehicle1 != null && vehicle1.Exists())
            {
                vehicle1.PlaceOnNextStreetProperly();
                Vector3 dimensions = vehicle1.Model.GetDimensions();
                float width = dimensions.X;
                float length = dimensions.Y;

                this.vehicle2 = new LVehicle(vehicle1.GetOffsetPosition(new Vector3(0, length / 2 + 8, 0)), Common.GetRandomCollectionValue<string>(this.vehicleModels));
                if (vehicle2 != null && vehicle2.Exists())
                {
                    //vehicle2.PlaceOnNextStreetProperly();
                    DelayedCaller.Call(delegate
                    {
                        vehicle1.ApplyForceRelative(new Vector3(0, 60, 0));
                        DelayedCaller.Call(delegate
                        {
                            vehicle1.Speed = 0;
                            vehicle2.Speed = 0;
                            this.blip = Functions.CreateBlipForArea(vehicle1.Position, 20f);
                            this.blip.Display = BlipDisplay.ArrowAndMap;
                            this.blip.RouteActive = true;

                            vic1 = new LPed(vehicle1.Position.Around(2f), Common.GetRandomCollectionValue<string>(this.victimModels));
                            vic2 = new LPed(vehicle2.Position.Around(2f), Common.GetRandomCollectionValue<string>(this.victimModels));
                            
                            vic1.WarpIntoVehicle(vehicle1, VehicleSeat.Driver);
                            vic2.WarpIntoVehicle(vehicle2, VehicleSeat.Driver);
                            vic1.Health = 50;
                            vic2.Health = 20;
                            vic1.LeaveVehicle();
                            vic2.LeaveVehicle();
                            DelayedCaller.Call(delegate
                            {
                                vic1.Task.FightAgainst(vic2);
                                vic2.Task.FightAgainst(vic1);
                            }, this, 1000);
                            
                            
                        }, this, 500);
                    }, this, 1000);

                    Functions.PrintText(Functions.GetStringFromLanguageFile("CALLOUT_GET_TO_CRIME_SCENE"), 8000);
                    this.RegisterStateCallback(EPedState.WaitingForPlayer, this.WaitingForPlayer);
                    this.RegisterStateCallback(EPedState.PlayerOnScene, this.PlayerOnScene);
                    this.RegisterStateCallback(EPedState.None, this.CalloutOver);
                    this.State = EPedState.WaitingForPlayer;
                }
                else
                {
                    Functions.AddTextToTextwall("Disregard previous, situation is code 4", "CONTROL"); // vehicle 2 didn't spawn
                    this.End();
                }
            }
            else
            {
                Functions.AddTextToTextwall("Disregard previous, situation is code 4", "CONTROL"); // vehicle 1 didn't spawn
                this.End();
            }
            return true;
        }

        private void WaitingForPlayer()
        {
            if (LPlayer.LocalPlayer.Ped.Position.DistanceTo2D(vehicle1.Position) <= 25)
            {
                Functions.AddTextToTextwall("Control I'm on scene of the RTC now", "Officer " + LPlayer.LocalPlayer.Username);
                this.State = EPedState.PlayerOnScene;
            }
        }

        private void PlayerOnScene()
        {
            if (vehicle1.Exists())
            {
                vehicle1.NoLongerNeeded();
            }
            if (vehicle2.Exists())
            {
                vehicle2.NoLongerNeeded();
            }
            Functions.PrintText("Clear the crime scene and get traffic flowing again", 4000);
            this.State = EPedState.None;
        }

        private void CalloutOver()
        {
            if (!vehicle1.Exists() && !vehicle2.Exists())
            {
                this.End();
                Functions.AddTextToTextwall("Control, crime scene is clear, continuing with patrol", "Officer " + LPlayer.LocalPlayer.Username);
                Functions.AddTextToTextwall("10-4", "CONTROL");
            }
        }


        public override void Process()
        {
            base.Process();
        }

        public override void End()
        {
            base.End();
            if (vehicle1 != null && vehicle1.Exists())
            {
                this.vehicle1.Delete();
            }
            if (vehicle2 != null && vehicle2.Exists())
            {
                this.vehicle2.Delete();
            }
            if (this.blip != null && this.blip.Exists())
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
        
