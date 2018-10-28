// This script allows for automated override management for planetary liftoff
// to conserve fuel.

/******************************************************************************
Quick setup:
1. Put thrusters you want to control in group "Thrusters UP"
2. Have remote control block on grid
3. Timer named "Rocket Timer Block" set to run this script with argument "TIMER"
4. Run script to start.

******************************************************************************/

string ascentThrustersGroup = "Thrusters UP"; // Group with liftoff thrusters
string ascentTimerName = "Launch control Timer Block"; // Set up to run this programmable block
string referenceBlock = "Remote Control - Reference";
string lcdDisplay = "Launch control LCD Panel"; // Optional LCD display with basic information.

double step = 0.5;
double minStep = 0.0001;
double targetSpeed = 250;
double speed, angle;
double gravityTreshold = 0; // Specifies at how many gyro script will stop, 0g by default.

bool reachedTargetSpeedOnce;
bool isPreviousCorrectionIncrease;

GyroController gyroController;
List<IMyGyro> gyros;
List<IMyThrust> thrusters;
IMyShipController controlBlock;
IMyTimerBlock timer;
IMyTextPanel lcd;

void Main(string args = "START") {
    controlBlock = GridTerminalSystem.GetBlockWithName(referenceBlock) as IMyShipController;
    timer = GridTerminalSystem.GetBlockWithName(ascentTimerName) as IMyTimerBlock;
    lcd = GridTerminalSystem.GetBlockWithName(lcdDisplay) as IMyTextPanel;

    if (args == "START") {
        isPreviousCorrectionIncrease = true;
        reachedTargetSpeedOnce = false;
    }

    if (lcd != null) {
        lcd.WritePublicTitle("Launch control");
        lcd.WritePublicText(""); // Clear LCD
    }

    thrusters = GetBlocksInGroup<IMyThrust>(ascentThrustersGroup);
    gyros = GetBlocksOfType<IMyGyro>();
    gyroController = new GyroController(controlBlock, gyros, Base6Directions.Direction.Down);

    var gravity = controlBlock.GetNaturalGravity();
    var gravityLength = gravity.Length();
    var escaped = gravityLength <= gravityTreshold;
    gravity.Normalize();

    if (controlBlock == null) {
        WriteLine("No control block found on grid.");
        WriteLine("Terminating script.");
        return;
    }

    if (timer == null) {
        WriteLine($"\"{ascentTimerName}\" not found on grid.");
        WriteLine("Terminating script.");
        return;
    }

    if (thrusters == null || thrusters.Count == 0) {
        WriteLine($"No thrusters found in \"{ascentThrustersGroup}\" group.");
        WriteLine("Terminating script.");
        return;
    }

    speed = controlBlock.GetShipSpeed();

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

    timer.SetValue("TriggerDelay", 1f);
    timer.ApplyAction("Start");

    if (args == "STOP" || escaped) {
        thrusters.ForEach(t => t.SetValueFloat("Override", 0));
        gyroController.Stop();
        timer.ApplyAction("Stop");
        ClearOutput();
        WriteLine("Launch control ended.");
    }
}

/// <summary>
/// Writes one or more lines to the output.
/// </summary>
void WriteLine(params string[] input) {
    var line = String.Join("\n", input) + "\n";
    if (lcd != null) {
        lcd.WritePublicText(line, true);
    }

    Echo(line);
}

void ClearOutput() {
    if (lcd != null) {
        lcd.WritePublicText("");
    }
}

void ApplyThrust() {
    var decreaseStep = false;
    var reachedTargetSpeed = speed >= 0.99 * targetSpeed;

    if (reachedTargetSpeed) {
        reachedTargetSpeedOnce = true;
    }

    if (!reachedTargetSpeedOnce) {
        WriteLine("Setting max value");
        thrusters.ForEach(t => t.SetValueFloat("Override", t.MaxThrust));
    } else if (reachedTargetSpeed) {
        SetThrusterMultiplier(step);
        isPreviousCorrectionIncrease = false;

        decreaseStep = isPreviousCorrectionIncrease;
    } else if (!reachedTargetSpeed) {
        SetThrusterMultiplier(1 + step);
        isPreviousCorrectionIncrease = true;

        decreaseStep = !isPreviousCorrectionIncrease;
    }

    if (decreaseStep) {
        step = Math.Max(step * 0.75, minStep);
    }
}

void SetThrusterMultiplier(double multiplier) {
    WriteLine($"Apply thrust multiplier {multiplier}");
    thrusters.ForEach(t => t.SetValueFloat("Override", t.CurrentThrust * (float)multiplier));
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

    for(var i = 0; i < blocks.Count; i++) {
        result.Add((T)blocks[i]);
    }

    return result;
}

void SetDampeners(bool enabled) {
    if (controlBlock.DampenersOverride != enabled) {
        controlBlock.GetActionWithName("DampenersOverride").Apply(controlBlock);
    }
}

#region Gyro
partial class GyroController: MyGridProgram {
    // Originally from: http://forums.keenswh.com/threads/aligning-ship-to-planet-gravity.7373513/#post-1286885461

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
        double powerCoefficient = 0.3
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
            gyro.GetActionWithName("OnOff_On").Apply(gyro);
            if (gyro.GyroOverride) {
                gyro.GetActionWithName("Override").Apply(gyro);
            }
        });
    }
}
#endregion
