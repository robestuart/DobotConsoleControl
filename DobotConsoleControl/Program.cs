using DobotClientDemo.CPlusDll;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace DobotConsoleControl
{
    // Some code was used from this website:
    // https://www.codeproject.com/Articles/816301/Csharp-Building-a-Useful-Extensible-NET-Console-Ap

    class Program
    {
        private static RobotPoint pickPoint = new RobotPoint(228.7848, -96.1451, -20, -22.7647);
        private static RobotPoint placePoint = new RobotPoint(225.6651, 118.4177, -20, 27.6882);
        private static RobotPoint chillPoint = new RobotPoint(247.658, 61.7616, 50, 14);
        private static RobotPoint homePoint = new RobotPoint(250, 0, 0, 0);
        private static bool isConnected = false;
        private System.Timers.Timer posTimer = new System.Timers.Timer();
        private static Pose pose = new Pose();
        private static ulong lastCmdNumber = 0;
        private static double clearHeight = 50;
        private static bool waitToContinue = false;
        private enum PRGSTATE { INPUT, RUN, CONTINUE };
        private static PRGSTATE state = PRGSTATE.INPUT;
        private static double stackHeight = 0;
        private static int layerCount = 0;
        private static double dwellTime = 1000;
        private static double layerHeight = 1;
        private static double shortPause = 200;
        private static float velocityRatio = 30;
        private static float accelerationRatio = 30;

        private static float lowMode = 30;
        private static float medMode = 60;
        private static float highMode = 100;



        static void Main(string[] args)
        {
            WriteToConsole("WELCOME TO THE DOBOT HELPER\n");
            WriteToConsole("Here are some helpful commands:\n");
            WriteToConsole("Syntax is as follows: command(parameter1,parameter2)");
            WriteToConsole("Here are some available commands: ");
            WriteToConsole("setdwell\tsetlayerheight\tstackone");
            //WriteToConsole("home\tstop\tgetpose\tgeterror\n");

            StartDobot();
            Run();
            DobotDll.DisconnectDobot();
        }

        static void Run()
        {
            ulong currentCmdIndex = 0;
            while (true)
            {
                Thread.Sleep(200);
                switch (state)
                {
                    case PRGSTATE.RUN:
                        ulong tmpIndex = 0;
                        DobotDll.GetQueuedCmdCurrentIndex(ref tmpIndex);
                        if (tmpIndex != currentCmdIndex)
                        {
                            currentCmdIndex = tmpIndex;
                            Console.Write($"\rQueue command index: {currentCmdIndex} out of {lastCmdNumber}");

                            if (currentCmdIndex == lastCmdNumber)
                            {
                                Console.Write("\rQueue finished execution\n");
                                WriteToConsole($"Currently at\theight:  {stackHeight}\tlayer#:  {layerCount}");
                                state = PRGSTATE.INPUT;
                            }
                        }
                        break;

                    case PRGSTATE.CONTINUE:
                        
                        break;

                    case PRGSTATE.INPUT:
                    //check dobot cmd index

                        var consoleInput = ReadFromConsole();
                        if (string.IsNullOrWhiteSpace(consoleInput) && !waitToContinue) continue;


                        try
                        {
                            // Execute the command:
                            string result = Execute(consoleInput);

                            // Write out the result:
                            WriteToConsole(result);
                        }
                        catch (Exception ex)
                        {
                            // OOPS! Something went wrong - Write out the problem:
                            WriteToConsole(ex.Message);
                        }
                        break;
                }
            }
        }




        static string Execute(string command)
        {
            char[] delimeters = { '(', ',', ')' };
            string[] commands = command.Split(delimeters);

            /*
            foreach (string c in commands)
            {
                WriteToConsole(c);
            }*/

            if (waitToContinue && string.IsNullOrWhiteSpace(command))
                return null;
            
            switch (commands[0])
            {
                case "stackone":
                    stackOne();
                    break;

                case "runprog":
                    runProg();
                    break;

                case "vac":
                    if (commands[1].Equals("on"))
                        vac(true);
                    else if (commands[1].Equals("off"))
                        vac(false);
                    else
                        invalidCommand();
                break;

                case "setmode":
                    switch (commands[1])
                    {
                        case "low":
                            velocityRatio = lowMode;
                            accelerationRatio = lowMode;
                            SetParam();
                            WriteToConsole("Speed set to low");
                            break;

                        case "med":
                            velocityRatio = medMode;
                            accelerationRatio = medMode;
                            SetParam();
                            WriteToConsole("Speed set to med");
                            break;

                        case "high":
                            velocityRatio = highMode;
                            accelerationRatio = highMode;
                            SetParam();
                            WriteToConsole("Speed set to high");
                            break;
                    }
                    break;
                case "go":
                    if (commands[1].Equals("pick"))
                        goPoint(pickPoint, PTPMode.PTPMOVLXYZMode);
                    else if (commands[1].Equals("place"))
                        goPoint(placePoint, PTPMode.PTPMOVLXYZMode);
                        break;
                case "setlayerheight":
                    layerHeight = Double.Parse(commands[1]);
                    break;

                case "setdwell":
                    dwellTime = Double.Parse(commands[1]);

                    WriteToConsole($"New dwell time: {dwellTime}");
                    break;

                case "setpoint":
                    if (commands[1].Equals("pick"))
                        changePoint(ref pickPoint, Double.Parse(commands[2]), Double.Parse(commands[3]), Double.Parse(commands[4]), Double.Parse(commands[5]));
                    else if (commands[1].Equals("place"))
                        changePoint(ref placePoint, Double.Parse(commands[2]), Double.Parse(commands[3]), Double.Parse(commands[4]), Double.Parse(commands[5]));
                    else if (commands[1].Equals("home"))
                        changePoint(ref homePoint, Double.Parse(commands[2]), Double.Parse(commands[3]), Double.Parse(commands[4]), Double.Parse(commands[5]));
                    break;

                case "home":
                    HOMEParams hParams;
                    hParams.x = (float) homePoint.X;
                    hParams.y = (float)homePoint.Y;
                    hParams.z = (float)homePoint.Z;
                    hParams.r = (float)homePoint.R;
                    DobotDll.SetHOMEParams(ref hParams, false, ref lastCmdNumber);

                    break;

                case "gethome":
                    WriteToConsole(getHome());
                    break;
                default:
                    invalidCommand();
                    break;
            }
            
            return string.Format("Executed the {0} Command", command);
        }

        private static void invalidCommand()
        {
            WriteToConsole("Invalid Command Entered");
        }

        public static void WriteToConsole(string message = "")
        {
            if (message.Length > 0)
            {
                Console.WriteLine(message);
            }
        }

        const string _readPrompt = "console> ";
        public static string ReadFromConsole(string promptMessage = "")
        {
            // Show a prompt, and get input:
            Console.Write(_readPrompt + promptMessage);
            return Console.ReadLine();
        }

        private static void changePoint(ref RobotPoint point, double x, double y, double z, double r)
        {
            point = new RobotPoint(x, y, z, r);
        }

#region DobotStuff
        private static void StartDobot()
        {
            StringBuilder fwType = new StringBuilder(60);
            StringBuilder version = new StringBuilder(60);

            int ret = DobotDll.ConnectDobot("", 115200, fwType, version);

            //WriteToConsole(ret.ToString());
            Thread.Sleep(1000);
            //start connect
            if (ret != (int)DobotConnect.DobotConnect_NoError)
            {
                Console.WriteLine("Connect error");
                return;
            }
            Console.WriteLine("Connected to Dobot");

            isConnected = true;
            DobotDll.SetCmdTimeout(3000);

            //Must set when sensor does not exist       <-- from original code
            //DobotDll.ResetPose(true, 45, 45)          <-- from original code

            //Get Name
            string deviceName = "Dobot Magician";
            DobotDll.SetDeviceName(deviceName);

            StringBuilder deviceSN = new StringBuilder(64);
            DobotDll.GetDeviceSN(deviceSN, 64);

            DobotDll.SetQueuedCmdClear();

            SetParam();

            AlarmTest();
        }

        private static void vac(bool on)
        {
            DobotDll.SetEndEffectorSuctionCup(true, on, true, ref lastCmdNumber);
            WriteToConsole($"added to queue: switch VACUUM {on}");
        }

        private static string getHome()
        {
            HOMEParams hParams = new HOMEParams();
            DobotDll.GetHOMEParams(ref hParams);
            return hParams.ToString();
        }

        private static void stackOne()
        {
            goHighFromCurrent(pickPoint);
            vac(true);
            wait(shortPause);
            RobotPoint stackPoint = (RobotPoint)placePoint.Clone();
            stackHeight = stackPoint.Z + layerCount * layerHeight;
            stackPoint.Z = stackHeight;
            goHigh(pickPoint, stackPoint);
            wait(dwellTime);
            goHigh(stackPoint, pickPoint);
            wait(shortPause);
            vac(false);
            goHigh(pickPoint, chillPoint);
            layerCount++;
            state = PRGSTATE.RUN;
            Console.Write("\n");
        }

        private static void runProg()
        {
            goHighFromCurrent(chillPoint);
            goHigh(chillPoint, pickPoint);
            vac(true);
            goHigh(pickPoint, placePoint);
            goHigh(placePoint, pickPoint);
            vac(false);
            state = PRGSTATE.RUN;
        }

        private static RobotPoint highPoint(RobotPoint point)
        {
            RobotPoint newPoint = (RobotPoint) point.Clone();
            newPoint.Z = clearHeight;
            return newPoint;
        }

        private static void goHighFromCurrent(RobotPoint point)
        {
            DobotDll.GetPose(ref pose);

            RobotPoint currentPosition = new RobotPoint(pose.x, pose.y, pose.y, pose.rHead);
            goPoint(highPoint(currentPosition));
            goPoint(highPoint(point));
            goPoint(point);
        }

        private static void goHigh(RobotPoint point1, RobotPoint point2)
        {
            //WriteToConsole($"point2 start {point2.Z}");
            goPoint(highPoint(point1));
            goPoint(highPoint(point2));
            //WriteToConsole($"point2 end {point2.Z}");
            goPoint(point2);
        }

        private static void wait(double ms)
        {
            WAITCmd waitCmd;
            waitCmd.timeout = (UInt32)ms;
            DobotDll.SetWAITCmd(ref waitCmd, true, ref lastCmdNumber);
        }

        private static void goPoint(RobotPoint point)
        {
            goPoint(point, PTPMode.PTPMOVLXYZMode);
        }

        private static void goPoint(RobotPoint point, PTPMode mode) 
        {
            ptp(mode, point.X, point.Y, point.Z, point.R);
        }

        private static UInt64 ptp(PTPMode style, double x, double y, double z, double r)
        {
            PTPCmd pdbCmd;
            
            pdbCmd.ptpMode = (byte) style;
            pdbCmd.x = (float)x;
            pdbCmd.y = (float)y;
            pdbCmd.z = (float)z;
            pdbCmd.rHead = (float)r;
            while (true)
            {
                int ret = DobotDll.SetPTPCmd(ref pdbCmd, true, ref lastCmdNumber);
                if (ret == 0)
                    break;
            }

            WriteToConsole($"added to queue: goto point \t\tX={x}\tY={y}\tZ={z}\tR={r}");

            return lastCmdNumber;
        }

        private static void SetParam()
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
            pbdParam.velocityRatio = velocityRatio;//30;
            pbdParam.accelerationRatio = accelerationRatio;//30;
            DobotDll.SetPTPCommonParams(ref pbdParam, false, ref cmdIndex);
        }

        private static void AlarmTest()
        {
            int ret;
            byte[] alarmsState = new byte[32];
            UInt32 len = 32;
            ret = DobotDll.GetAlarmsState(alarmsState, ref len, alarmsState.Length);
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
                                { // reset
                                    //Get Alarm status: reset
                                    break;
                                }
                            /* other status*/
                            default:
                                break;
                        }
                    }
                }
            }
            //DobotDll.ClearAllAlarmsState();
        }

        /// <summary>
        /// Used to periodically get information about the dobot's pose to update the UI, unecessary in console program
        /// </summary>
        private void StartGetPose()
        {
            posTimer.Elapsed += new System.Timers.ElapsedEventHandler(PosTimer_Tick);   //calls PosTimer_Tick every time the interval has elapsed
            posTimer.Interval = 600;
            posTimer.Start();
        }

        private void PosTimer_Tick(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!isConnected)       //if Dobot is not connected return from method
                return;

            DobotDll.GetPose(ref pose);     //uses DLL to check the pose of the robot and sets it to the pose class variable

            //this.Dispatcher.BeginInvoke((Action)delegate()

            //this was supposed to executed by the dispatcher:
            /*
            tbJoint1Angle.Text = pose.jointAngle[0].ToString();
            tbJoint1Angle.Text = pose.jointAngle[1].ToString();
            tbJoint1Angle.Text = pose.jointAngle[2].ToString();
            tbJoint1Angle.Text = pose.jointAngle[3].ToString();

            if (sync.IsChecked == true)
            {
                X.Text = pose.x.ToString();
                Y.Text = pose.y.ToString();
                Z.Text = pose.z.ToString();
            }
            */
        }

        public class RobotPoint : ICloneable 
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Z { get; set; }
            public double R { get; set; }

            public RobotPoint(double x, double y, double z, double r)
            {
                X = x;
                Y = y;
                Z = z;
                R = r;
            }

            public object Clone()
            {
                return new RobotPoint(X, Y, Z, R);
            }
        }
    }
#endregion
}
