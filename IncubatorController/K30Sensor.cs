using Microsoft.SPOT.Hardware;
using System.Threading;


namespace NetduinoPlus.Controler
{
    class K30Sensor
    {
        public enum ECO2Result
        {
            ValidResult,
            ChecksumError,
            ReadIncomplete,
            NoReadDataTransfered,
            NoWriteDataTransfered,
            UnknownResult
        }

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

        public ECO2Result ReadCO2(ref int co2Data)
        {
          co2Data = 0;
          ECO2Result result = ECO2Result.UnknownResult;
          I2CDevice.Configuration slaveConfig = new I2CDevice.Configuration(0x7F, 100);

          byte[] dataWrite = new byte[4] { 0x22, 0x00, 0x08, 0x2A };
          int transferred = I2CBus.GetInstance().Write(slaveConfig, dataWrite, 500);

          if (transferred > 0)
          {
              Thread.Sleep(10);

              byte[] dataRead = new byte[4] { 0x00, 0x00, 0x00, 0x00 };
              transferred = I2CBus.GetInstance().Read(slaveConfig, dataRead, 500);

              if (transferred > 0)
              {
                  if ((dataRead[0] & 0x01) == 1)
                  {
                      co2Data |= dataRead[1] & 0xFF;
                      co2Data = co2Data << 8;
                      co2Data |= dataRead[2] & 0xFF;

                      if (dataRead[3] == CheckSum(dataRead, 3))
                      {
                          result = ECO2Result.ValidResult;
                      }
                      else
                      {
                          result = ECO2Result.ChecksumError;
                      }
                  }
                  else
                  {
                      result = ECO2Result.ReadIncomplete;
                  }
              }
              else
              {
                  result = ECO2Result.NoReadDataTransfered;
              }
          }
          else
          {
              result = ECO2Result.NoWriteDataTransfered;
          }

          return result;
        }
        #endregion


        #region Private Methods
        private byte CheckSum(byte[] buf, byte count)
        {
            byte i = 0;
            byte sum = 0;

            while (count > 0)
            {
                sum += buf[i++];
                count--;
            }

            return sum;
        } 
        #endregion
    }
}
