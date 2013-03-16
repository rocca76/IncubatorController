using System;
using Microsoft.SPOT;
using System.Threading;
using SecretLabs.NETMF.Hardware.NetduinoPlus;
using NetduinoPlus.Controler;

namespace Sensirion.SHT11
{
    public sealed class SHT11Sensor
    {
        #region Private Variables
        private static SHT11 _sht11 = null;
        private static readonly SHT11Sensor _instance = new SHT11Sensor();
        private bool _isReady = false;
        #endregion


        #region Constructors
        private SHT11Sensor() {}
        #endregion


        #region Events
        #endregion


        #region Public Properties
        public static SHT11Sensor Instance
        {
            get { return _instance; }
        }
        #endregion


        #region Public Methods
        public double ReadTemperature()
        {
            double temperature = 0.0;

            if (IsReady())
            {
                temperature = _sht11.ReadTemperature(SHT11.SHT11VDD_Voltages.VDD_3_5V, SHT11.SHT11TemperatureUnits.Celcius);
            }

            return temperature;
        }

        public double ReadRelativeHumidity()
        {
            double relativeHumidity = 0.0;

            if (IsReady())
            {
                relativeHumidity = _sht11.ReadRelativeHumidity(SHT11.SHT11VDD_Voltages.VDD_3_5V);
            }

            return relativeHumidity;
        }
        #endregion


        #region Private Methods
        public bool IsReady()
        {
            if (_sht11 == null)
            {
                _sht11 = new SHT11(new SHT11_GPIO_IOProvider(Pins.GPIO_PIN_D0, Pins.GPIO_PIN_D1));

                if (_sht11.SoftReset() == false)
                {
                    // Set Temperature and Humidity to 14/12 bit
                    if (_sht11.WriteStatusRegister((SHT11.SHT11Settings.NullFlag)))
                    {
                        LogFile.Error("Error while writing status register SHT11");
                    }
                    else
                    {
                        _isReady = true;
                    }
                }
                else
                {
                    LogFile.Error("Error while resetting SHT11");
                }
            }

            return _isReady;
        }
        #endregion
    }
}
