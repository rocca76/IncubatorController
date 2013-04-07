using System;
using System.Threading;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;
using IncubatorController.Utility;

//Smart Personal Object Technology

namespace NetduinoPlus.Controler
{
    public class Program
    {
        #region Private Variables
        //private static Timer _processTimer = null;
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
          //_processTimer = new Timer(new TimerCallback(OnProcessTimer), null, 0, 1000);

          Thread processThread = new Thread(new ThreadStart(ProcessThread));
          processThread.Priority = ThreadPriority.AboveNormal;
          processThread.Start();

          Thread.Sleep(Timeout.Infinite);
        }

        private static void button_OnInterrupt(uint data1, uint data2, DateTime time)
        {
            PowerState.RebootDevice(true);
        }

        private static void ProcessThread()
        {
            while (true)
            {
                try
                {
                    Stopwatch stopwatch = Stopwatch.StartNew();

                    ProcessControl.Instance.ProcessData();

                    stopwatch.Stop();

                    if (stopwatch.ElapsedMilliseconds > 1000)
                    {
                        LogFile.Application("ProcessData duration: " + stopwatch.ElapsedMilliseconds.ToString() + "ms");
                    }
                    else
                    {
                        int timeDiff = 1000 - (int)stopwatch.ElapsedMilliseconds;
                        Thread.Sleep(timeDiff);
                    }
                }
                catch (Exception ex)
                {
                    LogFile.Exception(ex.ToString());
                }
            }
        }

        private static void OnProcessTimer(object state)
        {
          /*try
          {
            Stopwatch stopwatch = Stopwatch.StartNew();

            ProcessControl.Instance.ProcessData();

            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds > 1000)
            {
                LogFile.Application("ProcessData duration: " + stopwatch.ElapsedMilliseconds.ToString() + "ms");
            }
          }
          catch (Exception ex)
          {
              LogFile.Exception(ex.ToString());
          }*/
        }
    }
}
