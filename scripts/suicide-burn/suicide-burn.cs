// Originally from https://steamcommunity.com/sharedfiles/filedetails/?id=767891298

/*How to set it up:
1) put this script in a programmable block, check code and remember and exit
2) set a timer block to run the programmable block, trigger-now itself AND start itself (1 sec delay)
3) create a group of ground facing HYDROGEN thrusters called THR (name is customizable)
4) place a cockpit or a remote control called reference (name is customizable). this block must face the sky/space
   TIP: if you want the freedom to place your cockpit facing were you want just add a remote control
5) OPTIONAL and suggested: add to the toolbar the programmable block set to run with the argument: start
6) OPTIONAL and suggested: add to the toolbar the programmable block set to run with the argument: stop
7) OPTIONAL and suggested: add to the toolbar the programmable block set to run with the argument: fall
8) modify the Mandatory settings below if needed (it's very likely you will have to modify this settings)
9) make sure to own all blocks
10) NEW OPTIONAL FEATURE: if you want to trigger a timer block when ships enters gravity, lands, etc...
    you just have to put a Prefix in front of the timer block you want to trigger. all 3 supported
    Prefixes are below (line 114).
11) NEW OPTIONAL FEATURE: if you want to trigger a timer block when the ship reach a certain altitude
    you just have to put a prefix in the name of the timer block, more info on the prefix at line 105

How to use:
1) trigger now the timer
2) position your ship above the landing zone
3) run programmable block with start argument (suggested to use toolbar) if you use start argument outside
   natural gravity the script will just wait for the ship to enter the gravity field and then will automatically
   start the auto alignment feature. if you want it to be completely automatic (for a drop pod for example)
   you have to set AutoFall setting to true.
4) (if you do not use AutoFall setting) when you are ready to drop run programmable block with fall argument
(suggested to use toolbar) if you are landing on a flat surface i suggest to disable inertial dumpeners.
5) when near the ground (50 meters) you will think you are going to die, but don't worry, it will be
   ok as long as you set the script up correctly
6) if something goes wrong you can use the stop argument to get back the control of the ship. stopping
   the script from running is not enough, because you will have to turn off overrides manually.

NOTE: point 1) 2) and 3) can be performed in no particular order.

Further explanations of why a fall argument is needed can be found on the workshop page
*/

// Mandatory settings (You have to modify this unless they are already set correctly)
// THIS IS REALLY IMPORTANT: set this to your world's inventory multiplier setting
// (REALISTIC = 1)
int InventoryMultiplier = 10;

// SET THIS TO THE DISPLAYED ALTITUDE WHEN YOUR SHIP IS ON THE GROUND.
// if your ship doesn't stop in time add some meters here, but not too much otherwise
// script will stop too far from the ground and the ship will free fall on the ground.
// TIP: you can set this to 0 if you plan to stop your ship away from the ground, this
// value is important only when landing on the ground.
double AltitudeMargin = 0;

// WARNING: The behaviour of this setting has changed in the update 2, to achieve the
// same results for your already existing creations you need to change this value to the
// previous value minus the AltitudeMargin value.
// Before the update this value had to be greater than AltitudeMargin, now it's not
// needed anymore. when your ship goes below the StopAltitude plus this value and
// your speed is below StopSpeed setting, the script will disable itself.
// If you are using the SmartDeactivation feature, the script will disable itself only
// if your ship is below the StopAltitude plus this value. More info below (line 91)
double DisableMargin = 15;

// Name of the cockpit/remote control used to get the direction of the
// ship. this block must face the the sky/space.
string ShipControllerName = "Remote Control - Reference";

// This group must contain the hydrogen thrusters that are facing the
// surface of the planet. this thrusters will be used to stop the ship
// at the last second (more or less)
string HydrogenThrustersGroupName = "Thrusters UP";

// Optional LCDs with basic information.
string lcdSearchName = "LCD Suicide Burn";

// If set to 0 the ship will stop on the surface, otherwise will stop at the altitude
// specified. if you set an high value i recommended to keep inertial dampeners on
// unless you want your ship destroyed.
double StopAltitude = 1000;

