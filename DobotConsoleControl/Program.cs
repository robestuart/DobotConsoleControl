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

namespace DobotConsoleControl
{
    // Some code was used or referenced from these websites:
    // https://www.codeproject.com/Articles/816301/Csharp-Building-a-Useful-Extensible-NET-Console-Ap
    // https://social.msdn.microsoft.com/Forums/vstudio/en-US/707e9ae1-a53f-4918-8ac4-62a1eddb3c4a/detecting-console-application-exit-in-c?forum=csharpgeneral
/*
    abstract class RbtAction
    {
        public enum ACTION
        {
            move,
            vacuum,
            dwell
        }

        public static ACTION _action;
        protected static ulong _cmdNumber = 0;

        public static ulong CmdNumber { get => _cmdNumber; set => _cmdNumber = value; }

        public abstract ulong queueAction();
        public abstract string report();
        
    }

    class MoveAction : RbtAction
    {
        public static RobotPoint _point;
        public MoveAction (RobotPoint point)
        {
            _action = ACTION.move;
            _point = point;
        }

        public override ulong queueAction()
        {
            PTPCmd pdbCmd;

            pdbCmd.ptpMode = (byte)PTPMode.PTPMOVLXYZMode;
            pdbCmd.x = (float) _point.X;
            pdbCmd.y = (float) _point.Y;
            pdbCmd.z = (float) _point.Z;
            pdbCmd.rHead = (float) _point.R;

            // communication thing, keeps trying to tell it to do a command until it takes
            while (true)
            {
                int ret = DobotDll.SetPTPCmd(ref pdbCmd, true, ref _cmdNumber);
                if (ret == 0)
                    break;
            }

            return _cmdNumber;
        }

        public override string report()
        {
            throw new NotImplementedException();
        }
    }

    class VacAction : RbtAction
    {
        public static bool _on;
        public VacAction(bool vacOn)
        {
            _action = ACTION.vacuum;
            _on = vacOn;
        }

        public override ulong queueAction()
        {
            DobotDll.SetEndEffectorSuctionCup(true, _on, true, ref _cmdNumber);

            return _cmdNumber;
        }

        public override string report()
        {
            throw new NotImplementedException();
        }
    }

    class DwellAction: RbtAction
    {
        public static int _time;
        public DwellAction(int time)
        {
            _action = ACTION.dwell;
            _time = time;
        }

        public override ulong queueAction()
        {
            WAITCmd waitCmd;
            waitCmd.timeout = (UInt32) _time;
            DobotDll.SetWAITCmd(ref waitCmd, true, ref _cmdNumber);
            return _cmdNumber;
        }

        public override string report()
        {
            throw new NotImplementedException();
        }
    }
    */
    public static class PrgConsole
    {
        const string READ_PROMPT = "console> ";

        public enum ERRORS
        {
            INVALID_NUMBER,
            NONAME_POINT
        }

        public enum WARNINGS
        {
            STOPPING
        }

        private const string QUEUED = "+\tQUEUED:\t";
        private const string FORMAT_STRING = "{0:000.000}";//"+000.000;-000.000";//"{000:0.00}";

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
                default:
                    txt = "UKNOWN ERROR";
                    break;
            }

            Error(txt);
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

        //private static Dictionary<string, RobotPoint> points = new Dictionary<string, RobotPoint>();

        private ArduinoComm arduino = new ArduinoComm();

        private static RobotPoint heightCheck = new RobotPoint(181.7, 2.77, -35, -10.6148);
        private static RobotPoint lithPickup = new RobotPoint(167.1955, -147.1283, -58.5127, -10.6148);
        private static RobotPoint buildPlace = new RobotPoint(168.2096, 3.2643, -47.5, -10.6148);
        private static RobotPoint pickPoint = (RobotPoint)lithPickup.Clone();
        private static RobotPoint pickHigh = new RobotPoint(167.1955, -147.1283, 50, -10.6148);
        private static RobotPoint placePoint = (RobotPoint)buildPlace.Clone();
        //private static RobotPoint placeHigh = (RobotPoint)buildPlace.Clone();
        //private static RobotPoint pickPoint = new RobotPoint(228.7848, -96.1451, -20, -22.7647);
        //private static RobotPoint placePoint = new RobotPoint(225.6651, 118.4177, -20, 27.6882);
        private static RobotPoint chillPoint = new RobotPoint(28.8, -191.653, 7.206, -81);//new RobotPoint(247.658, 61.7616, 50, 14);
        private static RobotPoint homePoint = new RobotPoint(250, 0, 0, 0);


