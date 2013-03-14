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
            Exception,
            Error
        }

        #region Private Variables
        private static readonly LogFile _instance = new LogFile();
        private static readonly object _lockObject = new object();
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
        private void RemovableMedia_Insert(object sender, MediaEventArgs e)
        {
            _sdCardAvailable = true;
            Application("SD card detected.");
        }
        private void RemovableMedia_Eject(object sender, MediaEventArgs e)
        {
            _sdCardAvailable = false;
            Debug.Print("SD card ejected.");
        }

        public static void DetectSDCardDirectory(String path)
        {
            try
            {
                DirectoryInfo dirinfo = new DirectoryInfo(path);
                if (dirinfo.Exists)
                {
                    _sdCardAvailable = true;
                }
            }
            catch (Exception ex)
            {
                LogFile.Exception(ex.ToString());
            }
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

                  lock(_lockObject)
                  {
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
