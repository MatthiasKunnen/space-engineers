# Launch control
This scripts allows you to escape gravity at minimal fuel consumption and
performs a turn and burn when escaped.

## Features
### Maintaining speed
Maintaining a speed at the exact thrust required, drastically reducing fuel
consumption compared to holding space. Thrust is spread over all engines.

### Automatic alignment
Automatically aligns your ships with the planet for the shortest escape and
minimal worry.

### Turn and burn
Executes a turn-and-burn manoeuvre when escaped in order to halt the ship. This
spares you the hassle of manually turning the ships without artificial horizon
and leaves your right over your original launch location.

### LCD
Displays allow passenger to see progress of the launch control sequence.

## Configuration
Certain factors of the launch control sequence are customizable using the custom
data feature on the programmable block.

### Custom data format
The custom data must be formatted in a certain way in order to be read out
correctly. The format is:
```
<variable>=<value>  
<variable2>=<value2>
```
The most important parts are:
* Each variable is on their own line
* The variable and value are separated by a single `=` and no spaces.

## Variables
#### Control block name
Name: _referenceBlockName_  
Default value: `Remote Control - Reference`

#### Thrust control group
Name: _thrustersGroupName_  
Default value: `Thrusters UP`

#### Target speed
Name: _targetSpeed_  
Default value: `100`

#### Target speed variation
Name: _targetSpeedVariation_  
Default value: `0.01`

The percentage of variation that exist between the target speed vs actual speed
before thrust is applied to correct it. This exists because the calculation of
thrust required vs actual thrust requires differs slightly for currently
unknown reasons.

#### LCD search name
Name: _lcdSearchName_  
Default value: `LCD Launch Control`

Multiple LCDs are supported.

#### Margin of error - thrust
Name: _marginOfErrorThrust_  
Default value: `1.01`

The multiplier to apply to the applied thrust. This exists because the
calculation of thrust required vs actual thrust requires differs slightly for
currently unknown reasons. If you notice frequent bursts of thrust to make up
for lost speed, increase this multiplier.

#### Gravity threshold
Name: _gravityTreshold_  
Default value: `0`

Specifies at how many m/s² the turn and burn will be initiated.
