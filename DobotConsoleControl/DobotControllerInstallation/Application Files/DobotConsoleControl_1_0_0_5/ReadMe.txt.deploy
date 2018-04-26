*************************************************************************************
ClearAlarms()
CheckAlarms()
ClearQueue()
StartQueue()
Reconnect()
StackOne()
Vac(on|off)
SetMode(low|med|high)



--- these should be the only commands you really need ---
Help()
Home()

SetLayerHeight(millimeters) or SetLayerHeight(mm, mm, ... mm) if you stack beyond defined it will repeat the last defined
SetDwell(milliseconds) or SetDwell(ms, ms, ... ms) if you stack beyond defined, it will repeat the last defined
SetSafeHeight(height)		safe clearance height for robot
SetArduinoHome(offset)		offset from limit switch for z platform to home to

AMove(z)								move z platform
Go(x|y|z|r, number)						incremental movement along an axis
Go(direct|high|hover, named_point)		absolute movement to pre-defined points

ReadCurrentPoint()
StoreCurrentPoint(name)

ChangePickZ(millimeters)	easy tuning of pick z height
ChangeBuildZ(millimeters)	easy tuning of build z height

ResetLayers()
StackOne()

SaveConfig(name)
LoadConfig(name)
ListConfig

SavePoints(name)
LoadPoints(name)
ListPoints

points names:
	PICK
	PLACE
	TRANSITION
	CHILL

*************************************************************************************


If you want to change any defined points:
	press and hold the button on the robot arm to release the stepper motors
	move the robot arm into place
	release the button to re-engage stepper motors
	make small variations in the robot arm placement using:
		Go(x,00.000) or Go(y, 0.0000) or Go(z, 0.00) or Go(r, 00.000)
	Save the point to RAM:
		StoreCurrentPoint(PointName)
	Save the points to the xml file:
		SavePoints(filename) or SavePoints() will save to default file

Check on point positions by:
	Go(high, PointName)
