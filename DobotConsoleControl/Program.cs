#define DEBUG


using DobotClientDemo.CPlusDll;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using static DobotConsoleControl.Dobot;
using System.Configuration;
using System.Reflection;

namespace DobotConsoleControl
{

    public static class PrgConsole
    {
        const string READ_PROMPT = "console> ";

        public enum ERRORS
        {
            INVALID_NUMBER,
            NONAME_POINT,
            FILE
        }

        public enum WARNINGS
        {
            STOPPING
        }

        private const string QUEUED = "+\tQUEUED:\t";
        private const string FORMAT_STRING = "{0:000.000}";

        public static string NumString(double num)
        {
            return string.Format(FORMAT_STRING, num);
        }

        public static void invalidCommand()
        {
            WriteToConsole("Invalid Command Entered");
        }

        private static void WriteToConsole(string message = "", int feedback = 0)
        {
            if (feedback == 1)
            {
                message = "  +" + message;
            }
            else if (feedback == 2)
            {
                message = "..." + message;
            }

            if (message.Length > 0)
            {
                Console.WriteLine(message);
            }


        }


        public static string ReadFromConsole(string promptMessage = "")
        {
            // Show a prompt, and get input:
            Console.Write(READ_PROMPT + promptMessage);
            return Console.ReadLine();
        }

        public static void WriteWelcome()
        {
            PrgConsole.WriteToConsole("\n\nWELCOME TO THE DOBOT HELPER\n");
            PrgConsole.WriteToConsole("Syntax is as follows: command(parameter1,parameter2)");
            PrgConsole.WriteHelp();
            PrgConsole.WriteToConsole("You can type \"Help()\" to show the above information again\nYOU SHOULD START BY RUNNING := home()\n");
        }

        public static void WriteQueueStatus(ulong commandIndex, ulong finalCommandIndex)
        {
            Console.Write($"\rQueue command index: {commandIndex} out of {finalCommandIndex}");
        }

        public static void WriteHelp()
        {
            string readMe = System.IO.File.ReadAllText("ReadMe.txt");
            WriteToConsole("\n");
            WriteToConsole(readMe);
            WriteToConsole("\n");
        }

        public static void AddedToQueueVac(bool on)
        {
            string state = on ? "ON" : "OFF";
            WriteToConsole(QUEUED + $"VACUUM\t" + state);
        }

        public static void AddedToQueueWait(double ms)
        {
            WriteToConsole(QUEUED + $"WAIT\t{ms} ms");
        }

        public static void AddedToQueue(RobotPoint point)
        {
            WriteToConsole(QUEUED + $"POINT\tX= {PrgConsole.NumString(point.X)}\tY= {PrgConsole.NumString(point.Y)}\tZ= {PrgConsole.NumString(point.Z)}\tR= {PrgConsole.NumString(point.R)}");
        }

        public static void SetVariable(string txt)
        {
            WriteInfo("Set new variable " + txt);
        }

        public static void StackUpdate(int layer, double height)
        {
            WriteToConsole($"Currently at\theight:  {height}\tlayer#:  {layer}\n", 2);
        }

        public static void SpeedChange(SPEED speed)
        {
            WriteInfo("Speed changed to: " + speed);
        }

        public static void PointSaved(RobotPoint point)
        {
            WriteInfo("Point saved: " + point.Name);
        }

        public static void WriteInfo(string txt)
        {
            WriteToConsole("\tINFO:\t" + txt);
        }

        public static void Error(string txt)
        {
            WriteToConsole("!!!!\tERROR: " + txt);
        }

        public static void Error(ERRORS error)
        {
            string txt;
            switch (error)
            {
                case ERRORS.INVALID_NUMBER:
                    txt = "DID NOT ENTER A VALID NUMBER";
                    break;
                case ERRORS.NONAME_POINT:
                    txt = "POINT DOES NOT HAVE A VALID NAME TO ALLOW IT TO BE SAVED";
                    break;
                case ERRORS.FILE:
                    txt = "FILE IO ERROR";
                    break;
                default:
                    txt = "UKNOWN ERROR";
                    break;
            }

            Error(txt);
        }

        public static void Warning(string warning)
        {
            WriteToConsole("!!!!\tERROR: " + warning);
        }

        public static void Warning(WARNINGS warning)
        {
            string txt;
            switch (warning)
            {
                case WARNINGS.STOPPING:
                    txt = "Attempting to stop Dobot";
                    break;
                default:
                    txt = "UKNOWN WARNING";
                    break;
            }

            WriteToConsole("!!!!\tERROR: " + txt);
        }

