using System;
using System.Threading;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;
using IncubatorController.Utility;

//Smart Personal Object Technology

namespace NetduinoPlus.Controler
{
    public class Program
    {
        #region Private Variables
        private static Timer _processTimer = null;
        private static InterruptPort _onBoardButton = new InterruptPort(Pins.ONBOARD_SW1, false, Port.ResistorMode.Disabled, Port.InterruptMode.InterruptEdgeLow);
        #endregion

        #region Public Properties
        #endregion

        #region Events
        #endregion

        public static void Main()
        {
          NetworkCommunication.Instance.DetectAvailability();

          _onBoardButton.OnInterrupt += new NativeEventHandler(button_OnInterrupt);
          _processTimer = new Timer(new TimerCallback(OnProcessTimer), null, 0, 1000);

          Thread.Sleep(Timeout.Infinite);
        }

        private static void button_OnInterrupt(uint data1, uint data2, DateTime time)
        {
            PowerState.RebootDevice(true);
        }

        private static void OnProcessTimer(object state)
        {
          try
          {
            ProcessControl.Instance.ProcessData();
          }
          catch (Exception ex)
          {
              LogFile.Exception(ex.ToString());
          }
        }
    }
}
