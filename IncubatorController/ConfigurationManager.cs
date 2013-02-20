using System;
using Microsoft.SPOT;
using System.Collections;
using System.IO;

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
    }
}