        public static void WriteAlarms(List<string> alarms)
        {
            foreach (string alarm in alarms) {
                WriteToConsole("\tALARM:\t" + alarm);
            }
        }

    }

    class Program
    {
        //private static string DATA_FILE = "data.txt";

        private ArduinoComm arduino = new ArduinoComm();

        private static RobotPoint heightCheck = new RobotPoint(181.7, 2.77, -35, -10.6148);
        private static RobotPoint lithPickup = new RobotPoint(167.1955, -147.1283, -58.5127, -10.6148);
        private static RobotPoint buildPlace = new RobotPoint(168.2096, 3.2643, -47.5, -10.6148);
        private static RobotPoint pickPoint = (RobotPoint)lithPickup.Clone();
        private static RobotPoint pickHigh = new RobotPoint(167.1955, -147.1283, 50, -10.6148);
        private static RobotPoint placePoint = (RobotPoint)buildPlace.Clone();
        private static RobotPoint chillPoint = new RobotPoint(28.8, -191.653, 7.206, -81);
        private static RobotPoint homePoint = new RobotPoint(250, 0, 0, 0);


        private System.Timers.Timer posTimer = new System.Timers.Timer();
        private static Pose pose = new Pose();
        private static bool waitToContinue = false;
        private enum PRGSTATE { INPUT, RUN, CONTINUE, RUNSQ };
        private static PRGSTATE state = PRGSTATE.INPUT;

        private static double stackHeight = 0;
        private static int currentLayer = 0;

        private static bool isClosing = false;


        static void Main(string[] args)
        {
            // load default configuration and default points
            FileIO.InitializeDirectory();

            FileIO.LoadConfig();
            FileIO.LoadPoints();

            // used to intercept CTRL-C command and make dobot E-stop
            SetConsoleCtrlHandler(new HandlerRoutine(ConsoleCtrlCheck), true);

            PrgConsole.WriteWelcome();
            Dobot.Start();
            ArduinoComm.Connect();

            Run();
            Dobot.Disconnect();
        }

        static void Run()
        {
            ulong currentCmdIndex = 0;
            while (!isClosing)
            {
                Thread.Sleep(100);
                switch (state)
                {
                    case PRGSTATE.RUN:
                        ulong tmpIndex = 0;
                        DobotDll.GetQueuedCmdCurrentIndex(ref tmpIndex);
                        if (tmpIndex != currentCmdIndex)
                        {
                            currentCmdIndex = tmpIndex;
                            PrgConsole.WriteQueueStatus(currentCmdIndex, Dobot.LastCmdNumber);

                            if (currentCmdIndex == Dobot.LastCmdNumber)
                            {
                                Console.Write("\n");
                                PrgConsole.WriteInfo("Queue finsihed execution\n");
                                PrgConsole.StackUpdate(currentLayer, stackHeight);
                                Dobot.CheckAlarms();

                                //TODO ArduinoComm.Move(step);
                                state = PRGSTATE.INPUT;
                            }
                        }
                        break;

                    case PRGSTATE.CONTINUE:

                        break;

                    // waits for user input
                    case PRGSTATE.INPUT:

                        var consoleInput = PrgConsole.ReadFromConsole();
                        if (string.IsNullOrWhiteSpace(consoleInput) && !waitToContinue) continue;

                        // attempts to parse the input
                        try
                        {
                            // Execute the command:
                            string result = Execute(consoleInput);

                            // Write out the result:
                            //PrgConsole.WriteToConsole(result);
                        }
                        catch (Exception ex)
                        {
                            // OOPS! Something went wrong - Write out the problem:
                            PrgConsole.Error(ex.Message);
                        }
                        break;
                }
            }
        }
        
