using DobotClientDemo.CPlusDll;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Configuration;

//TODO vector of dwell times


namespace DobotConsoleControl
{
    public static class Dobot
    {
        private const bool Z_PLATFORM = true;


        // DEFAULT POINT NAMES
        public const string PICK = "PICK";
        public const string BUILD = "BUILD";
        public const string CHILL = "CHILL";
        private const string HOME = "HOME";
        public const string PRE_PLACE = "PRE_PLACE";
        public const string TRANSITION = "TRANSITION";

        public static double DwellTimeDefault { get; set; } = 2000;       //ms default
        public static List<double> DwellTimes { get; set; } = new List<double>();
        private static double DEFAULT_LAYER_HEIGHT { get; set; } = 1;     //mm default
        public static List<double> LayerHeights { get; set; } = new List<double>();
        public static double ShortPause { get; set; } = 200;    //ms default
        public static double SafeHeight { get; set; } = 90;     //mm default
        private static Dictionary<string, RobotPoint> points = new Dictionary<string, RobotPoint>();


        // book keeping of the robot
        private static bool isConnected = false;
        public static bool IsConnected
        {
            get
            {
                return isConnected;
            }
            private set
            {
                isConnected = value;
            }
        }


        private static ulong lastCmdNumber = 0;
        public static ulong LastCmdNumber
        {
            get
            {
                return lastCmdNumber;
            }
        }

        // speed settings
        //private static float velocityRatio = 100;
        //private static float accelerationRatio = 100;

        // speed setting constants
        public enum SPEED
        {
            Low = 30,
            Med = 60,
            High = 100
        };
        private static SPEED speed;
        
        //private const float LOW_MODE = 30;
        //private const float MED_MODE = 60;
        //private const float HIGH_MODE = 100;

        // keeps track of all the robot points
        

        /// <summary>
        /// Connects to the Dobot, performs pre-operation checks and setup.
        /// </summary>
        /// <returns></returns>
        public static int Start()
        {
            //points = FileIO.ReadPoints();
            // sets the clearance height at which the robot should move so it doesn't hit any objects
            //RobotPoint.SetSafeHeight(SafeHeight);

            StringBuilder fwType = new StringBuilder(60);
            StringBuilder version = new StringBuilder(60);

            int ret = DobotDll.ConnectDobot("", 115200, fwType, version);

            //WriteToConsole(ret.ToString());
            Thread.Sleep(1000);
            //start connect
            if (ret != (int)DobotConnect.DobotConnect_NoError)
            {
                Console.WriteLine("!!!!!!!!! Connection error !!!!!!!!!");
                return 1;
            }
            Console.WriteLine("Connected to Dobot");

            IsConnected = true;

            DobotDll.SetQueuedCmdClear();
            DobotDll.SetQueuedCmdStartExec();

            DobotDll.SetCmdTimeout(3000);

            //Must set when sensor does not exist       <-- from original code
            //DobotDll.ResetPose(true, 45, 45)          <-- from original code

            //Get Name
            string deviceName = "Dobot Magician";
            DobotDll.SetDeviceName(deviceName);

            StringBuilder deviceSN = new StringBuilder(64);
            DobotDll.GetDeviceSN(deviceSN, 64);

            DobotDll.SetQueuedCmdClear();

            // Sets the default speed to high
            SetParam(SPEED.High);

            CheckAlarms();
            return 0;
        }

        /// <summary>
        /// Sets default operating parameters for the Dobot
        /// </summary>
        public static void SetParam(SPEED _speed)
        {
            UInt64 cmdIndex = 0;
            JOGJointParams jsParam;
            jsParam.velocity = new float[] { 200, 200, 200, 200 };
            jsParam.acceleration = new float[] { 200, 200, 200, 200 };
            DobotDll.SetJOGJointParams(ref jsParam, false, ref cmdIndex);

            JOGCommonParams jdParam;
            jdParam.velocityRatio = 100;
            jdParam.accelerationRatio = 100;
            DobotDll.SetJOGCommonParams(ref jdParam, false, ref cmdIndex);

            PTPJointParams pbsParam;
            pbsParam.velocity = new float[] { 200, 200, 200, 200 };
            pbsParam.acceleration = new float[] { 200, 200, 200, 200 };
            DobotDll.SetPTPJointParams(ref pbsParam, false, ref cmdIndex);

            PTPCoordinateParams cpbsParam;
            cpbsParam.xyzVelocity = 100;
            cpbsParam.xyzAcceleration = 100;
            cpbsParam.rVelocity = 100;
            cpbsParam.rAcceleration = 100;
            DobotDll.SetPTPCoordinateParams(ref cpbsParam, false, ref cmdIndex);

            PTPJumpParams pjp;
            pjp.jumpHeight = 20;
            pjp.zLimit = 100;
            DobotDll.SetPTPJumpParams(ref pjp, false, ref cmdIndex);

            PTPCommonParams pbdParam;
            pbdParam.velocityRatio = (int) _speed;//velocityRatio;//30;
            pbdParam.accelerationRatio = (int) _speed;//accelerationRatio;//30;
            DobotDll.SetPTPCommonParams(ref pbdParam, false, ref cmdIndex);

            speed = _speed;
            PrgConsole.SpeedChange(speed);
        }

