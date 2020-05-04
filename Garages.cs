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


namespace CalloutsPlus
{
    using System.Windows.Forms;

    using GTA;
    using GTA.Forms;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Input;
    using LCPD_First_Response.Engine.Scripting.Plugins;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.API;

    class Garages
    {
        ArrowCheckpoint arrowGar1, arrowGar2, arrowGar3;
        
        public Garages()
        {
            arrowGar1 = new ArrowCheckpoint(new GTA.Vector3(-417.86f, 1136.49f, 12.47f), System.Drawing.Color.Yellow, CallbackFunction); // Algonquin top left - near bridge
            arrowGar2 = new ArrowCheckpoint(new GTA.Vector3(-881.42f, 1300.54f, 21.59f), System.Drawing.Color.Yellow, CallbackFunction); // Alderney top right - near bridge
            arrowGar3 = new ArrowCheckpoint(new GTA.Vector3(70.82f, 1247.86f, 15.62f), System.Drawing.Color.Yellow, CallbackFunction); // Algonquin top right - big one

            arrowGar1.BlipIcon = BlipIcon.Building_Garage;
            arrowGar2.BlipIcon = BlipIcon.Building_Garage;
            arrowGar3.BlipIcon = BlipIcon.Building_Garage;
            
        }
        private void CallbackFunction()
        {
            if (LPlayer.LocalPlayer.Ped.IsInVehicle())
            {
                LPlayer.LocalPlayer.LastVehicle.Speed = 0;
                Game.FadeScreenOut(1000);
                DelayedCaller.Call(delegate
                {
                    LPlayer.LocalPlayer.LastVehicle.Repair();
                    Functions.PrintText("Vehicle Repaired!", 2000);
                    Game.FadeScreenIn(1000);
                }, this, 2000);
            }
            else
            {
                Functions.PrintText("You need a vehicle", 3000);
            }


        }

    }
}
