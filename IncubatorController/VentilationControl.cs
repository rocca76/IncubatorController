using System;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;

namespace NetduinoPlus.Controler
{
    class VentilationControl
    {
        const double RELATIVE_HUMIDITY_TRAP_DELTA = 3.0;
        const double RELATIVE_HUMIDITY_FAN_DELTA = 10.0;

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
        private static VentilationControl _instance = null;
        private static readonly object LockObject = new object();
        private TimeSpan _duration = TimeSpan.Zero;
        private VentilationState _ventilationState = VentilationState.Stopped;
        private FanStateEnum _fanState = FanStateEnum.Stopped;
        private TrapStateEnum _trapState = TrapStateEnum.Closed;
        private bool _openTrap = false;
        private bool _startFan = false;
        private int _fanEnabled = 0;
        private int _intervalTargetMinutes = 0;
        private int _durationTargetSeconds = 0;
        private bool _fanForced = false;

        private OutputPort outFan = new OutputPort(Pins.GPIO_PIN_D9, false);   //Fan
        private OutputPort outTrap = new OutputPort(Pins.GPIO_PIN_D10, false); //Trap
        #endregion


        #region Public Properties
        public VentilationControl.FanStateEnum FanState
        {
            get { return _fanState; }
        }

        public TimeSpan Duration
        {
            get { return _duration; }
        }

        public VentilationControl.TrapStateEnum TrapState
        {
            get { return _trapState; }
        }

        public VentilationControl.VentilationState State
        {
            get { return _ventilationState; }
        }

        public int FanEnabled
        {
            get { return _fanEnabled; }
            set { _fanEnabled = value; }
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
        #endregion


        #region Public Methods
        public static VentilationControl GetInstance()
        {
            lock (LockObject)
            {
                if (_instance == null)
                {
                    _instance = new VentilationControl();
                }

                return _instance;
            }
        }

        public void ManageState()
        {
            if (_duration > TimeSpan.Zero)
            {
                _duration = _duration.Subtract(new TimeSpan(0, 0, 1));
            }

            if (ProcessControl.GetInstance().CurrentCO2 > 0 && ProcessControl.GetInstance().TargetCO2 != ProcessControl.CO2_DISABLE)
            {
                //Sensor control
                if (ProcessControl.GetInstance().CurrentCO2 > ProcessControl.GetInstance().TargetCO2)
                {
                    if (_fanEnabled == 1)
                    {
                        _startFan = true;
                    }

                    _openTrap = true;
                }
                else
                {
                    _startFan = false;
                    _openTrap = false;
                }
            }
            else
            {
                //Sequence control
                if (_duration == TimeSpan.Zero)
                {
                    if (_ventilationState == VentilationState.Stopped && _intervalTargetMinutes > 0 && _durationTargetSeconds > 0)
                    {
                        _ventilationState = VentilationState.Started;
                        _duration = new TimeSpan(0, 0, _durationTargetSeconds);

                        _openTrap = true;

                        if (_fanEnabled == 1)
                        {
                            _startFan = true;
                        }
                    }
                    else if (_ventilationState == VentilationState.Started && _intervalTargetMinutes > 0 && _durationTargetSeconds > 0)
                    {
                        _ventilationState = VentilationState.Stopped;
                        _duration = new TimeSpan(0, IntervalTargetMinutes, 0);

                        _openTrap = false;
                        _startFan = false;
                    }
                }

                if (_intervalTargetMinutes == 0 && _durationTargetSeconds == 0)
                {
                    _ventilationState = VentilationState.Stopped;

                    _duration = TimeSpan.Zero;
                    _startFan = false;
                    _openTrap = false;
                }
            }


            ///////////////////////////////
            // Protection by trap

            bool openTrapForced = false;

            if (ProcessControl.GetInstance().MaxTemperatureLimitReached == 1)
            {
                openTrapForced = true;
            }

            double limitMax = ProcessControl.GetInstance().TargetRelativeHumidity + RELATIVE_HUMIDITY_TRAP_DELTA;
            if ( (ProcessControl.GetInstance().CurrentRelativeHumidity > limitMax) && ProcessControl.GetInstance().TargetRelativeHumidity > 0 )
            {
                openTrapForced = true;
            }

            if (openTrapForced)
            {
                _trapState = TrapStateEnum.Opened;
                outTrap.Write(true);
            }
            else
            {
                if (_startFan == false)
                {
                    _fanForced = false;
                }

                if (_openTrap && _trapState == TrapStateEnum.Closed)
                {
                    _trapState = TrapStateEnum.Opened;
                    outTrap.Write(true);
                }
                else if (_openTrap == false && _trapState == TrapStateEnum.Opened)
                {
                    _trapState = TrapStateEnum.Closed;
                    outTrap.Write(false);
                }
            }

            //////// Protection by fan

            limitMax = ProcessControl.GetInstance().TargetRelativeHumidity + RELATIVE_HUMIDITY_FAN_DELTA;
            if ( (ProcessControl.GetInstance().CurrentRelativeHumidity > limitMax) && ProcessControl.GetInstance().TargetRelativeHumidity > 0 )
            {
                _fanForced = true;
            }

            if (_fanForced)
            {
                _fanState = FanStateEnum.Running;
                outFan.Write(true);
            }
            else
            {
                if (_startFan && _fanState == FanStateEnum.Stopped)
                {
                    _fanState = FanStateEnum.Running;
                    outFan.Write(true);
                }
                else if (_startFan == false && _fanState == FanStateEnum.Running)
                {
                    _fanState = FanStateEnum.Stopped;
                    outFan.Write(false);
                }
            }
        }
        #endregion


        #region Private Methods
        #endregion
    }
}
