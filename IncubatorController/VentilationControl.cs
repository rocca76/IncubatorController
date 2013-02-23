using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;

namespace NetduinoPlus.Controler
{
    class VentilationControl
    {
        const int RUNING_DURATION = 4; //seconds
        const int WAITING_DURATION = 30;//7200; //seconds

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
            bool openTrap = false;

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
                    openTrap = true;
                }
                else
                {
                    _fan = FanStateEnum.Stopped;
                    outFan.Write(false);
                    openTrap = false;
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
                        openTrap = true;
                    }
                    else if (_fan == FanStateEnum.Running)
                    {
                        _fan = FanStateEnum.Stopped;
                        _duration = new TimeSpan(0, 0, WAITING_DURATION);
                        outFan.Write(false);
                        openTrap = false;
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
                if (openTrap)
                {
                    _trap = TrapStateEnum.Opened;
                }
                else
                {
                    _trap = TrapStateEnum.Closed;
                }

                outTrap.Write(openTrap);
            }
        }
        #endregion


        #region Private Methods
        #endregion
    }
}
