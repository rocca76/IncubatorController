using System;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;

namespace NetduinoPlus.Controler
{
    class VentilationControl
    {
        const int CO2_DISABLE = 9999;
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
        private int _currentCO2 = 0;
        private int _targetCO2 = CO2_DISABLE;
        private bool _fanForced = false;

        private OutputPort outFan = new OutputPort(Pins.GPIO_PIN_D9, false);   //Fan
        private OutputPort outTrap = new OutputPort(Pins.GPIO_PIN_D10, false); //Trap
        #endregion


        #region Public Properties
        public int CurrentCO2
        {
            get { return _currentCO2; }
            set { _currentCO2 = value; }
        }

        public int TargetCO2
        {
            get { return _targetCO2; }
            set { _targetCO2 = value; }
        }

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
            get { return ManageState(); }
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

        public void ReadCO2()
        {
            int co2Data = 0;
            K30Sensor.ECO2Result result = K30Sensor.GetInstance().ReadCO2(ref co2Data);
            LogFile.Application("CO2: " + co2Data.ToString());

            if (result == K30Sensor.ECO2Result.ValidResult)
            {
                _currentCO2 = co2Data;
            }
            else
            {
                switch (result)
                {
                    case K30Sensor.ECO2Result.ChecksumError:
                        LogFile.Application("CO2: Checksum Error");
                    break;
                    case K30Sensor.ECO2Result.ReadIncomplete:
                        LogFile.Application("CO2: Read Incomplete");
                    break;
                    case K30Sensor.ECO2Result.NoReadDataTransfered:
                        LogFile.Application("CO2: No Read Data Transfered");
                    break;
                    case K30Sensor.ECO2Result.NoWriteDataTransfered:
                        LogFile.Application("CO2: No Write Data Transfered");
                    break;
                    case K30Sensor.ECO2Result.UnknownResult:
                        LogFile.Application("CO2: Unknown Error");
                    break;
                }
            }
        }

        public VentilationControl.VentilationState ManageState()
        {
            if (_duration > TimeSpan.Zero)
            {
                _duration = _duration.Subtract(new TimeSpan(0, 0, 1));
            }

            if (_currentCO2 > 0 && _targetCO2 != CO2_DISABLE)
            {
                //Sensor control
                if (VentilationControl.GetInstance().CurrentCO2 > VentilationControl.GetInstance().TargetCO2)
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

            return _ventilationState;
        }
        #endregion


        #region Private Methods
        #endregion
    }
}
