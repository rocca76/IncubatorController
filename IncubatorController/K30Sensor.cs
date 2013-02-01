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

        public static int ReadCO2()
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

            int sum = 0;
            sum = dataRead[0] + dataRead[1] + dataRead[2];
            sum = sum % 256;

            if (sum == dataRead[3])
            {
                return co2Value;
            }

            return 0;
        }

        #endregion


        #region Private Methods
        #endregion
    }
}
