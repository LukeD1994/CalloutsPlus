using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalloutsPlus
{
    using GTA;
    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Scripting.Plugins;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.API;


    class PlayerFunctions : GameScript
    {
        private bool hazard, IsTrafficStop;
        private GTA.Timer qTimer;
        CalloutsPlusMain m = new CalloutsPlusMain();

        private LPed suspect;
        public LVehicle pCar, sCar = null;

        public void RepairVehicle(Vehicle vehiclePar)
        {
            Vehicle vehicle = vehiclePar;
            Vector3 dimensions = vehicle.Model.GetDimensions();
            float width = dimensions.X;
            float length = dimensions.Y;

            Vector3 leftCorner = vehicle.GetOffsetPosition(new Vector3(-width / 2, length / 2 - 0.5f, 0));
            Vector3 rightCorner = vehicle.GetOffsetPosition(new Vector3(width / 2, length / 2 - 0.5f, 0));

            //Build rectangle by extending our left/ight corner vectors with the ful length now
            Vector3 leftOutmostCorner = vehicle.GetOffsetPosition(new Vector3(-width / 2, length - 1, 0));
            Vector3 rightOutmostCorner = vehicle.GetOffsetPosition(new Vector3(width / 2, length - 1, 0));

            Vector3 position = LPlayer.LocalPlayer.Ped.Position;
            bool isInRectangle = ((leftOutmostCorner.X - leftCorner.X) * (position.Y - leftCorner.Y) - (leftOutmostCorner.Y - leftCorner.Y) * (position.X - leftCorner.X) < 0)
                && ((rightOutmostCorner.X - rightCorner.X) * (position.Y - rightCorner.Y) - (rightOutmostCorner.Y - rightCorner.Y) * (position.X - rightCorner.X) >= 0)
                && ((rightCorner.X - leftCorner.X) * (position.Y - leftCorner.Y) - (rightCorner.Y - leftCorner.Y) * (position.X - leftCorner.X) >= 0)
                && ((rightOutmostCorner.X - leftOutmostCorner.X) * (position.Y - leftOutmostCorner.Y) - (rightOutmostCorner.Y - leftOutmostCorner.Y) * (position.X - leftOutmostCorner.X) < 0);

            if (isInRectangle)
            {
                //Functions.PrintHelp("ARGH SPAM");
                Vehicle gtaVeh = vehicle; //World.GetClosestVehicle(LPlayer.LocalPlayer.Ped.Position, 2.0f);
                if (gtaVeh != null && gtaVeh.Exists() && !LPlayer.LocalPlayer.Ped.IsInVehicle())
                {
                    vehicle = LVehicle.FromGTAVehicle(gtaVeh);
                    LPlayer.LocalPlayer.Ped.Task.ClearAll();

                    float H = vehicle.Heading;
                    LPlayer.LocalPlayer.Ped.Task.TurnTo(vehicle.Position);
                    LPlayer.LocalPlayer.Ped.Heading = H + 180;
                    DelayedCaller.Call(RepairFunction1, this, 600, vehicle);
                }
            }
        }

        private void RepairFunction1(params object[] parameter)
        {
            Vehicle vehicle = parameter[0] as Vehicle;

            LPlayer.LocalPlayer.Ped.Animation.Play(new AnimationSet("amb@bridgecops"), "open_boot", 4.0f);
            GTA.Native.Function.Call("OPEN_CAR_DOOR", vehicle, 4);
            DelayedCaller.Call(RepairFunction2, this, 1500, vehicle);
        }

        private void RepairFunction2(params object[] parameter)
        {
            Vehicle vehicle = parameter[0] as Vehicle;
            LPlayer.LocalPlayer.Ped.Animation.Play(new AnimationSet("misstaxidepot"), "workunderbonnet", 4.0f);
            vehicle.EngineHealth = 800;
            DelayedCaller.Call(RepairFunction3, this, 8000, vehicle);
        }

        private void RepairFunction3(params object[] parameter)
        {
            Vehicle vehicle = parameter[0] as Vehicle;
            LPlayer.LocalPlayer.Ped.Animation.Play(new AnimationSet("amb@bridgecops"), "close_boot", 4.0f);
            GTA.Native.Function.Call("SHUT_CAR_DOOR", vehicle, 4);
            Functions.PrintText("This vehicle's engine has been repaired", 4000);
            if (vehicle.Model == "POLICE" || vehicle.Model == "POLICE2" || vehicle.Model == "POLICE3" || vehicle.Model == "POLICE4")
            {
                Functions.PrintHelp("This police vehicle will need taking to a police station mechanic to be fully repaired.");
            }
        }

        public void VehicleIdCheck()
        {
            Functions.AddTextToTextwall("Control can I get some details on this vehicle please?", "Officer " + LPlayer.LocalPlayer.Username);
            Vehicle car = null;
            if (Functions.IsPlayerPerformingPullover() && CalloutsPlusMain.AutoVehicleCheck)
            {
                LHandle pullover = Functions.GetCurrentPullover();
                if (pullover != null)
                {
                    car = Functions.GetPulloverVehicle(pullover);
                }
            }
            else
            {
                if (LPlayer.LocalPlayer.Ped.IsInVehicle())
                {
                    Vehicle vehicle = LPlayer.LocalPlayer.LastVehicle;
                    Vector3 dimensions = vehicle.Model.GetDimensions();
                    float width = dimensions.X;
                    float length = dimensions.Y;

                    Vector3 centerPoint = vehicle.GetOffsetPosition(new Vector3(0, length / 2 + 4, 0));
                    car = World.GetClosestVehicle(centerPoint, 2.0f);
                }
                else
                {
                    LPlayer.LocalPlayer.Ped.PlayWalkieTalkieAnimation("");
                    car = World.GetClosestVehicle(LPlayer.LocalPlayer.Ped.Position, 3.0f);
                }
            }

            string flags = "";

            if (car != null && car.Exists())
            {
                Functions.AddTextToTextwall("Affirmative, standby for details", "CONTROL");
                if (car.Metadata.registration == null)
                {
                    bool isRegistered = Common.GetRandomBool(0, 10, 1);
                    if (isRegistered == true)
                    {
                        car.Metadata.registration = "UNREGISTERED";
                    }
                    else
                    {
                        car.Metadata.registration = Common.GetRandomValue(1990, 2014);
                    }

                }
                if (car.Metadata.stolen == null)
                {
                    car.Metadata.stolen = Common.GetRandomBool(0, 8, 1);
                }
                if (car.Metadata.insured == null)
                {
                    car.Metadata.insured = Common.GetRandomBool(0, 6, 1);
                }
                if (car.Metadata.tax == null)
                {
                    car.Metadata.tax = Common.GetRandomBool(0, 6, 1);
                }
                if (car.Name.Equals("TAXI") || car.Name.Equals("TAXI2"))
                {
                    if (car.Metadata.taxi == null)
                    {
                        car.Metadata.taxi = Common.GetRandomBool(0, 10, 1);
                    }
                }
                DelayedCaller.Call(delegate
                {
                    Functions.AddTextToTextwall("Vehicle Make: " + car.Name);
                    Functions.AddTextToTextwall("Vehicle Colour: " + car.Color);
                    Functions.AddTextToTextwall("Registration Year: " + car.Metadata.registration);

                    if (car.Metadata.stolen == true)
                    {
                        flags += "STOLEN, ";
                    }
                    if (car.Metadata.insured == true)
                    {
                        flags += "UNINSURED, ";
                    }
                    if (car.Metadata.tax == true)
                    {
                        flags += "NO TAX/SORN, ";
                    }
                    if (car.Metadata.taxi == true)
                    {
                        flags += "INVALID TAXI LICENSE, ";
                    }

                    Functions.AddTextToTextwall("Flags: " + flags);
                    Functions.AddTextToTextwall("END");
                }, this, 5000);
            }
            else
            {
                Functions.AddTextToTextwall("We're unable to fetch vehicle details. Verify the target and try again", "CONTROL");
            }
        }

        public void Revive()
        {
            Ped[] peds = World.GetAllPeds();
            var sortedList = from element in peds
                             where element.Exists() && element != LPlayer.LocalPlayer.Ped
                             orderby element.Position.DistanceTo2D(LPlayer.LocalPlayer.Ped.Position)
                             select element;

            Ped victim = sortedList.FirstOrDefault();

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
                            GTA.Native.Function.Call("REVIVE_INJURED_PED", victim);

                            DelayedCaller.Call(delegate
                            {
                                victim.Task.ClearAll();
                                Functions.PrintHelp("The victim's condition was stabilized, call for a paramedic to have them taken to a hospital.");
                                //victim.Task.EnterVehicle(LPlayer.LocalPlayer.LastVehicle, VehicleSeat.AnyPassengerSeat);

                            }, this, 2000);

                        }, this, 6000);
                    }, this, 2000);
                }, this, 2000);
            }
            else
            {
                Functions.PrintText("There is no injured ped here", 4000);
            }
        }

        public void StopLights()
        {
            if (hazard == false) // If it is false, pressing J will turn the lights on
            {
                LPlayer.LocalPlayer.LastVehicle.HazardLightsOn = true;
                Functions.PrintText("Hazard lights are on", 2000);
                hazard = true;

            }
            else // Else it has to be on, so turn them off again
            {
                LPlayer.LocalPlayer.LastVehicle.HazardLightsOn = false;
                Functions.PrintText("Hazard lights are off", 2000);
                hazard = false;
            }
        }

        public void Questioning()
        {
            pCar = LPlayer.LocalPlayer.Ped.IsInVehicle() ? LPlayer.LocalPlayer.Ped.CurrentVehicle : LPlayer.LocalPlayer.LastVehicle;
            //sCar = m.pulloverCar;//LVehicle.FromGTAVehicle(World.GetClosestVehicle(LPlayer.LocalPlayer.LastVehicle.GetOffsetPosition(new Vector3(0, pCar.Model.GetDimensions().Y / 2 + 4, 0)), 5f));
            //sCar.AttachBlip();
            suspect = LPlayer.LocalPlayer.LastVehicle.GetPedOnSeat(LPlayer.LocalPlayer.LastVehicle.IsSeatFree(VehicleSeat.LeftRear) ? VehicleSeat.RightRear : VehicleSeat.LeftRear);
            if (suspect.Exists()) // If the suspect does exist we can go ahead and question them
            {
                if (sCar !=null && sCar.Exists()) // Check for the suspect's vehicle, if it doesn't exist then this isn't a traffic stop.
                {
                    IsTrafficStop = true;
                    Functions.PrintHelp("~KEY_DIALOG_1~ Conduct Breathtest~n~~KEY_DIALOG_2~ Seize Car~n~~KEY_DIALOG_3~ Release Ped~n~~KEY_DIALOG_4~ Cancel");
                    qTimer = new GTA.Timer(10);
                    qTimer.Start();
                    qTimer.Tick += qTimer_Tick;
                }
                else
                {
                    IsTrafficStop = false;
                    Functions.PrintHelp("You can only question a suspect from a pullover at this time.");
                    m.HasQuestioningFired = false;
                    //Functions.PrintHelp("~KEY_DIALOG_1~ Option 2~n~~KEY_DIALOG_2~ Option 2~n~~KEY_DIALOG_3~ Release Ped~n~~KEY_DIALOG_4~ Cancel");
                }               
            }
            else
            {
                m.HasQuestioningFired = false;

            }
        }

        void qTimer_Tick(object sender, EventArgs e)
        {
            if (IsTrafficStop)
            {
                if (m.IsKeyPressed(System.Windows.Forms.Keys.F9, System.Windows.Forms.Keys.None, true))
                {
                    //LPlayer.LocalPlayer.Ped.DrawTextAboveHead("I suspect you've been Driving While Intoxicated, I need you to take a breath test", 3000);
                    Functions.PrintHelp("Conducting breathtest...");
                    DelayedCaller.Call(delegate
                    {
                        int BreathTestScore;
                        if (suspect.IsRagdoll)
                        {
                            BreathTestScore = Common.GetRandomValue(CalloutsPlusMain.DrinkDriveLimit, CalloutsPlusMain.DrinkDriveLimit + 100);
                        }
                        else
                        {
                            BreathTestScore = Common.GetRandomValue(0, CalloutsPlusMain.DrinkDriveLimit + 100);
                        }
                        String result;
                        String measurement = "mg/100ml";
                        double multiplier = Math.Floor((double)BreathTestScore / CalloutsPlusMain.DrinkDriveLimit);

                        if (BreathTestScore >= CalloutsPlusMain.DrinkDriveLimit)
                        {
                            result = "~r~FAIL~w~";
                        }
                        else
                        {
                            result = "~g~PASS~w~";
                        }
                        //Finally print the results
                        Functions.PrintHelp("Result: " + result + "~n~Score: " + BreathTestScore + measurement + " (Limit: " + CalloutsPlusMain.DrinkDriveLimit + "mg/100ml)~n~" + multiplier + "x Legal limit");
                        m.HasQuestioningFired = false;
                        qTimer.Stop();
                    }, this, 4000);
                }
                else if (m.IsKeyPressed(System.Windows.Forms.Keys.F10, System.Windows.Forms.Keys.None, true))
                {
                    //LPlayer.LocalPlayer.Ped.DrawTextAboveHead("Your car is being seized by the LCPD, you're going to have to walk from now on.", 3000);
                    Functions.PrintHelp("You've seized this ped's car, deal with it how you see fit.");
                    DelayedCaller.Call(delegate
                    {
                        int response = Common.GetRandomValue(0, 3);
                        switch (response)
                        {
                            case 0:
                                suspect.DrawTextAboveHead("Oh great, just what I wanted!", 4000);
                                break;
                            case 1:
                                suspect.DrawTextAboveHead("No! My car!", 4000);
                                break;
                            case 2:
                                suspect.DrawTextAboveHead("What?! Why? this isn't fair", 4000);
                                break;
                            case 3:
                                suspect.DrawTextAboveHead("Don't you fucks earn enough money to buy your own car?", 4000);
                                break;
                        }
                        suspect.NoLongerNeeded();
                        sCar.PassengersLeaveVehicle(true);
                        m.HasQuestioningFired = false;
                        qTimer.Stop();
                    }, this, 2000);
                }
                else if (m.IsKeyPressed(System.Windows.Forms.Keys.F11, System.Windows.Forms.Keys.None, true))
                {
                    //LPlayer.LocalPlayer.Ped.DrawTextAboveHead("Alright, you're free to go", 3000);
                    Functions.PrintHelp("You released the ped, they will return to their vehicle and drive away.");
                    suspect.Task.CruiseWithVehicle(sCar, 20f, true);
                    suspect.Task.AlwaysKeepTask = true;
                    suspect.WantedByPolice = false;
                    DelayedCaller.Call(delegate
                    {
                        suspect.NoLongerNeeded();
                        sCar.NoLongerNeeded();
                        m.HasQuestioningFired = false;
                        qTimer.Stop();
                    }, this, 5000);
                }
                else if (m.IsKeyPressed(System.Windows.Forms.Keys.F12, System.Windows.Forms.Keys.None, true))
                {
                    if (CalloutsPlusMain.QuestionSuspectModifierKey != System.Windows.Forms.Keys.None)
                    {
                        Functions.PrintHelp("Menu Cancelled, you can press " + CalloutsPlusMain.QuestionSuspectModifierKey +"+"+CalloutsPlusMain.QuestionSuspectKey + " again to restart or pull the suspect out of your car to deal with him as normal.");
                    }
                    else
                    {
                        Functions.PrintHelp("Menu Cancelled, you can press " + CalloutsPlusMain.QuestionSuspectKey + " again to restart or pull the suspect out of your car to deal with him as normal.");
                    }
                    
                    m.HasQuestioningFired = false;
                    qTimer.Stop();
                }
            }
            else
            {
                if (m.IsKeyPressed(System.Windows.Forms.Keys.F9, System.Windows.Forms.Keys.None, true))
                {
                    Functions.PrintHelp("You attempt to extort the suspect...");

                    if (suspect.ItemsCarried == LPed.EPedItem.StolenCards)
                    {

                    }



                    m.HasQuestioningFired = false;
                    qTimer.Stop();
                }
                else if (m.IsKeyPressed(System.Windows.Forms.Keys.F10, System.Windows.Forms.Keys.None, true))
                {
                    Functions.PrintHelp("You picked option 2, it does fuck all");
                    m.HasQuestioningFired = false;
                    qTimer.Stop();
                }
                else if (m.IsKeyPressed(System.Windows.Forms.Keys.F11, System.Windows.Forms.Keys.None, true))
                {
                    //LPlayer.LocalPlayer.Ped.DrawTextAboveHead("Alright, you're free to go", 3000);
                    Functions.PrintHelp("You released the ped, they will leave your vehicle.");
                    suspect.WantedByPolice = false;
                    DelayedCaller.Call(delegate
                    {
                        suspect.NoLongerNeeded();
                        m.HasQuestioningFired = false;
                        qTimer.Stop();
                    }, this, 5000);
                }
                else if (m.IsKeyPressed(System.Windows.Forms.Keys.F12, System.Windows.Forms.Keys.None, true))
                {
                    if (CalloutsPlusMain.QuestionSuspectModifierKey != System.Windows.Forms.Keys.None)
                    {
                        Functions.PrintHelp("Menu Cancelled, you can press " + CalloutsPlusMain.QuestionSuspectModifierKey + "+" + CalloutsPlusMain.QuestionSuspectKey + " again to restart or pull the suspect out of your car to deal with him as normal.");
                    }
                    else
                    {
                        Functions.PrintHelp("Menu Cancelled, you can press " + CalloutsPlusMain.QuestionSuspectKey + " again to restart or pull the suspect out of your car to deal with him as normal.");
                    }
                    m.HasQuestioningFired = false;
                    qTimer.Stop();
                }
            }
            
        }
    }
}
