using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript
{
    public partial class Program : MyGridProgram
    {
        private MyIni _ini = new MyIni();
        private Vehicle _vehicle;
        private const string _section = "AutoRover";
        private Vector3? _target;

        public Program()
        {
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();

            _ini.TryParse(Me.CustomData);

            GridTerminalSystem.GetBlockGroupWithName(_ini.Get(_section, "WheelGroup").ToString()).GetBlocks(blocks);

            _vehicle = new Vehicle(
                blocks.Cast<IMyMotorSuspension>(),
                (IMyShipController)GridTerminalSystem.GetBlockWithName(_ini.Get(_section, "Controller").ToString()))
            {
                Echo = Echo
            };

            double[] values = _ini.Get(_section, "Target").ToString().Split(':').Skip(2).Take(3).Select(Convert.ToDouble).ToArray();
            _target = new Vector3(values[0], values[1], values[2]);

            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        public void Save()
        {
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (argument == "reset")
            {
                _vehicle.Drive(0, 0);
                Runtime.UpdateFrequency = UpdateFrequency.None;
                return;
            }
            _vehicle.DriveTo(_target.Value, 10);
        }
    }
}
