using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using System.Threading;
using NetduinoPlusTesting;

namespace NetduinoPlus.Controler
{
    public enum K30Command : byte
    {
        ReadTemperature = 0x00,
        ReadWriteRegister = 0x01
    };

    public enum K30Config : byte
    {
        READY = 0x40,
        STANDBY = 0x80
    };

    class K30Device
    {
        private I2CDevice.Configuration _slaveConfig;
        private const int TransactionTimeout = 3000; // ms
        private const byte ClockRateKHz = 100;
        public byte Address { get; private set; }

        public K30Device(byte address)
        {
            Address = address;
            _slaveConfig = new I2CDevice.Configuration(address, ClockRateKHz);
        }

        public void Read()
        {
            byte[] dataWrite = new byte[4] { 0x22, 0x00, 0x08, 0x2A };
            I2CBus.GetInstance().Write(_slaveConfig, dataWrite, TransactionTimeout);

            Thread.Sleep(100);

            byte[] dataRead = new byte[4] { 0x00, 0x00, 0x00, 0x00 };
            I2CBus.GetInstance().Read(_slaveConfig, dataRead, TransactionTimeout);

            int co2_value = 0;
            co2_value |= dataRead[1] & 0xFF;
            co2_value = co2_value << 8;
            co2_value |= dataRead[2] & 0xFF;

            int sum = 0;                              //Checksum Byte
            sum = dataRead[0] + dataRead[1] + dataRead[2];

            if (sum == dataRead[3])
            {
                Debug.Print(co2_value.ToString());
            }
            else
            {
                Debug.Print("Failure!");
            }
        }

        public bool IsReady()
        {
            bool bready = false;
            byte ret = ReadRegister();
            if ((ret | (byte)K30Config.READY) == (byte)K30Config.READY)
            {
                bready = true;
            }

            return bready;
        }

        public byte ReadRegister()
        {
            byte[] data = new byte[1];
            I2CBus.GetInstance().ReadRegister(_slaveConfig, (byte)K30Command.ReadWriteRegister, data, TransactionTimeout);

            return data[0];
        }
    }
}
