using System;
using Microsoft.SPOT;
using System.IO;
using Microsoft.SPOT.IO;

namespace NetduinoPlus.Controler
{
    public class LogFile
    {
        private enum ELogType
        {
            Application,
            Network,
            Exception,
            Error
        }

        #region Private Variables
        private static LogFile _instance = null;
        private static bool _sdCardAvailable = false;
        private static readonly object LockObject = new object();
        #endregion


        #region Constructors
        private LogFile() { }
        #endregion


        #region Events
        #endregion


        #region Public Properties
        public bool SDCardAvailable
        {
            get { return _sdCardAvailable; }
        }
        #endregion


        #region Public Static Methods
        public static void InitInstance()
        {
            if (_instance == null)
            {
                _instance = new LogFile();

                DirectoryInfo dirinfo = new DirectoryInfo(@"\SD");
                if (dirinfo.Exists)
                {
                    _sdCardAvailable = true;

                    Application("SD Detected");
                }
                else
                {
                    Debug.Print("SD Not Detected");
                }

                RemovableMedia.Insert += new InsertEventHandler(RemovableMedia_Insert);
                RemovableMedia.Eject += new EjectEventHandler(RemovableMedia_Eject);
            }
        }

        private static void RemovableMedia_Insert(object sender, MediaEventArgs e)
        {
            _sdCardAvailable = true;
            LogFile.Application("SD card inserted.");
        }
        private static void RemovableMedia_Eject(object sender, MediaEventArgs e)
        {
            _sdCardAvailable = false;
            Debug.Print("SD card ejected.");
        }

        public static void Application(string log)
        {
            Log(ELogType.Application, log);
        }

        public static void Network(string log)
        {
            Log(ELogType.Network, log);
        }

        public static void Exception(string log)
        {
            Log(ELogType.Exception, log);
        }

        public static void Error(string log)
        {
            Log(ELogType.Error, log);
        }

        private static void Log(ELogType type, string log)
        {
            try
            {
                lock (LockObject)
                {
                    Debug.Print(log);

                    if (_sdCardAvailable)
                    {
                        String path = @"SD\";

                        switch (type)
                        {
                            case ELogType.Application:
                                path = @"SD\ApplicationLog.txt";
                                break;
                            case ELogType.Network:
                                path = @"SD\NetworkLog.txt";
                                break;
                            case ELogType.Exception:
                                path = @"SD\Exceptionlog.txt";
                                break;
                            case ELogType.Error:
                                path = @"SD\Errorlog.txt";
                                break;
                        }

                        using (StreamWriter streamWriter = new StreamWriter(path, true))
                        {
                            streamWriter.WriteLine(DateTime.Now.ToString() + ": " + log.Trim());
                        }
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