// Script will deactivate itself if the ships speed is below StopSpeed setting
// TIP: if your ship touches the ground but then goes to the sky, you may want to set a greater StopSpeed value
// THIS TIP APPLIES ONLY IF SmartDeactivation IS TURNED OFF, IF IT'S ON I SUGGEST TO LEAVE StopSpeed to 0
// IF YOU DECIDE TO TURN OFF SmartDeactivation YOU MUST SET StopSpeed TO SOMETHING ABOVE 0
// (i suggest a low value like 1 if your ship is big and heavy, or a value like 5 if its a small drop pod)
double StopSpeed = 0;

// As soon as start command is run, script will automatically run also fall command, useful for a completely
// automatic setup. (just need to use the start argument anywhere and as soon as you enter a gravity field
// script will do its job automatically)
bool AutoFall = true;

// The script will turn off its Autopilot feature as soon as the ship starts to change
// direction (e.g. when touches the ground). As a safety this feature works only when the ship is
// near the ground (e.g. when altitude is below StopAltitude + AltitudeMargin + DisableMargin)
// What this means in practice? Your ship will be able to land more safely on flat surfaces and sloped
// surfaces without the problem that sometimes happens: the ship flyies to the sky.
// NOTE: if your ship is big is still not recommended to land on the side of the mountains because
// The altitude is calculated from the point of the terrain below the center of the ship.
// NOTE 2: This feature is perfect for drop pods, even if you are going to land on the side of a mountain
// NOTE 3: This feature is recommended also for big ships because makes the landing softer.
// NOTE 4: If this feature is on you can set StopSpeed to 0 (i strongly suggest to do it).
bool SmartDeactivation = true;

// Prevent wiggle during alignment, 0.5 seems to work but you are free to
// experiment different values. if the ship wiggles to much lower this value.
double RotationSpeedLimit = 0.8;

// Main script body
bool Autopilot = false;
bool AutoFallUsed = false;
Vector3D OldVelocity3D = new Vector3D(0, 0, 0);
List<IMyTextPanel> lcds;

