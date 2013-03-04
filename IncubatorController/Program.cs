using System;
using System.Threading;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;
using Microsoft.SPOT;
using Microsoft.SPOT.Net.NetworkInformation;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Collections;

//Smart Personal Object Technology

namespace NetduinoPlus.Controler
{
    public class Program
    {
        #region Private Variables
        private OutputPort _led = new OutputPort(Pins.ONBOARD_LED, false);
        private bool _toggle = true;
        private Timer _processTimer = null;
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

            ProcessControl.LoadConfiguration();

            NetworkCommunication.EventHandlerMessageReceived += new ReceivedEventHandler(OnMessageReceived);
            NetworkCommunication.InitInstance();

            _processTimer = new Timer(new TimerCallback(OnProcessTimer), null, 0, 1000);
        }

        private void OnProcessTimer(object state)
        {
          try
          {
              //WriteFile();

              ProcessControl.GetInstance().ReadTemperature();
              ProcessControl.GetInstance().ReadRelativeHumidity();
              VentilationControl.GetInstance().ReadCO2();

              ProcessControl.GetInstance().SetOutputPin();

              ProcessData();
          }
          catch (SocketException se)
          {
              Debug.Print("Unable to connect or send through socket");
              Debug.Print(se.ToString());
              PowerState.RebootDevice(true);
          }
          catch (Exception ex)
          {
              Debug.Print(ex.ToString());
              PowerState.RebootDevice(true);
          }
        }
        
        private void OnMessageReceived(String message)
        {
            string[] parts = message.Split(' ');

            if (parts[0] == "TIME")
            {
                DateTime presentTime = new DateTime(int.Parse(parts[1]), int.Parse(parts[2]), int.Parse(parts[3]), int.Parse(parts[4]), int.Parse(parts[5]), int.Parse(parts[6]), int.Parse(parts[7]));
                Utility.SetLocalTime(presentTime);
            }
            else if (parts[0] == "TARGET_TEMPERATURE")
            {
                ProcessControl.GetInstance().TargetTemperature = double.Parse(parts[1]);
            }
            else if (parts[0] == "LIMIT_MAX_TEMPERATURE")
            {
                ProcessControl.GetInstance().LimitMaxTemperature = double.Parse(parts[1]);
            }
            else if (parts[0] == "TARGET_RELATIVE_HUMIDITY")
            {
                ProcessControl.GetInstance().TargetRelativeHumidity = double.Parse(parts[1]);
            }
            else if (parts[0] == "TARGET_VENTILATION")
            {
                VentilationControl.GetInstance().FanEnabled = int.Parse(parts[1]);
                VentilationControl.GetInstance().IntervalTargetMinutes = int.Parse(parts[2]);
                VentilationControl.GetInstance().DurationTargetSeconds = int.Parse(parts[3]);
                VentilationControl.GetInstance().TargetCO2 = int.Parse(parts[4]);
            }
            else if (parts[0] == "ACTUATOR_MODE")
            {
                ProcessControl.GetInstance().SetActuatorMode(parts[1]);
            }
            else if (parts[0] == "ACTUATOR_OPEN")
            {
                ProcessControl.GetInstance().SetActuatorOpen( int.Parse(parts[1]) );
            }
            else if (parts[0] == "ACTUATOR_CLOSE")
            {
                ProcessControl.GetInstance().SetActuatorClose( int.Parse(parts[1]) );
            }
        }

        private void WriteFile()
        {
            Hashtable ht = new Hashtable();
            ht.Add("TemperatureTarget", 37.2); // key, value

            //(string)ht["A"];

            foreach (DictionaryEntry de in ht)
            {
                if ((string)de.Key == "TemperatureTarget")
                {
                    double v = (double)de.Value;
                }
                
            }


            String[] lines = { "First line", "Second line", "Third line" };

            using (StreamWriter file = new StreamWriter(@"SD\IncubateurTarget.txt"))
            {
                foreach (String line in lines)
                {
                    file.WriteLine(line);
                }
            }

            //string[] parts = message.Split(' ');

            StringBuilder data = new StringBuilder();
            data.Append(DateTime.Now.ToString());
            data.Append(";");
            data.Append(ProcessControl.GetInstance().TargetTemperature.ToString("F2"));
            data.Append(";");
            data.Append(ProcessControl.GetInstance().TargetRelativeHumidity.ToString("F2"));
            data.Append(";");
            data.Append(VentilationControl.GetInstance().TargetCO2.ToString());

            WriteFile writeFile = new WriteFile(data.ToString());
            writeFile.Start();
        }

