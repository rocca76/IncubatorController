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
        private LogFile(){}
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

        public static void Init()
        {
            try
            {
                DirectoryInfo dirinfo = new DirectoryInfo(@"\SD");

                if (dirinfo.Exists)
                {
                  if (_sdCardAvailable == false)
                  {
                    Application("SD card detected.");
                    _sdCardAvailable = true;
                  }
                }
                else
                {
                  Application("SD card not detected.");
                }

                RemovableMedia.Insert += new InsertEventHandler(RemovableMedia_Insert);
                RemovableMedia.Eject += new EjectEventHandler(RemovableMedia_Eject);
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
              Debug.Print(DateTime.Now.ToString("T") + ";" + log);

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
