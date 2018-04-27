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

        private static string localAppDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        private static string appFolder = Path.Combine(localAppDataFolder, "DobotConsoleControl");

        public static void InitializeDirectory()
        {
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }
            string pointsDestination = Path.Combine(appFolder, defaultPoints + ".xml");

            string configDestination = Path.Combine(appFolder, defaultConfig + ".xml");

            string pointsSource = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, defaultPoints + ".xml");
            string configSource = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, defaultConfig + ".xml");

            if (!File.Exists(pointsDestination))
                File.Copy(pointsSource, pointsDestination);

            if (!File.Exists(configDestination))
                File.Copy(configSource, configDestination);
        }

        public static void SaveConfig(string name)
        {

            CryoForgeSettings cf = new CryoForgeSettings();
            //cf.points = Dobot.GetPoints();
            cf.dwellTimes = Dobot.DwellTimes;
            cf.clearHeight = RobotPoint.SafeHeight;
            cf.layerHeights = Dobot.LayerHeights;
            cf.arduinoOffset = ArduinoComm.zInit;

            string filePath = Path.Combine(appFolder, name + ".xml");

            using (StreamWriter sw = new StreamWriter(filePath, false))
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
            string filePath = Path.Combine(appFolder, name + ".xml");

            //name = name + ".xml";

            if (!File.Exists(filePath))//name))
            {
                PrgConsole.Error("The file specified does not exist, make sure you only entered the name and not the file extension");
                return;
            }
            
            using (StreamReader sr = new StreamReader(filePath))
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
            //name = name + ".xml";
            string filePath = Path.Combine(appFolder, name + ".xml");

            List<RobotPoint> robotPoints = new List<RobotPoint>(_points.Values);

            XmlSerializer serializer = new XmlSerializer(typeof(List<RobotPoint>));            
            using (StreamWriter writer = new StreamWriter(filePath, false))    //currently overwrites file instead of appending
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
            string filePath = Path.Combine(appFolder, name + ".xml");
            //name = name + ".xml";
            if (!File.Exists(filePath))
                Dobot.SeedPoints();
            List<RobotPoint> pointList = new List<RobotPoint>();

            XmlSerializer serializer = new XmlSerializer(typeof(List<RobotPoint>));

            // if the document has unknown nodes handle with UnknownNode and UnknownAttribute events.
            serializer.UnknownNode += new XmlNodeEventHandler(serializer_UnknownNode);
            serializer.UnknownAttribute += new XmlAttributeEventHandler(serializer_UnknownAttribute);

            Dictionary<string, RobotPoint> points = new Dictionary<string, RobotPoint>();
            //RobotPoint point;
            using (StreamReader sr = new StreamReader(filePath, false))
            {
                pointList = (List<RobotPoint>)serializer.Deserialize(sr);
                foreach (RobotPoint rp in pointList)
                {
                    points[rp.Name] = rp;
                    Dobot.AddPoint(rp);
                }
                
                sr.Close();
            }

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

}
