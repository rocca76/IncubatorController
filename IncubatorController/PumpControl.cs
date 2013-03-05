using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;

namespace NetduinoPlus.Controler
{
    class PumpControl
    {
        const int RUNING_DURATION = 1;   // seconds
        const int WAITING_DURATION = 600; // 10 minutes

        public enum PumpStateEnum
        {
            Stopped,
            Running
        }

        #region Private Variables
        private static PumpControl _pumpControl = null;
        private TimeSpan _duration = TimeSpan.Zero;
        private DateTime _lastActivation;
        private PumpStateEnum _pumpState = PumpStateEnum.Stopped;

        private OutputPort outPump = new OutputPort(Pins.GPIO_PIN_D6, false);  //Pump
        #endregion

        #region Public Properties
        public PumpControl.PumpStateEnum PumpState
        {
            get { return _pumpState; }
        }

        public TimeSpan Duration
        {
            get { return _duration; }
        }
        #endregion


        #region Constructors
        #endregion


        #region Public Methods
        public static PumpControl GetInstance()
        {
            if (_pumpControl == null)
            {
                _pumpControl = new PumpControl();
            }

            return _pumpControl;
        }

        public void ManageState()
        {
            if (_duration > TimeSpan.Zero)
            {
                _duration = _duration.Subtract(new TimeSpan(0, 0, 1));
            }

            if (ProcessControl.GetInstance().CurrentRelativeHumidity > 0)
            {
              double temperatureDelta = Abs( ProcessControl.GetInstance().TargetTemperature - ProcessControl.GetInstance().CurrentTemperature );

              if ((ProcessControl.GetInstance().CurrentRelativeHumidity < ProcessControl.GetInstance().TargetRelativeHumidity) && (temperatureDelta <= 1))
              {
                if (_duration == TimeSpan.Zero)
                {
                  TimeSpan timeDelta = DateTime.Now.Subtract(_lastActivation);

                  if (_pumpState == PumpStateEnum.Stopped && timeDelta.Minutes > 1 )
                  {
                    _pumpState = PumpStateEnum.Running;
                    _duration = new TimeSpan(0, 0, RUNING_DURATION);
                    outPump.Write(true);

                    _lastActivation = DateTime.Now;
                  }
                  else if (_pumpState == PumpStateEnum.Running)
                  {
                    _pumpState = PumpStateEnum.Stopped;
                    _duration = new TimeSpan(0, 0, WAITING_DURATION);
                    outPump.Write(false);
                  }
                }
              }
              else
              {
                _pumpState = PumpStateEnum.Stopped;
                _duration = TimeSpan.Zero;
                outPump.Write(false);
              }
          }
          else
          {
              _pumpState = PumpStateEnum.Stopped;
              _duration = TimeSpan.Zero;
              outPump.Write(false);
          }
        }
        #endregion


        #region Private Methods
        private static double Abs(double value)
        {
          return (value >= 0) ? value : -value;
        }
        #endregion
    }
}
