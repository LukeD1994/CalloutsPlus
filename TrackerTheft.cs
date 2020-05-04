namespace CalloutsPlus.Callouts
{
    using System.Linq;

    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.LCPDFR.API;
    using LCPD_First_Response.LCPDFR.Callouts;

    //Callout for the tracker theft, a stolen vehicle fitted with a police tracker
    [CalloutInfo("Tracker", ECalloutProbability.Medium)]
    internal class TrackerTheft : Callout
    {
        private string[] criminalModels = { "M_Y_THIEF", "M_Y_THIEF", "M_Y_GRUS_LO_01", "M_Y_GRU2_LO_01", "M_Y_GMAF_LO_01", "M_Y_GMAF_HI_01", "M_Y_GTRI_LO_01", "M_Y_GTRI_LO_02", "M_Y_GALB_LO_01", "M_Y_GALB_LO_02" };
        private string[] vehicleModels = { "ADMIRAL", "TURISMO", "COGNOSCENTI", "ORACLE", "SENTINEL", "SCHAFTER", "COMET" };

        private LHandle pursuit;
        private LPed criminal;
        private LVehicle vehicle;
        private Vector3 spawnPosition;
        private Timer timer;
        private bool isTrackerActive = false;
        private int signal;
        private Blip blip;

        public TrackerTheft()
        {
            spawnPosition = World.GetNextPositionOnStreet(LPlayer.LocalPlayer.Ped.Position.Around(400f));
            
            while (spawnPosition.DistanceTo(LPlayer.LocalPlayer.Ped.Position) < 100.0f)
            {
                spawnPosition = World.GetNextPositionOnStreet(LPlayer.LocalPlayer.Ped.Position.Around(400.0f));
            }

            if (spawnPosition == Vector3.Zero)
            {
                // It obviously failed, set the position to be the player's position and the distance check will catch it.
                spawnPosition = LPlayer.LocalPlayer.Ped.Position;
            }

            // Show user where the pursuit is about to happen
            ShowCalloutAreaBlipBeforeAccepting(spawnPosition, 50f);
            AddMinimumDistanceCheck(80f, spawnPosition);

            CalloutMessage = string.Format("Available units around " + Functions.GetAreaStringFromPosition(spawnPosition) + " a vehicle fitted with a tracker has been reported stolen, please respond.");
            Functions.PlaySoundUsingPosition("INS_AVAILABLE_UNITS_RESPOND_TO CRIM_A_STOLEN_VEHICLE", spawnPosition);

        }

        public override bool OnCalloutAccepted()
        {
            base.OnCalloutAccepted();

            //create the vehicle and then check to make sure it exists, if so begin creating the criminal
            vehicle = new LVehicle(World.GetNextPositionOnStreet(spawnPosition), Common.GetRandomCollectionValue<string>(vehicleModels));
            if (vehicle != null && vehicle.Exists())
            {
                Functions.AddToScriptDeletionList(vehicle, this);
                vehicle.PlaceOnNextStreetProperly();

                criminal = new LPed(World.GetNextPositionOnStreet(vehicle.Position), Common.GetRandomCollectionValue<string>(criminalModels));
                if (criminal.Exists())
                {
                    //if criminal exists, warp into vehicle and begin driving
                    criminal.WarpIntoVehicle(vehicle, VehicleSeat.Driver);
                    criminal.Task.CruiseWithVehicle(vehicle, 20f, true);

                    //add him to the deletion list and set him owned by the script
                    Functions.AddToScriptDeletionList(criminal, this);
                    if (!Functions.DoesPedHaveAnOwner(criminal))
                    {
                        Functions.SetPedIsOwnedByScript(criminal, this, true);
                    }

                    blip = Functions.CreateBlipForArea(spawnPosition, 20f);
                    blip.Display = BlipDisplay.ArrowAndMap;
                    blip.RouteActive = true;

                    //Functions.PrintText("Get to the reported location and find the stolen vehicle", 4000);
                    Functions.AddTextToTextwall("Dispatch I'll head that up, enabling tracker now. Do we know the vehicle make?", LPlayer.LocalPlayer.Username);
                    Functions.AddTextToTextwall("Details show the vehicle to be a " + vehicle.Name, "CONTROL");
                    timer = new GTA.Timer(1000);
                    timer.Tick += timer_Tick;
                    timer.Start();
                }
                else
                {
                    Functions.AddTextToTextwall("Disregard, situation is code 4", "CONTROL");
                    //end
                }
            }
            else
            {
                Functions.AddTextToTextwall("Disregard, situation is code 4", "CONTROL");
                //end
            }

            return true;
        }

        //timer to handle the tracker beeps
        void timer_Tick(object sender, System.EventArgs e)
        {
            if (LPlayer.LocalPlayer.Ped.Position.DistanceTo(spawnPosition) < 40f)
            {
                isTrackerActive = true;
                blip.Delete();
            }
            //simple bool check, if the player has reached the scene the tracker activates
            if (isTrackerActive)
            {
                signal = (500 - (int)LPlayer.LocalPlayer.Ped.Position.DistanceTo2D(vehicle.Position)) / 5;
                if (signal <= 0)
                {
                    Functions.PrintText("Signal Strenght: 0%", 1000);
                }
                else if (signal < -100)
                {
                    Functions.AddTextToTextwall("Control we've lost the vehicle's signal entirely, resuming patrol", LPlayer.LocalPlayer.Username);
                    isTrackerActive = false;
                    timer.Stop();
                    End();
                }
                else
                {
                    Functions.PrintText("Signal Strength: " + signal + "%", 1000);
                }

                if (signal >=92)//LPlayer.LocalPlayer.Ped.HasSpottedPed(criminal, false))
                {
                    isTrackerActive = false;
                    pursuit = Functions.CreatePursuit();
                    Functions.AddPedToPursuit(pursuit, criminal);
                    Functions.SetPursuitCalledIn(pursuit, true);
                    Functions.SetPursuitCopsCanJoin(pursuit, true);
                    Functions.SetPursuitIsActiveForPlayer(pursuit, true);
                    Functions.AddTextToTextwall("Control I've got a visual on the stolen vehicle, engaging.", LPlayer.LocalPlayer.Username);
                }
            }
            
        }

        public override void Process()
        {
            base.Process();

            //if the player spots the criminal it calls in the pursuit instantly and disables the tracker
            

            if (criminal.HasBeenArrested)
            {
                Functions.PrintText("All arrested!", 5000);
                SetCalloutFinished(true, true, true);
                End();
            }
            
        }

        public override void End()
        {
            base.End();

            if (vehicle.Exists())
            {
                vehicle.NoLongerNeeded();
            }

            if (pursuit != null)
            {
                Functions.ForceEndPursuit(pursuit);
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