        static string Execute(string command)
        {
            char[] delimeters = { '(', ',', ')' };
            List<string> commands = new List<string>(command.Split(delimeters));
            foreach (string cmd in commands)
            {
                cmd.Trim();
            }

            if (waitToContinue && string.IsNullOrWhiteSpace(command))
                return null;

            switch (commands[0])
            {
                case "ClearAlarms":
                    Dobot.ClearAlarms();
                    break;
                case "CheckAlarms":
                    Dobot.CheckAlarms();
                    break;
                case "ClearQueue":
                    Dobot.ClearQueue();
                    break;
                case "StartQueue":
                    Dobot.StartQueue();
                    break;
                case "Reconnect":
                    Dobot.Start();
                    break;
                case "Help":
                    PrgConsole.WriteHelp();
                    break;
                case "StackOne":
                    Dobot.StackOne(ref stackHeight, ref currentLayer);
                    state = PRGSTATE.RUN;
                    Console.Write("\n");
                    break;
                case "Vac":
                    if (commands[1].Equals("on"))
                        Dobot.Vac(true);
                    else if (commands[1].Equals("off"))
                        Dobot.Vac(false);
                    else
                        PrgConsole.invalidCommand();
                    break;

                case "SetMode":
                    switch (commands[1])
                    {
                        case "low":
                            Dobot.SetParam(SPEED.Low);
                            break;

                        case "med":
                            Dobot.SetParam(SPEED.Med);
                            break;

                        case "high":
                            Dobot.SetParam(SPEED.High);
                            break;
                    }
                    break;

                case "Go":
                    RobotPoint rPoint = null;
                    switch (commands[2])
                    {
                        case "pick":
                            Dobot.GoPoint(Dobot.PICK);
                            break;
                        case "place":
                            Dobot.GoPoint(Dobot.BUILD);
                            break;
                        case "chill":
                            Dobot.GoPoint(Dobot.CHILL);
                            break;
                    }

                    switch (commands[1])
                    {
                        case "x":
                            double x;
                            if (Double.TryParse(commands[2], out x))
                                Dobot.GoXInc(x);
                            else
                                PrgConsole.Error(PrgConsole.ERRORS.INVALID_NUMBER);
                            break;
                        case "y":
                            double y;
                            if (Double.TryParse(commands[2], out y))
                                Dobot.GoYInc(y);
                            else
                                PrgConsole.Error(PrgConsole.ERRORS.INVALID_NUMBER);
                            break;
                        case "z":
                            double z;
                            if (Double.TryParse(commands[2], out z))
                                Dobot.GoZInc(z);
                            else
                                PrgConsole.Error(PrgConsole.ERRORS.INVALID_NUMBER);

                            break;
                        case "r":
                            double r;
                            if (Double.TryParse(commands[2], out r))
                                Dobot.GoRInc(r);
                            else
                                PrgConsole.Error(PrgConsole.ERRORS.INVALID_NUMBER);

                            break;
                        case "direct":
                            Dobot.GoPoint(commands[2]);
                            break;
                        case "high":
                            Dobot.GoHighFromCurrent(commands[2]);
                            break;
                        case "hover":
                            Dobot.GoHoverFromCurrent(commands[2]);
                            break;
                    }
                    break;
                case "SetArduinoHome":
                    double aHeight;
                    if (Double.TryParse(commands[1], out aHeight))
                    {
                        ArduinoComm.zInit = aHeight;
                    }
                    
                    break;
                case "SetLayerHeight":
                    double lHeight;

                    List<double> LayerHeights = new List<double>();
                    for (int i = 1; i < commands.Count - 1; i++)
                    {
                        if (double.TryParse(commands[i], out lHeight))
                        {
                            LayerHeights.Add(lHeight);
                        }
                        else
                            break;
                    }
                    Dobot.SetLayerHeights(LayerHeights);
                break;

                case "SetSafeHeight":
                    double height = Double.Parse(commands[1]);
                    RobotPoint.SetSafeHeight(height);
                    break;
                case "SetDwell":
                    double dwellTime;
                    List<double> DwellTimes = new List<double>();
                    for (int i = 1; i < commands.Count-1; i++)
                    {
                        if (double.TryParse(commands[i], out dwellTime))
                        {
                            DwellTimes.Add(dwellTime);
                        }
                        else
                            break;
                    }

                    Dobot.SetDwellTimes(DwellTimes);
                    PrgConsole.SetVariable("DWELL_TIMES");
                    break;

                case "ChangePickZ":
                    Dobot.ChangePickZ(Double.Parse(commands[1]));
                    break;
                case "ChangeBuildZ":
                    Dobot.ChangeBuildZ(Double.Parse(commands[1]));
                    break;
                case "ReadCurrentPoint":
                    Dobot.DisplayCurrentPoint();
                    break;

                case "StoreCurrentPoint":
                    Dobot.StoreCurrentPoint(commands[1]);
                    break;

                case "ZPlatformRaise":
                    ArduinoComm.Home();
                    break;

                case "ZPlatformStart":
                    ArduinoComm.MoveStart();
                    break;
                case "SavePoints":
                    if (commands.Count > 1)
                        Dobot.SavePointsToFile(commands[1]);
                    else
                        Dobot.SavePointsToFile();
                    break;
                case "LoadPoints":
                    if (commands.Count > 1)
                        FileIO.LoadPoints(commands[1]);
                    else
                        FileIO.LoadPoints();
                    break;
                case "ListPoints":
                    foreach (RobotPoint rp in Dobot.GetPoints()){
                        PrgConsole.WriteInfo(rp.ToString());
                    }
                    break;
                case "ListConfig":
                    PrgConsole.WriteInfo("Robot Safe Height:\t" + RobotPoint.SafeHeight);
                    PrgConsole.WriteInfo("Dwell times:\t");
                    StringBuilder sb = new StringBuilder();
                    foreach (double dt in Dobot.DwellTimes)
                    {
                        sb.Append(dt + ", ");
                    }
                    PrgConsole.WriteInfo(sb.ToString());
                    PrgConsole.WriteInfo("Layer heights:\t");
                    sb = new StringBuilder();
                    foreach (double lh in Dobot.LayerHeights)
                    {
                        sb.Append(lh + ", ");
                    }
                    PrgConsole.WriteInfo(sb.ToString());
                    PrgConsole.WriteInfo("Arduino home offset:\t" + ArduinoComm.zInit);
                    break;

                case "SaveConfig":
                    if (commands.Count > 1)
                        FileIO.SaveConfig(commands[1]);
                    break;
                case "LoadConfig":
                    if (commands.Count > 1)
                        FileIO.LoadConfig(commands[1]);
                    else
                        FileIO.LoadConfig();
                    break;

                case "LayerStatus":
                    PrgConsole.StackUpdate(currentLayer, stackHeight);
                    break;

                case "ResetLayers":
                    stackHeight = 0;
                    currentLayer = 0;
                    ArduinoComm.Home();
                    break;

                case "setpoint":
                    if (commands[1].Equals("pick"))
                        changePoint(ref pickPoint, Double.Parse(commands[2]), Double.Parse(commands[3]), Double.Parse(commands[4]), Double.Parse(commands[5]));
                    else if (commands[1].Equals("place"))
                        changePoint(ref placePoint, Double.Parse(commands[2]), Double.Parse(commands[3]), Double.Parse(commands[4]), Double.Parse(commands[5]));
                    else if (commands[1].Equals("home"))
                        changePoint(ref homePoint, Double.Parse(commands[2]), Double.Parse(commands[3]), Double.Parse(commands[4]), Double.Parse(commands[5]));
                    break;

                //case "SeedPoints":
                //    Dobot.SeedPoints();
                //    break;

                

                case "Home":
                    Dobot.Home();
                    ArduinoComm.Home();
                    ArduinoComm.MoveStart();
                    break;
                case "ArduinoConnect":
                    try
                    {
                        ArduinoComm.Connect();
                    } catch(Exception e)
                    {
                        PrgConsole.Warning("Arduino error, probably not on or not connected");
                    }
                    break;
                case "AHome":
                    ArduinoComm.Home();
                    break;
                case "AMove":
                    ArduinoComm.Move(Double.Parse(commands[1]));
                    break;
                case "AIsOpen":
                    ArduinoComm.isOpen();
                    break;

                
                default:
                    PrgConsole.invalidCommand();
                    break;
            }

            return "";//string.Format("Executed the {0} Command", command);
        }      

        private static void changePoint(ref RobotPoint point, double x, double y, double z, double r)
        {
            point = new RobotPoint(x, y, z, r);
        }
        
        [DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);      //external receives a deleagete
        public delegate bool HandlerRoutine();      //delegate type to be used as handler routine for SetConsoleCtrlHandler
       
        /// <summary>
        /// Should force the dobot to E-stop if you close the program
        /// </summary>
        /// <returns></returns>
        private static bool ConsoleCtrlCheck()
        {
            PrgConsole.Warning(PrgConsole.WARNINGS.STOPPING);//("!!! Attempting to stop Dobot !!!\n");
            DobotDll.SetQueuedCmdForceStopExec();
            DobotDll.SetQueuedCmdClear();
            return true;
        }
        
        #region DobotStuff

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
            if (!Dobot.IsConnected)       //if Dobot is not connected return from method
                return;

            DobotDll.GetPose(ref pose);     //uses DLL to check the pose of the robot and sets it to the pose class variable


        }


        
    }
#endregion
}
