using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;

namespace NetduinoPlus.Controler
{
    public sealed class HeatingControl
    {
        #region Private Variables
        private static readonly HeatingControl _instance = new HeatingControl();
        private int _heatPower = 0;
        private OutputPort out250W = new OutputPort(Pins.GPIO_PIN_D4, false);  //250W
        private OutputPort out500W = new OutputPort(Pins.GPIO_PIN_D5, false);  //500W
        #endregion


        #region Public Properties
        public static HeatingControl Instance
        {
            get { return _instance; }
        }

        public int HeatPower
        {
            get { return _heatPower; }
        }
        #endregion


        #region Constructors
        private HeatingControl() { }
        #endregion


        #region Public Methods
        public void ManageState()
        {
            double temperature = ProcessControl.Instance.Temperature;
            double target = ProcessControl.Instance.TargetTemperature;
            double temperatureMax = ProcessControl.Instance.TemperatureMax;

            if (temperature > 0 && temperature < temperatureMax)
            {
                if (temperature < (target - 0.5))
                {
                    _heatPower = 750;
                }
                else if (temperature >= (target - 0.5) && temperature < (target - 0.25))
                {
                    _heatPower = 500;
                }
                else if (temperature >= (target - 0.25) && temperature < target)
                {
                    _heatPower = 250;
                }
                else if (temperature >= target)
                {
                    _heatPower = 0;
                }
            }
            else
            {
                _heatPower = 0;
            }

            ProcessControl.Instance.TemperatureMaxReached = (temperature > temperatureMax);

            SetOutputPin();
        }
        #endregion


        #region Private Methods
        private void SetOutputPin()
        {
            switch (_heatPower)
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
    }
}
