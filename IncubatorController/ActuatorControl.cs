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
        private TiltMode _tiltMode = TiltMode.Manual;
        private TiltState _tiltState = TiltState.Stopped;
        private static ActuatorControl _actuatorControl = null;

        private OutputPort out7 = new OutputPort(Pins.GPIO_PIN_D7, false);
        private OutputPort out8 = new OutputPort(Pins.GPIO_PIN_D8, false);

        public enum TiltMode
        {
            Manual,
            Auto
        }

        public enum TiltState
        {
            Open,
            Close,
            Opening,
            Closing,
            Stopped
        }

        #region Public Properties
        public String Mode
        {
            get
            {
                String modeTxt = "Mode: ";

                switch (_tiltMode)
                {
                    case TiltMode.Manual:
                        modeTxt += "Manuel";
                    break;
                    case TiltMode.Auto:
                        modeTxt += "Automatique";
                    break;
                }

                return modeTxt;
            }
        }

        public String State
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
            if (_tiltMode == TiltMode.Manual)
            {
                out7.Write(true);
                out8.Write(false);

                _tiltState = TiltState.Opening;
            }
        }

        public void Close()
        {
            if (_tiltMode == TiltMode.Manual)
            {
                out7.Write(false);
                out8.Write(true);

                _tiltState = TiltState.Closing;
            }
        }

        public void Stop()
        {
            if (_tiltMode == TiltMode.Manual)
            {
                out7.Write(false);
                out8.Write(false);

                _tiltState = TiltState.Stopped;
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
                _tiltMode = TiltMode.Manual;
                _tiltState = TiltState.Stopped;
            }
            else if (mode == "AUTO")
            {
                _tiltMode = TiltMode.Auto;
            }
        }

        private String ManageState()
        {
            String stateTxt = "État: ";

            if (_duration > TimeSpan.Zero)
            {
                _duration = _duration.Subtract(new TimeSpan(0, 0, 1));
            }

            if (_tiltMode == TiltMode.Manual)
            {
                switch (_tiltState)
                {
                    case TiltState.Stopped:
                        stateTxt += "Arrêté";
                        break;
                    case TiltState.Opening:
                        stateTxt += "Ouverture...";
                        break;
                    case TiltState.Closing:
                        stateTxt += "Fermeture...";
                        break;
                }
            }
            else if (_tiltMode == TiltMode.Auto)
            {
                if (_autoModeReady)
                {
                    if (_tiltState == TiltState.Closing)
                    {
                        if (_duration == TimeSpan.Zero)
                        {
                            //Start waiting period
                            _duration = new TimeSpan(0, 0, 10);
                            _tiltState = TiltState.Close;
                            out7.Write(false);
                            out8.Write(false);
                        }
                    }
                    else if (_tiltState == TiltState.Open || _tiltState == TiltState.Close)
                    {
                        if (_duration == TimeSpan.Zero)
                        {
                            //Start moving actuator
                            _duration = new TimeSpan(0, 0, 4);

                            if (_tiltState == TiltState.Open)
                            {
                                _tiltState = TiltState.Closing;
                                out7.Write(false);
                                out8.Write(true);
                            }
                            else if (_tiltState == TiltState.Close)
                            {
                                _tiltState = TiltState.Opening;
                                out7.Write(true);
                                out8.Write(false);
                            }
                        }
                    }
                    else if (_tiltState == TiltState.Opening)
                    {
                        if (_duration == TimeSpan.Zero)
                        {
                            //Start waiting period
                            _duration = new TimeSpan(0, 0, 10);
                            _tiltState = TiltState.Open;
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
                            _tiltState = TiltState.Closing;
                            _autoModeInitializing = true;
                        }
                    }
                }

                switch (_tiltState)
                {
                    case TiltState.Open:
                    case TiltState.Close:
                        stateTxt += "Déplacement dans " + _duration.ToString() + "";
                        break;
                    case TiltState.Opening:
                        stateTxt += "Ouverture...";
                        break;
                    case TiltState.Closing:
                        stateTxt += "Fermeture...";
                        break;
                }
            }

            return stateTxt;
        }
    }
}
