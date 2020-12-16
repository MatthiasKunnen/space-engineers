# Spinner driller
A script for drilling down with spinning drills

## Config
The config is done via custom data of the programmable block and is formatted as an INI file.
The general section has the following properties:

- *LcdInfo* The name of the LCD on which progress will be displayed
- *PistonLowerSpeed* The speed in m/s used to lower the pistons with the `LOWER` command
- *DrillSpeedMeterPerSecond* The speed of the drill at the edge in m/s, otherwise known as tangential
velocity

All other sections describe a spinner.

- *Arms* How many arms the spinner has
- *DrillGroupName* The name of this spinner's drill group
- *PistonGroupName* The name of this spinner's piston group
- *RotorName* The name of this spinner's rotor

Example:
```ini
[general]
LcdInfo=MyLcdName
PistonLowerSpeed=0.5

[1]
Arms=2
DrillGroupName=Spinner drills 1
PistonGroupName=Spinner pistons 1
RotorName=Spinner rotor 1

[2]
Arms=2
DrillGroupName=Spinner drills 2
PistonGroupName=Spinner pistons 2
RotorName=Spinner rotor 2
```

## Commands
### `LOWER`
Lowers the pistons with locked rotors.

### `PARSE`
Parses the custom data, finds blocks, and registers current angle of the rotors.

### `PERSIST_PISTON_POSITION`
Sets the maximum limit of all pistons to their current position.

### `START`
Start the process:
- Enable rotors, pistons, and drills
- Sets the maximum limit of all pistons to their current position.
- Set speed of the rotors and pistons

### `PARSE`
Parses the custom data, finds blocks, and registers current angle of the rotors.

### `STOP`
Gracefully stops the process:
- Stops rotors
- Stops drills

### `STOP_EMERGENCY`
Executes emergency stop:
- Stops rotors
- Stops drills
- Disables pistons

### `TOGGLE_SPEED`
Switch between Slow and Fast mode. This changes the speed of the rotors and the distance the
pistons lower on each cycle. Fast mode requires tunneling (holding right mouse). This can be used
to tunnel until the ore is reached.

## Usage
1. Create the structure
1. Configure the custom data of the programmable block
1. Run `LOWER`
1. Run `PARSE`
1. Run `START`
1. Use fast mode by running `TOGGLE_SPEED` (optional)
