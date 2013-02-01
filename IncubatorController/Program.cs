using System;
using System.Threading;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;
using Microsoft.SPOT;
using Microsoft.SPOT.Net.NetworkInformation;
using Sensirion.SHT11;
using System.Text;

//Smart Personal Object Technology

namespace NetduinoPlus.Controler
{
    public class Program
    {
        #region Private Variables
        private OutputPort _led = new OutputPort(Pins.ONBOARD_LED, false);
        private bool _toggle = true;
        private Timer _SensorTimer = null;
        #endregion

        #region Public Properties
        #endregion

        #region Events
        public InterruptPort button;
        #endregion

        public static void Main()
        {
            new Program().Run();
            Thread.Sleep(Timeout.Infinite);
        }

        private void Run()
        {
            button = new InterruptPort(Pins.ONBOARD_SW1, false, Port.ResistorMode.Disabled, Port.InterruptMode.InterruptEdgeLow);
            button.OnInterrupt += new NativeEventHandler(button_OnInterrupt);

            NetworkCommunication.EventHandlerMessageReceived += new ReceivedEventHandler(OnMessageReceived);
            NetworkCommunication.InitInstance();

            SHT11Sensor.InitInstance();
            K30Sensor.InitInstance();
            _SensorTimer = new Timer(new TimerCallback(OnReadSensor), null, 0, 2000);
        }

        private void OnReadSensor(object state)
        {
            try
            {
                StringBuilder xmlBuilder = new StringBuilder();

                xmlBuilder.Append("<netduino>");
                xmlBuilder.Append("<data timestamp='2013-02-01'>");
                xmlBuilder.Append("<temperature>");
                xmlBuilder.Append(SHT11Sensor.ReadTemperature().ToString("F2"));
                xmlBuilder.Append("</temperature>");
                xmlBuilder.Append("<relativehumidity>");
                xmlBuilder.Append(SHT11Sensor.ReadRelativeHumidity().ToString("F2"));
                xmlBuilder.Append("</relativehumidity>");
                xmlBuilder.Append("<co2>");
                xmlBuilder.Append(K30Sensor.ReadCO2().ToString());
                xmlBuilder.Append("</co2>");
                xmlBuilder.Append("</data>");
                xmlBuilder.Append("</netduino>");

                NetworkCommunication.Send(xmlBuilder.ToString());
            }
            catch (Exception ex)
            {
                Debug.Print(ex.ToString());
            }
        }
        
        private void OnMessageReceived(String message)
        {
            string[] parts = message.Split(' ');

            if (parts[0] == "TIME")
            {
                SetTime(int.Parse(parts[1]), int.Parse(parts[2]), int.Parse(parts[3]), int.Parse(parts[4]), int.Parse(parts[5]), int.Parse(parts[6]), int.Parse(parts[7]));
            }
        }

        private static void SetTime(int year, int month, int day, int hour, int minute, int second, int millisecond)
        {
            DateTime presentTime = new DateTime(year, month, day, hour, minute, second, millisecond);
            Microsoft.SPOT.Hardware.Utility.SetLocalTime(presentTime);
        }

        private void button_OnInterrupt(uint data1, uint data2, DateTime time)
        {
            /*for(int i=0; i < 10; i++)
            {
                _led.Write(toggle);
                Thread.Sleep(500);
                toggle = !toggle;
            }*/
        }
    }
}
