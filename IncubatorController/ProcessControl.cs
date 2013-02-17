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

        private double _targetTemperature = 0.0;
        private double _currentTemperature = 0.0;
        private double _currentRelativeHumidity = 0.0;
        private int _currentCO2 = 0;
        private int _heatPower = 0;

        private OutputPort out3 = new OutputPort(Pins.GPIO_PIN_D3, false);
        private OutputPort out4 = new OutputPort(Pins.GPIO_PIN_D4, false);
        private OutputPort out5 = new OutputPort(Pins.GPIO_PIN_D5, false);
        private OutputPort out6 = new OutputPort(Pins.GPIO_PIN_D6, false);

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

        public int HeatPower
        {
            get { return _heatPower; }
            set { _heatPower = value; }
        }

        public String TiltMode
        {
            get { return ActuatorControl.GetInstance().Mode; }
        }

        public String TiltState
        {
            get { return ActuatorControl.GetInstance().State; }
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
        }

        public void ReadCO2()
        {
            CurrentCO2 = K30Sensor.ReadCO2();
        }

        public void OpenActuator()
        {
            ActuatorControl.GetInstance().Open();
        }

        public void CloseActuator()
        {
            ActuatorControl.GetInstance().Close();
        }

        public void StopActuator()
        {
            ActuatorControl.GetInstance().Stop();
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
