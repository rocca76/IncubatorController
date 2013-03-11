using System;
using Microsoft.SPOT;
using System.Collections;
using System.IO;
using System.Text;

namespace NetduinoPlus.Controler
{
    public static class ConfigurationManager
    {
        private const string APPSETTINGS_SECTION = "appSettings";
        private const string ADD = "add";
        private const string KEY = "key";
        private const string VALUE = "value";

        private static String rootDir = @"\SD\";
        private static Hashtable appSettings;

        static ConfigurationManager()
        {
            appSettings = new Hashtable();
        }

        public static string GetAppSetting(string key)
        {
            return GetAppSetting(key, null);
        }

        public static string GetAppSetting(string key, string defaultValue)
        {
            if (!appSettings.Contains(key))
            {
                return defaultValue;
            }

            return (string)appSettings[key];
        }

        public static void Load()
        {
            //WriteFileDictionaryEntry();

            //ReadFile();

            //const string filePath = @"SD\\Incubator.config";
            //using (Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            //{

            //}

            //appSettings.Add(key, value);
        }

        public static string ReadFile(String sFilename)
        {
            string content = "";

            if (File.Exists(rootDir + sFilename))
            {
                StreamReader myFile = new StreamReader(new FileStream(rootDir + sFilename, FileMode.Open, FileAccess.Read));
                content = myFile.ReadToEnd();
                myFile.Close();
            }
            return content;
        }

        private static void WriteFileDictionaryEntry()
        {
            Hashtable ht = new Hashtable();
            ht.Add("TemperatureTarget", 37.2); // key, value

            //(string)ht["A"];

            foreach (DictionaryEntry de in ht)
            {
                if ((string)de.Key == "TemperatureTarget")
                {
                    double v = (double)de.Value;
                }

            }


            String[] lines = { "First line", "Second line", "Third line" };

            using (StreamWriter file = new StreamWriter(@"SD\IncubateurTarget.txt"))
            {
                foreach (String line in lines)
                {
                    file.WriteLine(line);
                }
            }

            //string[] parts = message.Split(' ');

            StringBuilder data = new StringBuilder();
            data.Append(DateTime.Now.ToString());
            data.Append(";");
            data.Append(ProcessControl.GetInstance().TargetTemperature.ToString("F2"));
            data.Append(";");
            data.Append(ProcessControl.GetInstance().TargetRelativeHumidity.ToString("F2"));
            data.Append(";");
            data.Append(ProcessControl.GetInstance().TargetCO2.ToString());
        }
    }
}
