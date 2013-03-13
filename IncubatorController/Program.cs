using System;
using System.Threading;
using System.Diagnostics;

//Smart Personal Object Technology

namespace NetduinoPlus.Controler
{
    public class Program
    {
        #region Private Variables
        private static Timer _processTimer = null;
        #endregion

        #region Public Properties
        #endregion

        #region Events
        #endregion

        public static void Main()
        {
            new Program().Run();
            Thread.Sleep(Timeout.Infinite);
        }

        private void Run()
        {
            LogFile.GetInstance().Initialize();
            ProcessControl.GetInstance().LoadConfiguration();

            _processTimer = new Timer(new TimerCallback(OnProcessTimer), null, 0, 1000);
        }

        private void OnProcessTimer(object state)
        {
          try
          {
            Stopwatch stopwatch = Stopwatch.StartNew();

            ProcessControl.GetInstance().ProcessData();
            NetworkCommunication.Instance.NotifySender();

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
