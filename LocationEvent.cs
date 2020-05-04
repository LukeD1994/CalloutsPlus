namespace CalloutsPlus.Callouts
{
    using System.Linq;

    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.LCPDFR.API;
    using LCPD_First_Response.LCPDFR.Callouts;
    using System.Collections.Generic;

    //Callout for the location based events
    [CalloutInfo("locevent", ECalloutProbability.Medium)]
    internal class LocationEvent : Callout
    {
        //Declaration of variables to be used 

        private string[] criminalModels = { "M_Y_THIEF", "M_Y_THIEF", "M_Y_GRUS_LO_01", "M_Y_GRU2_LO_01", "M_Y_GMAF_LO_01", "M_Y_GMAF_HI_01", "M_Y_GTRI_LO_01", "M_Y_GTRI_LO_02", "M_Y_GALB_LO_01", "M_Y_GALB_LO_02" };
        private string[] vehicleModels = { "ADMIRAL", "TURISMO", "COGNOSCENTI", "ORACLE", "SENTINEL", "SCHAFTER", "COMET" };
        private static Dictionary<SpawnPoint, string> barPositions = new Dictionary<SpawnPoint, string>()
                                                                         {
                                                                             { new SpawnPoint(0.0f, new Vector3(20.94f, 981.63f, 15.63f)), "Superstar_Cafe" },
                                                                             { new SpawnPoint(0.0f, new Vector3(-166.44f, 599.95f, 14.71f)), "Majestic_Hotel" },
                                                                             { new SpawnPoint(0.0f, new Vector3(-173.56f, 287.72f, 14.88f)), "Burgershot_Alg" },
                                                                             { new SpawnPoint(0.0f, new Vector3(-123.75f, 70.19f, 14.81f)), "Cluckinbell_Alg" },
                                                                             { new SpawnPoint(0.0f, new Vector3(10.60f, -663.82f, 17.87f)), "Persues_Alg" },
                                                                             { new SpawnPoint(0.0f, new Vector3(-335.42f, 1393.53f, 12.92f)), "Twat_Alg" },
                                                                             { new SpawnPoint(0.0f, new Vector3(-280.44f, 1363.46f, 25.64f)), "Modo_Nr_Alg" },
                                                                             { new SpawnPoint(0.0f, new Vector3(1083.21f, 641.24f, 38.71f)), "Laundromat_Duk"},
                                                                             { new SpawnPoint(0.0f, new Vector3(957.57f, -271.29f, 18.12f)), "Cabaret_Bro"},
                                                                             { new SpawnPoint(0.0f, new Vector3(884.34f, -484.17f, 15.88f)), "69diner_Bro"},
                                                                             { new SpawnPoint(0.0f, new Vector3(971.75f, -171.43f, 24.19f)), "Twat_Bro"},
                                                                             { new SpawnPoint(0.0f, new Vector3(1641.01f, 225.92f, 25.21f)), "Burgershot_Bro"},
                                                                             { new SpawnPoint(0.0f, new Vector3(1108.82f, 1586.81f, 16.91f)), "Burgershot_Boh"},
                                                                         };
        private string roomName;
        private LHandle pursuit;
        private LPed criminal;
        private Vector3 spawnPosition;
        private bool IsGunman, HasPedBeenDesignated;
        private Blip blip;
        //Constructor
        public LocationEvent()
        {
            var closestBar = (from element in barPositions.Keys
                              orderby element.Position.DistanceTo2D(LPlayer.LocalPlayer.Ped.Position)
                              select element).First();

            roomName = barPositions[closestBar];
            string place = "";
            this.spawnPosition = closestBar.Position;
            this.ShowCalloutAreaBlipBeforeAccepting(this.spawnPosition, 50f);
            this.AddMinimumDistanceCheck(80f, this.spawnPosition);

            switch (roomName)
            {
                case "Superstar_Cafe":
                    place = "Superstar Cafe, ";
                    break;
                case "Majestic_Hotel":
                    place = "The Majestic Hotel, ";
                    break;
                case "Burgershot_Alg":
                    place = "Burgershot, ";
                    break;
                case "Cluckinbell_Alg":
                    place = "Cluckin Bell, ";
                    break;
                case "Persues_Alg":
                    place = "Persues clothes store, ";
                    break;
                case "Twat_Alg":
                    place = "TW@T Internet Cafe, ";
                    break;
                case "Modo_Nr_Alg":
                    place = "MODO clothes store, ";
                    break;
                case "Laundromat_Duk":
                    place = "a Laundromat, ";
                    break;
                case "Cabaret_Bro":
                    place = "the Cabaret Club, ";
                    break;
                case "69diner_Bro":
                    place = "a diner, ";
                    break;
                case "Twat_Bro":
                    place = "TW@T Internet Cafe, ";
                    break;
                case "Burgershot_Bro":
                    place = "Burgershot, ";
                    break;
                case "Burgershot_Boh":
                    place = "Burgershot, ";
                    break;
            }
            this.CalloutMessage = string.Format("Available units respond to suspicious activity at " + place + Functions.GetAreaStringFromPosition(this.spawnPosition));
            Functions.PlaySoundUsingPosition("INS_AVAILABLE_UNITS_RESPOND_TO CRIM_SUSPICIOUS_ACTIVITY", this.spawnPosition);
        }

        //Fired when callout is accepted
        public override bool OnCalloutAccepted()
        {
            base.OnCalloutAccepted();

            this.blip = Functions.CreateBlipForArea(this.spawnPosition, 20f);
            this.blip.Display = BlipDisplay.ArrowAndMap;
            this.blip.RouteActive = true;


            this.criminal = new LPed(this.spawnPosition.Around(5f), Common.GetRandomCollectionValue<string>(this.criminalModels));
            if (this.criminal.Exists())
            {
                this.criminal.CurrentRoom = Room.FromString(roomName);
                //criminal.Task.RunTo(this.spawnPosition);
                if (!Functions.DoesPedHaveAnOwner(this.criminal))
                {
                    Functions.SetPedIsOwnedByScript(this.criminal, this, true);
                }
                IsGunman = Common.GetRandomBool(0, 5, 1);
                HasPedBeenDesignated = false;

                Functions.PrintText(Functions.GetStringFromLanguageFile("CALLOUT_GET_TO_CRIME_SCENE"), 8000);
                return true;
            }
            else
            {
                Functions.AddTextToTextwall("Disregard, situation code 4.", "CONTROL");
                return false;//this.End();
            }
        }

        //game tick
        public override void Process()
        {
            base.Process();
            if (!HasPedBeenDesignated)
            {
                if (LPlayer.LocalPlayer.Ped.Position.DistanceTo(this.spawnPosition) <= 20f)
                {
                    this.blip.Delete();
                    if (IsGunman)
                    {
                        this.criminal.EquipWeapon();
                        this.pursuit = Functions.CreatePursuit();
                        Functions.AddPedToPursuit(this.pursuit, this.criminal);
                        Functions.SetPursuitCalledIn(pursuit, true);
                        Functions.SetPursuitCopsCanJoin(pursuit, true);
                        Functions.SetPursuitForceSuspectsToFight(pursuit, true);
                        Functions.SetPursuitIsActiveForPlayer(pursuit, true);
                        Functions.AddTextToTextwall("Control I've got a suspect with a gun, send backup!", LPlayer.LocalPlayer.Username);
                        Functions.AddTextToTextwall("Affirmative, units around you have been advised.", "CONTROL");
                        
                        HasPedBeenDesignated = true;
                    }
                    else
                    {
                        this.criminal.ItemsCarried = LPed.EPedItem.Drugs;
                        this.criminal.Weapons.Knife.Select();
                        this.criminal.StartKillingSpree(true);
                        Functions.PrintHelp("This suspect doesn't look right, he may be under the influence of an illegal susbstance");
                        criminal.AttachBlip();
                        HasPedBeenDesignated = true;
                    }
                }
            }


            if (this.criminal.HasBeenArrested)
            {
                this.End();
            }
        }

        //Called when callout is over
        public override void End()
        {
            base.End();

            if (this.criminal.Exists())
            {
                this.criminal.NoLongerNeeded();
                criminal.AttachBlip().Delete();
            }
            if (pursuit != null)
            {
                Functions.ForceEndPursuit(pursuit);
            }
        }

        //Delete peds if they leave script
        public override void PedLeftScript(LPed ped)
        {
            base.PedLeftScript(ped);

            Functions.RemoveFromDeletionList(ped, this);
            Functions.SetPedIsOwnedByScript(ped, this, false);
        }
    }
}