        public static bool CheckAlarms()
        {
            int ret;
            byte[] alarmsState = new byte[32];
            UInt32 len = 32;
            ret = DobotDll.GetAlarmsState(alarmsState, ref len, alarmsState.Length);

            List<string> alarmStrings = new List<string>();

            for (int i = 0; i < alarmsState.Length; i++)
            {
                byte alarm = alarmsState[i];
                for (int j = 0; j < 8; j++)
                {
                    if ((alarm & 0x01 << j) > 0)
                    {
                        int alarmIndex = i * 8 + j;


                        switch (alarmIndex)
                        {
                            case 0x00:
                                {
                                    alarmStrings.Add("Get Alarm status: reset");
                                    break;
                                }
                            case 0x01:
                                {
                                    alarmStrings.Add("Get Alarm status: undefined instruction");
                                    break;
                                }
                            case 0x02:
                                {
                                    alarmStrings.Add("Get Alarm status: file system error");
                                    break;
                                }
                            case 0x03:
                                {
                                    alarmStrings.Add("Get Alarm status: comm failure");
                                    break;
                                }
                            case 0x04:
                                {
                                    alarmStrings.Add("Get Alarm status: angle sensor read error");
                                    break;
                                }
                            case 0x11:
                                {
                                    alarmStrings.Add("Get Alarm status: inverse resolve alarm");
                                    break;
                                }
                            case 0x12:
                                {
                                    alarmStrings.Add("Get Alarm status: inverse resolve limit");
                                    break;
                                }
                            case 0x13:
                                {
                                    alarmStrings.Add("Get Alarm status: data repetition");
                                    break;
                                }
                            case 0x15:
                                {
                                    alarmStrings.Add("Get Alarm status: arc input parameter alarm");
                                    break;
                                }
                            case 0x21:
                                {
                                    alarmStrings.Add("Get Alarm status: inverse resolve alarm");
                                    break;
                                }
                            case 0x22:
                                {
                                    alarmStrings.Add("Get Alarm status: inverse resolve limit");
                                    break;
                                }
                            case 0x40:
                                {
                                    alarmStrings.Add("Get Alarm status: joint 1 positive limit alarm");
                                    break;
                                }
                            case 0x41:
                                {
                                    alarmStrings.Add("Get Alarm status: joint 1 negative limit alarm");
                                    break;
                                }
                            case 0x42:
                                {
                                    alarmStrings.Add("Get Alarm status: joint 2 positinve limit alarm");
                                    break;
                                }
                            case 0x43:
                                {
                                    alarmStrings.Add("Get Alarm status: joint 2 negative limit alarm");
                                    break;
                                }
                            case 0x44:
                                {
                                    alarmStrings.Add("Get Alarm status: joint 3 positive limit alarm");
                                    break;
                                }
                            case 0x45:
                                {
                                    alarmStrings.Add("Get Alarm status: joint 3 negative limit alarm");
                                    break;
                                }
                            case 0x46:
                                {
                                    alarmStrings.Add("Get Alarm status: joint 4 positive limit alarm");
                                    break;
                                }
                            case 0x47:
                                {
                                    alarmStrings.Add("Get Alarm status: joint 4 negative limit alarm");
                                    break;
                                }

                            case 0x48:
                                {
                                    alarmStrings.Add("Get Alarm status: parallegram positive limit alarm");
                                    break;
                                }
                            case 0x49:
                                {
                                    alarmStrings.Add("Get Alarm status: parallegram negative limit alarm");
                                    break;
                                }
                            default:
                                alarmStrings.Add("Unknown error code.");
                                break;
                        }
                    }
                }
            }

            PrgConsole.WriteAlarms(alarmStrings);
            return alarmStrings.Count>0;
        }

