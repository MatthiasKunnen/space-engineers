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
double currentOverride = 1;
double gravityTreshold = 0.2; // Specifies at how many g script will stop, 0g by default.

bool reachedTargetSpeedOnce;
bool isPreviousCorrectionIncrease;

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

    currentOverride = GetCurrentOverride();
    speed = controlBlock.GetShipSpeed();

    ApplyThrust();

    timer.SetValue("TriggerDelay", 1f);
    timer.ApplyAction("Start");

    angle = Math.Acos(Vector3D.Dot(Vector3D.Normalize(controlBlock.GetNaturalGravity()),
    Vector3D.Normalize(-controlBlock.GetShipVelocities().LinearVelocity))) * 180 / Math.PI;
    WriteLine($"Ship speed: {Math.Round(speed, 1)} m/s");
    WriteLine($"Target: {Math.Round(targetSpeed, 1)} m/s");
    WriteLine($"Angle deviation: {Math.Round(angle)}°");

    if (controlBlock.GetNaturalGravity().Length() <= gravityTreshold || args == "STOP") {
        thrusters.ForEach(t => t.SetValueFloat("Override", 0));

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

double GetCurrentOverride() {
    return thrusters.Average(t => t.CurrentThrust);
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
