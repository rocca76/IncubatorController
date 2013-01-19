using System;
using Microsoft.SPOT;
using System.Threading;
using SecretLabs.NETMF.Hardware.NetduinoPlus;

namespace Sensirion.SHT11
{
    class SHT11Sensor
    {
        #region Private Variables
        private static Thread _readingThread = null;
        private static SHT11Sensor _sensirion = null;
        private static SHT11_GPIO_IOProvider SHT11_IO = new SHT11_GPIO_IOProvider(Pins.GPIO_PIN_D1, Pins.GPIO_PIN_D2);
        private static SHT11 SHT11 = new SHT11(SHT11_IO);
        #endregion


        #region Constructors
        private SHT11Sensor() { }
        #endregion


        #region Events
        //public static event ReceivedEventHandler EventHandlerMessageReceived;
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
                    Debug.Print("Error while resetting SHT11");
                }

                // Set Temperature and Humidity to 14/12 bit
                if (SHT11.WriteStatusRegister((SHT11.SHT11Settings.NullFlag)))
                {
                    Debug.Print("Error while writing status register SHT11");
                }
            }

            //_sensirion.ReadingThread();
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
        private void ReadingThread()
        {
            _readingThread = new Thread(new ThreadStart(ReadindSHT11Thread));
            _readingThread.Start();
        }

        private void ReadindSHT11Thread()
        {
            try
            {
                while (true)
                {
                    double temperature = SHT11.ReadTemperature(SHT11.SHT11VDD_Voltages.VDD_3_5V, SHT11.SHT11TemperatureUnits.Celcius);
                    double humidity = SHT11.ReadRelativeHumidity(SHT11.SHT11VDD_Voltages.VDD_3_5V);
                    Debug.Print("T:" + temperature.ToString("F2") + "  RH:" + humidity.ToString("F2"));

                    Thread.Sleep(5000);
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.ToString());
            }
        }
        #endregion
    }
}
