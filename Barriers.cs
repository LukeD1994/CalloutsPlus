
using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using LCPD_First_Response.Engine;
using LCPD_First_Response.LCPDFR.API;
using Object = GTA.Object;

namespace CalloutsPlus
{
    class Barriers
    {
        private List<Object> barriers;
        private Timer barrierTimer;
        LVehicle poVeh = null;

        public Barriers()
        {
            barriers = new List<Object>();
            barrierTimer = new Timer(300);
            barrierTimer.Tick += Barrier_Tick;
        }

        public void PlaceBarrier()
        {
            for (var i = 0; i < barriers.Count; i++)
            {
                if (!LPlayer.LocalPlayer.Ped.IsInVehicle() && barriers[i].Exists())
                {
                    if (barriers[i].Position.DistanceTo(LPlayer.LocalPlayer.Ped.Position) < 1.7f)
                    {
                        foreach (Vehicle veh in World.GetVehicles(barriers[i].Position, 12f))
                        {
                            if (veh.Exists() && !veh.isSeatFree(VehicleSeat.Driver) && veh.Model.Hash != 2452219115 && (long)veh.Model.Hash != 1171614426 && (long)veh.Model.Hash != 569305213)
                            {
                                veh.GetPedOnSeat(VehicleSeat.Driver).Task.CruiseWithVehicle(veh, 20f, true);
                                veh.NoLongerNeeded();
                            }
                        }
                        foreach (Ped p in World.GetPeds(barriers[i].Position, 3f))
                        {
                            if (p != LPlayer.LocalPlayer.Ped && !p.isInVehicle() && p.Model.Hash != 1136499716 && (long)p.Model.Hash != 3119890080)
                            {
                                //p.FreezePosition = false;
                                p.Task.ClearAll();
                                p.Metadata.isBlocked = null;
                                p.NoLongerNeeded();
                            }
                            
                        }
                        barriers[i].NoLongerNeeded();
                        barriers[i].Delete();
                        barriers[i] = null;
                        barriers.Remove(barriers[i]);
                        return;
                    }
                }
            }
            if (barriers.Count == 10)
            {
                Functions.PrintHelp("You've placed the maximum number of barriers");
            }
            else
            {
                Vector3 spawnPos = LPlayer.LocalPlayer.Ped.GetOffsetPosition(new Vector3(0.0f, 1.2f, 0.0f));
                Object barrier = World.CreateObject("CJ_BARRIER_2", spawnPos);

                barrier.Position = new Vector3(barrier.Position.X, barrier.Position.Y, World.GetGroundZ(barrier.Position));
                barrier.Heading = LPlayer.LocalPlayer.Ped.Heading;
                barrier.FreezePosition = true;
                barriers.Add(barrier);
            }
            barrierTimer.Start();
        }

        private void Barrier_Tick(object sender, EventArgs e)
        {
            foreach (var t in barriers.Where(t => t.Exists()))
            {
                foreach (var p in World.GetPeds(t.Position, 4f))
                {
                    if (p != LPlayer.LocalPlayer.Ped && !p.isInVehicle())
                    {
                        if (p.Metadata.isBlocked == null) 
                        {
                            p.Metadata.isBlocked = true;
                        }

                        if (p.Metadata.isBlocked == true)
                        {
                            p.Task.Wait(-1);
                            p.Task.AlwaysKeepTask = true;
                        }
                        else
                        {
                            p.Task.ClearAll();
                            p.NoLongerNeeded();
                        }
                            
                    }
                    if (Common.GetRandomBool(0, 200, 1))//1 in 200 chance to walk away
                    {
                        p.Metadata.isBlocked = false; 
                    }
                }

                foreach (var v in World.GetVehicles(t.Position, 23f))
                {
                    if (Functions.IsPlayerPerformingPullover())
                    {
                        LHandle pullover = Functions.GetCurrentPullover();
                        poVeh = Functions.GetPulloverVehicle(pullover);
                    }
                    if (poVeh != null && poVeh.Exists())
                    {
                        if (v.Exists() && v != poVeh && !v.isSeatFree(VehicleSeat.Driver) && v.Model.Hash != 2452219115 &&
                            (long) v.Model.Hash != 1171614426)
                        {
                            Vector3 dimensions = v.Model.GetDimensions();
                            float num = t.Position.DistanceTo(v.GetOffsetPosition(new Vector3(0.0f, dimensions.Y, 0.0f)));
                            if (num < 6f)
                            {
                                v.GetPedOnSeat(VehicleSeat.Driver).Task.CruiseWithVehicle(v, 0f, true);
                            }
                            else if (num < 10.0f)
                            {
                                v.GetPedOnSeat(VehicleSeat.Driver).Task.CruiseWithVehicle(v, 4f, true);
                            }
                            else if (num < 15.0f)
                            {
                                v.GetPedOnSeat(VehicleSeat.Driver).Task.CruiseWithVehicle(v, 8f, true);
                            }
                            else if (num < 20.0f)
                            {
                                v.GetPedOnSeat(VehicleSeat.Driver).Task.CruiseWithVehicle(v, 12f, true);
                            }
                        }
                    }
                    else
                    {
                        if (v.Exists() && !v.isSeatFree(VehicleSeat.Driver) && v.Model.Hash != 2452219115 && (long)v.Model.Hash != 1171614426)
                        {
                            Vector3 dimensions = v.Model.GetDimensions();
                            float num = t.Position.DistanceTo(v.GetOffsetPosition(new Vector3(0.0f, dimensions.Y, 0.0f)));
                            if (num < 6f)
                            {
                                v.GetPedOnSeat(VehicleSeat.Driver).Task.CruiseWithVehicle(v, 0f, true);
                            }
                            else if (num < 10.0f)
                            {
                                v.GetPedOnSeat(VehicleSeat.Driver).Task.CruiseWithVehicle(v, 4f, true);
                            }
                            else if (num < 15.0f)
                            {
                                v.GetPedOnSeat(VehicleSeat.Driver).Task.CruiseWithVehicle(v, 8f, true);
                            }
                            else if (num < 20.0f)
                            {
                                v.GetPedOnSeat(VehicleSeat.Driver).Task.CruiseWithVehicle(v, 12f, true);
                            }
                        }
                    }
                    
                }
            }
        }
    }
}
