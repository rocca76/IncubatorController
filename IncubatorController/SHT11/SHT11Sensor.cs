using System;
using Microsoft.SPOT;
using System.Threading;
using SecretLabs.NETMF.Hardware.NetduinoPlus;
using NetduinoPlus.Controler;

namespace Sensirion.SHT11
{
    class SHT11Sensor
    {
        #region Private Variables
        private static SHT11Sensor _sensirion = null;
        private static SHT11_GPIO_IOProvider SHT11_IO = new SHT11_GPIO_IOProvider(Pins.GPIO_PIN_D1, Pins.GPIO_PIN_D2);
        private static SHT11 SHT11 = new SHT11(SHT11_IO);
        #endregion


        #region Constructors
        private SHT11Sensor() { }
        #endregion


        #region Events
        #endregion


        #region Public Properties
        #endregion


        #region Public Static Methods
        public static void InitInstance()
        {
            if (_sensirion == null)
            {
                _sensirion = new SHT11Sensor();

                if (SHT11.SoftReset())
                {
                    LogFile.Error("Error while resetting SHT11");
                }

                // Set Temperature and Humidity to 14/12 bit
                if (SHT11.WriteStatusRegister((SHT11.SHT11Settings.NullFlag)))
                {
                    LogFile.Error("Error while writing status register SHT11");
                }
            }
        }

        public static double ReadTemperature()
        {
            return SHT11.ReadTemperature(SHT11.SHT11VDD_Voltages.VDD_3_5V, SHT11.SHT11TemperatureUnits.Celcius);
        }

        public static double ReadRelativeHumidity()
        {
            return SHT11.ReadRelativeHumidity(SHT11.SHT11VDD_Voltages.VDD_3_5V);
        }
        #endregion


        #region Private Methods
        #endregion
    }
}