        private void ProcessData()
        {
            StringBuilder xmlBuilder = new StringBuilder();
            xmlBuilder.Append("<netduino>");
            xmlBuilder.Append("<data timestamp='" + DateTime.Now.ToString() + "'>");

            xmlBuilder.Append("<temperature>");
            xmlBuilder.Append(ProcessControl.GetInstance().CurrentTemperature.ToString("F2"));
            xmlBuilder.Append("</temperature>");
            xmlBuilder.Append("<targettemperature>");
            xmlBuilder.Append(ProcessControl.GetInstance().TargetTemperature.ToString("F2"));
            xmlBuilder.Append("</targettemperature>");
            xmlBuilder.Append("<limitmaxtemperature>");
            xmlBuilder.Append(ProcessControl.GetInstance().LimitMaxTemperature.ToString("F2"));
            xmlBuilder.Append("</limitmaxtemperature>");
            xmlBuilder.Append("<maxtemperaturereached>");
            xmlBuilder.Append(ProcessControl.GetInstance().MaxTemperatureLimitReached.ToString());
            xmlBuilder.Append("</maxtemperaturereached>");
            xmlBuilder.Append("<heatpower>");
            xmlBuilder.Append(ProcessControl.GetInstance().HeatPower.ToString());
            xmlBuilder.Append("</heatpower>");

            xmlBuilder.Append("<relativehumidity>");
            xmlBuilder.Append(ProcessControl.GetInstance().CurrentRelativeHumidity.ToString("F2"));
            xmlBuilder.Append("</relativehumidity>");
            xmlBuilder.Append("<targetrelativehumidity>");
            xmlBuilder.Append(ProcessControl.GetInstance().TargetRelativeHumidity.ToString("F2"));
            xmlBuilder.Append("</targetrelativehumidity>");
            xmlBuilder.Append("<pumpstate>");
            xmlBuilder.Append(PumpControl.GetInstance().PumpState.ToString());
            xmlBuilder.Append("</pumpstate>");
            xmlBuilder.Append("<pumpduration>");
            xmlBuilder.Append(PumpControl.GetInstance().Duration.ToString());
            xmlBuilder.Append("</pumpduration>");

            xmlBuilder.Append("<co2>");
            xmlBuilder.Append(VentilationControl.GetInstance().CurrentCO2.ToString());
            xmlBuilder.Append("</co2>");
            xmlBuilder.Append("<targetco2>");
            xmlBuilder.Append(VentilationControl.GetInstance().TargetCO2.ToString());
            xmlBuilder.Append("</targetco2>");

            xmlBuilder.Append("<trapstate>");
            xmlBuilder.Append(VentilationControl.GetInstance().TrapState.ToString());
            xmlBuilder.Append("</trapstate>");
            xmlBuilder.Append("<fanstate>");
            xmlBuilder.Append(VentilationControl.GetInstance().FanState.ToString());
            xmlBuilder.Append("</fanstate>");
            xmlBuilder.Append("<ventilationduration>");
            xmlBuilder.Append(VentilationControl.GetInstance().Duration.ToString());
            xmlBuilder.Append("</ventilationduration>");
            xmlBuilder.Append("<ventilationfanenabled>");
            xmlBuilder.Append(VentilationControl.GetInstance().FanEnabled.ToString()); // Fan used
            xmlBuilder.Append("</ventilationfanenabled>");
            xmlBuilder.Append("<ventilationIntervaltarget>");
            xmlBuilder.Append(VentilationControl.GetInstance().IntervalTargetMinutes.ToString()); //minutes
            xmlBuilder.Append("</ventilationIntervaltarget>");
            xmlBuilder.Append("<ventilationdurationtarget>");
            xmlBuilder.Append(VentilationControl.GetInstance().DurationTargetSeconds.ToString()); // seconds
            xmlBuilder.Append("</ventilationdurationtarget>");
            xmlBuilder.Append("<ventilationdstate>");
            xmlBuilder.Append(VentilationControl.GetInstance().State.ToString()); //Started or stopped
            xmlBuilder.Append("</ventilationdstate>");


            xmlBuilder.Append("<actuatormode>");
            xmlBuilder.Append(ActuatorControl.GetInstance().Mode.ToString());
            xmlBuilder.Append("</actuatormode>");
            xmlBuilder.Append("<actuatorstate>");
            xmlBuilder.Append(ActuatorControl.GetInstance().State.ToString());
            xmlBuilder.Append("</actuatorstate>");
            xmlBuilder.Append("<actuatorduration>");
            xmlBuilder.Append(ActuatorControl.GetInstance().Duration.ToString());
            xmlBuilder.Append("</actuatorduration>");
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