public void Main(string input) {
    lcds = SearchBlocksWithName<IMyTextPanel>(lcdSearchName);
    ClearOutput();

    if (input == "start") {
        Runtime.UpdateFrequency = UpdateFrequency.Update10;
        Autopilot = true;
    }

    IMyShipController controlBlock = (IMyShipController)GridTerminalSystem.GetBlockWithName(ShipControllerName);

    double altitude = 0;
    bool InsideNaturalGravity = controlBlock.TryGetPlanetElevation(MyPlanetElevation.Surface, out altitude);

    Vector3D velocity3D = controlBlock.GetShipVelocities().LinearVelocity;

    if (!InsideNaturalGravity) {
        if (Autopilot) {
            WriteLine("Waiting for entering natural gravity");
            if (input == "stop") {
                Autopilot = false;
                WriteLine("Autopilot deactivated (manually)");
            }
        }
        return;
    } else {
        if (Autopilot && AutoFall) {
            if (!AutoFallUsed) {
                input = "fall";
                AutoFallUsed = true;
            }
        }
    }

    List<IMyThrust> thrusters = GetBlocksInGroup<IMyThrust>(HydrogenThrustersGroupName);
    ThrustController thrustController = new ThrustController(thrusters);

    List<AdvGyro> AdvancedGyros = new List<AdvGyro>();
    AdvancedGyros = AdvGyro.GetAllGyros(GridTerminalSystem, controlBlock, true);

    Vector3D gravity = controlBlock.GetNaturalGravity();
    Vector3D position = controlBlock.GetPosition(); // ship coords
    double gravityStrength = gravity.Length(); // gravity in m/s^2
    double totalMass = controlBlock.CalculateShipMass().TotalMass; // ship total mass including cargo mass
    double baseMass = controlBlock.CalculateShipMass().BaseMass; // mass of the ship without cargo
    double cargoMass = totalMass - baseMass; // mass of the cargo
    double actualMass = baseMass + (cargoMass / InventoryMultiplier); // the mass the game uses for physics calculation
    double shipWeight = actualMass * gravityStrength; // weight in newtons of the ship
    double velocity = controlBlock.GetShipSpeed(); // ship velocity
    double brakeDistance = CalculateBrakeDistance(gravityStrength, actualMass, altitude, thrustController.availableThrust, velocity);
    double brakeAltitude = StopAltitude + brakeDistance; // at this altitude the ship will start slowing Down

    if (Autopilot) {
        Vector3D Target = position - gravity;

        var x = Math.Floor(Target.GetDim(0)).ToString();
        var y = Math.Floor(Target.GetDim(1)).ToString();
        var z = Math.Floor(Target.GetDim(2)).ToString();

        float range = (float)((controlBlock.GetPosition() - Target).Length());
        float gyroPitch = 0;
        float gyroYaw = 0;

        GetDirectionTo(Target, controlBlock, ref gyroPitch, ref gyroYaw);

        float speedMultiplier = (float)Math.Sqrt(Math.Pow(Math.Sqrt(gyroPitch * gyroPitch + gyroYaw * gyroYaw), 3)) * 0.01f;

        var angularVelocity = controlBlock.GetShipVelocities().AngularVelocity;
        var totalAngularVelocity = angularVelocity.Length();

        if (totalAngularVelocity <= RotationSpeedLimit) {
            AdvGyro.SetAllGyros(AdvancedGyros, true, gyroPitch * speedMultiplier, gyroYaw * speedMultiplier, 0);
        } else {
            AdvGyro.SetAllGyros(AdvancedGyros, true, 0, 0, 0);
        }

        if (input == "fall") {
            // This is a workaround to a game bug (ship speed greater than speed limit when free falling in natural gravity)
            // Pros: your ship will not crash. Cons: you will waste a tiny amount of hydrogen.
            thrustController.ApplyThrust(1);
        }

        if (altitude <= (brakeAltitude + AltitudeMargin)) {
            // BRAKE!!!
            thrustController.ApplyFullThrust(); // Maybe just enable dampeners
        }

        if (altitude <= (StopAltitude + DisableMargin + AltitudeMargin)) {
            if (velocity < StopSpeed) {
                deactivate(AdvancedGyros);
                WriteLine("Autopilot deactivated (automatically)");
            }

            if (SmartDeactivation) {
                if (OldVelocity3D.X * velocity3D.X < 0 || OldVelocity3D.Y * velocity3D.Y < 0 || OldVelocity3D.Z * velocity3D.Z < 0) {
                    deactivate(AdvancedGyros);
                    WriteLine("Autopilot deactivated (automatically)");
                }
            }
        }
    }

    OldVelocity3D = velocity3D;

    if (input == "stop") {
        Runtime.UpdateFrequency = UpdateFrequency.None;
        thrustController.Stop();
        deactivate(AdvancedGyros);
        WriteLine("Autopilot deactivated (manually)");
    }
}

