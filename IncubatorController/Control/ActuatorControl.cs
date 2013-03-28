using System;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;

namespace NetduinoPlus.Controler
{
    public sealed class ActuatorControl
    {
        public enum ActuatorCommand
        {
            Start,
            Stop,
            Pause,
            Unknown
        }

        public enum ActuatorState
        {
            Open,
            Opening,
            Close,
            Closing,
            Stopped,
            Paused,
            Unknown
        }

        #region Private Variables
        private const int TILT_INTERVAL = 2;    // hours
        private const int ACTUARTOR_DELAY = 26; // seconds

        private static readonly ActuatorControl _instance = new ActuatorControl();
        private bool _autoModeReady = false;
        private bool _autoModeInitializing = false;
        private TimeSpan _duration = TimeSpan.Zero;
        private ActuatorCommand _actuatorCommand = ActuatorCommand.Unknown;
        private ActuatorState _actuatorState = ActuatorState.Unknown;
        private ActuatorState _actuatorPreviousState = ActuatorState.Unknown;

        private OutputPort outOpen = new OutputPort(Pins.GPIO_PIN_D7, false);
        private OutputPort outClose = new OutputPort(Pins.GPIO_PIN_D8, false);
        #endregion


        #region Public Properties
        public static ActuatorControl Instance
        {
            get { return _instance; }
        }

        public ActuatorControl.ActuatorCommand Command
        {
            get { return _actuatorCommand; }
            set { _actuatorCommand = value; }
        }

        public ActuatorControl.ActuatorState State
        {
            get { return _actuatorState; }
        }

        public TimeSpan Duration
        {
            get { return _duration; }
        }
        #endregion


        #region Constructors
        private ActuatorControl() { }
        #endregion


        #region Public Methods
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

        public void ManageState()
        {
            if (_duration > TimeSpan.Zero && _actuatorState != ActuatorState.Paused)
            {
                _duration = _duration.Subtract(new TimeSpan(0, 0, 1));
            }

            if (_actuatorCommand == ActuatorCommand.Start)
            {
                if (_autoModeReady)
                {
                    if (_actuatorState == ActuatorState.Closing)
                    {
                        if (_duration == TimeSpan.Zero)
                        {
                            //Start waiting period
                            _duration = new TimeSpan(TILT_INTERVAL, 0, 0);
                            _actuatorState = ActuatorState.Close;
                        }
                    }
                    else if (_actuatorState == ActuatorState.Open || _actuatorState == ActuatorState.Close || _actuatorState == ActuatorState.Stopped)
                    {
                        if (_duration == TimeSpan.Zero)
                        {
                            //Start moving actuator
                            _duration = new TimeSpan(0, 0, ACTUARTOR_DELAY);

                            if (_actuatorState == ActuatorState.Open)
                            {
                                _actuatorState = ActuatorState.Closing;
                            }
                            else if (_actuatorState == ActuatorState.Close)
                            {
                                _actuatorState = ActuatorState.Opening;
                            }
                        }
                    }
                    else if (_actuatorState == ActuatorState.Opening)
                    {
                        if (_duration == TimeSpan.Zero)
                        {
                            //Start waiting period
                            _duration = new TimeSpan(TILT_INTERVAL, 0, 0);
                            _actuatorState = ActuatorState.Open;
                        }
                    }
                    else if (_actuatorState == ActuatorState.Paused)
                    {
                        _actuatorState = _actuatorPreviousState;
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
                        }
                    }
                }
            }
            else if (_actuatorCommand == ActuatorCommand.Stop)
            {
                _actuatorState = ActuatorState.Stopped;
                _duration = TimeSpan.Zero;
                _autoModeReady = false;
                _autoModeInitializing = false;
            }
            else if (_actuatorCommand == ActuatorCommand.Pause)
            {
                _actuatorState = ActuatorState.Paused;
            }

            if (_actuatorPreviousState != _actuatorState)
            {
                SetOutputState();

                if (_actuatorState != ActuatorState.Paused)
                {
                    _actuatorPreviousState = _actuatorState;
                }
            }
        }
        #endregion


        #region Private Methods
        private void SetOutputState()
        {
            if (_actuatorState == ActuatorState.Closing)
            {
                outOpen.Write(false);
                outClose.Write(true);
            }
            else if (_actuatorState == ActuatorState.Close || _actuatorState == ActuatorState.Open || 
                     _actuatorState == ActuatorState.Stopped || _actuatorState == ActuatorState.Paused)
            {
                outOpen.Write(false);
                outClose.Write(false);
            }
            else if (_actuatorState == ActuatorState.Opening)
            {
                outOpen.Write(true);
                outClose.Write(false);
            }
        }
        #endregion
    }
}
