using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace IngameScript {
    partial class Program {

        public struct SpinnerCycleStatus {
            public double ChangeInAngle;
            public double CurrentAngle;
            public bool CycleFinished;
            public double NormalizedAngle;
        }

        public class SpinnerInfo {
            public int Arms { get; set; }
            /// <summary>
            /// When the drill bounces of the thing it is drilling the check performed to see if
            /// the rotor's angle has looped back from 360 to 0 could detect this as the spinner
            /// doing a full 360. To prevent this, we add the bounce back protection to the current
            /// angle. With protection set to 50, the spinner needs to bounce back 50° before it
            /// could be confused with the angle looping back.
            /// </summary>
            public double BounceBackProtectionDegrees { get; set; } = 100;
            public List<IMyShipDrill> Drills { get; set; }
            public List<IMyPistonBase> Pistons { get; set; }
            public IMyMotorAdvancedStator Rotor { get; set; }
            public double StartAngle { get; private set; }

            private bool? _lastIsFast;
            private float? _targetVelocityRotor;
            public float GetTargetVelocityRotorRad(bool isFast, float drillSpeedMetersPerSecond) {
                if (_targetVelocityRotor == null || _lastIsFast != isFast) {
                    _lastIsFast = isFast;
                    var radius = (Drills.Count() - 1) / Arms * Constants.LargeBlockSize;
                    _targetVelocityRotor = drillSpeedMetersPerSecond / radius;

                    if (isFast) {
                        _targetVelocityRotor *= 10;
                    }
                }

                return (float)_targetVelocityRotor;
            }


            private double _lastAngle = Double.NegativeInfinity;
            /// <summary>
            /// Normalization is applied to combat the angle looping back to zero after reaching
            /// 360.
            /// </summary>
            private bool _applyNormalization = false;
            public SpinnerCycleStatus CalculateCycleStatus() {
                var currentAngle = Utils.RadiansToDegrees(Rotor.Angle);
                _applyNormalization = _applyNormalization || _lastAngle > (currentAngle + BounceBackProtectionDegrees);
                var normalizedAngle = currentAngle + (_applyNormalization ? 360 : 0);
                var changeInAngle = Math.Abs(StartAngle - normalizedAngle);
                var cycleFinished = changeInAngle > (360 / Arms);

                _lastAngle = currentAngle;

                if (cycleFinished) {
                    _applyNormalization = false;
                    StartAngle = currentAngle;
                }

                return new SpinnerCycleStatus() {
                    CurrentAngle = currentAngle,
                    ChangeInAngle = changeInAngle,
                    CycleFinished = cycleFinished,
                    NormalizedAngle = normalizedAngle,
                };
            }

            public static SpinnerInfo FromIni(MyIni ini, string sectionName, LookupHelper lookupHelper) {
                var drillGroupNames = GetNamesWithPrefix(ini, sectionName, "DrillGroupName");
                var pistonGroupNames = GetNamesWithPrefix(ini, sectionName, "PistonGroupName");
                var rotorNames = GetNamesWithPrefix(ini, sectionName, "RotorName");

                var spinner = new SpinnerInfo() {
                    Arms = ini.Get(sectionName, "Arms").ToInt32(),
                    Drills = lookupHelper.GetBlocksInFirstGroup<IMyShipDrill>(drillGroupNames),
                    Pistons = lookupHelper.GetBlocksInFirstGroup<IMyPistonBase>(pistonGroupNames),
                    Rotor = lookupHelper.GetFirstBlockWithName<IMyMotorAdvancedStator>(rotorNames),
                };

                spinner.StartAngle = Utils.RadiansToDegrees(spinner.Rotor.Angle);

                return spinner;
            }

            private static List<string> GetNamesWithPrefix(MyIni ini, string sectionName, string key) {
                var names = new List<string>();

                var value = ini.Get(sectionName, key);

                if (!value.IsEmpty) {
                    names.Add(value.ToString());
                }

                var prefixValue = ini.Get("General", $"{key}Prefix");

                if (!prefixValue.IsEmpty) {
                    names.Add($"{prefixValue} {sectionName}");
                }

                return names;
            }
        }
    }
}
