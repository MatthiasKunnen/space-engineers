using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace IngameScript
{
    /// <summary>
    /// Originally from: http://forums.keenswh.com/threads/aligning-ship-to-planet-gravity.7373513/#post-1286885461
    /// </summary>
    class GyroController
    {

        /// <summary>
        /// The alignment of the ship
        /// </summary>
        Base6Directions.Direction alignment;

        /// <summary>
        /// How much power to use. 0 to 1.0
        /// </summary>
        double powerCoefficient;

        /// <summary>
        /// The ship controller used
        /// </summary>
        IMyShipController gyroControl;

        /// <summary>
        /// The list of gyros to use for aiming
        /// </summary>
        List<IMyGyro> gyros;

        /// <summary>
        /// How tight to maintain aim. Lower is tighter. Default is 0.01f
        /// </summary>
        float minAngleRad = 0.01f;

        public GyroController(
            IMyShipController gyroControl,
            List<IMyGyro> gyros,
            Base6Directions.Direction alignment,
            double powerCoefficient = 1
        ) {
            this.gyroControl = gyroControl;
            this.gyros = gyros;
            this.alignment = alignment;
            this.powerCoefficient = powerCoefficient;
        }

        /// <summary>
        /// Align the ship/grid with the given vector.
        /// </summary>
        /// <param name="alignment">The alignment of the ship.</param>
        /// <param name="vDirection">the vector to aim for.</param>
        /// <returns>true if aligned. Meaning the angle of error is less than minAngleRad</returns>
        public bool Align(Vector3D vDirection, Base6Directions.Direction? alignment = null) {
            bool aligned = true;
            Matrix orientation;
            gyroControl.Orientation.GetMatrix(out orientation);

            Vector3D down = orientation.GetDirectionVector(alignment ?? this.alignment);
            vDirection.Normalize();

            gyros.ForEach(gyro => {
                gyro.Orientation.GetMatrix(out orientation);

                var localCurrent = Vector3D.Transform(down, MatrixD.Transpose(orientation));
                var localTarget = Vector3D.Transform(vDirection, MatrixD.Transpose(gyro.WorldMatrix.GetOrientation()));

                // Since the gyro ui lies, we are not trying to control yaw, pitch, roll
                // but rather we need a rotation vector (axis around which to rotate)
                var rot = Vector3D.Cross(localCurrent, localTarget);
                double dot2 = Vector3D.Dot(localCurrent, localTarget);
                double ang = rot.Length();
                ang = Math.Atan2(ang, Math.Sqrt(Math.Max(0.0, 1.0 - ang * ang)));
                if (dot2 < 0) ang = Math.PI - ang; // Compensate for >+/-90
                if (ang < minAngleRad) {
                    gyro.GyroOverride = false;
                    return;
                }

                float yawMax = (float)(2 * Math.PI);

                double ctrlLvl = yawMax * (ang / Math.PI) * powerCoefficient;

                ctrlLvl = Math.Min(yawMax, ctrlLvl);
                ctrlLvl = Math.Max(0.01, ctrlLvl);
                rot.Normalize();
                rot *= ctrlLvl;

                float pitch = -(float)rot.X;
                gyro.Pitch = pitch;

                float yaw = -(float)rot.Y;
                gyro.Yaw = yaw;

                float roll = -(float)rot.Z;
                gyro.Roll = roll;

                gyro.GyroOverride = true;

                aligned = false;
            });
            return aligned;
        }

        /// <summary>
        /// Turns off all overrides on controlled Gyros
        /// </summary>
        public void Stop() {
            gyros.ForEach(gyro => {
                if (gyro.GyroOverride) {
                    gyro.GetActionWithName("Override").Apply(gyro);
                    gyro.Pitch = 0;
                    gyro.Yaw = 0;
                    gyro.Roll = 0;
                }
            });
        }
    }
}
