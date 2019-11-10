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

namespace IngameScript {
    partial class Program : MyGridProgram {
        string thrustersGroupName = "Thrusters UP"; // Group with liftoff thrusters
        string referenceBlockName = "Remote Control - Reference";
        string lcdSearchName = "LCD Launch control"; // Optional LCD with basic information.


        double marginOfErrorThrust = 1.01;
        double targetSpeed = 100;

        // The percentage of variation that exist between the target speed vs actual speed
        // before thrust is applied to correct it. This exists because the calculation of
        // thrust required vs actual thrust requires differs slightly for currently
        // unknown reasons.
        double targetSpeedVariation = 0.01;
        double speed, angle;
        double gravityStrength;

        // Specifies at how many m/s² the turn and burn will be initiated.
        double gravityTreshold = 0;

        bool reachedTopSpeedOnce;

        string turnAndBurn;

        Vector3D gravity;
        Vector3D lastObservedGravity;
        GyroController gyroController;
        List<IMyGyro> gyros;
        ThrustController thrustController;
        List<IMyThrust> thrusters;
        IMyShipController controlBlock;
        List<IMyTextPanel> lcds;

        public void Main(string args = "START") {
            Config config = new Config(Me.CustomData);

            config.Set(ref thrustersGroupName, "thrustersGroupName");
            config.Set(ref referenceBlockName, "referenceBlockName");
            config.Set(ref lcdSearchName, "lcdSearchName");
            config.Set<double>(ref marginOfErrorThrust, "marginOfErrorThrust");
            config.Set<double>(ref targetSpeed, "targetSpeed");
            config.Set<double>(ref targetSpeedVariation, "targetSpeedVariation");
            config.Set<double>(ref gravityTreshold, "gravityTreshold");

            controlBlock = GridTerminalSystem.GetBlockWithName(referenceBlockName) as IMyShipController;
            lcds = SearchBlocksWithName<IMyTextPanel>(lcdSearchName);

            if (args == "START") {
                Runtime.UpdateFrequency = UpdateFrequency.Update10;
                reachedTopSpeedOnce = false;
                turnAndBurn = null;
            }

            lcds.ForEach(lcd => {
                lcd.WritePublicTitle("Launch control");
                lcd.WritePublicText(""); // Clear LCD
            });

            if (controlBlock == null) {
                WriteLine("No control block found on grid.");
                WriteLine("Terminating script.");
                return;
            }

            thrusters = GetBlocksInGroup<IMyThrust>(thrustersGroupName);
            thrustController = new ThrustController(thrusters);
            gyros = GetBlocksOfType<IMyGyro>();
            gyroController = new GyroController(controlBlock, gyros, Base6Directions.Direction.Down, 0.8);

            gravity = controlBlock.GetNaturalGravity();
            gravityStrength = gravity.Length();
            var escaped = gravityStrength <= gravityTreshold;
            gravity.Normalize();

            if (gravityStrength != 0) {
                lastObservedGravity = gravity;
            }

            if (thrusters == null || thrusters.Count == 0) {
                WriteLine($"No thrusters found in \"{thrustersGroupName}\" group.");
                WriteLine("Terminating script.");
                return;
            }

            speed = controlBlock.GetShipSpeed();
            if (speed > targetSpeed) {
                reachedTopSpeedOnce = true;
            }

            WriteLine($"Ship speed: {Math.Round(speed, 1)} m/s");
            WriteLine($"Target: {Math.Round(targetSpeed, 1)} m/s");

            if (!escaped) {
                ApplyThrust();
                gyroController.Align(gravity);
                angle = Math.Acos(
                    Vector3D.Dot(
                        Vector3D.Normalize(controlBlock.GetNaturalGravity()),
                        Vector3D.Normalize(-controlBlock.GetShipVelocities().LinearVelocity)
                    )
                ) * 180 / Math.PI;

                WriteLine($"Angle deviation: {Math.Round(angle)}°");
            }

            if (escaped) {
                if (turnAndBurn == null) {
                    thrustController.Stop();
                    SetDampeners(false);
                }

                turnAndBurn = gyroController.Align(lastObservedGravity, Base6Directions.Direction.Up) ? "aligned" : "started";
                WriteLine($"Turn and burn: {turnAndBurn}");
            }

            if (args == "STOP" || (escaped && turnAndBurn == "aligned")) {
                thrustController.Stop();
                gyroController.Stop();
                SetDampeners(true);
                Runtime.UpdateFrequency = UpdateFrequency.None;
                ClearOutput();
                WriteLine("Launch control ended.");
            }
        }

        /// <summary>
        /// Writes one or more lines to the output.
        /// </summary>
        void WriteLine(params string[] input) {
            var line = String.Join("\n", input) + "\n";
            lcds.ForEach(lcd => {
                lcd.WritePublicText(line, true);
            });

            Echo(line);
        }

        void ClearOutput() {
            lcds.ForEach(lcd => {
                lcd.WritePublicText("");
            });
        }

        double CalculateRequiredThrust() {
            var mass = controlBlock.CalculateShipMass().TotalMass;
            var requiredThrust = mass * gravityStrength;

            return requiredThrust;
        }

        void ApplyThrust() {
            var reachedTargetSpeed = speed >= (1 - targetSpeedVariation) * targetSpeed;

            if (reachedTopSpeedOnce && reachedTargetSpeed) {
                var requiredThrust = CalculateRequiredThrust();
                thrustController.ApplyThrust(requiredThrust * marginOfErrorThrust);
            } else {
                thrustController.ApplyFullThrust();
            }
        }

        List<T> GetBlocksInGroup<T>(string groupName) where T : class {
            var groups = new List<IMyBlockGroup>();
            GridTerminalSystem.GetBlockGroups(groups);

            for (int i = 0; i < groups.Count; i++) {
                if (groups[i].Name == groupName) {
                    var groupBlocks = new List<IMyTerminalBlock>();
                    var result = new List<T>();

                    groups[i].GetBlocks(groupBlocks);
                    for (int t = 0; t < groupBlocks.Count; t++) {
                        result.Add(groupBlocks[t] as T);
                    }

                    return result;
                }
            }

            return null;
        }

        List<T> GetBlocksOfType<T>() where T : class {
            var result = new List<T>();
            var blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<T>(blocks);

            for (var i = 0; i < blocks.Count; i++) {
                result.Add((T)blocks[i]);
            }

            return result;
        }

        List<T> SearchBlocksWithName<T>(string name) where T : class {
            var result = new List<T>();
            var blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(name, blocks);

            for (var i = 0; i < blocks.Count; i++) {
                result.Add((T)blocks[i]);
            }

            return result;
        }

        void SetDampeners(bool enabled) {
            if (controlBlock.DampenersOverride != enabled) {
                controlBlock.GetActionWithName("DampenersOverride").Apply(controlBlock);
            }
        }
    }
}
