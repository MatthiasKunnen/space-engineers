# Spinner driller
A script for drilling down with spinning drills

## Config
The config is done via custom data of the programmable block and is formatted as an INI file.
The general section has the following properties:

- **DrillGroupNamePrefix** If you do not specify _DrillGroupName_ in the spinner config, this
  prefix will be used, appended with the section name, to find the drills of the spinner.  
  e.g. with `DrillGroupNamePrefix=Drills` and section name equal to `Maroon`, the script will
  search for a group with name `Drills Maroon`. If one is not found it will try `DrillGroupName`
  in the `Maroon` section.
- **DrillSpeedMeterPerSecond** The speed of the drill at the edge in m/s, otherwise known as tangential
  velocity
- **LcdInfo** The name of the LCD on which progress will be displayed
- **PistonLowerSpeed** The speed in m/s used to lower the pistons with the `LOWER` command
- **PistonGroupNamePrefix** If you do not specify _PistonGroupName_ in the spinner config, this prefix
  will be used, appended with the section name, to find the pistons of the spinner.
- *RotorNamePrefix* If you do not specify _RotorGroupName_ in the spinner config, this prefix will
  be used, appended with the section name, to find the rotors of the spinner.

All other sections describe a spinner. The section name is the name of the spinner.

- **Arms** How many arms the spinner has
- **DrillGroupName** The name of this spinner's drill group
- **PistonGroupName** The name of this spinner's piston group
- **RotorName** The name of this spinner's rotor

Example:
```ini
[general]
DrillGroupNamePrefix=Spinner drills
LcdInfo=MyLcdName
PistonGroupNamePrefix=Spinner pistons
PistonLowerSpeed=0.5
RotorNamePrefix=Spinner rotor

[Yellow]
Arms=2

[Maroon]
Arms=2
; The following drill group name overwrites the prefix search
DrillGroupName=Spinner drills Maroon
PistonGroupName=Spinner pistons Maroon
RotorName=Spinner rotor Maroon
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
