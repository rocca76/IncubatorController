using System;
using Microsoft.SPOT;
using System.Threading;
using Sensirion.SHT11;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;

namespace NetduinoPlus.Controler
{
    class ProcessControl
    {
        #region Private Variables
        private static ProcessControl _instance = null;
        private static readonly object LockObject = new object();

        private double _currentTemperature = 0.0;
        private double _targetTemperature = 0.0;
        private int _heatPower = 0;

        private double _currentRelativeHumidity = 0.0;
        private double _targetRelativeHumidity = 0.0;
        private int _pump = 0;

        private int _currentCO2 = 0;
        private int _targetCO2 = 0;
        private int _fan = 0;

        private OutputPort out3 = new OutputPort(Pins.GPIO_PIN_D3, false);
        private OutputPort out4 = new OutputPort(Pins.GPIO_PIN_D4, false);
        private OutputPort out5 = new OutputPort(Pins.GPIO_PIN_D5, false);
        private OutputPort out6 = new OutputPort(Pins.GPIO_PIN_D6, false);

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

        public int HeatPower
        {
            get { return _heatPower; }
            set { _heatPower = value; }
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

        public int Pump
        {
            get { return _pump; }
            set { _pump = value; }
        }

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

        public int Fan
        {
            get { return _fan; }
            set { _fan = value; }
        }


        public ActuatorControl.ActuatorMode ActuatorMode
        {
            get { return ActuatorControl.GetInstance().Mode; }
        }

        public ActuatorControl.ActuatorState ActuatorState
        {
            get { return ActuatorControl.GetInstance().State; }
        }

        public TimeSpan ActuatorDuration
        {
            get { return ActuatorControl.GetInstance().Duration; }
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

            if (CurrentTemperature < (TargetTemperature - 5))
            {
                HeatPower = 1500;
            }
            else if (CurrentTemperature >= (TargetTemperature - 5) && CurrentTemperature < (TargetTemperature - 4))
            {
                HeatPower = 1250;
            }
            else if (CurrentTemperature >= (TargetTemperature - 4) && CurrentTemperature < (TargetTemperature - 3))
            {
                HeatPower = 1000;
            }
            else if (CurrentTemperature >= (TargetTemperature - 3) && CurrentTemperature < (TargetTemperature - 2))
            {
                HeatPower = 750;
            }
            else if (CurrentTemperature >= (TargetTemperature - 2) && CurrentTemperature < (TargetTemperature - 1))
            {
                HeatPower = 500;
            }
            else if (CurrentTemperature >= (TargetTemperature - 1) && CurrentTemperature < TargetTemperature)
            {
                HeatPower = 250;
            }
            else if (CurrentTemperature >= TargetTemperature)
            {
                HeatPower = 0;
            }

            SetHeatPowerOutputPin();
        }

        public void ReadRelativeHumidity()
        {
            CurrentRelativeHumidity = SHT11Sensor.ReadRelativeHumidity();

            if (CurrentRelativeHumidity < TargetRelativeHumidity)
            {
                Pump = 1;
            }
            else
            {
                Pump = 0;
            }
        }

        public void ReadCO2()
        {
            CurrentCO2 = K30Sensor.ReadCO2();

            if (CurrentCO2 > TargetCO2)
            {
                Fan = 1;
            }
            else
            {
                Fan = 0;
            }
        }

        public void SetActuatorMode(String mode)
        {
            ActuatorControl.GetInstance().SetMode(mode);
        }
        #endregion

        #region Private Methods
        private void SetHeatPowerOutputPin()
        {
            switch(HeatPower)
            {
                case 0:
                {
                    out3.Write(false);
                    out4.Write(false);
                    out5.Write(false);
                    out6.Write(false);
                }
                break;
                case 250:
                {
                    out3.Write(true);
                    out4.Write(false);
                    out5.Write(false);
                    out6.Write(false);
                }
                break;
                case 500:
                {
                    out3.Write(false);
                    out4.Write(false);
                    out5.Write(true);
                    out6.Write(false);
                }
                break;
                case 750:
                {
                    out3.Write(true);
                    out4.Write(false);
                    out5.Write(true);
                    out6.Write(false);
                }
                break;
                case 1000:
                {
                    out3.Write(false);
                    out4.Write(false);
                    out5.Write(true);
                    out6.Write(true);
                }
                break;
                case 1250:
                {
                    out3.Write(false);
                    out4.Write(true);
                    out5.Write(true);
                    out6.Write(true);
                }
                break;
                case 1500:
                {
                    out3.Write(true);
                    out4.Write(true);
                    out5.Write(true);
                    out6.Write(true);
                }
                break;
            }

        }
        #endregion
    }
}
