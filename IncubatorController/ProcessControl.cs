using System;
using Microsoft.SPOT;
using System.Threading;
using Sensirion.SHT11;

namespace NetduinoPlus.Controler
{
    class ProcessControl
    {
        #region Private Variables
        private static ProcessControl _instance = null;
        private static readonly object LockObject = new object();

        private double _targetTemperature = 0.0;
        private double _currentTemperature = 0.0;
        private double _currentRelativeHumidity = 0.0;
        private int _currentCO2 = 0;
        private int _heatPower = 0;
        #endregion

        #region Public Properties
        public double TargetTemperature
        {
            get { return _targetTemperature; }
            set { _targetTemperature = value; }
        }

        public double CurrentTemperature
        {
            get { return _currentTemperature; }
            set { _currentTemperature = value; }
        }

        public double CurrentRelativeHumidity
        {
            get { return _currentRelativeHumidity; }
            set { _currentRelativeHumidity = value; }
        }

        public int CurrentCO2
        {
            get { return _currentCO2; }
            set { _currentCO2 = value; }
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

        public void ReadTemperature()
        {
            CurrentTemperature = SHT11Sensor.ReadTemperature();

            if ( CurrentTemperature < 20 )
            {
                _heatPower = 1500;
            }
            else if (CurrentTemperature >= 20 && CurrentTemperature < 22)
            {
                _heatPower = 1250;
            }
            else if (CurrentTemperature >= 22 && CurrentTemperature < 24)
            {
                _heatPower = 1000;
            }
            else if (CurrentTemperature >= 24 && CurrentTemperature < 26)
            {
                _heatPower = 750;
            }
            else if (CurrentTemperature >= 26 && CurrentTemperature < 28)
            {
                _heatPower = 500;
            }
            else if (CurrentTemperature >= 28 && CurrentTemperature < 30)
            {
                _heatPower = 250;
            }

        }

        public void ReadRelativeHumidity()
        {
            CurrentRelativeHumidity = SHT11Sensor.ReadRelativeHumidity();
        }

        public void ReadCO2()
        {
            CurrentCO2 = K30Sensor.ReadCO2();
        }
        #endregion

        #region Private Methods
        #endregion
    }
}
