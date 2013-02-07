using System;
using System.Threading;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;
using Microsoft.SPOT;
using Microsoft.SPOT.Net.NetworkInformation;
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

            _SensorTimer = new Timer(new TimerCallback(OnReadSensor), null, 0, 1000);
        }

        private void OnReadSensor(object state)
        {
          try
          {
              ProcessControl.GetInstance().ReadTemperature();
              ProcessControl.GetInstance().ReadRelativeHumidity();
              ProcessControl.GetInstance().ReadCO2();

              SendData();
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
                DateTime presentTime = new DateTime(int.Parse(parts[1]), int.Parse(parts[2]), int.Parse(parts[3]), int.Parse(parts[4]), int.Parse(parts[5]), int.Parse(parts[6]), int.Parse(parts[7]));
                Microsoft.SPOT.Hardware.Utility.SetLocalTime(presentTime);
            }
            else if (parts[0] == "TARGET_TEMPERATURE")
            {
                ProcessControl.GetInstance().TargetTemperature = double.Parse(parts[1]);
            }
        }

        private void SendData()
        {
            StringBuilder xmlBuilder = new StringBuilder();
            xmlBuilder.Append("<netduino>");
            xmlBuilder.Append("<data timestamp='2013-02-01'>");
            xmlBuilder.Append("<temperature>");
            xmlBuilder.Append(ProcessControl.GetInstance().CurrentTemperature.ToString("F2"));
            xmlBuilder.Append("</temperature>");
            xmlBuilder.Append("<targettemperature>");
            xmlBuilder.Append(ProcessControl.GetInstance().TargetTemperature.ToString("F2"));
            xmlBuilder.Append("</targettemperature>");
            xmlBuilder.Append("<heatpower>");
            xmlBuilder.Append(ProcessControl.GetInstance().HeatPower.ToString());
            xmlBuilder.Append("</heatpower>");
            xmlBuilder.Append("<relativehumidity>");
            xmlBuilder.Append(ProcessControl.GetInstance().CurrentRelativeHumidity.ToString("F2"));
            xmlBuilder.Append("</relativehumidity>");
            xmlBuilder.Append("<co2>");
            xmlBuilder.Append(ProcessControl.GetInstance().CurrentCO2.ToString());
            xmlBuilder.Append("</co2>");
            xmlBuilder.Append("</data>");
            xmlBuilder.Append("</netduino>");
            NetworkCommunication.Send(xmlBuilder.ToString());
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
