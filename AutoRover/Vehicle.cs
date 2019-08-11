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
    public partial class Program
    {
        public class Vehicle
        {
            public struct Wheel
            {
                public IMyMotorSuspension Suspension;
                public float Power;
                public float Height;
                public float Friction;
            }

            public Wheel[] Wheels;
            public IMyShipController Controller;
            public Action<string> Echo = _ => { };

            public Vehicle(IEnumerable<IMyMotorSuspension> wheels, IMyShipController controller)
            {
                Wheels = wheels.Select(x => new Wheel
                {
                    Suspension = x,
                    Power = x.Power,
                    Height = x.Height,
                    Friction = x.Friction,
                }).ToArray();

                Controller = controller;
            }

            public void WheelSettings(float powerFactor = 0, float powerOffset = 0, float heightFactor = 0, float heightOffset = 0, float frictionFactor = 0, float frictionOffset = 0)
            {
                for (int i = 0; i < Wheels.Length; i++)
                {
                    var wheel = Wheels[i];
                    IMyMotorSuspension suspension = wheel.Suspension;

                    suspension.Power = (suspension.Power * powerFactor) + powerOffset;
                    suspension.Height = (suspension.Height * heightFactor) + heightOffset;
                    suspension.Friction = (suspension.Friction * frictionFactor) + frictionOffset;
                }
            }

            public void Drive(float propulsionOverride, float steeringOverride)
            {
                Vector3D forward = Controller.WorldMatrix.Forward;
                Vector3D com = Controller.CenterOfMass;

                for (int i = 0; i < Wheels.Length; i++)
                {
                    var wheel = Wheels[i];
                    IMyMotorSuspension suspension = wheel.Suspension;

                    float propulsionFactor = (float)suspension.WorldMatrix.Left.Dot(forward);

                    suspension.SetValueFloat("Propulsion override", propulsionFactor * propulsionOverride);

                    Vector3D offset = com - suspension.GetPosition();
                    float steeringFactor = (float)offset.Dot(forward);

                    suspension.SetValueFloat("Steer override", steeringFactor * steeringOverride);
                }
            }

            public void DriveTo(Vector3D target, float minDistance) =>
                DriveInWorldDirection(target - Controller.GetPosition(), minDistance);

            public void DriveInWorldDirection(Vector3D worldDirection, float minDistance) =>
                DriveInLocalDirection(Vector3D.TransformNormal(worldDirection, MatrixD.Transpose(Controller.WorldMatrix)), minDistance);

            public void DriveInLocalDirection(Vector3D direction, float minDistance)
            {
                double distance = direction.Length();

                if (distance <= minDistance) return;

                double angle = -direction.X / direction.Length();

                Drive((float)Math.Min(1, distance/minDistance/10), (float)angle);
            }
        }
    }
}
