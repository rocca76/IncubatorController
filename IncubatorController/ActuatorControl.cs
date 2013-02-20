using System;
using Microsoft.SPOT;
using System.Threading;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;

namespace NetduinoPlus.Controler
{
    class ActuatorControl
    {
        const int ACTUARTOR_DELAY = 12; //seconds
        const int TILT_PERIOD = 60;//7200; //seconds

        private bool _autoModeReady = false;
        private bool _autoModeInitializing = false;
        private TimeSpan _duration = TimeSpan.Zero;
        private ActuatorMode _actuatorMode = ActuatorMode.Manual;
        private ActuatorState _actuatorState = ActuatorState.Unknown;
        private static ActuatorControl _actuatorControl = null;

        private OutputPort out7 = new OutputPort(Pins.GPIO_PIN_D7, false);
        private OutputPort out8 = new OutputPort(Pins.GPIO_PIN_D8, false);

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

        public void SetMode(String mode)
        {
            out7.Write(false);
            out8.Write(false);

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
                out7.Write(false);
                out8.Write(false);
            }
            else if (_actuatorMode == ActuatorMode.ManualCentered)
            {
                if (_actuatorState == ActuatorState.Close || _actuatorState == ActuatorState.Open)
                {
                    _duration = new TimeSpan(0, 0, ACTUARTOR_DELAY / 2);

                    if (_actuatorState == ActuatorState.Close)
                    {
                        _actuatorState = ActuatorState.Opening;
                        out7.Write(true);
                        out8.Write(false);
                    }
                    else if (_actuatorState == ActuatorState.Open)
                    {
                        _actuatorState = ActuatorState.Closing;
                        out7.Write(false);
                        out8.Write(true);
                    }
                }
                else if (_actuatorState == ActuatorState.Closing || _actuatorState == ActuatorState.Opening)
                {
                    if (_duration == TimeSpan.Zero)
                    {
                        _actuatorState = ActuatorState.Stopped;
                        out7.Write(false);
                        out8.Write(false);
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
                            out7.Write(false);
                            out8.Write(false);
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
                                out7.Write(false);
                                out8.Write(true);
                            }
                            else if (_actuatorState == ActuatorState.Close)
                            {
                                _actuatorState = ActuatorState.Opening;
                                out7.Write(true);
                                out8.Write(false);
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
                            out7.Write(false);
                            out8.Write(false);
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
                            out7.Write(false);
                            out8.Write(true);
                        }
                    }
                }
            }

            return _actuatorState;
        }
    }
}