void ClearOutput() {
    lcds.ForEach(lcd => {
        lcd.WritePublicText("");
    });
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

double CalculateBrakeDistance(double gravityStrength, double actualMass, double altitude, double maxthrust, double speed) {
    double shipWeight = actualMass * gravityStrength;
    double brakeForce = maxthrust - shipWeight;
    double deceleration = brakeForce / actualMass;
    double brakeDistance = (speed * speed) / (2 * deceleration);

    return brakeDistance;
}

private void deactivate(List<AdvGyro> AdvancedGyros) {
    AdvGyro.FreeAllGyros(AdvancedGyros);

    Autopilot = false;
    AutoFallUsed = false;
}

class ThrustController {

    // The available thrust in N
    public readonly double availableThrust;

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

class AdvGyro {
    public IMyTerminalBlock Gyro {
        get;
        private set;
    }

    public float Pitch {
        get{ return Gyro.GetValueFloat(strPitch) * intPitch; }
        set{ Gyro.SetValueFloat(strPitch, value * intPitch); }
    }

    public float Yaw {
        get{ return Gyro.GetValueFloat(strYaw) * intYaw; }
        set{ Gyro.SetValueFloat(strYaw, value * intYaw); }
    }

    public float Roll {
        get{ return Gyro.GetValueFloat(strRoll) * intRoll; }
        set{ Gyro.SetValueFloat(strRoll, value * intRoll); }
    }

    public float Power {
        get{ return Gyro.GetValueFloat("Power"); }
        set{ Gyro.SetValueFloat("Power", value); }
    }

    public bool Override {
        get{ return Gyro.GetValue<bool>("Override"); }
        set{ Gyro.SetValue<bool>("Override", value); }
    }

    public bool Enabled {
        get{ return Gyro.GetValue<bool>("OnOff"); }
        set{ Gyro.SetValue<bool>("OnOff", value); }
    }

    private string strPitch;
    private int intPitch;
    private string strYaw;
    private int intYaw;
    private string strRoll;
    private int intRoll;

    public AdvGyro(IMyTerminalBlock MyGyro, IMyTerminalBlock ForwardCockpit) {
        Gyro = MyGyro;
        Orientate(ForwardCockpit);
    }

    public void Free() {
        this.Pitch = 0;
        this.Yaw = 0;
        this.Roll = 0;
        this.Override = false;
    }

    public static List<AdvGyro> GetAllGyros(IMyGridTerminalSystem Term, IMyTerminalBlock ForwardCockpit, bool OnlyOwnGrid = true)  {
        List<IMyTerminalBlock> AllGyros = new List<IMyTerminalBlock>();
        Term.GetBlocksOfType<IMyGyro>(AllGyros);
        if (OnlyOwnGrid) {
            AllGyros.RemoveAll(x => x.CubeGrid != ForwardCockpit.CubeGrid);
        }

        List<AdvGyro> AdvGyros = new List<AdvGyro>();
        foreach (IMyTerminalBlock g in AllGyros) {
            AdvGyro NewAdvGyro = new AdvGyro(g, ForwardCockpit);
            AdvGyros.Add(NewAdvGyro);
        }

        return AdvGyros;
    }

    public static void SetAllGyros(
        List<AdvGyro> AllGyros,
        bool AutoOverride = true,
        float? NewPitch = null,
        float? NewYaw = null,
        float? NewRoll = null
    ) {
        foreach(AdvGyro g in AllGyros) {
            if (NewPitch.HasValue) {
                g.Pitch = (float)NewPitch;
            }

            if (NewYaw.HasValue) {
                g.Yaw = (float)NewYaw;
            }

            if (NewRoll.HasValue) {
                g.Roll = (float)NewRoll;
            }

            if (AutoOverride) {
                if (g.Override == false) {
                    g.Override = true;
                }
            }
        }
    }

    public static void FreeAllGyros(List<AdvGyro> AllGyros) {
        foreach(AdvGyro g in AllGyros) {
            g.Free();
        }
    }

    // Big thanks to Skleroz for this awesome stuff.
    private void Orientate(IMyTerminalBlock ReferencePoint) {
        Vector3 V3For = Base6Directions.GetVector(ReferencePoint.Orientation.TransformDirection(Base6Directions.Direction.Forward));
        Vector3 V3Up = Base6Directions.GetVector(ReferencePoint.Orientation.TransformDirection(Base6Directions.Direction.Up));
        V3For.Normalize();
        V3Up.Normalize();
        Base6Directions.Direction B6DFor = Base6Directions.GetDirection(V3For);
        Base6Directions.Direction B6DTop = Base6Directions.GetDirection(V3Up);
        Base6Directions.Direction B6DLeft = Base6Directions.GetLeft(B6DTop, B6DFor);
        Base6Directions.Direction GyroUp = Gyro.Orientation.TransformDirectionInverse(B6DTop);
        Base6Directions.Direction GyroForward = Gyro.Orientation.TransformDirectionInverse(B6DFor);
        Base6Directions.Direction GyroLeft = Gyro.Orientation.TransformDirectionInverse(B6DLeft);

        switch (GyroUp) {
            case Base6Directions.Direction.Up:
                strYaw = "Yaw";
                intYaw = 1;
                break;
            case Base6Directions.Direction.Down:
                strYaw = "Yaw";
                intYaw = -1;
                break;
            case Base6Directions.Direction.Left:
                strYaw = "Pitch";
                intYaw = 1;
                break;
            case Base6Directions.Direction.Right:
                strYaw = "Pitch";
                intYaw = -1;
                break;
            case Base6Directions.Direction.Backward:
                strYaw = "Roll";
                intYaw = 1;
                break;
            case Base6Directions.Direction.Forward:
                strYaw = "Roll";
                intYaw = -1;
                break;
        }

        switch (GyroLeft) {
            case Base6Directions.Direction.Up:
                strPitch = "Yaw";
                intPitch = 1;
                break;
            case Base6Directions.Direction.Down:
                strPitch = "Yaw";
                intPitch = -1;
                break;
            case Base6Directions.Direction.Left:
                strPitch = "Pitch";
                intPitch = 1;
                break;
            case Base6Directions.Direction.Right:
                strPitch = "Pitch";
                intPitch = -1;
                break;
            case Base6Directions.Direction.Backward:
                strPitch = "Roll";
                intPitch = 1;
                break;
            case Base6Directions.Direction.Forward:
                strPitch = "Roll";
                intPitch = -1;
                break;
        }

        switch (GyroForward) {
            case Base6Directions.Direction.Up:
                strRoll = "Yaw";
                intRoll = -1;
                break;
            case Base6Directions.Direction.Down:
                strRoll = "Yaw";
                intRoll = 1;
                break;
            case Base6Directions.Direction.Left:
                strRoll = "Pitch";
                intRoll = -1;
                break;
            case Base6Directions.Direction.Right:
                strRoll = "Pitch";
                intRoll = 1;
                break;
            case Base6Directions.Direction.Backward:
                strRoll = "Roll";
                intRoll = -1;
                break;
            case Base6Directions.Direction.Forward:
                strRoll = "Roll";
                intRoll = 1;
                break;
        }
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

List<T> SearchBlocksWithName<T>(string name) where T : class {
    var result = new List<T>();
    var blocks = new List<IMyTerminalBlock>();
    GridTerminalSystem.SearchBlocksOfName(name, blocks);

    blocks.ForEach(block => result.Add((T)block));

    return result;
}

void GetDirectionTo(
    VRageMath.Vector3D TV,
    IMyTerminalBlock origin,
    ref float pitch,
    ref float yaw
) {
    VRageMath.Vector3D OV = origin.GetPosition(); // Get positions of reference blocks.
    VRageMath.Vector3D FV = origin.WorldMatrix.Forward + origin.GetPosition();
    VRageMath.Vector3D UV = origin.WorldMatrix.Up + origin.GetPosition();
    VRageMath.Vector3D RV = origin.WorldMatrix.Right + origin.GetPosition();

    float TVOV = (float)((OV - TV).Length()); // Get magnitudes of vectors.

    float TVFV = (float)((FV - TV).Length());
    float TVUV = (float)((UV - TV).Length());
    float TVRV = (float)((RV - TV).Length());

    float OVFV = (float)((FV - OV).Length());
    float OVUV = (float)((UV - OV).Length());
    float OVRV = (float)((RV - OV).Length());

    float ThetaP = (float)(Math.Acos((TVUV * TVUV - OVUV * OVUV - TVOV * TVOV) / (-2 * OVUV * TVOV)));
    // Use law of cosines to determine angles.
    float ThetaY = (float)(Math.Acos((TVRV * TVRV - OVRV * OVRV - TVOV * TVOV) / (-2 * OVRV * TVOV)));

    float RPitch = (float)(90 - (ThetaP * 180 / Math.PI)); // Convert from radians to degrees.
    float RYaw = (float)(90 - (ThetaY * 180 / Math.PI));

    if (TVOV < TVFV) RPitch = 180 - RPitch;// Normalize angles to -180 to 180 degrees.
    if (RPitch > 180) RPitch = -1 * (360 - RPitch);

    if (TVOV < TVFV) RYaw = 180 - RYaw;
    if (RYaw > 180) RYaw = -1 * (360 - RYaw);

    pitch = RPitch; // Set Pitch and Yaw outputs.
    yaw = RYaw;
}