        //private static bool isConnected = false;
        private System.Timers.Timer posTimer = new System.Timers.Timer();
        private static Pose pose = new Pose();
        //private static ulong lastCmdNumber = 0;
        //private static double clearHeight = 30;
        private static bool waitToContinue = false;
        private enum PRGSTATE { INPUT, RUN, CONTINUE, RUNSQ };
        private static PRGSTATE state = PRGSTATE.INPUT;

        private static double stackHeight = 0;
        private static int currentLayer = 0;

        //private static int dwellTime = 3000;

        //private static double layerHeight = 1.25;

        //private static double shortPause = 200;
        //private static float velocityRatio = 100;
        //private static float accelerationRatio = 100;

        //private static float lowMode = 30;
        //private static float medMode = 60;
        //private static float highMode = 100;

        private static bool isClosing = false;

        //iniitalizes the queue that's used to bypass the hardware queue which was creating problems
        //with ignoring commands for no reason

        //private static Queue<RbtAction> pointQ = new Queue<RbtAction>();

        static void Main(string[] args)            // changed from static
        {
            //TODO read points from XML
            // add all the pre-defined points to the point dictionary

            /*points.Add("heightCheck", heightCheck);
            points.Add("lithPickup", lithPickup);
            points.Add("buildPlace", buildPlace);
            points.Add("pickPoint", pickPoint);
            points.Add("pickHigh", pickHigh);
            points.Add("placePoint", placePoint);
            points.Add("chillPoint", chillPoint);
            points.Add("homePoint", homePoint);
            */


            //WriteToConsole("home\tstop\tgetpose\tgeterror\n");

            // used to intercept CTRL-C command and make dobot E-stop
            SetConsoleCtrlHandler(new HandlerRoutine(ConsoleCtrlCheck), true);

            PrgConsole.WriteWelcome();
            Dobot.Start();//StartDobot();
            ArduinoComm.Connect();

            Run();
            Dobot.Disconnect();//DobotDll.DisconnectDobot();
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
                            PrgConsole.WriteQueueStatus(currentCmdIndex, Dobot.LastCmdNumber);//lastCmdNumber);//Console.Write($"\rQueue command index: {currentCmdIndex} out of {lastCmdNumber}");

                            if (currentCmdIndex == Dobot.LastCmdNumber)//lastCmdNumber)
                            {
                                Console.Write("\n");
                                PrgConsole.WriteInfo("Queue finsihed execution\n");   //Console.Write("\rQueue finished execution\n");
                                PrgConsole.StackUpdate(currentLayer, stackHeight);    //PrgConsole.WriteToConsole($"Currently at\theight:  {stackHeight}\tlayer#:  {layerCount}", 2);
                                Dobot.CheckAlarms();

                                //TODO ArduinoComm.Move(step);
                                state = PRGSTATE.INPUT;
                            }
                        }
                        break;

