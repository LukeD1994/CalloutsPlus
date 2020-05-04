using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using CalloutsPlus.Callouts;
using CalloutsPlus.Properties;
using LCPD_First_Response.Engine.Scripting.Entities;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using GTA;
using GTA.Forms;

using LCPD_First_Response.Engine;
using LCPD_First_Response.Engine.Input;
using LCPD_First_Response.Engine.Scripting.Plugins;
using LCPD_First_Response.Engine.Timers;
using LCPD_First_Response.LCPDFR.API;

namespace CalloutsPlus
{
    [PluginInfo("Callouts+", false, true)]
    public class CalloutsPlusMain : Plugin
    {
        
        Version versionID = typeof(CalloutsPlusMain).Assembly.GetName().Version; // This should now always link to the assembly version

        //Defined variable keys for the configuration file to assign
        public static Keys RequestParamedicKey, RequestParamedicModifierKey, RequestRemovalKey, RequestRemovalModifierKey, RevivePedKey, RevivePedModifierKey,
            RepairEngineKey, RepairEngineModifierKey, VehicleHazardsKey, VehicleHazardsModifierKey, VehicleCheckKey, VehicleCheckModifierKey, TowTruckKey, TowTruckModifierKey,
            PlaceBarrierKey, PlaceBarrierModifierKey, QuestionSuspectKey, QuestionSuspectModifierKey;

        public int ParamedicPlayerHealthBelow;
        public static int DrinkDriveLimit;
        public static bool QuickSpawnMethod;
        public static bool AutoVehicleCheck;

        public bool HasVehicleBeenChecked = false;
        public bool HasQuestioningFired = false;




        public LVehicle pulloverCar;

        //creating an instance of the various function classes to be used in the main process
        Garages gar1 = new Garages();
        Barriers barriers;
        Dispatcher dispatcher;
        PlayerFunctions playerFunc;


