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
                        _targetVelocityRotor *= 5;
                    }
                }

                return (float)_targetVelocityRotor;
            }

            public static SpinnerInfo FromIni(MyIni ini, string sectionName, LookupHelper lookupHelper) {
                return new SpinnerInfo() {
                    Arms = ini.Get(sectionName, "Arms").ToInt32(),
                    Drills = lookupHelper.GetBlocksInGroup<IMyShipDrill>(ini.Get(sectionName, "DrillGroupName").ToString(), true),
                    Pistons = lookupHelper.GetBlocksInGroup<IMyPistonBase>(ini.Get(sectionName, "PistonGroupName").ToString(), true),
                    Rotor = lookupHelper.GetBlockWithName<IMyMotorAdvancedStator>(ini.Get(sectionName, "RotorName").ToString(), true),
                    StartAngle = ini.Get(sectionName, "StartPosition").ToDouble(999),
                };
            }
        }
    }
}