        public static void SetLayerHeights(List<double> _layerHeights)
        {
            LayerHeights = _layerHeights;
        }

        public static void SetDwellTimes(List<double> _dwellTimes)
        {
            DwellTimes = _dwellTimes;
        }

        /// <summary>
        /// Returns the point programmed as "home" in the Dobot firmware.
        /// </summary>
        /// <returns></returns>
        public static string GetHome()
        {
            HOMEParams hParams = new HOMEParams();
            DobotDll.GetHOMEParams(ref hParams);
            return hParams.ToString();
        }

        /// <summary>
        /// Clears any errors the Dobot may have stored.
        /// </summary>
        public static void ClearAlarms()
        {
            DobotDll.ClearAllAlarmsState();
            PrgConsole.WriteInfo("Alarms cleared");
        }

        public static int AddPoint(RobotPoint point)
        {
            if (point.Name == "")
            {
                PrgConsole.Error(PrgConsole.ERRORS.NONAME_POINT);
                return 1;
            }
            else
            {
                points[point.Name] = point;//points.Add(point.Name, point);
                return 0;
            }
        }

        public static void SeedPoints()
        {

            if (Z_PLATFORM)
            {
                RobotPoint pickPoint = new RobotPoint(PICK, 301, -41, -68.5, 27);
                RobotPoint transitionPoint = new RobotPoint(TRANSITION, 190, 96.4, 100.5, 27);
                RobotPoint buildPlace = new RobotPoint(BUILD, 203, 218.5, 56.7, 27);
                
                RobotPoint chillPoint = new RobotPoint(CHILL, 193.6, 27.4, 100.5, 45);
                RobotPoint homePoint = new RobotPoint(HOME, 250, 0, 0, 0);
                AddPoint(pickPoint);
                AddPoint(buildPlace);
                AddPoint(homePoint);
                AddPoint(chillPoint);
                AddPoint(transitionPoint);
            } else
            {// Original points for acrylic platform
                RobotPoint buildPlace = new RobotPoint(BUILD, 168.2096, 3.2643, -47.5, -10.6148);
                RobotPoint pickPoint = new RobotPoint(PICK, 167.1955, -147.1283, -58.5127, -10.6148);
                RobotPoint chillPoint = new RobotPoint(CHILL, 28.8, -191.653, 7.206, -81);
                RobotPoint homePoint = new RobotPoint(HOME, 250, 0, 0, 0);
                AddPoint(pickPoint);
                AddPoint(buildPlace);
                AddPoint(homePoint);
                AddPoint(chillPoint);
            }
            

            FileIO.SavePoints(points);
        }

