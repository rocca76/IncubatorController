using System;
using Microsoft.SPOT;
using System.Threading;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;

namespace NetduinoPlus.Controler
{
    class ActuatorControl
    {
        const int ACTUARTOR_DELAY = 12; // 12 seconds
        const int TILT_PERIOD = 7200;   // seconds (2hr)

        private bool _autoModeReady = false;
        private bool _autoModeInitializing = false;
        private TimeSpan _duration = TimeSpan.Zero;
        private ActuatorMode _actuatorMode = ActuatorMode.Manual;
        private ActuatorState _actuatorState = ActuatorState.Unknown;
        private static ActuatorControl _actuatorControl = null;

        private OutputPort outOpen = new OutputPort(Pins.GPIO_PIN_D7, false);
        private OutputPort outClose = new OutputPort(Pins.GPIO_PIN_D8, false);

        public enum ActuatorMode
        {
            Manual,
            ManualCentered,
            Auto
        }

        public enum ActuatorState
        {
            Open,
            Opening,
            Close,
            Closing,
            Stopped,
            Unknown
        }

        #region Public Properties
        public ActuatorControl.ActuatorMode Mode
        {
            get { return _actuatorMode; }
        }

        public ActuatorControl.ActuatorState State
        {
            get { return ManageState(); }
        }

        public TimeSpan Duration
        {
            get { return _duration; }
        }
        #endregion

        public static ActuatorControl GetInstance()
        {
            if (_actuatorControl == null)
            {
                _actuatorControl = new ActuatorControl();
            }

            return _actuatorControl;            
        }

        public void Open(int open)
        {
            if (open == 0)
            {
                outOpen.Write(false);
            }
            else if (open == 1)
            {
                outOpen.Write(true);
            }
        }

        public void Close(int close)
        {
            if (close == 0)
            {
                outClose.Write(false);
            }
            else if (close == 1)
            {
                outClose.Write(true);
            }
        }

        public void SetMode(String mode)
        {
            outOpen.Write(false);
            outClose.Write(false);

            if (mode == "MANUAL" || mode =="MANUAL_CENTERED")
            {
                _autoModeReady = false;
                _autoModeInitializing = false;
                _duration = TimeSpan.Zero;

                if (mode == "MANUAL")
                {
                    _actuatorMode = ActuatorMode.Manual;
                }
                else if (mode == "MANUAL_CENTERED")
                {
                    _actuatorMode = ActuatorMode.ManualCentered;
                }
            }
            else if (mode == "AUTO")
            {
                _actuatorMode = ActuatorMode.Auto;
            }
        }

        private ActuatorControl.ActuatorState ManageState()
        {
            if (_duration > TimeSpan.Zero)
            {
                _duration = _duration.Subtract(new TimeSpan(0, 0, 1));
            }

            if (_actuatorMode == ActuatorMode.Manual && _actuatorState != ActuatorState.Stopped)
            {
                _actuatorState = ActuatorState.Stopped;
                outOpen.Write(false);
                outClose.Write(false);
            }
            else if (_actuatorMode == ActuatorMode.ManualCentered)
            {
                if (_actuatorState == ActuatorState.Close || _actuatorState == ActuatorState.Open)
                {
                    _duration = new TimeSpan(0, 0, ACTUARTOR_DELAY / 2);

                    if (_actuatorState == ActuatorState.Close)
                    {
                        _actuatorState = ActuatorState.Opening;
                        outOpen.Write(true);
                        outClose.Write(false);
                    }
                    else if (_actuatorState == ActuatorState.Open)
                    {
                        _actuatorState = ActuatorState.Closing;
                        outOpen.Write(false);
                        outClose.Write(true);
                    }
                }
                else if (_actuatorState == ActuatorState.Closing || _actuatorState == ActuatorState.Opening)
                {
                    if (_duration == TimeSpan.Zero)
                    {
                        _actuatorState = ActuatorState.Stopped;
                        outOpen.Write(false);
                        outClose.Write(false);
                    }
                }
            }
            else if (_actuatorMode == ActuatorMode.Auto)
            {
                if (_autoModeReady)
                {
                    if (_actuatorState == ActuatorState.Closing)
                    {
                        if (_duration == TimeSpan.Zero)
                        {
                            //Start waiting period
                            _duration = new TimeSpan(0, 0, TILT_PERIOD);
                            _actuatorState = ActuatorState.Close;
                            outOpen.Write(false);
                            outClose.Write(false);
                        }
                    }
                    else if (_actuatorState == ActuatorState.Open || _actuatorState == ActuatorState.Close)
                    {
                        if (_duration == TimeSpan.Zero)
                        {
                            //Start moving actuator
                            _duration = new TimeSpan(0, 0, ACTUARTOR_DELAY);

                            if (_actuatorState == ActuatorState.Open)
                            {
                                _actuatorState = ActuatorState.Closing;
                                outOpen.Write(false);
                                outClose.Write(true);
                            }
                            else if (_actuatorState == ActuatorState.Close)
                            {
                                _actuatorState = ActuatorState.Opening;
                                outOpen.Write(true);
                                outClose.Write(false);
                            }
                        }
                    }
                    else if (_actuatorState == ActuatorState.Opening)
                    {
                        if (_duration == TimeSpan.Zero)
                        {
                            //Start waiting period
                            _duration = new TimeSpan(0, 0, TILT_PERIOD);
                            _actuatorState = ActuatorState.Open;
                            outOpen.Write(false);
                            outClose.Write(false);
                        }
                    }
                }
                else
                {
                    if (_duration == TimeSpan.Zero)
                    {
                        if (_autoModeInitializing)
                        {
                            //Initializing actuator completed
                            _autoModeReady = true;
                            _autoModeInitializing = false;
                        }
                        else
                        {
                            //Start initializing actuator
                            _duration = new TimeSpan(0, 0, ACTUARTOR_DELAY);
                            _autoModeInitializing = true;
                            _actuatorState = ActuatorState.Closing;
                            outOpen.Write(false);
                            outClose.Write(true);
                        }
                    }
                }
            }

            return _actuatorState;
        }
    }
}
