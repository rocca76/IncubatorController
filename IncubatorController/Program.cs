using System;
using System.Threading;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;
using System.Text;
using System.Net.Sockets;

//Smart Personal Object Technology

namespace NetduinoPlus.Controler
{
    public class Program
    {
        #region Private Variables
        private Timer _processTimer = null;
        #endregion

        #region Public Properties
        #endregion

        #region Events
        #endregion

        public static void Main()
        {
            new Program().Run();
            Thread.Sleep(Timeout.Infinite);
        }

        private void Run()
        {
            LogFile.InitInstance();
            ProcessControl.LoadConfiguration();
            NetworkCommunication.InitInstance();

            _processTimer = new Timer(new TimerCallback(OnProcessTimer), null, 0, 1000);
        }

        private void OnProcessTimer(object state)
        {
          try
          {
              ProcessControl.GetInstance().ReadTemperature();
              ProcessControl.GetInstance().ReadRelativeHumidity();
              VentilationControl.GetInstance().ReadCO2();

              ProcessControl.GetInstance().SetOutputPin();

              ProcessData();
          }
          catch (SocketException se)
          {
              LogFile.Exception(se.ToString());
          }
          catch (Exception ex)
          {
              LogFile.Exception(ex.ToString());
              //PowerState.RebootDevice(true);
          }
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

            //NetworkCommunication.Send(xmlBuilder.ToString());
        }
    }
}
