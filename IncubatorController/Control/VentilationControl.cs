using System;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;

namespace NetduinoPlus.Controler
{
    public sealed class VentilationControl
    {
        const int CO2_TOLERANCE = 300;
        const double RELATIVE_HUMIDITY_TRAP_DELTA = 2.0;
        const double RELATIVE_HUMIDITY_FAN_DELTA = 6.0;

        public enum FanStateEnum
        {
            Stopped,
            Running
        }

        public enum TrapStateEnum
        {
            Closed,
            Opened
        }

        public enum VentilationState
        {
            Stopped,
            Started
        }

        #region Private Variables
        private static VentilationControl _instance = new VentilationControl();
        private VentilationState _ventilationState = VentilationState.Stopped;
        private FanStateEnum _fanState = FanStateEnum.Stopped;
        private TrapStateEnum _trapState = TrapStateEnum.Closed;
        private int _intervalTargetMinutes = 0;
        private int _durationTargetSeconds = 0;
        private TimeSpan _duration = TimeSpan.Zero;
        private bool _toggleVentilationState = false;
        private bool _ventilationStandby = false;

        private OutputPort outFan = new OutputPort(Pins.GPIO_PIN_D9, false);
        private OutputPort outTrap = new OutputPort(Pins.GPIO_PIN_D10, false);
        #endregion


        #region Public Properties
        public static VentilationControl Instance
        {
            get { return _instance; }
        }

        public VentilationControl.TrapStateEnum TrapState
        {
            get { return _trapState; }
        }

        public VentilationControl.FanStateEnum FanState
        {
            get { return _fanState; }
        }

        public VentilationControl.VentilationState State
        {
            get { return _ventilationState; }
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

        public TimeSpan Duration
        {
            get { return _duration; }
        }

        public bool Standby
        {
            get { return _ventilationStandby; }
        }
        #endregion


        #region Constructors
        private VentilationControl() { }
        #endregion


        #region Public Methods
        public void ManageState()
        {
            if (_duration > TimeSpan.Zero)
            {
                _duration = _duration.Subtract(new TimeSpan(0, 0, 1));
            }

            if (ProcessControl.Instance.CO2 > 0)
            {
                if (_ventilationState == VentilationState.Stopped)
                {
                    if (ProcessControl.Instance.CO2 > (ProcessControl.Instance.TargetCO2 + CO2_TOLERANCE))
                    {
                        if (ProcessControl.Instance.TargetCO2 > 0)
                        {
                            _ventilationState = VentilationState.Started;
                            _trapState = TrapStateEnum.Opened;
                            _fanState = FanStateEnum.Running;
                        }
                    }
                }
                else if (_ventilationState == VentilationState.Started)
                {
                    if (ProcessControl.Instance.CO2 <= ProcessControl.Instance.TargetCO2)
                    {
                        _ventilationState = VentilationState.Stopped;
                        _duration = TimeSpan.Zero;
                        _toggleVentilationState = false;
                        _ventilationStandby = false;
                    }
                }
            }
            else
            {
                _ventilationState = VentilationState.Stopped;
                _duration = TimeSpan.Zero;
                _toggleVentilationState = false;
                _ventilationStandby = false;
            }

            //////////////////////////////////////////////////////////////////

            if (_ventilationState == VentilationState.Started)
            {
                if (_duration == TimeSpan.Zero)
                {
                    if (_toggleVentilationState)
                    {
                        _duration = new TimeSpan(0, 0, _intervalTargetMinutes);
                        _ventilationStandby = true;

                        _trapState = TrapStateEnum.Closed;
                        _fanState = FanStateEnum.Stopped;
                    }
                    else
                    {
                        _duration = new TimeSpan(0, 0, _durationTargetSeconds);
                        _ventilationStandby = false;
                    }

                    _toggleVentilationState = !_toggleVentilationState;
                }
            }

            //////////////////////////////////////////////////////////////////

            if (ProcessControl.Instance.RelativeHumidity > 0)
            {
                if (_trapState == TrapStateEnum.Closed)
                {
                    if (ProcessControl.Instance.RelativeHumidity >= (ProcessControl.Instance.TargetRelativeHumidity + RELATIVE_HUMIDITY_TRAP_DELTA))
                    {
                        if (ProcessControl.Instance.TargetRelativeHumidity > 0 || (_ventilationState == VentilationState.Started && _ventilationStandby == false))
                        {
                            _trapState = TrapStateEnum.Opened;
                        }
                    }
                    else if (_ventilationState == VentilationState.Started && _ventilationStandby == false)
                    {
                        _trapState = TrapStateEnum.Opened;
                    }
                }
                else if (_trapState == TrapStateEnum.Opened)
                {
                    if (_ventilationState != VentilationState.Started)
                    {
                        if ((ProcessControl.Instance.RelativeHumidity < ProcessControl.Instance.TargetRelativeHumidity)
                            || ProcessControl.Instance.TargetRelativeHumidity == 0)
                        {
                            _trapState = TrapStateEnum.Closed;
                        }
                    }
                }


                if (_fanState == FanStateEnum.Stopped)
                {
                    if (ProcessControl.Instance.RelativeHumidity >= (ProcessControl.Instance.TargetRelativeHumidity + RELATIVE_HUMIDITY_FAN_DELTA))
                    {
                        if (ProcessControl.Instance.TargetRelativeHumidity > 0 || (_ventilationState == VentilationState.Started && _ventilationStandby == false))
                        {
                            _fanState = FanStateEnum.Running;
                        }
                    }
                    else if (_ventilationState == VentilationState.Started && _ventilationStandby == false)
                    {
                        _fanState = FanStateEnum.Running;                            
                    }
                }
                else if (_fanState == FanStateEnum.Running)
                {
                    if (_ventilationState != VentilationState.Started)
                    {
                        if ((ProcessControl.Instance.RelativeHumidity < (ProcessControl.Instance.TargetRelativeHumidity + RELATIVE_HUMIDITY_TRAP_DELTA))
                            || ProcessControl.Instance.TargetRelativeHumidity == 0)
                        {
                            _fanState = FanStateEnum.Stopped;
                        }
                    }
                }
            }
            else
            {
                _trapState = TrapStateEnum.Closed;
                _fanState = FanStateEnum.Stopped;
            }

            if (ProcessControl.Instance.TargetCO2 == 0)
            {
                _ventilationState = VentilationState.Stopped;
                _duration = TimeSpan.Zero;
                _toggleVentilationState = false;
                _ventilationStandby = false;
            }

            if (ProcessControl.Instance.TargetRelativeHumidity == 0 && ProcessControl.Instance.TargetCO2 == 0)
            {
                _trapState = TrapStateEnum.Closed;
                _fanState = FanStateEnum.Stopped;
            }

            if (ProcessControl.Instance.Temperature > ProcessControl.Instance.TemperatureMax)
            {
                _trapState = TrapStateEnum.Opened;
            }

            SetOutputState();
        }
        #endregion


        #region Private Methods
        private void SetOutputState()
        {
            if (_trapState == TrapStateEnum.Opened)
            {
                outTrap.Write(true);
            }
            else
            {
                outTrap.Write(false);
            }

            if (_fanState == FanStateEnum.Running)
            {
                outFan.Write(true);
            }
            else
            {
                outFan.Write(false);
            }
        }

        private bool IsValidTarget()
        {
            return ProcessControl.Instance.TargetRelativeHumidity > 0 && ProcessControl.Instance.TargetCO2 > 0;
        }

        private bool IsValidDelay()
        {
            return _intervalTargetMinutes > 0 && _durationTargetSeconds > 0;
        }
        #endregion
    }
}
