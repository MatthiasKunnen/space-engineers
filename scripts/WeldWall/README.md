Weld Wall
================

This scripts handles welding a blueprint using a weld wall. 

# How it works
The script combines precalculated component requirements and on-the-fly production to quickly build
any blueprint.

It: 
1. Checks how much components are required to build the blueprint  
   To do this, it uses a disconnected assembler and precalculated requirements using
   <https://se-blueprint-requirements.herokuapp.com>
1. Queues any required components
1. Checks periodically if the necessary components are available
1. Turns on welders
1. Extends/Retracts pistons at configurable velocity
1. Runs optional timer blocks at certain events to allow for extensive configuration

## Adding a blueprint
Load the blueprint into the designated projector. The blueprint LCD should display the id of the
blueprint. Edit the LCD and copy the ID to the custom data of the programmable block. The id should
become the section name. Go to <https://se-blueprint-requirements.herokuapp.com> (might take a few
seconds to load). Locate your blueprint file on your computer. It should be located under
`%appdata%\SpaceEngineers\Blueprints`. Upload the .sbc file of the blueprint and copy the generated
information. Add this information to the custom data. Example:

```ini
[general]
PistonGroup=WeldWallPistons

[ID_HERE]
Name=My cool ship
MyObjectBuilder_CubeBlock/LargeBlockArmorSlope=32
MyObjectBuilder_CubeBlock/LargeBlockArmorBlock=31
```

Now run the programmable block with the `PARSE` argument. The blueprint info LCD should now display
the name of the blueprint. You can now edit the projector offset to align the blueprint as you see
fit. The offset can be saved by running the `SAVE_OFFSET` argument. This information will be added
to the custom data of the programmable block.

## Start welding

1. Make sure a blueprint is loaded and the blueprint info LCD reports the correct blueprint name
1. Run the `PREPARE` command. This will make the script queue any necessary components
1. When the script detects it has all the necessary components, it will either run the
   WeldReadyTimer block or start welding if the timer block does not exist

## Retract/Recall projector

Run the `RETRACT` command to recall the projector by compressing the pistons.

# Config/Custom data
The config format is that of an INI file which uses `key=value` lines. No spaces should exists
around the `=`.

| Key                             | Default                     | Value                                                                                                                                                                                |
|---------------------------------|-----------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| CalculatorAssembler             | WeldWallCalculatorAssembler | The name of the assembler used in calculations. This assembler should not produce anything. Disconnect it                                                                            |
| LcdBlueprintInfo                | WeldWallBlueprintInfoLcd    | The name of the weld wall projector                                                                                                                                                  |
| LcdComponentStatus              | WeldWallComponentStatus     | The name of the weld wall projector                                                                                                                                                  |
| PistonGroup                     | WeldWallPistons             | The name of the group containing the weld wall pistons                                                                                                                               |
| ProductionAssembler             | WeldWallProductionAssembler | The name of the assembler on which missing components will be queued                                                                                                                 |
| Projector                       | WeldWallProjector           | The name of the weld wall projector                                                                                                                                                  |
| RetractVelocity                 | 2                           | The velocity of retracting the projector in m/s                                                                                                                                      |
| WeldEndedTimer                  | WeldWallEndTimer            | The name of the optional timer block that will be run when welding ended                                                                                                             |
| WeldReadyTimer                  | WeldWallReadyTimer          | The name of the optional timer block that will be run when welding can start. Execute the WELD argument to start welding                                                             |
| WeldVelocity                    | 0.1                         | The velocity of the welding in m/s                                                                                                                                                   |
| WelderGroup                     | WeldWallWelders             | The name of the group containing the weld wall welders                                                                                                                               |