        public override void Initialize()
        {

            barriers = new Barriers();
            dispatcher = new Dispatcher();
            playerFunc = new PlayerFunctions();

            //Creates a configuration file and then registers all of the lines to variables
            IniFile ini = new IniFile(Path.GetDirectoryName(Application.ExecutablePath) + @"\LCPDFR\plugins\CalloutsPlus.ini");
            //KEYBINDS REGISTERED HERE
            RequestParamedicKey = (Keys)Enum.Parse(typeof(Keys), ini.IniReadValue("KEYBINDS", "RequestParamedic"));
            RequestParamedicModifierKey = (Keys)Enum.Parse(typeof(Keys), ini.IniReadValue("KEYBINDS", "RequestParamedicModifier"));
            RequestRemovalKey = (Keys)Enum.Parse(typeof(Keys), ini.IniReadValue("KEYBINDS", "RequestRemoval"));
            RequestRemovalModifierKey = (Keys)Enum.Parse(typeof(Keys), ini.IniReadValue("KEYBINDS", "RequestRemovalModifier"));
            RevivePedKey = (Keys)Enum.Parse(typeof(Keys), ini.IniReadValue("KEYBINDS", "RevivePed"));
            RevivePedModifierKey = (Keys)Enum.Parse(typeof(Keys), ini.IniReadValue("KEYBINDS", "RevivePedModifier"));
            RepairEngineKey = (Keys)Enum.Parse(typeof(Keys), ini.IniReadValue("KEYBINDS", "RepairEngine"));
            RepairEngineModifierKey = (Keys)Enum.Parse(typeof(Keys), ini.IniReadValue("KEYBINDS", "RepairEngineModifier"));
            VehicleHazardsKey = (Keys)Enum.Parse(typeof(Keys), ini.IniReadValue("KEYBINDS", "VehicleHazards"));
            VehicleHazardsModifierKey = (Keys)Enum.Parse(typeof(Keys), ini.IniReadValue("KEYBINDS", "VehicleHazardsModifier"));
            VehicleCheckKey = (Keys)Enum.Parse(typeof(Keys), ini.IniReadValue("KEYBINDS", "VehicleCheck"));
            VehicleCheckModifierKey = (Keys)Enum.Parse(typeof(Keys), ini.IniReadValue("KEYBINDS", "VehicleCheckModifier"));
            TowTruckKey = (Keys)Enum.Parse(typeof(Keys), ini.IniReadValue("KEYBINDS", "TowTruck"));
            TowTruckModifierKey = (Keys)Enum.Parse(typeof(Keys), ini.IniReadValue("KEYBINDS", "TowTruckModifier"));
            PlaceBarrierKey = (Keys)Enum.Parse(typeof(Keys), ini.IniReadValue("KEYBINDS", "PlaceBarrier"));
            PlaceBarrierModifierKey = (Keys)Enum.Parse(typeof(Keys), ini.IniReadValue("KEYBINDS", "PlaceBarrierModifier"));
            QuestionSuspectKey = (Keys)Enum.Parse(typeof(Keys), ini.IniReadValue("KEYBINDS", "QuestionSuspect"));
            QuestionSuspectModifierKey = (Keys)Enum.Parse(typeof(Keys), ini.IniReadValue("KEYBINDS", "QuestionSuspectModifier"));

            // Determine paramedic health of player
            String paramedicPlayerHealth = ini.IniReadValue("MAIN", "ParamedicPlayerHealthBelow");
            Int32.TryParse(paramedicPlayerHealth, out ParamedicPlayerHealthBelow);

            //UK or US style breath test and value
            String DrinkDriveLimit1 = ini.IniReadValue("TRAFFICSTOP", "DrinkDriveLimit");
            Int32.TryParse(DrinkDriveLimit1, out DrinkDriveLimit);

            // Determine if the quick spawn method should be used
            String QuickSpawnMethod1 = ini.IniReadValue("MAIN", "QuickSpawnMethod");
            bool.TryParse(QuickSpawnMethod1, out QuickSpawnMethod);

            // Determine if the vehicle check should be automatic or manual
            String AutoVehicleCheck1 = ini.IniReadValue("MAIN", "AutoVehicleCheck");
            bool.TryParse(AutoVehicleCheck1, out AutoVehicleCheck);

            // Bind console commands
            this.RegisterConsoleCommands();

            // Listen for on duty event
            Functions.OnOnDutyStateChanged += this.Functions_OnOnDutyStateChanged;
            Log.Info("Plugin detected version: " + versionID, this);
            Log.Info("Checking calloutsPlus.ini file...", this);
            if (QuickSpawnMethod)
            {
                Log.Info("QuickSpawnMethod set to TRUE, all dispatch vehicles will spawn significantly closer to the player", this);
            }
            if (AutoVehicleCheck)
            {
                Log.Info("AutoVehicleCheck set to TRUE, the vehicle check will be called on each player pullover", this);
            }
            Log.Info("Reading keybinds from configuration file", this);
            Log.Info("Plugin started", this);

        }
        //Checks for the change of state when the player toggles duty
        public void Functions_OnOnDutyStateChanged(bool onDuty)
        {
            if (onDuty)
            {
                Functions.RegisterCallout(typeof(CruiserTheft));
                Functions.RegisterCallout(typeof(Manhunt));
                Functions.RegisterCallout(typeof(Murder));
                Functions.RegisterCallout(typeof(Races));
                //Functions.RegisterCallout(typeof(Riot));
                Functions.RegisterCallout(typeof(RTC));
                Functions.RegisterCallout(typeof(LocationEvent));
                Functions.RegisterCallout(typeof(Breakdown));
                Functions.RegisterCallout(typeof(OfficerDown));
                Functions.RegisterCallout(typeof(TrackerTheft));

                Functions.AddTextToTextwall("Using Callouts+ Version: " + versionID, "SYSTEM");


            }
        }
        //Main tick for the plugin
        public override void Process()
        {

            //Testing area
            
            //end


            if (LPlayer.LocalPlayer.IsOnDuty && AutoVehicleCheck)
            {
                if (Functions.IsPlayerPerformingPullover())
                {
                    LHandle pullover = Functions.GetCurrentPullover();
                    if (pullover != null)
                    {
                        if (HasVehicleBeenChecked == false)
                        {
                            HasVehicleBeenChecked = true;
                            DelayedCaller.Call(delegate
                            {
                                playerFunc.VehicleIdCheck();

                            }, this, 2000);

                        }
                    }
                }
                else
                {
                    HasVehicleBeenChecked = false;
                }
            }

            if (LPlayer.LocalPlayer.LastVehicle != null)
            {
                //Keybind check for the repair function
                if (IsKeyPressed(RepairEngineKey, RepairEngineModifierKey, false))
                {
                    playerFunc.RepairVehicle(LPlayer.LocalPlayer.LastVehicle);
                }
                //Keybind check for the vehicle hazard lights toggle
                if (IsKeyPressed(VehicleHazardsKey, VehicleHazardsModifierKey, false))
                {
                    playerFunc.StopLights();
                }

            }

            //Keybind check for revive system
            if (IsKeyPressed(RevivePedKey, RevivePedModifierKey, false))
            {
                playerFunc.Revive();
            }
            //Keybind check for removal team dispatch
            if (IsKeyPressed(RequestRemovalKey, RequestRemovalModifierKey, true))
            {
                if (dispatcher.removalCalledOut == false)
                {
                    dispatcher.RequestRemoval();
                    dispatcher.removalCalledOut = true;
                }
                else
                {
                    if (dispatcher.removalCanCancel)
                    {
                        Functions.AddTextToTextwall("Dispatch, situation code 4. Removal no longer required", "Officer " + LPlayer.LocalPlayer.Username);
                        Functions.AddTextToTextwall("10-4, removal has been advised.", "DISPATCH");
                        dispatcher.driver.NoLongerNeeded();
                        dispatcher.dropOffDriver.NoLongerNeeded();
                        dispatcher.copCar.AttachBlip().Delete();
                        dispatcher.copCar.NoLongerNeeded();
                        if (dispatcher.removalCar.Exists())
                        {
                            dispatcher.removalCar.NoLongerNeeded();
                        }

                        dispatcher.removalCalledOut = false;
                    }

                }
            }
            //Keybind check for paramedic dispatch
            if (IsKeyPressed(RequestParamedicKey, RequestParamedicModifierKey, true))
            {
                if (dispatcher.paramedicCalledOut == false)
                {
                    DelayedCaller.Call(delegate
                    {
                        dispatcher.RequestMedic();
                        dispatcher.paramedicCalledOut = true;
                    }, this, 12000);

                }
                else
                {
                    if (dispatcher.paramedicCanCancel == true)
                    {
                        Functions.AddTextToTextwall("Dispatch, situation code 4. Paramedic no longer required", "Officer " + LPlayer.LocalPlayer.Username);
                        Functions.AddTextToTextwall("10-4, paramedic has been advised.", "DISPATCH");
                        dispatcher.para1.NoLongerNeeded();
                        dispatcher.ambulance.AttachBlip().Delete();
                        dispatcher.ambulance.NoLongerNeeded();

                        dispatcher.paramedicCalledOut = false;
                    }

                }
            }
            if (IsKeyPressed(VehicleCheckKey, VehicleCheckModifierKey, true))
            {
                playerFunc.VehicleIdCheck();
            }
            //Keybind check for tow truck dispatch
            if (IsKeyPressed(TowTruckKey, TowTruckModifierKey, true))
            {
                if (dispatcher.towTruckCalledOut == false)
                {
                    dispatcher.RequestTowTruck();
                    dispatcher.towTruckCalledOut = true;
                }
                else
                {
                    if (dispatcher.towTruckCanCancel == true)
                    {
                        Functions.AddTextToTextwall("Dispatch, situation code 4. Tow truck no longer required", "Officer " + LPlayer.LocalPlayer.Username);
                        Functions.AddTextToTextwall("10-4, tow truck has been advised.", "DISPATCH");
                        dispatcher.truckDriver.NoLongerNeeded();
                        dispatcher.truck.AttachBlip().Delete();
                        dispatcher.truck.NoLongerNeeded();

                        dispatcher.towTruckCalledOut = false;
                    }
                }
            }
            //Keybind check for barrier placement
            if (IsKeyPressed(PlaceBarrierKey, PlaceBarrierModifierKey, true))
            {
                barriers.PlaceBarrier();
            }

            //Keybind for the questioning feature
            if (LPlayer.LocalPlayer.IsOnDuty)
            {
                if (Functions.IsPlayerPerformingPullover())
                {
                    LHandle po = Functions.GetCurrentPullover();
                    pulloverCar = Functions.GetPulloverVehicle(po);
                    playerFunc.sCar = pulloverCar;
                }
                if (LPlayer.LocalPlayer.LastVehicle != null)
                {
                    if (!HasQuestioningFired)
                    {
                        var suspect = LPlayer.LocalPlayer.LastVehicle.GetPedOnSeat(LPlayer.LocalPlayer.LastVehicle.IsSeatFree(VehicleSeat.LeftRear) ? VehicleSeat.RightRear : VehicleSeat.LeftRear);
                        if (suspect != null && suspect.Exists() && suspect.Model != "M_Y_COP")
                        {
                            Functions.PrintHelp("You've detained a ~r~suspect~w~ inside your vehicle. You can question them by entering the driver's seat and pressing Q.");
                            HasQuestioningFired = true;
                        }
                    }
                }
            }

            if (LPlayer.LocalPlayer.Ped.IsInVehicle() && IsKeyPressed(QuestionSuspectKey, QuestionSuspectModifierKey, true))
            {
                playerFunc.Questioning();
            }


        }
        //Method for clearing anything when the script ends
        public override void Finally()
        {
        }

