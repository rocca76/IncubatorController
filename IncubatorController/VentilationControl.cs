using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;

namespace NetduinoPlus.Controler
{
    class VentilationControl
    {
        const int RUNING_DURATION = 5;   // 300 seconds (5min)
        const int WAITING_DURATION = 10; // 21600 seconds (6hr)

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

        #region Private Variables
        private static VentilationControl _ventilationControl = null;
        private TimeSpan _duration = TimeSpan.Zero;
        private FanStateEnum _fan = FanStateEnum.Stopped;
        private TrapStateEnum _trap = TrapStateEnum.Closed;
        private bool _openTrap = false;

        private OutputPort outFan = new OutputPort(Pins.GPIO_PIN_D9, false);   //Fan
        private OutputPort outTrap = new OutputPort(Pins.GPIO_PIN_D10, false); //Trap
        #endregion


        #region Public Properties
        public VentilationControl.FanStateEnum FanState
        {
            get  { return _fan; }
        }

        public TimeSpan Duration
        {
            get { return _duration; }
        }

        public VentilationControl.TrapStateEnum TrapState
        {
            get { return _trap; }
        }
        #endregion


        #region Constructors
        #endregion


        #region Public Methods
        public static VentilationControl GetInstance()
        {
            if (_ventilationControl == null)
            {
                _ventilationControl = new VentilationControl();
            }

            return _ventilationControl;
        }

        public void ManageState()
        {
            if (_duration > TimeSpan.Zero)
            {
                _duration = _duration.Subtract(new TimeSpan(0, 0, 1));
            }

            if (ProcessControl.GetInstance().CurrentCO2 > 0)
            {
                //Sensor control
                if (ProcessControl.GetInstance().CurrentCO2 > ProcessControl.GetInstance().TargetCO2)
                {
                    _fan = FanStateEnum.Running;
                    outFan.Write(true);
                    _openTrap = true;
                }
                else
                {
                    _fan = FanStateEnum.Stopped;
                    outFan.Write(false);
                    _openTrap = false;
                }
            }
            else
            {
                //Sequence control
                if (_duration == TimeSpan.Zero)
                {
                    if (_fan == FanStateEnum.Stopped)
                    {
                        _fan = FanStateEnum.Running;
                        _duration = new TimeSpan(0, 0, RUNING_DURATION);
                        outFan.Write(true);
                        _openTrap = true;
                    }
                    else if (_fan == FanStateEnum.Running)
                    {
                        _fan = FanStateEnum.Stopped;
                        _duration = new TimeSpan(0, 0, WAITING_DURATION);
                        outFan.Write(false);
                        _openTrap = false;
                    }
                }
            }

            if (ProcessControl.GetInstance().MaxTemperatureLimitReached == 1)
            {
                _trap = TrapStateEnum.Opened;
                outTrap.Write(true);
            }
            else
            {
                if (_openTrap && _trap == TrapStateEnum.Closed)
                {
                    _trap = TrapStateEnum.Opened;
                    outTrap.Write(true);
                }
                else if (_openTrap == false && _trap == TrapStateEnum.Opened)
                {
                    _trap = TrapStateEnum.Closed;
                    outTrap.Write(false);
                }
            }
        }
        #endregion


        #region Private Methods
        #endregion
    }
}
