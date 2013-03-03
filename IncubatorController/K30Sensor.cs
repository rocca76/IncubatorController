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
        private static K30Sensor _instance = null;
        private static readonly object LockObject = new object();
        #endregion


        #region Constructors
        private K30Sensor() { }
        #endregion


        #region Events
        #endregion


        #region Public Properties
        #endregion


        #region Public Static Methods
        public static K30Sensor GetInstance()
        {
            lock (LockObject)
            {
                if (_instance == null)
                {
                    _instance = new K30Sensor();
                }
                return _instance;
            }
        }

        public static int ReadCO2()
        {
          int co2 = 0;

          for (byte retry = 0; retry < 10; retry++)
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

          if (co2 == 0)
            {
                co2 = ReadSensor();
            }

          return co2;
        }
        #endregion


        #region Private Methods
        private static int ReadSensor()
        {
          I2CDevice.Configuration slaveConfig = new I2CDevice.Configuration(0x7F, 100);

          byte[] dataWrite = new byte[4] { 0x22, 0x00, 0x08, 0x2A };
          I2CBus.GetInstance().Write(slaveConfig, dataWrite, 3000);

          Thread.Sleep(10);

          byte[] dataRead = new byte[4] { 0x00, 0x00, 0x00, 0x00 };
          I2CBus.GetInstance().Read(slaveConfig, dataRead, 3000);

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