                    // Runs using the software queue, trying to fix a bug with hardware operation
                    // this will be slower than hardware queue
                    /*case PRGSTATE.RUNSQ:
                        ulong hIndex = 0;
                        DobotDll.GetQueuedCmdCurrentIndex(ref hIndex);

                        if (hIndex == lastCmdNumber)
                        {
                            // hardware is idle
                            if (pointQ.Count != 0)      //makes sure something is in the queue
                            {
                                RbtAction rbtAction = pointQ.Dequeue();
                                lastCmdNumber = rbtAction.queueAction();
                            }
                            else    // if nothing is in the software queue and the hardware is idle you're finished give control back to user
                            {
                                Console.Write("\rQueue finished execution\n");
                                PrgConsole.WriteToConsole($"Currently at\theight:  {stackHeight}\tlayer#:  {layerCount}");
                                state = PRGSTATE.INPUT;
                                break;
                            }
                        } else
                        {
                            // updates user on progress
                            currentCmdIndex = hIndex;
                            Console.Write($"\rQueue command index: {currentCmdIndex} out of {lastCmdNumber}");
                        }

                        /*
                        if (hIndex != currentCmdIndex)
                        {
                            currentCmdIndex = hIndex;
                            Console.Write($"\rQueue command index: {currentCmdIndex} out of {lastCmdNumber}");

                            if (currentCmdIndex == lastCmdNumber)
                            {
                                Console.Write("\rQueue finished execution\n");
                                WriteToConsole($"Currently at\theight:  {stackHeight}\tlayer#:  {layerCount}");
                                state = PRGSTATE.INPUT;
                            }
                        }*/
                        //break;


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
            /*
            foreach (string c in commands)
            {
                WriteToConsole(c);
            }*/

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
                    Dobot.ClearQueue();//DobotDll.SetQueuedCmdClear();
                    break;
                case "StartQueue":
                    Dobot.StartQueue();// DobotDll.SetQueuedCmdStartExec();
                    break;
                case "Reconnect":
                    Dobot.Start();//StartDobot();
                    break;
                case "Help":
                    PrgConsole.WriteHelp();
                    break;
                    //case "swStackOne":
                    //    swStackOne();
                    //break;
                case "StackOne":
                    Dobot.StackOne(ref stackHeight, ref currentLayer);
                    state = PRGSTATE.RUN;
                    Console.Write("\n");
                    break;

                /*case "runprog":
                    runProg();
                    break;
                */
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
                            /*
                            velocityRatio = lowMode;
                            accelerationRatio = lowMode;
                            SetParam();
                            PrgConsole.WriteToConsole("Speed set to low");*/
                            break;

                        case "med":
                            Dobot.SetParam(SPEED.Med);
                            /*
                            velocityRatio = medMode;
                            accelerationRatio = medMode;
                            SetParam();
                            PrgConsole.WriteToConsole("Speed set to med");*/
                            break;

