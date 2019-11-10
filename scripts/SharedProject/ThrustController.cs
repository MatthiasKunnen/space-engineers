using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IngameScript {
    class ThrustController {

        // The available thrust in N
        public double availableThrust { get; private set; }

        List<IMyThrust> thrusters;

        public ThrustController(List<IMyThrust> thrusters) {
            if (thrusters.Count == 0) {
                throw new ArgumentException("At least one thruster required");
            }

            this.availableThrust = thrusters.Sum(t => t.MaxThrust);
            this.thrusters = thrusters;
        }

        /// <summary>
        /// Apply thrust evenly distributed over all engines.
        /// </summary>
        /// <param name="N">
        /// The amount of thrust to apply in newton. If more thrust is requested
        /// than available, the maximum amount of thrust will be applied.
        /// </param>
        public void ApplyThrust(double N) {
            if (N == 0) {
                this.Stop();
                return;
            }

            if (N >= availableThrust) {
                this.ApplyFullThrust();
                return;
            }

            double percentagePerThruster = N / availableThrust;

            this.thrusters.ForEach(t => {
                var thrustToApply = t.MaxThrust * percentagePerThruster;
                t.ThrustOverride = (float)thrustToApply;
                N -= thrustToApply;
            });

            // Correct rounding errors
            var thruster = this.thrusters[0];
            thruster.ThrustOverride += (float)N;
        }

        public void ApplyFullThrust() {
            thrusters.ForEach(t => t.ThrustOverride = t.MaxThrust);
        }

        public void Stop() {
            thrusters.ForEach(t => t.ThrustOverride = 0);
        }
    }
}
