using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using System.Threading;
using NetduinoPlusTesting;

namespace NetduinoPlus.Controler
{
    class K30Sensor
    {
        #region Private Variables
        private static K30Sensor _k30 = null;
        private static I2CDevice.Configuration _slaveConfig = new I2CDevice.Configuration(0x7F, 100);
        private static int TransactionTimeout = 3000;
        #endregion


        #region Constructors
        private K30Sensor() { }
        #endregion


        #region Events
        #endregion


        #region Public Properties
        #endregion


        #region Public Static Methods
        public static void InitInstance()
        {
            if (_k30 == null)
            {
                _k30 = new K30Sensor();

                //Init Parameters here
            }
        }

        public static int ReadCO2(byte maxRetry)
        {
          int co2 = 0;

          for (byte retry = 0; retry < maxRetry; retry++)
          {
            try
            {
              co2 = ReadSensor();
            }
            catch (Exception ex)
            {
              Debug.Print(ex.ToString() + " - CO2 = " + co2.ToString());
              co2 = 0;
            }

            if (co2 == 0)
            {
              Thread.Sleep(100);
            }
            else
            {
              break;
            }
          }

          return co2;
        }
        #endregion


        #region Private Methods
        private static int ReadSensor()
        {
          byte[] dataWrite = new byte[4] { 0x22, 0x00, 0x08, 0x2A };
          I2CBus.GetInstance().Write(_slaveConfig, dataWrite, TransactionTimeout);

          Thread.Sleep(10);

          byte[] dataRead = new byte[4] { 0x00, 0x00, 0x00, 0x00 };
          I2CBus.GetInstance().Read(_slaveConfig, dataRead, TransactionTimeout);

          int co2Value = 0;
          co2Value |= dataRead[1] & 0xFF;
          co2Value = co2Value << 8;
          co2Value |= dataRead[2] & 0xFF;

          int sum = (dataRead[0] + dataRead[1] + dataRead[2]) % 256;

          if (sum != dataRead[3])
          {
            co2Value = 0;
          }

          return co2Value;
        }
        #endregion
    }
}
