using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace CalloutsPlus
{
    using GTA;
    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Scripting.Plugins;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.API;

    class Dispatcher : GameScript
    {
        private CalloutsPlusMain main;
        public bool paramedicCanCancel = false;
        public bool removalCanCancel = false;
        public bool towTruckCanCancel = false;

        public bool paramedicCalledOut = false;
        public bool removalCalledOut = false;
        public bool towTruckCalledOut = false;

        public LPed para1, dropOffDriver, driver, truckDriver;
        public Vehicle towedCar, removalCar;
        public LVehicle ambulance, copCar, truck;
        private GTA.Timer paramedicTimer, removalTimer, towTruckTimer;
        private List<Ped> victims;
        private State EMSStatus, TTStatus, RTStatus;
        private Ped currentVictim;
        private TaskSequence EMSTaskSeq, TTTaskSeq;
        private AnimationSet medic;

        public Dispatcher()
        {
            main = new CalloutsPlusMain();
            victims = new List<Ped>();
        }

        //Function for requesting a tow truck
        public void RequestTowTruck()
        {
            Functions.AddTextToTextwall("Can I get a removal truck at my location please",
                "OFFICER " + LPlayer.LocalPlayer.Username);
            towedCar = World.GetClosestVehicle(LPlayer.LocalPlayer.Ped.Position, 3f);
            if (towedCar != null && towedCar.Exists())
            {
                if (CalloutsPlusMain.QuickSpawnMethod == true)
                    // if quick spawn method is set to true, the vehicle will spawn much closer
                {
                    truck = new LVehicle(LPlayer.LocalPlayer.Ped.Position.Around(Common.GetRandomValue(30, 60)),
                        "PACKER");
                }
                else
                {
                    truck = new LVehicle(LPlayer.LocalPlayer.Ped.Position.Around(Common.GetRandomValue(100, 150)),
                        "PACKER");
                }

                if (truck != null && truck.Exists())
                {
                    truckDriver = new LPed(truck.Position.Around(2f), "m_y_mechanic_02");
                }

                if (towedCar.Model.isBoat)
                {
                    Functions.PrintText("[TOW TRUCK] We can't tow a boat!", 4000);
                    truck.NoLongerNeeded();
                    truckDriver.NoLongerNeeded();
                    towTruckCalledOut = false;
                }
                else if (towedCar.Model.isHelicopter)
                {
                    Functions.PrintText("[TOW TRUCK] We can't tow a helicopter!", 4000);
                    truck.NoLongerNeeded();
                    truckDriver.NoLongerNeeded();
                    towTruckCalledOut = false;
                }
                else if (towedCar.Model.Hash == 2053223216 || towedCar.Model.Hash == 850991848 ||
                         towedCar.Model.Hash == 2307837162 || towedCar.Model.Hash == 3581397346 ||
                         towedCar.Model.Hash == 1938952078 || towedCar.Model.Hash == 1353720154 ||
                         towedCar.Model.Hash == 904750859 || towedCar.Model.Hash == 569305213)
                {
                    Functions.PrintText(
                        "[TOW TRUCK] We can't tow that, it's too big. Send an impound cop instead.", 4000);
                    truck.NoLongerNeeded();
                    truckDriver.NoLongerNeeded();
                    towTruckCalledOut = false;
                }
                else
                {
                    if (truck != null && truck.Exists())
                    {
                        if (truckDriver != null && truckDriver.Exists())
                        {
                            truck.PlaceOnNextStreetProperly();
                            if (World.GetClosestVehicle(truck.Position, 5f).Exists() &&
                                World.GetClosestVehicle(truck.Position, 5f) != truck)
                            {
                                truck.Heading = World.GetClosestVehicle(truck.Position, 5f).Heading;

                            }

                            truck.Extras(1).Enabled = false;
                            truck.Extras(2).Enabled = false;
                            truck.Extras(3).Enabled = false;
                            truck.Extras(4).Enabled = false;

                            truckDriver.WarpIntoVehicle(truck, VehicleSeat.Driver);
                            truck.AttachBlip().Color = BlipColor.Yellow;
                            DelayedCaller.Call(delegate
                            {
                                Functions.AddTextToTextwall("Affirmative, a removal truck has been dispatched",
                                    "CONTROL");
                                towTruckCanCancel = true;
                                truckDriver.Task.DriveTo(LPlayer.LocalPlayer.Ped.Position, 20f, true, false);

                                GTA.Native.Function.Call("ADD_STUCK_CAR_CHECK",
                                    new GTA.Native.Parameter[] {(Vehicle) truck, 2f, 12000});
                                truckDriver.MakeProofTo(false, true, true, true, false);

                                TTStatus = State.Responding;
                                towTruckTimer = new GTA.Timer(1000);
                                towTruckTimer.Tick += TowTruck_Tick;
                                towTruckTimer.Start();
                            }, this, 100);
                        }
                        else
                        {
                            Functions.AddTextToTextwall("Negative we have no drivers available right now.",
                                "CONTROL");
                            truck.Delete();
                            towTruckCalledOut = false;
                        }

                    }
                    else
                    {
                        Functions.AddTextToTextwall("Negative at this time, we have no available trucks.", "CONTROL");
                        driver.Delete();
                        towTruckCalledOut = false;
                    }

                }
            }
            else
            {
                Functions.PrintText("There is no vehicle here, tow truck will not be sent", 4000);
                towTruckCalledOut = false;
            }

        }

        //Function for requesting a removal cop team
        public void RequestRemoval()
        {

            Functions.AddTextToTextwall("Control, I need a unit to remove a vehicle for me.",
                "Officer " + LPlayer.LocalPlayer.Username);
            LPlayer.LocalPlayer.Ped.PlayWalkieTalkieAnimation("");
            removalCar = World.GetClosestVehicle(LPlayer.LocalPlayer.Ped.Position, 3.0f);
            if (removalCar != null && removalCar.Exists())
            {
                if (CalloutsPlusMain.QuickSpawnMethod)
                {
                    copCar = new LVehicle(LPlayer.LocalPlayer.Ped.Position.Around(Common.GetRandomValue(30, 60)),
                        "POLICE");
                }
                else
                {
                    var copcars = World.GetAllVehicles("POLICE");
                    var selectedCop = copcars.FirstOrDefault();
                    if (selectedCop != null && selectedCop.Exists())
                    {
                        dropOffDriver = LPed.FromGTAPed(selectedCop.GetPedOnSeat(VehicleSeat.Driver));
                        if (dropOffDriver.Exists())
                        {
                            if (!Functions.DoesPedHaveAnOwner(dropOffDriver))
                            {
                                Functions.SetPedIsOwnedByScript(dropOffDriver, this, true);
                                driver = LPed.FromGTAPed(selectedCop.GetPedOnSeat(VehicleSeat.RightFront));
                                if (!driver.Exists())
                                {
                                    driver =
                                        LPed.FromGTAPed(selectedCop.CreatePedOnSeat(VehicleSeat.RightFront, "M_Y_COP"));
                                }
                                Functions.SetPedIsOwnedByScript(driver, this, true);
                                copCar = LVehicle.FromGTAVehicle(selectedCop);
                                copCar.AttachBlip().Color = BlipColor.Green;
                                //Functions.PrintHelp("Selected a cop car already in existance~n~Distance: " + copCar.Position.DistanceTo2D(LPlayer.LocalPlayer.Ped.Position));
                            }
                            dropOffDriver.Task.DriveTo(LPlayer.LocalPlayer.Ped.Position, 20f, true, true);
                            removalTimer = new GTA.Timer(1000);
                            removalTimer.Start();
                            removalTimer.Tick += (removal_Tick);
                            RTStatus = State.Responding;

                            GTA.Native.Function.Call("ADD_STUCK_CAR_CHECK",
                                new GTA.Native.Parameter[] {(Vehicle) copCar, 2f, 12000});
                            Functions.AddTextToTextwall("Affirmative, a unit is on it's way stand by.", "CONTROL");
                            removalCanCancel = true;
                        }
                        else
                        {
                            Functions.AddTextToTextwall("We were unable to locate a unit available, please try again later.", "CONTROL");
                            removalCalledOut = false;
                            removalCanCancel = false;
                        }
                    }
                    else
                    {
                        copCar =
                            new LVehicle(
                                World.GetNextPositionOnStreet(
                                    LPlayer.LocalPlayer.Ped.Position.Around(Common.GetRandomValue(100, 150))), "POLICE");
                        copCar.PlaceOnNextStreetProperly();
                        if (World.GetClosestVehicle(copCar.Position, 5f).Exists() &&
                            World.GetClosestVehicle(copCar.Position, 5f) != copCar)
                        {
                            copCar.Heading = World.GetClosestVehicle(copCar.Position, 5f).Heading;
                        }

                        dropOffDriver = new LPed(copCar.Position.Around(5f), "M_Y_COP");
                        driver = new LPed(copCar.Position.Around(5f), "M_Y_COP");
                        if (dropOffDriver.Exists() && driver.Exists())
                        {
                            dropOffDriver.WarpIntoVehicle(copCar, VehicleSeat.Driver);
                            driver.WarpIntoVehicle(copCar, VehicleSeat.RightFront);
                            copCar.AttachBlip().Color = BlipColor.Green;

                            dropOffDriver.Task.DriveTo(LPlayer.LocalPlayer.Ped.Position, 20f, true, true);
                            removalTimer = new GTA.Timer(1000);
                            removalTimer.Start();
                            removalTimer.Tick += (removal_Tick);
                            RTStatus =  State.Responding;

                            GTA.Native.Function.Call("ADD_STUCK_CAR_CHECK",
                                new GTA.Native.Parameter[] {(Vehicle) copCar, 2f, 12000});
                            Functions.AddTextToTextwall("Affirmative, a unit is on it's way stand by.", "CONTROL");
                            removalCanCancel = true;
                        }
                        else
                        {
                            Functions.AddTextToTextwall("We lost contact with the unit, sorry", "CONTROL");
                            if (copCar.Exists())
                            {
                                copCar.AttachBlip().Delete();
                                copCar.Delete();
                            }

                            removalCalledOut = false;
                        }

                    }
                }
            }
            else
            {
                Functions.PrintText("There is no vehicle here!", 4000);
                removalCalledOut = false;
            }

        }

        //Function for requesting an ambulance for a single ped
        public void RequestMedic()
        {
            //Functions.AddTextToTextwall("I need a paramedic ASAP", "Officer " + LPlayer.LocalPlayer.Username);
            //LPlayer.LocalPlayer.Ped.PlayWalkieTalkieAnimation("");
            if (CalloutsPlusMain.QuickSpawnMethod)
            {
                ambulance = new LVehicle(LPlayer.LocalPlayer.Ped.Position.Around(Common.GetRandomValue(30, 60)),
                    "AMBULANCE");
            }
            else
            {
                var ambulances = World.GetAllVehicles("AMBULANCE");
                var selectedAmbulance = ambulances.FirstOrDefault();

                if (selectedAmbulance != null && selectedAmbulance.Exists())
                {
                    ambulance = LVehicle.FromGTAVehicle(selectedAmbulance);
                    //Functions.PrintText("Found an ambulance in the game already, using", 4000);
                    para1 = ambulance.GetPedOnSeat(VehicleSeat.Driver);
                    if (para1.Exists())
                    {
                        para1.Task.ClearAllImmediately();
                        Functions.SetPedIsOwnedByScript(para1, this, true);
                        para1.IsRequiredForMission = true;
                        ambulance.IsRequiredForMission = true;
                        para1.Task.WarpIntoVehicle(ambulance, VehicleSeat.Driver);
                        DelayedCaller.Call(delegate
                        {
                            para1.Task.DriveTo(LPlayer.LocalPlayer.Ped.Position, 20f, false, false);
                            para1.Task.AlwaysKeepTask = true;
                            para1.BlockPermanentEvents = true;
                            ambulance.SirenActive = true;
                            ambulance.AttachBlip().Color = BlipColor.Green;

                            Functions.AddTextToTextwall("A paramedic has been dispatched to your location, stand-by",
                                "CONTROL");
                            //Functions.PlaySoundUsingPosition("INS_I_NEED_A_MEDICAL_TEAM_FOR_ERRR", LPlayer.LocalPlayer.Ped.Position);
                            GTA.Native.Function.Call("ADD_STUCK_CAR_CHECK",
                                new GTA.Native.Parameter[] {(Vehicle) ambulance, 2f, 12000});
                            paramedicCanCancel = true;
                            EMSStatus =  State.Responding;

                            medic = new AnimationSet("medic");

                            paramedicTimer = new GTA.Timer(1000);
                            paramedicTimer.Start();
                            paramedicTimer.Tick += (paramedic_Tick);
                        }, this, 2000);
                    }
                    else
                    {
                        para1 = ambulance.CreatePedOnSeat(VehicleSeat.Driver, "M_Y_PMEDIC");
                        para1.Task.ClearAllImmediately();
                        Functions.SetPedIsOwnedByScript(para1, this, true);
                        para1.Task.WarpIntoVehicle(ambulance, VehicleSeat.Driver);
                        DelayedCaller.Call(delegate
                        {
                            para1.Task.DriveTo(LPlayer.LocalPlayer.Ped.Position, 20f, false, false);
                            para1.Task.AlwaysKeepTask = true;
                            para1.BlockPermanentEvents = true;
                            ambulance.SirenActive = true;
                            ambulance.AttachBlip().Color = BlipColor.Green;

                            Functions.AddTextToTextwall("A paramedic has been dispatched to your location, stand-by",
                                "CONTROL");
                            //Functions.PlaySoundUsingPosition("INS_I_NEED_A_MEDICAL_TEAM_FOR_ERRR", LPlayer.LocalPlayer.Ped.Position);
                            GTA.Native.Function.Call("ADD_STUCK_CAR_CHECK",
                                new GTA.Native.Parameter[] {(Vehicle) ambulance, 2f, 12000});
                            paramedicCanCancel = true;
                            EMSStatus =  State.Responding;

                            medic = new AnimationSet("medic");

                            paramedicTimer = new GTA.Timer(1000);
                            paramedicTimer.Start();
                            paramedicTimer.Tick += (paramedic_Tick);
                        }, this, 2000);

                    }
                    if (ambulance.GetPedOnSeat(VehicleSeat.RightFront).Exists())
                    {
                        ambulance.GetPedOnSeat(VehicleSeat.RightFront).Delete();
                    }

                }
                else
                {
                    ambulance =
                        new LVehicle(
                            World.GetNextPositionOnStreet(
                                LPlayer.LocalPlayer.Ped.Position.Around(Common.GetRandomValue(100, 150))), "AMBULANCE");
                    if (ambulance.Exists())
                    {
                        ambulance.PlaceOnNextStreetProperly();
                        if (World.GetClosestVehicle(ambulance.Position, 5f).Exists() &&
                            World.GetClosestVehicle(ambulance.Position, 5f) != ambulance)
                        {
                            ambulance.Heading = World.GetClosestVehicle(ambulance.Position, 5f).Heading;
                        }
                        if (!para1.Exists())
                        {
                            para1 = new LPed(ambulance.Position.Around(5f), "M_Y_PMEDIC");

                            para1.Task.ClearAllImmediately();
                            Functions.SetPedIsOwnedByScript(para1, this, true);
                            para1.Task.WarpIntoVehicle(ambulance, VehicleSeat.Driver);
                            DelayedCaller.Call(delegate
                            {
                                para1.Task.DriveTo(LPlayer.LocalPlayer.Ped.Position, 20f, false, false);
                                para1.Task.AlwaysKeepTask = true;
                                para1.BlockPermanentEvents = true;
                                ambulance.SirenActive = true;
                                ambulance.AttachBlip().Color = BlipColor.Green;

                                Functions.AddTextToTextwall(
                                    "A paramedic has been dispatched to your location, stand-by",
                                    "CONTROL");
                                //Functions.PlaySoundUsingPosition("INS_I_NEED_A_MEDICAL_TEAM_FOR_ERRR", LPlayer.LocalPlayer.Ped.Position);
                                GTA.Native.Function.Call("ADD_STUCK_CAR_CHECK",
                                    new GTA.Native.Parameter[] {(Vehicle) ambulance, 2f, 12000});
                                paramedicCanCancel = true;
                                EMSStatus =  State.Responding;

                                medic = new AnimationSet("medic");

                                paramedicTimer = new GTA.Timer(1000);
                                paramedicTimer.Start();
                                paramedicTimer.Tick += (paramedic_Tick);
                            }, this, 2000);

                        }
                    }
                }
            }
        }
        //Removal team tick
        private void removal_Tick(object sender, EventArgs e)
        {
            switch (RTStatus)
            {
                case State.Responding:
                    if (GTA.Native.Function.Call<bool>("IS_CAR_STUCK", new GTA.Native.Parameter[] { (Vehicle)copCar }) || copCar.IsUpsideDown)
                    {
                        copCar.Position = World.GetNextPositionOnStreet(copCar.Position.Around(10f));
                        copCar.PlaceOnNextStreetProperly();
                    }
                    if (dropOffDriver.Position.DistanceTo2D(LPlayer.LocalPlayer.Ped.Position) <= 35f)
                    {
                        removalCanCancel = false;
                        dropOffDriver.Task.DriveTo(removalCar.Position, 8f, true, false);
                        if (dropOffDriver.Position.DistanceTo2D(removalCar.Position) <= 15f)
                        {
                            dropOffDriver.Task.Wait(5000);
                            driver.Task.LeaveVehicle(copCar, true);
                            RTStatus =  State.OnScene;
                            break;
                        }
                    }
                    break;
                case State.OnScene:
                    if (dropOffDriver.Exists())
                    {
                        dropOffDriver.NoLongerNeeded();
                    }
                    if (copCar.Exists())
                    {
                        copCar.AttachBlip().Delete();
                        copCar.NoLongerNeeded();
                    }


                    if (removalCar != null && removalCar.Exists())
                    {
                        driver.Task.CruiseWithVehicle(removalCar, 20f, true);
                        driver.Task.AlwaysKeepTask = true;
                        removalCar.DoorLock = DoorLock.None;
                        driver.DrawTextAboveHead("I'll take this back to the impound lot.", 4000);
                        if (driver.IsInVehicle(LVehicle.FromGTAVehicle(removalCar)))
                        {
                            RTStatus =  State.Leave;
                            break;
                        }
                    }
                    else
                    {
                        driver.DrawTextAboveHead("Oh, there's no car. Thanks for wasting my time buddy", 4000);
                        driver.NoLongerNeeded();
                        removalCanCancel = false;
                        removalCalledOut = false;
                        RTStatus =  State.None;
                        break;
                    }
                    break;
                case State.Leave:
                    driver.NoLongerNeeded();
                    removalCar.NoLongerNeeded();
                    removalCalledOut = false;
                    RTStatus = State.None;
                    break;
                case State.None:
                    removalTimer.Stop();
                    break;
            }
        }

        //Paramedic tick
        private void paramedic_Tick(object sender, EventArgs e)
        {
            switch (EMSStatus)
            {
                case State.Responding:
                    if (GTA.Native.Function.Call<bool>("IS_CAR_STUCK", new GTA.Native.Parameter[] { (Vehicle)ambulance }) || ambulance.IsUpsideDown)
                    {
                        ambulance.Position = World.GetNextPositionOnStreet(ambulance.Position.Around(10f));
                        ambulance.PlaceOnNextStreetProperly();
                    }
                    if (para1.Position.DistanceTo2D(LPlayer.LocalPlayer.Ped.Position) <= 35f)
                    {
                        paramedicCanCancel = false;
                        para1.Task.DriveTo(LPlayer.LocalPlayer.Ped.Position, 10f, false, true);
                        if (para1.Position.DistanceTo2D(LPlayer.LocalPlayer.Ped.Position) <= 15f)
                        {
                            ambulance.AttachBlip().Delete();
                            para1.Task.LeaveVehicle(ambulance, true);

                            Ped[] peds = World.GetAllPeds();
                            foreach (Ped p in peds)
                            {
                                if (p.Exists())
                                {
                                    if (!p.isAliveAndWell || p.isDead || p.isInjured || p.Health < main.ParamedicPlayerHealthBelow)
                                    {
                                        victims.Add(p);
                                    }
                                }
                            }

                            EMSStatus = State.OnScene;
                        }
                    }
                    break;
                case State.OnScene:
                    currentVictim = victims.FirstOrDefault();
                    //Functions.PrintText("Victims: " + victims.Count, 4000);
                    if (currentVictim == null)
                    {
                        //Log.Warning("No elements were found in the array so the script could not continue. The ambulance was set to no longer needed and will return to a normal ped.", this);
                        Functions.PrintHelp("There was no victim in the area, the ambulance will be detached.");
                        para1.Task.CruiseWithVehicle(ambulance, 20f, true);
                        ambulance.SirenActive = false;
                        EMSStatus = State.Leave;
                        break;
                    }
                    else
                    {
                        EMSStatus = State.RunToObject;
                        break;
                    }
                case State.RunToObject:
                    if (currentVictim.Exists() == false)
                    {
                        Functions.PrintText("There is no injured ped here", 4000);
                        para1.Task.CruiseWithVehicle(ambulance, 20f, true);
                        EMSStatus = State.Leave;
                        break;
                    }
                    else //they must exist
                    {
                        if (EMSTaskSeq == null)
                        {
                            currentVictim.AttachBlip();
                            EMSTaskSeq = new TaskSequence();
                            EMSTaskSeq.AddTask.RunTo(currentVictim.Position);
                            EMSTaskSeq.Perform(para1);
                        }

                        if (para1.Position.DistanceTo2D(currentVictim.Position) <= 5f)
                        {
                            EMSTaskSeq.Dispose();
                            EMSTaskSeq = null;
                            EMSStatus = State.PerformDuty;
                            break;
                        }
                        break;
                    }
                case State.PerformDuty:
                    if (EMSTaskSeq == null)
                    {
                        EMSTaskSeq = new TaskSequence();
                        EMSTaskSeq.AddTask.TurnTo(currentVictim);
                        EMSTaskSeq.AddTask.PlayAnimation(medic, "medic_cpr_in", 2f);
                        EMSTaskSeq.AddTask.PlayAnimation(medic, "medic_cpr_loop", 2f);
                        EMSTaskSeq.AddTask.PlayAnimation(medic, "medic_cpr_out", 2f);
                        EMSTaskSeq.Perform(para1);
                    }
                    if (para1.Animation.isPlaying(medic, "medic_cpr_out"))
                    {
                        EMSTaskSeq.Dispose();
                        EMSTaskSeq = null;
                        EMSStatus = State.EndDuty;
                        break;
                    }
                    break;
                case State.EndDuty:
                    if (currentVictim.isDead)
                    {
                        para1.DrawTextAboveHead("They're dead. We'll take him to the morgue for you", 4000);
                    }
                    else if (currentVictim == LPlayer.LocalPlayer.Ped)
                    {
                        para1.DrawTextAboveHead("I've patched you up, you're good to go", 4000);
                    }
                    else
                    {
                        para1.DrawTextAboveHead("They need to go to hospital immediately!", 4000);
                    }

                    if (currentVictim != null && currentVictim.Exists())
                    {
                        if (currentVictim != LPlayer.LocalPlayer.Ped)
                        {
                            currentVictim.AttachBlip().Delete();
                            currentVictim.Delete();
                            currentVictim = null;
                        }
                        else
                        {
                            currentVictim.AttachBlip().Delete();
                        }

                        victims.RemoveAt(0);
                        victims.Sort(delegate(Ped a, Ped b)
                        {
                            if (a == null && b == null) return 0;
                            else if (a == null) return -1;
                            else if (b == null) return 1;
                            else
                                return
                                    a.Position.DistanceTo2D(para1.Position)
                                        .CompareTo(b.Position.DistanceTo2D(para1.Position));
                        });
                    }

                    if (victims.Count > 0)
                    {
                        EMSStatus = State.OnScene;
                        break;
                    }
                    else
                    {
                        para1.Task.CruiseWithVehicle(ambulance, 20f, true);
                        para1.Task.AlwaysKeepTask = true;
                        EMSStatus = State.Leave;
                        break;
                    }
                case State.Leave:
                    para1.NoLongerNeeded();
                    ambulance.NoLongerNeeded();
                    paramedicCalledOut = false;
                    EMSStatus = State.None;
                    break;
                case State.None:
                    paramedicTimer.Stop();
                    break;
            }

        }

        //Tow truck tick
        private void TowTruck_Tick(object sender, EventArgs e)
        {
            switch (TTStatus)
            {
                case State.Responding:
                    if (GTA.Native.Function.Call<bool>("IS_CAR_STUCK", new GTA.Native.Parameter[] { (Vehicle)truck }) || truck.IsUpsideDown)
                    {
                        truck.Position = World.GetNextPositionOnStreet(truck.Position.Around(10f));
                        //truck.PlaceOnNextStreetProperly();
                    }
                    if (truck.Position.DistanceTo2D(LPlayer.LocalPlayer.Ped.Position) <= 35)
                    {
                        towTruckCanCancel = false;
                        truckDriver.Task.DriveTo(towedCar.Position, 8f, false, true);
                        if (truck.Position.DistanceTo2D(towedCar.Position) <= 15f)
                        {
                            truck.AttachBlip().Delete();
                            truck.HazardLightsOn = true;
                            truck.SoundHorn(2000);
                            truck.Extras(5).Enabled = true;
                            truckDriver.Task.LeaveVehicle(truck, true);
                            DelayedCaller.Call(delegate
                            {
                                TTStatus = State.RunToObject;
                            },this,2000);
                            
                        }
                    }
                    break;
                case State.RunToObject:
                    if (TTTaskSeq == null)
                    {
                        TTTaskSeq = new TaskSequence();
                        TTTaskSeq.AddTask.RunTo(towedCar.Position);
                        TTTaskSeq.AddTask.UseMobilePhone(5000);
                        TTTaskSeq.Perform(truckDriver);
                    }
                    if (truckDriver.Position.DistanceTo2D(towedCar.Position) <= 5f)
                    {
                        TTTaskSeq.Dispose();
                        TTTaskSeq = null;
                        truckDriver.DrawTextAboveHead("Sure, I'll remove this " + towedCar.Name + ", just gonna go ahead and load up", 5000);
                        TTStatus = State.PerformDuty;
                        
                    }
                    break;
                case State.PerformDuty:
                    if (towedCar != null && towedCar.Exists())
                    {
                        towedCar.EngineRunning = false;
                        towedCar.CloseAllDoors();

                        float num1 = 0.0f;
                        float num2;
                        float dimension = -towedCar.Model.GetDimensions().Y + towedCar.Model.GetDimensions().Y;
                        if (towedCar.Model.Hash == 1171614426 || towedCar.Model.Hash == -1987130134 || towedCar.Model.Hash == 583100975 || towedCar.Model.Hash == 1638119866)
                        {
                            num2 = towedCar.Model.GetDimensions().Y;
                        }
                        else
                        {
                            num1 = (float)Math.PI;
                            num2 = towedCar.Model.GetDimensions().Y - 0.150000005960464f;
                        }
                        if ((double)dimension < (double)4.34)
                        {
                            num2 = (float)4.34 / 2f;
                        }
                        int num3 = (int)byte.MaxValue;
                        while (num3 > 0)
                        {
                            GTA.Native.Function.Call("SET_VEHICLE_ALPHA", new GTA.Native.Parameter[] { towedCar, num3 });
                            num3 -= 12;
                        }

                        float num4 = 0.0f;
                        for (int i = 0; i < 50; ++i)
                        {
                            Vehicle veh = towedCar;
                            Vector3 pos = LPlayer.LocalPlayer.Ped.Position;
                            Vector3 street = World.GetNextPositionOnStreet(((Vector3)pos.Around((float)(180.0 + (double)i * 1.0))));
                            veh.Position = street;
                            towedCar.PlaceOnNextStreetProperly();
                            towedCar.PlaceOnGroundProperly();

                            num4 = (float)towedCar.Position.Z - World.GetGroundZ(towedCar.Position);
                            if (towedCar.Rotation.X < 2.0 && towedCar.Rotation.X > -2.0 && towedCar.Rotation.X != 0.0)
                            {
                                break;
                            }
                        }
                        Vehicle truckv = truck;
                        GTA.Native.Function.Call("ATTACH_CAR_TO_CAR", new GTA.Native.Parameter[] { towedCar, truckv, 0, 0.0f, 1.2 - num2, 0.11 + num4, 0.0f, 0.0f, num1 });
                        //truck.Extras(5).Enabled = false;
                        truckv.Extras(5).Enabled = false;
                        truckv.HazardLightsOn = false;
                        TTStatus = State.Leave;
                        break;
                    }
                    break;
                case State.Leave:
                    truckDriver.Task.CruiseWithVehicle(truck, 20f, true);
                    truckDriver.Task.AlwaysKeepTask = true;
                    if (truckDriver.IsInVehicle(truck))
                    {
                        TTStatus = State.None;
                        break;
                    }
                    break;
                case State.None:
                    truckDriver.NoLongerNeeded();
                    truck.NoLongerNeeded();
                    towedCar.NoLongerNeeded();
                    towTruckTimer.Stop();
                    towTruckCalledOut = false;
                    break;
            }
        }

        private enum State
        {
            Responding,
            OnScene,
            RunToObject,
            PerformDuty,
            EndDuty,
            Leave,
            None,
        }
    }
}
