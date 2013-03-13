using System;
using System.Threading;
using System.Diagnostics;

//Smart Personal Object Technology

namespace NetduinoPlus.Controler
{
    public class Program
    {
        #region Private Variables
        #endregion

        #region Public Properties
        #endregion

        #region Events
        #endregion

        public static void Main()
        {
            new Timer(new TimerCallback(OnProcessTimer), null, 0, 1000);
            Thread.Sleep(Timeout.Infinite);
        }

        private void Run()
        {
            //LogFile.GetInstance().Initialize();
            //ProcessControl.Instance.LoadConfiguration();
        }

        private static void OnProcessTimer(object state)
        {
          try
          {
            Stopwatch stopwatch = Stopwatch.StartNew();
            //ProcessControl.Instance.ProcessData();            }
            stopwatch.Stop();

            LogFile.Application("Process duration: " + stopwatch.ElapsedMilliseconds.ToString() + "ms");
          }
          catch (Exception ex)
          {
              LogFile.Exception(ex.ToString());
          }
        }
    }
}