        public static int StackOne(ref double currentStackHeight, ref int currentLayer)
        {
            double dwellTime = getDwellTime(currentLayer);
            double layerHeight = getLayerHeight(currentLayer);
            if (Z_PLATFORM)
            {
                ArduinoComm.Move(layerHeight);

                GoHighFromCurrent(points[PICK]);//pickPoint);
                Vac(true);
                Wait(ShortPause);
                
                if (true)
                {
                    RobotPoint pickClear = (RobotPoint)points[PICK].Clone();
                    pickClear.Z += 20;
                    GoPoint(pickClear);
                    GoPoint(points[TRANSITION]);

                    RobotPoint build = (RobotPoint)points[BUILD].Clone();
                    build.R = points[PICK].R;                               //makes sure rotation is same as pick since it's hard to keep that same when manually defining points
                    RobotPoint placeClear = (RobotPoint) build.Clone();
                    placeClear.Z = points[TRANSITION].Z;
                    GoPoint(placeClear);
                    GoPoint(build);//points[BUILD]);
                    Wait(dwellTime);
                    GoPoint(placeClear);
                    GoPoint(points[TRANSITION]);
                    GoPoint(pickClear);
                    Vac(false);
                    GoPoint(points[CHILL]);
                }
                else
                {

                    GoPoint(points[PICK].HighPoint());
                    GoPoint(points[TRANSITION].HighPoint());
                    GoPoint(points[PRE_PLACE].HighPoint());
                    GoHigh(points[PRE_PLACE].HighPoint(), points[BUILD]);
                    //GoHigh(points[PICK], stackPoint);//pickPoint, stackPoint);

                    //double dwellTime = getDwellTime(currentLayer);
                    Wait(dwellTime);


                    GoHigh(points[BUILD], points[PRE_PLACE].HighPoint());


                    //double layerHeight = getLayerHeight(currentLayer);
                    //ArduinoComm.Move(layerHeight);

                    RobotPoint depositPoint = (RobotPoint)points[PICK].Clone();
                    depositPoint.Z += 10;

                    GoHigh(points[PRE_PLACE].HighPoint(), depositPoint);
                    Wait(ShortPause);
                    Vac(false);
                    GoHigh(points[PICK], points[CHILL]);
                }
            }
            else
            {
                GoHighFromCurrent(points[PICK]);//pickPoint);
                Vac(true);
                Wait(ShortPause);
                RobotPoint stackPoint = (RobotPoint)points[BUILD].Clone();//placePoint.Clone();
                stackPoint.Z += currentStackHeight + layerHeight; //stackHeight = stackPoint.Z + layerCount * LayerHeight;
                                                                  //stackPoint.Z = stackHeight;
                GoHigh(points[PICK], stackPoint);//pickPoint, stackPoint);


                if (DwellTimes.Count > 1)
                {
                    if (currentLayer < DwellTimes.Count)
                        Wait(DwellTimes[currentLayer]);
                    else
                        Wait(DwellTimes[DwellTimes.Count - 1]);         //if you have exceeded the defined dwell times just repeat the last defined one indefinitely
                }
                else
                    Wait(DwellTimes[0]); // Wait(DwellTime);

                RobotPoint smoothEntryPoint = (RobotPoint)points[PICK].Clone();//pickPoint.Clone();
                smoothEntryPoint.X += 5;
                smoothEntryPoint.Z += 5;

                GoHigh(stackPoint, smoothEntryPoint);
                Wait(200);

                //RobotPoint smallPickHover = (RobotPoint)pickPoint.Clone();
                //smoothEntryPoint.Z += 7;

                //hqGoPoint(smallPickHover);
                //hqGoPoint(pickPoint);

                //hqGoHigh(stackPoint, pickPoint);
                Wait(ShortPause);
                Vac(false);
                Wait(200);
                GoHigh(points[PICK], points[CHILL]);//pickPoint, chillPoint);
                
                //state = PRGSTATE.RUN;
                //Console.Write("\n");
            }

            currentLayer++;//layerCount++;
            currentStackHeight = currentStackHeight + layerHeight;

            return 0;
        }

        private static double getDwellTime(int currentLayer)
        {
            double dwellTime = DwellTimeDefault;
            if (DwellTimes.Count > 1)
            {
                if (currentLayer < DwellTimes.Count)
                    dwellTime = DwellTimes[currentLayer];
                else
                    dwellTime = DwellTimes[DwellTimes.Count - 1];         //if you have exceeded the defined dwell times just repeat the last defined one indefinitely
            }
            else
                dwellTime = DwellTimes[0]; // Wait(DwellTime);

            return dwellTime;
        }

        private static double getLayerHeight(int currentLayer)
        {
            double layerHeight = DEFAULT_LAYER_HEIGHT;
            if (LayerHeights.Count > 1)
            {
                if (currentLayer < LayerHeights.Count)
                    layerHeight = LayerHeights[currentLayer];
                else
                    layerHeight = LayerHeights[LayerHeights.Count - 1];         //if you have exceeded the defined dwell times just repeat the last defined one indefinitely
            }
            else
                layerHeight = LayerHeights[0]; // Wait(DwellTime);

            return layerHeight;
        }

        private static RobotPoint getCurrentPosition()
        {
            Pose pose = new Pose();
            DobotDll.GetPose(ref pose);

            return new RobotPoint(pose);
        }

        public static void ChangePickZ(double newZ)
        {
            points[PICK].Z += newZ;
        }

        public static void ChangeBuildZ(double newZ)
        {
            points[BUILD].Z += newZ;
        }


        #region queuedCommands


        /// <summary>
        /// Controls the vacuum suction on the end effector.
        /// </summary>
        /// <param name="on"></param>
        public static void Vac(bool on)
        {
            DobotDll.SetEndEffectorSuctionCup(true, on, true, ref lastCmdNumber);
            PrgConsole.AddedToQueueVac(on);//PrgConsole.WriteToConsole($"added to queue: switch VACUUM {on}", 1);
        }

        /// <summary>
        /// Dobot wait command for hardware queue execution.
        /// </summary>
        /// <param name="ms"></param>
        public static void Wait(double ms)
        {
            WAITCmd waitCmd;
            waitCmd.timeout = (UInt32)ms;
            DobotDll.SetWAITCmd(ref waitCmd, true, ref lastCmdNumber);
            PrgConsole.AddedToQueueWait(ms);
        }

