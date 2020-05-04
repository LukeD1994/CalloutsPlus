namespace CalloutsPlus.Callouts
{
    using System.Linq;

    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.LCPDFR.API;
    using LCPD_First_Response.LCPDFR.Callouts;

    [CalloutInfo("Race", ECalloutProbability.Medium)]
    internal class Races : Callout
    {
        /// <summary>
        /// The internal type of the callout.
        /// </summary>
        private ECalloutType calloutType;

        /// <summary>
        /// Criminal models that can be used.
        /// </summary>
        private string[] criminalModels = { "M_Y_THIEF", "M_Y_THIEF", "M_Y_GRUS_LO_01", "M_Y_GRU2_LO_01", "M_Y_GMAF_LO_01", "M_Y_GMAF_HI_01", "M_Y_GTRI_LO_01", "M_Y_GTRI_LO_02", "M_Y_GALB_LO_01", "M_Y_GALB_LO_02" };

        /// <summary>
        /// Vehicle models that can be used.
        /// </summary>
        private string[] vehicleModels = new string[] { "FUTO", "SENTINAL", "SABREGT", "URANUS", "RUINER", "DUKES", "FELTZER" };

        /// <summary>
        /// The pursuit.
        /// </summary>
        private LHandle pursuit;
        //private LHandle pursuit2;

        /// <summary>
        /// The robbers.
        /// </summary>
        private LPed driver1;
        private LPed driver2;

        /// <summary>
        /// The vehicle.
        /// </summary>
        private LVehicle vehicle;
        private LVehicle vehicle2;

        /// <summary>
        /// The position at which the vehicles are spawned
        /// </summary>
        private Vector3 spawnPosition;

        /// <summary>
        /// Initializes a new instance of the <see cref="CruiserTheft"/> class.
        /// </summary>
        public Races()
        {
            // Determine type
            this.calloutType = ECalloutType.Race; //(ECalloutType)Common.GetRandomEnumValue(typeof(ECalloutType));

            // Get a good position
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

            // Show user where the pursuit is about to happen
            this.ShowCalloutAreaBlipBeforeAccepting(this.spawnPosition, 50f);
            this.AddMinimumDistanceCheck(80f, this.spawnPosition);

            // Set up message
            if (this.calloutType == ECalloutType.Race)
            {
                this.CalloutMessage = string.Format("Attention all units, illegal street race in progress, officer's requiring assitance.", Functions.GetAreaStringFromPosition(this.spawnPosition));

                Functions.PlaySoundUsingPosition("THIS_IS_CONTROL ATTENTION_ALL_UNITS INS_WE_HAVE_A_REPORT_OF_ERRR STREET_RACE_CRIME_AN_ILLEGAL_STREET_RACE_IN_PROGRESS IN_OR_ON_POSITION", this.spawnPosition);

            }
            else if (this.calloutType == ECalloutType.GangMeeting)
            {
                this.CalloutMessage = string.Format("Officers in need of assistance, gang members evading pursuit, all units please respond.", Functions.GetAreaStringFromPosition(this.spawnPosition));
                Functions.PlaySoundUsingPosition(ESound.PursuitAcknowledged, this.spawnPosition);
            }

        }

        /// <summary>
        /// The internal type of the callout.
        /// </summary>
        private enum ECalloutType
        {

            Race,

            GangMeeting,

        }


        /// <summary>
        /// Called when the callout has been accepted. Call base to set state to Running.
        /// </summary>
        /// <returns>
        /// True if callout was setup properly, false if it failed. Calls <see cref="End"/> when failed.
        /// </returns>
        public override bool OnCalloutAccepted()
        {
            base.OnCalloutAccepted();

            // Create pursuit instance
            this.pursuit = Functions.CreatePursuit();

            // Create 
            this.vehicle = new LVehicle(World.GetNextPositionOnStreet(this.spawnPosition), Common.GetRandomCollectionValue<string>(this.vehicleModels));
            this.vehicle2 = new LVehicle(World.GetNextPositionOnStreet(this.spawnPosition), Common.GetRandomCollectionValue<string>(this.vehicleModels));
            if (this.vehicle.Exists() && this.vehicle2.Exists())
            {
                // Ensure vehicle is freed on end
                Functions.AddToScriptDeletionList(this.vehicle, this);
                Functions.AddToScriptDeletionList(this.vehicle2, this);
                this.vehicle.PlaceOnNextStreetProperly();
                this.vehicle2.PlaceOnNextStreetProperly();

                // Create suspects
                // Spawn ped
                this.driver1 = new LPed(World.GetNextPositionOnStreet(this.vehicle.Position), Common.GetRandomCollectionValue<string>(this.criminalModels), LPed.EPedGroup.Criminal);
                this.driver2 = new LPed(World.GetNextPositionOnStreet(this.vehicle2.Position), Common.GetRandomCollectionValue<string>(this.criminalModels), LPed.EPedGroup.Criminal);
                if (this.driver1.Exists() && this.driver2.Exists())
                {
                    // If vehicle doesn't have a driver yet, warp robber as driver
                    if (!this.vehicle.HasDriver && !this.vehicle2.HasDriver)
                    {
                        this.driver1.WarpIntoVehicle(this.vehicle, VehicleSeat.Driver);
                        this.driver2.WarpIntoVehicle(this.vehicle2, VehicleSeat.Driver);
                    }

                    // Make ignore all events and give default weapon
                    this.driver1.BlockPermanentEvents = true;
                    this.driver1.Task.AlwaysKeepTask = true;
                    this.driver2.BlockPermanentEvents = true;
                    this.driver2.Task.AlwaysKeepTask = true;

                    if (this.calloutType == ECalloutType.GangMeeting)
                    {
                        this.driver1.EquipWeapon();
                        this.driver2.EquipWeapon();
                    }
                    else
                    {
                        // When not a robbery, 1/6 chance of weapons allowed only
                        bool allowWeapons = Common.GetRandomBool(0, 7, 1);
                        Functions.SetPursuitAllowWeaponsForSuspects(this.pursuit, allowWeapons);
                    }

                    // Add to deletion list and to pursuit
                    Functions.AddToScriptDeletionList(this.driver1, this);
                    Functions.AddToScriptDeletionList(this.driver2, this);
                    Functions.AddPedToPursuit(this.pursuit, this.driver1);
                    Functions.AddPedToPursuit(this.pursuit, this.driver2);

                    // Parity check, just to show how to normally use this API. No need if you just created the ped,
                    // since no other script could own it already
                    if (!Functions.DoesPedHaveAnOwner(this.driver1) && !Functions.DoesPedHaveAnOwner(this.driver2))
                    {
                        // Bind ped to script so it can't be used by other scripts, such as random scenarios
                        // Because we now own this script, we also have to define behavior what we want to do when another script
                        // takes over control, e.g. when being arrested. That's why we implement PedLeftScript below
                        Functions.SetPedIsOwnedByScript(this.driver1, this, true);
                        Functions.SetPedIsOwnedByScript(this.driver2, this, true);
                    }
                }
                // Create Police
                LVehicle copCar = new LVehicle(World.GetNextPositionOnStreet(this.vehicle.Position), "POLICE");
                if (copCar.Exists())
                {
                    Functions.AddToScriptDeletionList(copCar, this);
                    copCar.PlaceOnNextStreetProperly();

                    LPed copDriver = copCar.CreatePedOnSeat(VehicleSeat.Driver);

                    if (copDriver != null && copDriver.Exists())
                    {
                        Functions.AddToScriptDeletionList(copDriver, this);
                        copCar.SirenActive = true;
                        copDriver.SayAmbientSpeech("PULL_OVER_WARNING");
                    }
                }

                // Since we want other cops to join, set as called in already and also active it for player
                Functions.SetPursuitCalledIn(this.pursuit, true);
                Functions.SetPursuitIsActiveForPlayer(this.pursuit, true);

                // Show message to the player
                Functions.PrintText(Functions.GetStringFromLanguageFile("CALLOUT_ROBBERY_CATCH_UP"), 25000);
            }

            return true;
        }

        /// <summary>
        /// Called every tick to process all script logic. Call base when overriding.
        /// </summary>
        public override void Process()
        {
            base.Process();

            // Print text message when all suspect have been arrested
                if (driver1 != null && driver1.Exists() && driver2 != null && driver2.Exists())
                {
                    if (driver1.HasBeenArrested && driver2.HasBeenArrested)
                    {
                        Functions.PrintText("All arrested!", 5000);
                        this.SetCalloutFinished(true, true, true);
                        this.End();
                    }
                }


            // End this script is pursuit is no longer running, e.g. because all suspects are dead
            if (!Functions.IsPursuitStillRunning(this.pursuit))
            {
                this.SetCalloutFinished(true, true, true);
                this.End();
            }
        }

        /// <summary>
        /// Put all resource free logic here. This is either called by the calloutmanager to shutdown the callout or can be called by the 
        /// callout itself to execute the cleanup code. Call base to set state to None.
        /// </summary>
        public override void End()
        {
            base.End();

            // End pursuit if still running
            if (this.pursuit != null)
            {
                Functions.ForceEndPursuit(this.pursuit);
            }
        }

        /// <summary>
        /// Called when a ped assigned to the current script has left the script due to a more important action, such as being arrested by the player.
        /// This is invoked right before control is granted to the new script, so perform all necessary freeing actions right here.
        /// </summary>
        /// <param name="ped">The ped</param>
        public override void PedLeftScript(LPed ped)
        {
            base.PedLeftScript(ped);

            // Free ped
            Functions.RemoveFromDeletionList(ped, this);
            Functions.SetPedIsOwnedByScript(ped, this, false);
        }
    }
}