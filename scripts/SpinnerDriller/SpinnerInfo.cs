using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace IngameScript {
    partial class Program {
        public class SpinnerInfo {
            public int Arms { get; set; }
            public List<IMyShipDrill> Drills { get; set; }
            public List<IMyPistonBase> Pistons { get; set; }
            public IMyMotorAdvancedStator Rotor { get; set; }
            public double StartAngle { get; set; }

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

            public static SpinnerInfo FromIni(MyIni ini, string sectionName, LookupHelper lookupHelper) {
                var drillGroupNames = GetNamesWithPrefix(ini, sectionName, "DrillGroupName");
                var pistonGroupNames = GetNamesWithPrefix(ini, sectionName, "PistonGroupName");
                var rotorNames = GetNamesWithPrefix(ini, sectionName, "RotorName");

                return new SpinnerInfo() {
                    Arms = ini.Get(sectionName, "Arms").ToInt32(),
                    Drills = lookupHelper.GetBlocksInFirstGroup<IMyShipDrill>(drillGroupNames),
                    Pistons = lookupHelper.GetBlocksInFirstGroup<IMyPistonBase>(pistonGroupNames),
                    Rotor = lookupHelper.GetFirstBlockWithName<IMyMotorAdvancedStator>(rotorNames),
                    StartAngle = ini.Get(sectionName, "StartPosition").ToDouble(999),
                };
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
