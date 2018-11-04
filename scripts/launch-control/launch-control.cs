// This script allows for automated override management for planetary liftoff
// to conserve fuel.

/******************************************************************************
Quick setup:
1. Put thrusters you want to control in group "Thrusters UP"
2. Have remote control block on grid
4. Run script to start.

******************************************************************************/

string thrustersGroupName = "Thrusters UP"; // Group with liftoff thrusters
string referenceBlockName = "Remote Control - Reference";
string lcdSearchName = "LCD Launch control"; // Optional LCD with basic information.


double marginOfErrorThrust = 1.01;
double targetSpeed = 100;
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

void Main(string args = "START") {
    Config config = new Config(Me.CustomData);

    config.Set(ref thrustersGroupName, "thrustersGroupName");
    config.Set(ref referenceBlockName, "referenceBlockName");
    config.Set(ref lcdSearchName, "lcdSearchName");
    config.Set<double>(ref marginOfErrorThrust, "marginOfErrorThrust");
    config.Set<double>(ref targetSpeed, "targetSpeed");
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
    var reachedTargetSpeed = speed >= 0.99 * targetSpeed;

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

    for(var i = 0; i < blocks.Count; i++) {
        result.Add((T)blocks[i]);
    }

    return result;
}

List<T> SearchBlocksWithName<T>(string name) where T : class {
    var result = new List<T>();
    var blocks = new List<IMyTerminalBlock>();
    GridTerminalSystem.SearchBlocksOfName(name, blocks);

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

class Config {
    IDictionary<string, string> dictionary;

    public Config(string configData) {
        this.dictionary = configData.Split(new [] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('='))
            .ToDictionary(split => split[0], split => split[1]);
    }

    public T? Get<T>(string key, T? defaultValue = null) where T : struct {
        string value;
        return dictionary.TryGetValue(key, out value)
            ? (T)Convert.ChangeType(value, typeof(T))
            : defaultValue;
    }

    public string Get(string key, string defaultValue = null) {
        string value;
        return dictionary.TryGetValue(key, out value) ? value : defaultValue;
    }

    // Set if key exists
    public void Set<T>(ref T var, string key) where T : struct {
        var = this.Get<T>(key) ?? var;
    }

    public void Set(ref string var, string key) {
        var = this.Get(key) ?? var;
    }
}

class ThrustController {

    // The available thrust in N
    double availableThrust;

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
#endregion