        //Helper function to determine keybinds with modifiers PLUS onduty state for specific functions
        public bool IsKeyPressed(Keys keybind, Keys modifier, bool DutyCheck)
        {
            if (DutyCheck)
            {
                if (LPlayer.LocalPlayer.IsOnDuty)
                {
                    if (modifier == Keys.None)
                    {
                        return Functions.IsKeyDown(keybind);
                    }
                    else
                    {
                        return Functions.IsKeyStillDown(modifier) && Functions.IsKeyDown(keybind);
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (modifier == Keys.None)
                {
                    return Functions.IsKeyDown(keybind);
                }
                else
                {
                    return Functions.IsKeyStillDown(modifier) && Functions.IsKeyDown(keybind);
                }
            }


        }

        //Define a new console command and parameters
        [ConsoleCommand("StartCallout", false)]
        private void StartCallout(ParameterCollection parameterCollection)
        {
            if (parameterCollection.Count > 0)
            {
                string name = parameterCollection[0];
                Functions.StartCallout(name);
            }
            else
            {
                Game.Console.Print("StartCallout: No argument given.");
            }
        }

        //Command for saving location
        [ConsoleCommand("saveloc", false)]
        private void SaveLocation(ParameterCollection parameterCollection)
        {
            if (parameterCollection.Count > 0)
            {
                string name = parameterCollection[0];
                Log.Info("[SaveLocation(X,Y,Z)] " + name + " " + LPlayer.LocalPlayer.Ped.Position.X + "f, " + LPlayer.LocalPlayer.Ped.Position.Y + "f, " + LPlayer.LocalPlayer.Ped.Position.Z + "f", this);
                Functions.PrintHelp("Saved Location to log file");
            }
            else
            {
                Game.Console.Print("No location name entered");
            }

        }
    }
}