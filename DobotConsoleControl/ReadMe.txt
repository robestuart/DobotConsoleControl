*************************************************************************************
ClearAlarms()
CheckAlarms()
ClearQueue()
StartQueue()
Reconnect()
StackOne()
Vac(on|off)
SetMode(low|med|high)

Go(x|y|z|r, number)
Go(direct|high|hover, named_point)

ReadCurrentPoint()
StoreCurrentPoint(name)

SavePoints(fileName)
LoadPoints(fileName)

SaveConfig(fileName)
LoadConfig(fileName)

--- these should be the only commands you really need ---
Help()
Home()
SetLayerHeight(millimeters) or SetLayerHeight(mm, mm, ... mm) if you stack beyond defined it will repeat the last defined
SetDwell(milliseconds) or SetDwell(ms, ms, ... ms) if you stack beyond defined, it will repeat the last defined
ChangePickZ(millimeters)
ChangeBuildZ(millimeters)
ResetLayers()
StackOne()

*************************************************************************************


If you want to change any defined points:
	press and hold the button on the robot arm to release the stepper motors
	move the robot arm into place
	release the button to re-engage stepper motors
	make small variations in the robot arm placement using:
		Go(x,00.000) or Go(y, 0.0000) or Go(z, 0.00) or Go(r, 00.000)
	Save the point to RAM:
		SaveCurrentPoint(PointName)
	Save the points to the xml file:
		SavePoints(filename) or SavePoints() will save them

Check on point positions by:
	Go(high, PointName)

List saved points:
	ListPoints()

SaveConfig
LoadConfig

SavePoints
LoadPoints

PICK
PLACE
TRANSITION
CHILL