        public static void Home()
        {
            //SetParam(SPEED.Low);
            HOMEParams hParams;
            hParams.x = (float)points[HOME].X;//homePoint.X;
            hParams.y = (float)points[HOME].Y;//homePoint.Y;
            hParams.z = (float)points[HOME].Z;//homePoint.Z;
            hParams.r = (float)points[HOME].R;//homePoint.R;
            DobotDll.SetHOMEParams(ref hParams, true, ref lastCmdNumber);
            //DobotDll.GetPose(ref pose);

            // code to make sure head of the robot is not on something before homing
            RobotPoint elevatedPosition = getCurrentPosition();//new RobotPoint(pose.x, pose.y, pose.z, pose.rHead);
            elevatedPosition.Z += 25;
            GoPoint(elevatedPosition);
            Thread.Sleep(1000);
            DobotDll.SetQueuedCmdStopExec();
            DobotDll.SetQueuedCmdClear();
            DobotDll.SetQueuedCmdStartExec();
            Thread.Sleep(1000);
            HOMECmd hCmd;
            hCmd.temp = 1;
            DobotDll.SetHOMECmd(ref hCmd, false, ref lastCmdNumber);
            
        }
        
        /// <summary>
        /// Adds motions to the hardware queue
        /// Assuming the dobot starts at point1, it goes the clear point of point1 then over to the clear point of point 2 and finally down to point 2.
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        private static void GoHigh(RobotPoint point1, RobotPoint point2)
        {
            //WriteToConsole($"point2 start {point2.Z}");
            GoPoint(point1.HighPoint());
            GoPoint(point2.HighPoint());
            //WriteToConsole($"point2 end {point2.Z}");
            //RobotPoint pt2 = (RobotPoint) point2.Clone();
            //pt2.Z = clearHeight;
            //goPoint(pt2);
            GoPoint(point2);
        }

        /// <summary>
        /// Goes to the supplied RobotPoint first by going up to a safe height over with the same elevation and then finally down to the point.
        /// Supplies motion profile to the hardware queue for execution.
        /// </summary>
        /// <param name="point"></param>
        private static void GoHighFromCurrent(RobotPoint point)
        {
            //Pose pose = new Pose();
            //DobotDll.GetPose(ref pose);

            RobotPoint currentPosition = getCurrentPosition();//new RobotPoint(pose);
            
            GoPoint(currentPosition.HighPoint());
            GoPoint(point.HighPoint());
            GoPoint(point);
        }

        /// <summary>
        /// Public method to go to the input point safely cleared
        /// </summary>
        /// <param name="point"></param>
        public static void GoHighFromCurrent(string point)
        {
            GoHighFromCurrent(points[point]);
        }

        /// <summary>
        /// Provide public method to go the clear point from the current position.
        /// </summary>
        /// <param name="point"></param>
        public static void GoHoverFromCurrent(string point)
        {
            GoPoint(points[point].HighPoint());
        }

        /// <summary>
        /// Adds point to the hardware queue using standard PTPMOVLXYZMode
        /// </summary>
        /// <param name="point"></param>
        private static void GoPoint(RobotPoint point)
        {
            hardQPTP(PTPMode.PTPMOVLXYZMode, point);//point.X, point.Y, point.Z, point.R);
        }

        /// <summary>
        /// Provides a public method to go to points using the console.
        /// </summary>
        /// <param name="point"></param>
        public static void GoPoint(string point)
        {
            GoPoint(points[point]);
        }

        /// <summary>
        /// Adds the input to the x variable of the current position and adds to the queue.
        /// </summary>
        /// <param name="x"></param>
        public static void GoXInc(double x)
        {
            RobotPoint currentPostion = getCurrentPosition();

            currentPostion.X += x;
            GoPoint(currentPostion);
        }

        /// <summary>
        /// Adds the input to the y variable of the current position and adds to the queue.
        /// </summary>
        /// <param name="y"></param>
        public static void GoYInc(double y)
        {
            RobotPoint currentPostion = getCurrentPosition();

            currentPostion.Y += y;
            GoPoint(currentPostion);
        }

        /// <summary>
        /// Adds the input to the z variable of the current position and adds to the queue.
        /// </summary>
        /// <param name="z"></param>
        public static void GoZInc(double z)
        {
            RobotPoint currentPostion = getCurrentPosition();

            currentPostion.Z += z;
            GoPoint(currentPostion);
        }

