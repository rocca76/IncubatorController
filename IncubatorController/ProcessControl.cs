using System;
using Microsoft.SPOT;
using System.Threading;
using Sensirion.SHT11;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;
using System.IO;

namespace NetduinoPlus.Controler
{
    class ProcessControl
    {
        #region Private Variables
        private static ProcessControl _instance = null;
        private static readonly object LockObject = new object();

        private double _currentTemperature = 0.0;
        private double _targetTemperature = 0.0;
        private double _limitMaxTemperature = 39.5;
        private int _heatPower = 0;
        private int _maxTemperatureLimitReached = 0;

        private double _currentRelativeHumidity = 0.0;
        private double _targetRelativeHumidity = 0.0;

        private OutputPort out250W = new OutputPort(Pins.GPIO_PIN_D4, false);  //250W
        private OutputPort out500W = new OutputPort(Pins.GPIO_PIN_D5, false);  //500W
        #endregion
        

        #region Public Properties
        public double CurrentTemperature
        {
            get { return _currentTemperature; }
            set { _currentTemperature = value; }
        }

        public double TargetTemperature
        {
            get { return _targetTemperature; }
            set { _targetTemperature = value; }
        }

        public double LimitMaxTemperature
        {
            get { return _limitMaxTemperature; }
            set { _limitMaxTemperature = value; }
        }

        public int HeatPower
        {
            get { return _heatPower; }
            set { _heatPower = value; }
        }

        public int MaxTemperatureLimitReached
        {
            get { return _maxTemperatureLimitReached; }
            set { _maxTemperatureLimitReached = value; }
        }

        public double CurrentRelativeHumidity
        {
            get { return _currentRelativeHumidity; }
            set { _currentRelativeHumidity = value; }
        }

        public double TargetRelativeHumidity
        {
            get { return _targetRelativeHumidity; }
            set { _targetRelativeHumidity = value; }
        }
        #endregion

        #region Events
        #endregion

        #region Constructors
        public ProcessControl() 
        {
            SHT11Sensor.InitInstance();
        }
        #endregion

        #region Public Methods
        public static ProcessControl GetInstance()
        {
            lock (LockObject)
            {
                if (_instance == null)
                {
                    _instance = new ProcessControl();
                }
                return _instance;
            }
        }

        public static void LoadConfiguration()
        {
            ConfigurationManager.Load();
        }

        public void ReadTemperature()
        {
            CurrentTemperature = SHT11Sensor.ReadTemperature();

            if (CurrentTemperature > 0)
            {
                if (CurrentTemperature < (TargetTemperature - 0.5))
                {
                    HeatPower = 750;
                }
                else if (CurrentTemperature >= (TargetTemperature - 0.5) && CurrentTemperature < (TargetTemperature - 0.25))
                {
                    HeatPower = 500;
                }
                else if (CurrentTemperature >= (TargetTemperature - 0.25) && CurrentTemperature < TargetTemperature)
                {
                    HeatPower = 250;
                }
                else if (CurrentTemperature >= TargetTemperature)
                {
                    HeatPower = 0;
                }
            }
            else
            {
                HeatPower = 0;
            }

            if (CurrentTemperature > LimitMaxTemperature)
            {
                MaxTemperatureLimitReached = 1;
                HeatPower = 0;
            }
            else
            {
                MaxTemperatureLimitReached = 0;
            }
        }

        public void ReadRelativeHumidity()
        {
            CurrentRelativeHumidity = SHT11Sensor.ReadRelativeHumidity();
            PumpControl.GetInstance().ManageState();
        }

        public void SetActuatorMode(String mode)
        {
            ActuatorControl.GetInstance().SetMode(mode);
        }

        public void SetActuatorOpen(int open)
        {
            ActuatorControl.GetInstance().Open(open);
        }

        public void SetActuatorClose(int close)
        {
            ActuatorControl.GetInstance().Close(close);
        }

        public void SetOutputPin()
        {
            switch (HeatPower)
            {
                case 0:
                {
                    out250W.Write(false);
                    out500W.Write(false);
                }
                break;
                case 250:
                {
                    out250W.Write(true);
                    out500W.Write(false);
                }
                break;
                case 500:
                {
                    out250W.Write(false);
                    out500W.Write(true);
                }
                break;
                case 750:
                {
                    out250W.Write(true);
                    out500W.Write(true);
                }
                break;
            }
        }
        #endregion

        #region Private Methods
        #endregion
    }
}
