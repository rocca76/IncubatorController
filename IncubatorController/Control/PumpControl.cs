using System;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;

namespace NetduinoPlus.Controler
{
    public sealed class PumpControl
    {
        public enum PumpStateEnum
        {
            Stopped,
            Running
        }

        #region Private Variables
        private static PumpControl _instance = new PumpControl();
        private TimeSpan _duration = TimeSpan.Zero;
        private PumpStateEnum _pumpState = PumpStateEnum.Stopped;
        private int _intervalTargetMinutes = 0;
        private int _durationTargetSeconds = 0;
        private bool _forcePump = false;
        private OutputPort _outPump = new OutputPort(Pins.GPIO_PIN_D6, false);
        #endregion

        #region Public Properties
        public static PumpControl Instance
        {
            get { return _instance; }
        }

        public PumpControl.PumpStateEnum PumpState
        {
            get { return _pumpState; }
            set { _pumpState = value; }
        }

        public TimeSpan Duration
        {
            get { return _duration; }
            set { _duration = value; }
        }

        public int IntervalTargetMinutes
        {
          get { return _intervalTargetMinutes; }
          set { _intervalTargetMinutes = value; }
        }

        public int DurationTargetSeconds
        {
          get { return _durationTargetSeconds; }
          set { _durationTargetSeconds = value; }
        }
        #endregion


        #region Constructors
        private PumpControl() { }
        #endregion


        #region Public Methods
        public void ManageState()
        {
            if (_forcePump == false)
            {
                if (_duration > TimeSpan.Zero)
                {
                    _duration = _duration.Subtract(new TimeSpan(0, 0, 1));
                }

                if ((ProcessControl.Instance.RelativeHumidity > 0) &&
                    (ProcessControl.Instance.RelativeHumidity < ProcessControl.Instance.TargetRelativeHumidity))
                {
                    if (_duration == TimeSpan.Zero)
                    {
                        if (_pumpState == PumpStateEnum.Stopped && IsValidDelay())
                        {
                            _pumpState = PumpStateEnum.Running;
                            _duration = new TimeSpan(0, 0, _durationTargetSeconds);
                        }
                        else if (_pumpState == PumpStateEnum.Running && IsValidDelay())
                        {
                            _pumpState = PumpStateEnum.Stopped;
                            _duration = new TimeSpan(0, _intervalTargetMinutes, 0);
                        }
                    }
                }
                else
                {
                    _pumpState = PumpStateEnum.Stopped;
                    _duration = TimeSpan.Zero;
                }

                SetOutputState();
            }
        }

        public void Activate(int activate)
        {
            _duration = TimeSpan.Zero;
            _forcePump = activate == 1;

            if (_forcePump)
            {
                _pumpState = PumpControl.PumpStateEnum.Running;
                _outPump.Write(true);
            }
            else if (_forcePump)
            {
                _pumpState = PumpControl.PumpStateEnum.Stopped;
                _outPump.Write(false);
            }
        }
        #endregion


        #region Private Methods
        private void SetOutputState()
        {
          if (_pumpState == PumpStateEnum.Stopped)
          {
            _outPump.Write(false);
          }
          else if (_pumpState == PumpStateEnum.Running)
          {
            _outPump.Write(true);
          }
        }

        private bool IsValidDelay()
        {
          return _intervalTargetMinutes > 0 && _durationTargetSeconds > 0;
        }
        #endregion
    }
}
