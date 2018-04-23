using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace DobotConsoleControl
{
    [Serializable]
    public struct CryoForgeSettings
    {
        //public List<RobotPoint> points;
        public List<double> dwellTimes;
        public List<double> layerHeights;
        public double arduinoOffset;
        public double clearHeight;
    }

    static class FileIO
    {
        private static string dataDir = @"/data/";
        private static string defaultPoints = "DEFAULT_POINTS";
        private static string defaultConfig = "DEFAULT_CONFIG";

        public static void SaveConfig(string name)
        {

            CryoForgeSettings cf = new CryoForgeSettings();
            //cf.points = Dobot.GetPoints();
            cf.dwellTimes = Dobot.DwellTimes;
            cf.clearHeight = RobotPoint.SafeHeight;
            cf.layerHeights = Dobot.LayerHeights;
            cf.arduinoOffset = ArduinoComm.zInit;

            using (StreamWriter sw = new StreamWriter(name + ".xml", false))
            {
                XmlSerializer xs = new XmlSerializer(typeof(CryoForgeSettings));
                xs.Serialize(sw, cf);

                sw.Close();
            }
        }

        public static void LoadConfig()
        {
            LoadConfig(defaultConfig);
        }

        public static void LoadConfig(string name)
        {
            name = name + ".xml";

            if (!File.Exists(name))
            {
                PrgConsole.Error("The file specified does not exist, make sure you only entered the name and not the file extension");
                return;
            }
            
            using (StreamReader sr = new StreamReader(name))
            {
                XmlSerializer xs = new XmlSerializer(typeof(CryoForgeSettings));

                CryoForgeSettings cf = (CryoForgeSettings) xs.Deserialize(sr);
                sr.Close();

                RobotPoint.SetSafeHeight(cf.clearHeight);
                Dobot.SetDwellTimes(cf.dwellTimes);
                Dobot.SetLayerHeights(cf.layerHeights);
                ArduinoComm.zInit = cf.arduinoOffset;
            }
        }

        public static void SavePoints(Dictionary<string, RobotPoint> _points)
        {
            SavePoints(defaultPoints, _points);
        }

        /// <summary>
        /// Saves the supplied point to the XML file
        /// </summary>
        /// <param name="rPoint"></param>
        public static void SavePoints(string name, Dictionary<string, RobotPoint> _points)
        {
            name = name + ".xml";
            List<RobotPoint> robotPoints = new List<RobotPoint>(_points.Values);

            XmlSerializer serializer = new XmlSerializer(typeof(List<RobotPoint>));//RobotPoint));//(Dictionary<string,RobotPoint>));            
            using (StreamWriter writer = new StreamWriter(name, false))    //currently overwrites file instead of appending
            {

                serializer.Serialize(writer, robotPoints);
                //foreach (KeyValuePair<string, RobotPoint> entry in points) { 
                //    serializer.Serialize(writer, entry.Value);
                //}
                writer.Close();
            }
        }

        public static Dictionary<string, RobotPoint> LoadPoints()
        {
            return LoadPoints(defaultPoints);
        }

        /// <summary>
        /// Reads all points from the XML file and loads them into memory
        /// </summary>
        public static Dictionary<string, RobotPoint> LoadPoints(string name)
        {
            name = name + ".xml";
            if (!File.Exists(name))
                Dobot.SeedPoints();
            List<RobotPoint> pointList = new List<RobotPoint>();
            //Dictionary<string, RobotPoint> pointList = new Dictionary<string, RobotPoint>();
            XmlSerializer serializer = new XmlSerializer(typeof(List<RobotPoint>));//typeof(Dictionary<string,RobotPoint>));

            // if the document has unknown nodes handle with UnknownNode and UnknownAttribute events.
            serializer.UnknownNode += new XmlNodeEventHandler(serializer_UnknownNode);
            serializer.UnknownAttribute += new XmlAttributeEventHandler(serializer_UnknownAttribute);

            Dictionary<string, RobotPoint> points = new Dictionary<string, RobotPoint>();
            //RobotPoint point;
            using (StreamReader sr = new StreamReader(name, false))
            {
                pointList = (List<RobotPoint>)serializer.Deserialize(sr);
                foreach (RobotPoint rp in pointList)
                {
                    points[rp.Name] = rp;//.Add(rp.Name, rp);
                    Dobot.AddPoint(rp);
                }
                //point = (RobotPoint)serializer.Deserialize(sr);
                sr.Close();
            }

            //FileStream fs = new FileStream(DATA_FILE, FileMode.Open);
            //RobotPoint point;
            //point = (RobotPoint)serializer.Deserialize(fs);


            return points;
        }

        //TODO change this
        private static void serializer_UnknownNode(object sender, XmlNodeEventArgs e)
        {
            Console.WriteLine("Unknown Node:" + e.Name + "\t" + e.Text);
        }

        //TODO change this
        private static void serializer_UnknownAttribute(object sender, XmlAttributeEventArgs e)
        {
            System.Xml.XmlAttribute attr = e.Attr;
            Console.WriteLine("Unknown attribute " +
            attr.Name + "='" + attr.Value + "'");
        }



    }





    //public static class DataFile
    //{
    //    //JsonConvert.SerializeObject();


    //    private const string DATA_FILE = "Data.xml";

    //    /*
    //    public static void SaveConfig(string fileName)
    //    {
    //        DataContractJsonSerializer ser;
    //        //ser.WriteObject(ms, rp);

    //        using (StreamWriter sw = new StreamWriter(fileName + ".json", false))
    //        {
    //            ser = new DataContractJsonSerializer(typeof(List<int>));
    //            ser.WriteObject(sw, Dobot.DwellTimes);
    //            // Write dwell time list
    //            // write arduinoInit
    //            // write layerheight
    //            // write high height
    //            // write points
    //        }

    //    }

    //    private static string Serialize*/


    //    /// <summary>
    //    /// Saves the supplied point to the XML file
    //    /// </summary>
    //    /// <param name="rPoint"></param>
    //    public static void SavePoints(Dictionary<string, RobotPoint> _points)
    //    {
    //        List<RobotPoint> robotPoints = new List<RobotPoint>(_points.Values);

    //        XmlSerializer serializer = new XmlSerializer(typeof(List<RobotPoint>));//RobotPoint));//(Dictionary<string,RobotPoint>));            
    //        using (StreamWriter writer = new StreamWriter(DATA_FILE, false))    //currently overwrites file instead of appending
    //        {

    //            serializer.Serialize(writer, robotPoints);
    //            //foreach (KeyValuePair<string, RobotPoint> entry in points) { 
    //            //    serializer.Serialize(writer, entry.Value);
    //            //}
    //            writer.Close();
    //        }
    //    }

    //    /// <summary>
    //    /// Reads all points from the XML file and loads them into memory
    //    /// </summary>
    //    public static Dictionary<string, RobotPoint> ReadPoints()
    //    {
    //        if (!File.Exists(DATA_FILE))
    //            Dobot.SeedPoints();
    //        List<RobotPoint> pointList = new List<RobotPoint>();
    //        //Dictionary<string, RobotPoint> pointList = new Dictionary<string, RobotPoint>();
    //        XmlSerializer serializer = new XmlSerializer(typeof(List<RobotPoint>));//typeof(Dictionary<string,RobotPoint>));

    //        // if the document has unknown nodes handle with UnknownNode and UnknownAttribute events.
    //        serializer.UnknownNode += new XmlNodeEventHandler(serializer_UnknownNode);
    //        serializer.UnknownAttribute += new XmlAttributeEventHandler(serializer_UnknownAttribute);

    //        Dictionary<string, RobotPoint> points = new Dictionary<string, RobotPoint>();
    //        //RobotPoint point;
    //        using (StreamReader sr = new StreamReader(DATA_FILE, false))
    //        {
    //            pointList = (List<RobotPoint>)serializer.Deserialize(sr);
    //            foreach (RobotPoint rp in pointList)
    //            {
    //                points.Add(rp.Name, rp);
    //            }
    //            //point = (RobotPoint)serializer.Deserialize(sr);
    //            sr.Close();
    //        }

    //        //FileStream fs = new FileStream(DATA_FILE, FileMode.Open);
    //        //RobotPoint point;
    //        //point = (RobotPoint)serializer.Deserialize(fs);

    //        return points;
    //    }

    //    //TODO change this
    //    private static void serializer_UnknownNode(object sender, XmlNodeEventArgs e)
    //    {
    //        Console.WriteLine("Unknown Node:" + e.Name + "\t" + e.Text);
    //    }

    //    //TODO change this
    //    private static void serializer_UnknownAttribute(object sender, XmlAttributeEventArgs e)
    //    {
    //        System.Xml.XmlAttribute attr = e.Attr;
    //        Console.WriteLine("Unknown attribute " +
    //        attr.Name + "='" + attr.Value + "'");
    //    }

    //}

}
