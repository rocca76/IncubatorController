using System;
using System.Threading;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;
using System.Text;
using System.Net.Sockets;
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
            LogFile.InitInstance();
            ProcessControl.GetInstance().LoadConfiguration();
            NetworkCommunication.InitInstance();

            _processTimer = new Timer(new TimerCallback(OnProcessTimer), null, 0, 1000);
        }

        private void OnProcessTimer(object state)
        {
          try
          {
            Stopwatch stopwatch = Stopwatch.StartNew();

            ProcessControl.GetInstance().ProcessData();
            NetworkCommunication.GetInstance().NotifySender();

            stopwatch.Stop();
            LogFile.Network("Process duration: " + stopwatch.ElapsedMilliseconds.ToString() + "ms");
          }
          catch (Exception ex)
          {
              LogFile.Exception(ex.ToString());
          }
        }
    }
}
