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
        #endregion


        #region Constructors
        private LogFile() {}
        #endregion


        #region Events
        #endregion


        #region Public Properties
        #endregion


        #region Public Methods
        public static void Application(string log)
        {
            try
            {
                Debug.Print(DateTime.Now.ToString("T") + ";" + log);

                if (ConfigFile.Instance.SDCardAvailable)
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

                if (ConfigFile.Instance.SDCardAvailable)
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

                if (ConfigFile.Instance.SDCardAvailable)
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

                if (ConfigFile.Instance.SDCardAvailable)
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

                if (ConfigFile.Instance.SDCardAvailable)
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
