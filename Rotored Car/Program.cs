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
    internal partial class Program : MyGridProgram
    {
        private readonly string _cockpit = "Cockpit";
        private readonly float _speed = 60;
        private readonly float _steer = 50;
        private bool _thrusterPowered = false;

        private IMyShipController _control;
        private List<IMyMotorStator>[,] _motors;
        private List<IMyMotorStator>[] _steering;
        private List<IMyPistonBase>[,] _brakes;

        private bool _isAWD, _isFWD;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;

            Setup();

            ProcessArgument(Me.CustomData);
        }

        public void Setup()
        {
            _control = GridTerminalSystem.GetBlockWithName(_cockpit) as IMyShipController;

            var naming = new string[3, 2] { { "FL" , "FR"  },
                                            { "ML", "MR" },
                                            { "BL" , "BR" } };

            _motors = new List<IMyMotorStator>[naming.GetLength(1), naming.GetLength(0)];
            _steering = new List<IMyMotorStator>[naming.GetLength(1)];

            _brakes = new List<IMyPistonBase>[naming.GetLength(1), naming.GetLength(0)];

            List<IMyTerminalBlock> blocks;

            for (int i = 0; i < _motors.GetLength(0); i++)
            {
                for (int j = 0; j < _motors.GetLength(1); j++)
                {
                    blocks = new List<IMyTerminalBlock>();
                    GridTerminalSystem.SearchBlocksOfName("Motor " + naming[j, i], blocks, block => block is IMyMotorStator);
                    _motors[i, j] = new List<IMyMotorStator>();
                    foreach (var block in blocks) _motors[i, j].Add(block as IMyMotorStator);

                    blocks = new List<IMyTerminalBlock>();
                    GridTerminalSystem.SearchBlocksOfName("Brake " + naming[j, i], blocks, block => block is IMyPistonBase);
                    _brakes[i, j] = new List<IMyPistonBase>();
                    foreach (var block in blocks) _brakes[i, j].Add(block as IMyPistonBase);
                }

                blocks = new List<IMyTerminalBlock>();
                GridTerminalSystem.SearchBlocksOfName("Rotor Steering " + naming[0, i], blocks, block => block is IMyMotorStator);
                _steering[i] = new List<IMyMotorStator>();
                foreach (var block in blocks) _steering[i].Add(block as IMyMotorStator);
            }
        }

        public void Save()
        {
            Me.CustomData = (_thrusterPowered ? "thrust" : "wheel") + (_isAWD ? "awd" : (_isFWD ? "fwd" : "rwd"));
        }

        private float _timer = 0;

        public void Main(string arg, UpdateType updateSource)
        {
            try
            {
                Echo((_timer = (_timer + 0.2f) % 128) % 4 < 1 ? "-" : (_timer % 4 < 2 ? "\\" : (_timer % 4 < 3 ? "|" : "/")));
                if (_timer == 0) Setup();
                Echo("Powered by thruster: " + _thrusterPowered);
                Echo("Drive Type: " + (_isAWD ? "AWD" : (_isFWD ? "FWD" : "RWD")));

                ProcessArgument(arg);

                var move = _control.MoveIndicator;
                bool brake = _control.HandBrake || move.Y == 1;

                for (int i = 0; i < _motors.GetLength(0); i++)
                {
                    int sideFactor = (i == 0 ? 1 : -1);

                    for (int j = 0; j < _motors.GetLength(1); j++)
                    {
                        foreach (var motor in _motors[i, j])
                        {
                            motor.TargetVelocityRad = brake ? 0 : _speed * move.Z * sideFactor;
                            motor.Enabled = brake || (move.Z != 0 && (_isAWD || _isFWD ? j == 0 : j != 0) && !_thrusterPowered);
                        }

                        if (brake) foreach (var brakePiston in _brakes[i, j]) brakePiston.Velocity = 5;
                        else foreach (var brakePiston in _brakes[i, j]) brakePiston.Velocity = -5;
                    }

                    const float left = 18f;
                    float rotation = -move.X * _steer;
                    const float factor = 15f;

                    float div = (rotation + factor * (i / (_motors.GetLength(0) - 1) - 0.5f));
                    float steeringAngle = rotation != 0 ? (div != 0 ? (left / div) : 0) : 0;

                    foreach (var steer in _steering[i])
                    {
                        steer.TargetVelocityRad = steeringAngle - steer.Angle % ((float)Math.PI * 2) * 10f;
                    }
                }
            }
            catch (Exception e)
            {
                Echo(e.Message);
                throw;
            }
        }

        public void ProcessArgument(string arg)
        {
            foreach (string argument in arg.Split(';'))
            {
                switch (argument)
                {
                    case "thrust":
                        _thrusterPowered = true;
                        break;

                    case "wheel":
                        _thrusterPowered = false;
                        break;

                    case "awd":
                        _isAWD = true;
                        _isFWD = false;
                        break;

                    case "fwd":
                        _isAWD = false;
                        _isFWD = true;
                        break;

                    case "rwd":
                        _isAWD = _isFWD = false;
                        break;

                    case "update":
                        Setup();
                        break;
                }
            }
        }
    }
}
