Weld Wall
================

This scripts handles welding a blueprint using a weld wall.

# How it works
The script combines precalculated component requirements and on-the-fly production to quickly build
any blueprint.

It:
1. Checks how much components are required to build the blueprint  
   To do this, it uses the
   [Projections To Assembler mod](https://steamcommunity.com/sharedfiles/filedetails/?id=1289485324)
1. Queues any required components
1. Checks periodically if the necessary components are available
1. Turns on welders
1. Extends/Retracts pistons at configurable velocity
1. Runs optional timer blocks at certain events to allow for extensive configuration

## Adding a blueprint
Load the blueprint into the designated projector. The blueprint LCD should display the id of the
blueprint. Edit the LCD and copy the ID to the custom data of the programmable block. The id should
become the section name. Example:

```ini
[general]
PistonGroup=WeldWallPistons

[ID_HERE]
Name=My cool ship
```

Now run the programmable block with the `PARSE` argument. The blueprint info LCD should now display
the name of the blueprint. You can now edit the projector offset to align the blueprint as you see
fit. The offset can be saved by running the `SAVE_OFFSET` argument. This information will be added
to the custom data of the programmable block.

## Start welding

1. Make sure a blueprint is loaded and the blueprint info LCD reports the correct blueprint name
1. Run the `PREPARE` command. This will make the script queue any necessary components
1. When the script detects it has all the necessary components, it will either run the
   WeldReadyTimer block or start welding if the timer block does not exist. The WeldReadyTimer
   should run the `WELD` command to continue.
1. When the pistons are fully extended, the welders will turn off, and the WeldEndedTimer will be
   run if one exists.

## Retract/Recall projector

Run the `RETRACT` command to recall the projector by compressing the pistons.

## Active projector
Multiple projectors are supported. The projectors are set up using the `Projectors` key. Each
projector is defined by a friendly name and the name of the block separated by a colon. Example:

```
Projectors=Large:WeldWallLargeProjector Small:WeldWallSmallProjector Fighter:WeldWallFighterProjector
```

Set the active projector by running `SET_ACTIVE Small`. By default the first projector in the list
will be active.

# Config/Custom data
The config format is that of an INI file which uses `key=value` lines. No spaces should exists
around the `=`.

The following keys can be configured in the `general` section.

| Key                             | Default                       | Value                                                                                                                                                                                |
|---------------------------------|-------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| CalculatorAssembler             | WeldWallCalculatorAssembler   | The name of the assembler used in calculations. This assembler should not produce anything. Disconnect it                                                                            |
| LcdBlueprintInfo                | WeldWallBlueprintInfoLcd      | The name of the LCD displaying the blueprint info                                                                                                                                    |
| LcdComponentStatus              | WeldWallComponentStatus [LCD] | The name of the LCD displaying the status of components. Powered by [Automatic LCDs 2](https://steamcommunity.com/sharedfiles/filedetails/?id=822950976)                             |
| PistonGroup                     | WeldWallPistons               | The name of the group containing the weld wall pistons                                                                                                                               |
| ProductionAssembler             | WeldWallProductionAssembler   | The name of the assembler on which missing components will be queued                                                                                                                 |
| Projectors                      | Large:WeldWallProjectorLarge  | The list of projectors in ShortName:BlockName format (See Active Projector section)                                                                                                  |
| RetractVelocity                 | 4                             | The velocity of retracting the projector in m/s                                                                                                                                      |
| WeldEndedTimer                  | WeldWallEndTimer              | The name of the optional timer block that will be run when welding ended                                                                                                             |
| WeldReadyTimer                  | WeldWallReadyTimer            | The name of the optional timer block that will be run when welding can start. Execute the WELD argument to start welding                                                             |
| WeldVelocity                    | 1                             | The velocity of the welding in m/s                                                                                                                                                   |
| WelderGroup                     | WeldWallWelders               | The name of the group containing the weld wall welders                                                                                                                               |

The following keys can be configured for each blueprint

| Key                             | Default                     | Value                                                                                                                                                                                |
|---------------------------------|-----------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Name                            |                             | The name of the blueprint                                                                                                                                                            |
| ProjectionOffsetHorizontal      | 0                           | Projection offset across the x axis                                                                                                                                                  |
| ProjectionOffsetVertical        | 0                           | Projection offset across the y axis                                                                                                                                                  |
| ProjectionOffsetForward         | 0                           | Projection offset across the z axis                                                                                                                                                  |
| ProjectionPitch                 | 0                           | Projection pitch in degrees                                                                                                                                                          |
| ProjectionYaw                   | 0                           | Projection yaw in degrees                                                                                                                                                            |
| ProjectionRoll                  | 0                           | Projection roll in degrees                                                                                                                                                           |