        /// <summary>
        /// Adds the input to the r variable of the current position and adds to the queue.
        /// </summary>
        /// <param name="r"></param>
        public static void GoRInc(double r)
        {
            RobotPoint currentPostion = getCurrentPosition();

            currentPostion.R += r;
            GoPoint(currentPostion);
        }

        /// <summary>
        /// Gets the current position and saves it to the points dictionary.
        /// </summary>
        /// <param name="name"></param>
        public static void StoreCurrentPoint(string name)
        {
            RobotPoint currentPosition = getCurrentPosition();
            currentPosition.Name = name;
            points[name] = currentPosition;
            //points.Add(name, currentPosition);
            PrgConsole.PointSaved(currentPosition);
        }

        public static void DisplayCurrentPoint()
        {
            RobotPoint currentPosition = getCurrentPosition();
            PrgConsole.WriteInfo(currentPosition.ToString());
        }

        /// <summary>
        /// Adds a goto point to the hardware queue
        /// </summary>
        /// <param name="style"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        private static UInt64 hardQPTP(PTPMode style, RobotPoint point)//, double x, double y, double z, double r)
        {
            PTPCmd pdbCmd = point.PdbCmd(style);

            // communication thing, keeps trying to tell it to do a command until it takes
            //TODO add a max tries condition
            while (true)
            {
                int ret = DobotDll.SetPTPCmd(ref pdbCmd, true, ref lastCmdNumber);
                if (ret == 0)
                    break;
            }

            PrgConsole.AddedToQueue(point);
            return lastCmdNumber;
        }

        #endregion

        public static List<RobotPoint> GetPoints()
        {
            return new List<RobotPoint>(points.Values);
        }

        public static void SavePointsToFile(string name)
        {
            FileIO.SavePoints(name, points);
        }

        public static void SavePointsToFile()
        {
            FileIO.SavePoints(points);
        }

        public static int StartQueue()
        {
            DobotDll.SetQueuedCmdStartExec();

            return 0;
        }

        public static int ClearQueue()
        {
            DobotDll.SetQueuedCmdClear();

            return 0;
        }


        /// <summary>
        /// Disconnects the serial communication with the Dobot
        /// </summary>
        public static void Disconnect()
        {
            DobotDll.DisconnectDobot();
        }
    }

    [Serializable]
    [DataContract]
    public class RobotPoint : ICloneable
    {
        //[NonSerialized]
        public static double SafeHeight { get; set; } = 999; //{ get; set; }
        [NonSerialized]
        private static bool safeSet = false;
        public static bool IsSafeSet
        {
            get
            {
                return safeSet;
            }
            private set
            {
                safeSet = value;
            }
        }

        public static void SetSafeHeight(double _safeHeight)
        {
            SafeHeight = _safeHeight;
            IsSafeSet = true;
        }

        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public double X { get; set; }
        [DataMember]
        public double Y { get; set; }
        [DataMember]
        public double Z { get; set; }
        [DataMember]
        public double R { get; set; }

        public RobotPoint()
        {

        }

        public RobotPoint(Pose pose) : this(pose.x, pose.y, pose.z, pose.rHead) { }

        public RobotPoint(double x, double y, double z, double r) : this("", x, y, z, r) { }

        public RobotPoint(string _name, double x, double y, double z, double r)
        {
            Name = _name;
            X = x;
            Y = y;
            Z = z;
            R = r;
        }

        public object Clone()
        {
            return new RobotPoint(Name, X, Y, Z, R);
        }


        public RobotPoint HighPoint()
        {
            if (safeSet == false)
            {
                throw new Exception("SafeHeight not set in RobotPoint object");
            }
            else
            {
                RobotPoint newPoint = (RobotPoint)this.Clone();
                newPoint.Z = SafeHeight;
                return newPoint;
            }
        }

        public PTPCmd PdbCmd(PTPMode style)
        {
            PTPCmd pdbCmd;
            pdbCmd.ptpMode = (byte)style;
            pdbCmd.x = (float)X;
            pdbCmd.y = (float)Y;
            pdbCmd.z = (float)Z;
            pdbCmd.rHead = (float)R;

            return pdbCmd;
        }

        private const string FORMAT_STRING = "{0:000.000}";

        public override string ToString()
        {
            return $"\tpoint:\tname= {string.Format(FORMAT_STRING,Name)}\tx= {string.Format(FORMAT_STRING, X)}\ty= {string.Format(FORMAT_STRING, Y)}\tz= {string.Format(FORMAT_STRING, Z)}\tr= {string.Format(FORMAT_STRING, R)}";
        }
    }

}