                        case "high":
                            Dobot.SetParam(SPEED.High);
                            /*
                            velocityRatio = highMode;
                            accelerationRatio = highMode;
                            SetParam();
                            PrgConsole.WriteToConsole("Speed set to high");*/
                            break;
                    }
                    break;

                /*case "startcheck":
                    vac(false);
                    GoHighFromCurrent(pickPoint);
                    vac(true);
                    wait(shortPause);
                    GoHigh(pickPoint, heightCheck);
                    break;
                    */
                /*case "endcheck":
                    GoHighFromCurrent(pickPoint);
                    vac(false);
                    break;
                    */
                case "Go":
                    RobotPoint rPoint = null;
                    switch (commands[2])
                    {
                        case "pick":
                            Dobot.GoPoint(Dobot.PICK);
                            //rPoint = pickPoint;
                            //goPoint(pickPoint, PTPMode.PTPMOVLXYZMode);
                            break;
                        case "place":
                            Dobot.GoPoint(Dobot.BUILD_BOTTOM);
                            //rPoint = placePoint;
                            //goPoint(placePoint, PTPMode.PTPMOVLXYZMode);
                            break;
                        //case "pickplace":
                        //rPoint = pickPoint;
                        //goPickPlace();//goPoint(pickHigh, PTPMode.PTPMOVLXYZMode);
                        //break;
                        //case "pickhigh":
                        //goHighFromCurrent(pickPoint);
                        //  break;
                        //case "placehigh":
                        //goHighFromCurrent(placePoint);
                        //  break;
                        //case "zcheck":
                        //    rPoint = heightCheck;//goHighFromCurrent(heightCheck);
                        //   break;
                        case "chill":
                            Dobot.GoPoint(Dobot.CHILL);
                            //rPoint = chillPoint;
                            //goHighFromCurrent(chillPoint);
                            break;
                    }

                    //DobotDll.GetPose(ref pose);

                    //RobotPoint currentPosition = new RobotPoint(pose.x, pose.y, pose.z, pose.rHead);

                    switch (commands[1])
                    {
                        case "x":
                            double x;
                            if (Double.TryParse(commands[2], out x))
                                Dobot.GoXInc(x);
                            else
                                PrgConsole.Error(PrgConsole.ERRORS.INVALID_NUMBER);
                            //currentPosition.X += Double.Parse(commands[2]);
                            //Dobot.GoPoint(currentPosition, PTPMode.PTPMOVLXYZMode);
                            break;
                        case "y":
                            double y;
                            if (Double.TryParse(commands[2], out y))
                                Dobot.GoYInc(y);
                            else
                                PrgConsole.Error(PrgConsole.ERRORS.INVALID_NUMBER);

                            //currentPosition.Y += Double.Parse(commands[2]);
                            //GoPoint(currentPosition, PTPMode.PTPMOVLXYZMode);
                            break;
                        case "z":
                            double z;
                            if (Double.TryParse(commands[2], out z))
                                Dobot.GoZInc(z);
                            else
                                PrgConsole.Error(PrgConsole.ERRORS.INVALID_NUMBER);

                            //currentPosition.Z += Double.Parse(commands[2]);
                            //GoPoint(currentPosition, PTPMode.PTPMOVLXYZMode);
                            break;
                        case "r":
                            double r;
                            if (Double.TryParse(commands[2], out r))
                                Dobot.GoRInc(r);
                            else
                                PrgConsole.Error(PrgConsole.ERRORS.INVALID_NUMBER);

                            //currentPosition.R += Double.Parse(commands[2]);
                            //GoPoint(currentPosition, PTPMode.PTPMOVLXYZMode);
                            break;
                        case "direct":
                            Dobot.GoPoint(commands[2]);//GoPoint(rPoint, PTPMode.PTPMOVLXYZMode);
                            break;
                        case "high":
                            Dobot.GoHighFromCurrent(commands[2]);//GoHighFromCurrent(rPoint);
                            break;
                        case "hover":
                            Dobot.GoHoverFromCurrent(commands[2]);//goHoverFromCurrent(rPoint);
                            break;
                    }
                    /*
                    if (commands[1].Equals("pick"))
                        goPoint(pickPoint, PTPMode.PTPMOVLXYZMode);
                    else if (commands[1].Equals("place"))
                        goPoint(placePoint, PTPMode.PTPMOVLXYZMode);
                    else if (commands[1].EndsWith("pickplace"))
                        goPickPlace();//goPoint(pickHigh, PTPMode.PTPMOVLXYZMode);
                    else if (commands[1].EndsWith("pickhigh"))
                        goHighFromCurrent(pickPoint);
                    else if (commands[1].EndsWith("placehigh"))
                        goHighFromCurrent(placePoint);
                    else if (commands[1].EndsWith("heightCheck"))
                        goHighFromCurrent(heightCheck);
                    break;*/
                    break;
                case "SetLayerHeight":
                    double lHeight;
                    if (Double.TryParse(commands[1], out lHeight))
                    {
                        Dobot.LayerHeight = lHeight;//Double.Parse(commands[1]);
                        PrgConsole.SetVariable("LAYER_HEIGHT");
                        //PrgConsole.WriteToConsole($"New layer delta: {layerHeight} mm");
                    }
                    else
                        PrgConsole.Error(PrgConsole.ERRORS.INVALID_NUMBER);
                    break;

                case "SetDwell":

                    //TODO add code that takes a variable number of commands[1-inf]
                    int dwellTime;
                    List<int> DwellTimes = new List<int>();
                    for (int i = 1; i < commands.Count-1; i++)
                    {
                        if (int.TryParse(commands[i], out dwellTime))
                        {
                            DwellTimes.Add(dwellTime);
                            //Dobot.setDwellTime(i - 1, dwellTime);
                            //PrgConsole.SetVariable($"DWELL_TIME[{i-1}]");
                        }
                        else
                            break;
                    }

                    Dobot.SetDwellTimes(DwellTimes);
                    PrgConsole.SetVariable("DWELL_TIMES");

                    /*
                    if (int.TryParse(commands[1], out dwellTime))
                    {
                        Dobot.DwellTime = dwellTime;
                        PrgConsole.SetVariable("DWELL_TIME");
                    }
                    else
                        PrgConsole.Error(PrgConsole.ERRORS.INVALID_NUMBER);
                        */
                    //dwellTime = int.Parse(commands[1]);
                    //PrgConsole.WriteToConsole($"New dwell time: {dwellTime} ms");
                    break;

                case "ChangePickZ":
                    Dobot.ChangePickZ(Double.Parse(commands[1]));
                    break;
                case "ChangeBuildZ":
                    Dobot.ChangeBuildZ(Double.Parse(commands[1]));
                    break;
                case "GetCurrentPoint":
                    Dobot.DisplayCurrentPoint();
                    break;
                case "SaveCurrentPoint":
                    Dobot.SaveCurrentPoint(commands[1]);
                    break;
                case "SavePointsToFile":
                    Dobot.SavePointsToFile();
                    break;

                case "ResetLayers":
                    stackHeight = 0;
                    currentLayer = 0;
                    break;

                case "setpoint":
                    if (commands[1].Equals("pick"))
                        changePoint(ref pickPoint, Double.Parse(commands[2]), Double.Parse(commands[3]), Double.Parse(commands[4]), Double.Parse(commands[5]));
                    else if (commands[1].Equals("place"))
                        changePoint(ref placePoint, Double.Parse(commands[2]), Double.Parse(commands[3]), Double.Parse(commands[4]), Double.Parse(commands[5]));
                    else if (commands[1].Equals("home"))
                        changePoint(ref homePoint, Double.Parse(commands[2]), Double.Parse(commands[3]), Double.Parse(commands[4]), Double.Parse(commands[5]));
                    break;

                /*case "SeedPoints":
                    Dobot.SeedPoints();
                    break;*/

                /*case "ReadPoints":
                    DataFile.ReadPoints();
                    break;*/

                case "Home":
                    Dobot.Home();
                    ArduinoComm.Home();

                    /*HOMEParams hParams;
                    hParams.x = (float)homePoint.X;
                    hParams.y = (float)homePoint.Y;
                    hParams.z = (float)homePoint.Z;
                    hParams.r = (float)homePoint.R;
                    DobotDll.SetHOMEParams(ref hParams, true, ref lastCmdNumber);
                    DobotDll.GetPose(ref pose);

                    // code to make sure head of the robot is not on something before homing
                    currentPosition = new RobotPoint(pose.x, pose.y, pose.z, pose.rHead);
                    currentPosition.Z += 25;
                    hqGoPoint(currentPosition);
                    Thread.Sleep(1000);
                    DobotDll.SetQueuedCmdStopExec();
                    DobotDll.SetQueuedCmdClear();
                    DobotDll.SetQueuedCmdStartExec();
                    Thread.Sleep(1000);
                    HOMECmd hCmd;
                    hCmd.temp = 1;
                    DobotDll.SetHOMECmd(ref hCmd, false, ref lastCmdNumber);*/

                    break;
                //case "gohigh":
                //    goHighFromCurrent(placePoint);
                //    goHigh(placePoint, pickPoint);
                //    break;
                /*case "gethome":
                    PrgConsole.WriteToConsole(getHome());
                    break;
                    */
                /*case "createPoint":
                    createPoint();
                    break;*/
                case "ArduinoConnect":
                    ArduinoComm.Connect();
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
                /*case "GetPorts":
                    PrgConsole.WriteInfo(ArduinoComm.getPorts());                    
                    break;*/

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


        
    }
#endregion
}
