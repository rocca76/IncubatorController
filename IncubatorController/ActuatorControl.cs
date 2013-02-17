using System;
using Microsoft.SPOT;
using System.Threading;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;

namespace NetduinoPlus.Controler
{
    class ActuatorControl
    {
        private bool _autoModeReady = false;
        private bool _autoModeInitializing = false;
        private TimeSpan _duration = TimeSpan.Zero;
        private ActuatorMode _actuatorMode = ActuatorMode.Manual;
        private ActuatorState _actuatorState = ActuatorState.Stopped;
        private static ActuatorControl _actuatorControl = null;

        private OutputPort out7 = new OutputPort(Pins.GPIO_PIN_D7, false);
        private OutputPort out8 = new OutputPort(Pins.GPIO_PIN_D8, false);

        public enum ActuatorMode
        {
            Manual,
            Auto
        }

        public enum ActuatorState
        {
            Open,
            Close,
            Opening,
            Closing,
            Stopped
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
        #endregion

        public static ActuatorControl GetInstance()
        {
            if (_actuatorControl == null)
            {
                _actuatorControl = new ActuatorControl();
            }

            return _actuatorControl;            
        }

        public void Open()
        {
            if (_actuatorMode == ActuatorMode.Manual)
            {
                out7.Write(true);
                out8.Write(false);

                _actuatorState = ActuatorState.Opening;
            }
        }

        public void Close()
        {
            if (_actuatorMode == ActuatorMode.Manual)
            {
                out7.Write(false);
                out8.Write(true);

                _actuatorState = ActuatorState.Closing;
            }
        }

        public void Stop()
        {
            if (_actuatorMode == ActuatorMode.Manual)
            {
                out7.Write(false);
                out8.Write(false);

                _actuatorState = ActuatorState.Stopped;
            }
        }

        public void SetMode(String mode)
        {
            out7.Write(false);
            out8.Write(false);

            if (mode == "MANUAL")
            {
                _autoModeReady = false;
                _autoModeInitializing = false;
                _duration = TimeSpan.Zero;
                _actuatorMode = ActuatorMode.Manual;
                _actuatorState = ActuatorState.Stopped;
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

            if (_actuatorMode == ActuatorMode.Auto)
            {
                if (_autoModeReady)
                {
                    if (_actuatorState == ActuatorState.Closing)
                    {
                        if (_duration == TimeSpan.Zero)
                        {
                            //Start waiting period
                            _duration = new TimeSpan(0, 0, 10);
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
                            _duration = new TimeSpan(0, 0, 4);

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
                            _duration = new TimeSpan(0, 0, 10);
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
                            _duration = new TimeSpan(0, 0, 4);
                            _actuatorState = ActuatorState.Closing;
                            _autoModeInitializing = true;
                        }
                    }
                }
            }

            return _actuatorState;
        }
    }
}
