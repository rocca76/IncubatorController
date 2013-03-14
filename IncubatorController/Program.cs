using System;
using System.Threading;

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

        private static void OnProcessTimer(object state)
        {
          try
          {
            Stopwatch stopwatch = Stopwatch.StartNew();
            ProcessControl.Instance.ProcessData();
            stopwatch.Stop();

            LogFile.Application("Process data duration: " + stopwatch.ElapsedMilliseconds.ToString() + "ms");
          }
          catch (Exception ex)
          {
              LogFile.Exception(ex.ToString());
          }
        }
    }
}
