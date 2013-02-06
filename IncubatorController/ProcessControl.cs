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
        #endregion

        #region Public Properties
        public double TargetTemperature
        {
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
