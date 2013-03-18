using System;
using Microsoft.SPOT;
using System.IO;
using Microsoft.SPOT.IO;

namespace NetduinoPlus.Controler
{
    public sealed class LogFile
    {
        private enum ELogType
        {
            Application,
            Network,
            Sensor,
            Exception,
            Error
        }

        #region Private Variables
        private static readonly LogFile _instance = new LogFile();
        private static bool _sdCardAvailable = false;
        #endregion


        #region Constructors
        private LogFile() 
        {
            RemovableMedia.Insert += new InsertEventHandler(RemovableMedia_Insert);
            RemovableMedia.Eject += new EjectEventHandler(RemovableMedia_Eject);
        }
        #endregion


        #region Events
        #endregion


        #region Public Properties
        #endregion


        #region Public Methods
        private static void RemovableMedia_Insert(object sender, MediaEventArgs e)
        {
            _sdCardAvailable = true;
            Application("SD card detected.");
        }
        private static void RemovableMedia_Eject(object sender, MediaEventArgs e)
        {
            _sdCardAvailable = false;
            Debug.Print("SD card ejected.");
        }

        public static void Application(string log)
        {
            try
            {
                Debug.Print(DateTime.Now.ToString("T") + ";" + log);

                if (_sdCardAvailable)
                {
                    using (StreamWriter streamWriter = new StreamWriter(@"SD\ApplicationLog.txt", true))
                    {
                        streamWriter.WriteLine(DateTime.Now.ToString() + ": " + log.Trim());
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.ToString());
            }
        }

        public static void Network(string log)
        {
            try
            {
                Debug.Print(DateTime.Now.ToString("T") + ";" + log);

                if (_sdCardAvailable)
                {
                    using (StreamWriter streamWriter = new StreamWriter(@"SD\NetworkLog.txt", true))
                    {
                        streamWriter.WriteLine(DateTime.Now.ToString() + ": " + log.Trim());
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.ToString());
            }
        }

        public static void Sensor(string log)
        {
            try
            {
                Debug.Print(DateTime.Now.ToString("T") + ";" + log);

                if (_sdCardAvailable)
                {
                    using (StreamWriter streamWriter = new StreamWriter(@"SD\SensorLog.txt", true))
                    {
                        streamWriter.WriteLine(DateTime.Now.ToString() + ": " + log.Trim());
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.ToString());
            }
        }

        public static void Exception(string log)
        {
            try
            {
                Debug.Print(DateTime.Now.ToString("T") + ";" + log);

                if (_sdCardAvailable)
                {
                    using (StreamWriter streamWriter = new StreamWriter(@"SD\ExceptionLog.txt", true))
                    {
                        streamWriter.WriteLine(DateTime.Now.ToString() + ": " + log.Trim());
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.ToString());
            }
        }

        public static void Error(string log)
        {
            try
            {
                Debug.Print(DateTime.Now.ToString("T") + ";" + log);

                if (_sdCardAvailable)
                {
                    using (StreamWriter streamWriter = new StreamWriter(@"SD\ErrorLog.txt", true))
                    {
                        streamWriter.WriteLine(DateTime.Now.ToString() + ": " + log.Trim());
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.ToString());
            }
        }
        #endregion
    }
}
