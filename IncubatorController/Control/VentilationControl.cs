using System;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;

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
        #endregion


        #region Constructors
        private VentilationControl() { }
        #endregion


        #region Public Methods
        public void ManageState()
        {
            if (ProcessControl.Instance.CO2 > 0)
            {
                if (_ventilationState == VentilationState.Stopped)
                {
                    if (ProcessControl.Instance.CO2 > (ProcessControl.Instance.TargetCO2 + CO2_TOLERANCE))
                    {
                        _ventilationState = VentilationState.Started;
                        _trapState = TrapStateEnum.Opened;
                        _fanState = FanStateEnum.Running;
                    }
                }
                else if (_ventilationState == VentilationState.Started)
                {
                    if (ProcessControl.Instance.CO2 <= ProcessControl.Instance.TargetCO2)
                    {
                        _ventilationState = VentilationState.Stopped;
                        _trapState = TrapStateEnum.Closed;
                        _fanState = FanStateEnum.Stopped;
                    }
                }
            }
            else
            {
                _ventilationState = VentilationState.Stopped;
                _trapState = TrapStateEnum.Closed;
                _fanState = FanStateEnum.Stopped;
            }

            if (ProcessControl.Instance.RelativeHumidity > 0)
            {
                if (_trapState == TrapStateEnum.Closed)
                {
                    if (ProcessControl.Instance.RelativeHumidity >= (ProcessControl.Instance.TargetRelativeHumidity + RELATIVE_HUMIDITY_TRAP_DELTA))
                    {
                        _trapState = TrapStateEnum.Opened;
                    }
                }
                else if (_trapState == TrapStateEnum.Opened)
                {
                    if ((ProcessControl.Instance.RelativeHumidity < ProcessControl.Instance.TargetRelativeHumidity) &&
                        (_ventilationState == VentilationState.Stopped))
                    {
                        _trapState = TrapStateEnum.Closed;
                    }
                }

                if (_fanState == FanStateEnum.Stopped)
                {
                    if (ProcessControl.Instance.RelativeHumidity >= (ProcessControl.Instance.TargetRelativeHumidity + RELATIVE_HUMIDITY_FAN_DELTA))
                    {
                        _fanState = FanStateEnum.Running;
                    }
                }
                else if (_fanState == FanStateEnum.Running)
                {
                    if ((ProcessControl.Instance.RelativeHumidity < (ProcessControl.Instance.TargetRelativeHumidity + RELATIVE_HUMIDITY_TRAP_DELTA)) &&
                        (_ventilationState == VentilationState.Stopped))
                    {
                        _fanState = FanStateEnum.Stopped;
                    }
                }
            }

            SetOutputState();
        }
        #endregion


        #region Private Methods
        private void SetOutputState()
        {
            if (_ventilationState == VentilationState.Stopped)
            {
                outTrap.Write(false);
                outFan.Write(false);
            }
            else if (_ventilationState == VentilationState.Started)
            {
                outTrap.Write(true);
                outFan.Write(true);
            }
        }
        #endregion
    }
}
