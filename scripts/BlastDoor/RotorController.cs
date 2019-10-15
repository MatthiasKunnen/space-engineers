using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;

namespace IngameScript
{
    class RotorController
    {
        List<IMyMotorStator> rotors = new List<IMyMotorStator>();
        List<IMyMotorStator> rotorsNegative = new List<IMyMotorStator>();
        List<IMyMotorStator> rotorsNormal = new List<IMyMotorStator>();

        public float Velocity { get; private set; }

        public RotorController(List<IMyMotorStator> rotors, float velocity)
        {
            this.rotors = rotors;
            rotors.ForEach(rotor => {
                var config = new Config(rotor.CustomData);
                if (config.Get("Direction")?.ToUpper() == "NEGATIVE")
                {
                    this.rotorsNegative.Add(rotor);
                }
                else
                {
                    this.rotorsNormal.Add(rotor);
                }
            });
            this.Velocity = velocity;
        }

        public void Go(string direction)
        {
            var velocity = this.Velocity * (direction == "FORWARD" ? 1 : -1);

            this.rotorsNormal.ForEach(rotor => {
                rotor.Enabled = true;
                rotor.TargetVelocityRPM = velocity;
            });

            velocity *= -1;

            this.rotorsNegative.ForEach(rotor => {
                rotor.Enabled = true;
                rotor.TargetVelocityRPM = velocity;
            });
        }

        public void Stop()
        {
            this.rotors.ForEach(rotor => rotor.TargetVelocityRPM = 0);
        }
    }
}
