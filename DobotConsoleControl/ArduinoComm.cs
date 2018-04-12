﻿using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Management;
using System.Threading;

namespace DobotConsoleControl
{
    public class ArduinoComm
    {
        private const Int32 _baudRate = 9600;
        private const Int32 _dataBits = 8;
        private const Handshake _handshake = Handshake.None;
        private const Parity _parity = Parity.None;
        private static string _portName = "";
        private const StopBits _stopBits = StopBits.One;

        private static SerialPort _serialPort;
        enum CommState { IDLE, WAIT_FOR_ACK, WAIT_FOR_MOVE };
        CommState _commState = CommState.IDLE;

        private const int READ_TIMEOUT = 300;
        private const int ACK_WAIT = 200;

        private static bool _dataHandler = true;
        public static bool _moving { get; private set; } = false;

        private const string DELIMETER = ",";
        private const string ACK = "00";
        private const string ERROR = "01";
        private const string MOVE_FINISH = "000";
        
        public static bool Connect()
        {
            if (_portName == "")
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM WIN32_SerialPort"))
                {
                    string[] portnames = SerialPort.GetPortNames();
                    var ports = searcher.Get().Cast<ManagementBaseObject>().ToList();
                    foreach (ManagementBaseObject bObj in ports)
                    {
                        string caption = (string)bObj["Caption"];
                        List<string> splitString = new List<string>(caption.Split(' '));
                        string first = splitString[0].Trim();
                        Console.WriteLine(first);
                        if (first.Contains("Arduino"))//splitString[0].Equals("Arduino"))
                        {
                            _portName = (string) bObj["DeviceID"];
                            Console.WriteLine("SEND IT!!!");
                        }
                    }
                }
            }

            if (_serialPort != null ) {
                try
                {
                    _serialPort.Close();
                }
                catch (Exception e)
                {
                    Console.Write(e.ToString());
                }
            }
            _serialPort = new SerialPort(_portName, _baudRate, _parity, _dataBits, _stopBits);
            _serialPort.NewLine = "\n";
            _serialPort.ReadTimeout = READ_TIMEOUT;
            _serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
            _serialPort.Open();
            
            return true;
        }

        public static bool isOpen()
        {
            return _serialPort.IsOpen;
        }

        private static void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;

            string indata;

            try
            {
                indata = sp.ReadLine();
                string[] data = indata.Split(',');
                Console.WriteLine(data[0]);
                switch (data[0])
                {
                    case MOVE_FINISH:
                        Console.WriteLine("MOVE FINISHED!");
                        Console.WriteLine(_serialPort.BytesToRead);
                        _moving = false;
                        break;

                    default:
                        break;
                }
            } catch(Exception ex)
            {
                Console.Write(ex.ToString());
            }
        }

        static public string ReadLine()
        {
            return _serialPort.ReadLine();
        }

        static public bool Home()
        {
            return SendString("1,", true);
        }

        static public bool Move(double pos)
        {
            return SendString("2," + pos + ",", true);
        }

        static private bool SendString(string msg, bool moving)
        {
            // if there's already a move happening and it's a moving command don't send another
            if (moving && !_moving)
            {

                _dataHandler = false;
                _serialPort.WriteLine(msg);
                Console.WriteLine(msg);
                _moving = moving;
                return true;// WaitForAck();
            }
            else
                return false;
        }

        static private bool WaitForAck()
        {
            bool ack = false;
            Thread.Sleep(ACK_WAIT);
            string inData = _serialPort.ReadLine();
            Console.WriteLine(inData);

            switch (inData)
            {
                case ACK:
                    ack = true;
                    Console.WriteLine("Acknowledge received");
                    break;
                case ERROR:
                    Console.WriteLine("Communication error: Acknowledge not received.");
                    ack = false;
                    break;
                default:
                    ack = false;
                    break;
            }

            _dataHandler = true;

            return ack;
        }

    }
}
