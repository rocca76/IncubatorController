using System;
using Sensirion.SHT11;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;
using System.Text;
using System.Runtime.CompilerServices;

namespace NetduinoPlus.Controler
{
    public sealed class ProcessControl
    {
        public static readonly int CO2_DISABLE = 9999;
        private static readonly double TEMPERATURE_MAX = 39.5;

        #region Private Variables
        private static readonly ProcessControl _instance = new ProcessControl();
        private String _lockObject = "";
        
        private MovingAverage _temperatureAverage = new MovingAverage();
        private MovingAverage _relativeHumidityAverage = new MovingAverage();

        private double _temperature = 0.0;
        private double _targetTemperature = 0.0;
        private double _temperatureMax = TEMPERATURE_MAX;
        private bool _temperatureMaxReached = false;


        private double _relativeHumidity = 0.0;
        private double _targetRelativeHumidity = 0.0;

        private int _CO2 = 0;
        private int _targetCO2 = CO2_DISABLE;
        #endregion
        
        #region Public Properties
        public static ProcessControl Instance
        {
          get { return _instance; }
        }

        public double Temperature
        {
            get { return _temperature; }
            set { _temperature = value; }
        }

        public double TargetTemperature
        {
            get { return _targetTemperature; }
            set { _targetTemperature = value; }
        }

        public double TemperatureMax
        {
            get { return _temperatureMax; }
            set { _temperatureMax = value; }
        }

        public bool TemperatureMaxReached
        {
            get { return _temperatureMaxReached; }
            set { _temperatureMaxReached = value; }
        }

        public double RelativeHumidity
        {
            get { return _relativeHumidity; }
            set { _relativeHumidity = value; }
        }

        public double TargetRelativeHumidity
        {
            get { return _targetRelativeHumidity; }
            set { _targetRelativeHumidity = value; }
        }

        public int CO2
        {
            get { return _CO2; }
            set { _CO2 = value; }
        }

        public int TargetCO2
        {
            get { return _targetCO2; }
            set { _targetCO2 = value; }
        }

        public String DataOutput
        {
            get { return _lockObject; }
        }
        #endregion

        #region Events
        #endregion

        #region Constructors
        public ProcessControl() 
        {
            ListenerThread.CommandReceived += new ReceivedEventHandler(OnCommandReceived);
        }
        #endregion

        #region Public Methods
        //[MethodImpl(MethodImplOptions.Synchronized)]
        public void ProcessData()
        {
          lock(_lockObject)
          {
            ReadSensor();

            HeatingControl.Instance.ManageState();
            PumpControl.Instance.ManageState();
            VentilationControl.Instance.ManageState();
            ActuatorControl.Instance.ManageState();

            _lockObject = BuildDataOutput();
          }

          NetworkCommunication.Instance.NotifySenderThread();
        }
        #endregion

        #region Private Methods
        private void OnCommandReceived(String command)
        {
            string[] parts = command.Split(' ');

            if (parts[0] == "TIME")
            {
                DateTime presentTime = new DateTime(int.Parse(parts[1]), int.Parse(parts[2]), int.Parse(parts[3]), int.Parse(parts[4]), int.Parse(parts[5]), int.Parse(parts[6]), int.Parse(parts[7]));
                Utility.SetLocalTime(presentTime);

                NetworkCommunication.Instance.StartSender();
            }
            else if (parts[0] == "TARGET_TEMPERATURE")
            {
                ProcessControl.Instance.TargetTemperature = double.Parse(parts[1]);
            }
            else if (parts[0] == "LIMIT_MAX_TEMPERATURE")
            {
                ProcessControl.Instance.TemperatureMax = double.Parse(parts[1]);
            }
            else if (parts[0] == "TARGET_RELATIVE_HUMIDITY")
            {
                ProcessControl.Instance.TargetRelativeHumidity = double.Parse(parts[1]);
            }
            else if (parts[0] == "TARGET_VENTILATION")
            {
                VentilationControl.Instance.FanEnabled = int.Parse(parts[1]);
                VentilationControl.Instance.IntervalTargetMinutes = int.Parse(parts[2]);
                VentilationControl.Instance.DurationTargetSeconds = int.Parse(parts[3]);
                ProcessControl.Instance.TargetCO2 = int.Parse(parts[4]);
            }
            else if (parts[0] == "ACTUATOR_MODE")
            {
                ActuatorControl.Instance.SetMode(parts[1]);
            }
            else if (parts[0] == "ACTUATOR_OPEN")
            {
                ActuatorControl.Instance.Open(int.Parse(parts[1]));
            }
            else if (parts[0] == "ACTUATOR_CLOSE")
            {
                ActuatorControl.Instance.Close(int.Parse(parts[1]));
            }
        }

        private void ReadSensor()
        {
            ReadTemperature();
            ReadRelativeHumidity();
            ReadCO2();

            LogFile.Application(_temperature.ToString("F2") + ";" + _relativeHumidity.ToString("F2") + ";" + _CO2.ToString());
        }

        private void ReadTemperature()
        {
            double temperature = SHT11Sensor.Instance.ReadTemperature();
            _temperatureAverage.Push(temperature);
            _temperature = _temperatureAverage.Average;
        }

        private void ReadRelativeHumidity()
        {
            double relativeHumidity = SHT11Sensor.Instance.ReadRelativeHumidity();
            _relativeHumidityAverage.Push(relativeHumidity);
            _relativeHumidity = _relativeHumidityAverage.Average;
        }

        private void ReadCO2()
        {
            int co2Data = 0;
            K30Sensor.ECO2Result result = K30Sensor.Instance.ReadCO2(ref co2Data);

            if (result == K30Sensor.ECO2Result.ValidResult)
            {
                _CO2 = co2Data;
            }
            else
            {
                switch (result)
                {
                    case K30Sensor.ECO2Result.ChecksumError:
                        LogFile.Application("CO2: Checksum Error");
                        break;
                    case K30Sensor.ECO2Result.ReadIncomplete:
                        LogFile.Application("CO2: Read Incomplete");
                        break;
                    case K30Sensor.ECO2Result.NoReadDataTransfered:
                        LogFile.Application("CO2: No Read Data Transfered");
                        break;
                    case K30Sensor.ECO2Result.NoWriteDataTransfered:
                        LogFile.Application("CO2: No Write Data Transfered");
                        break;
                    case K30Sensor.ECO2Result.UnknownResult:
                        LogFile.Application("CO2: Unknown Error");
                        break;
                }
            }
        }

        public String BuildDataOutput()
        {
            String xmlBuilder;

            xmlBuilder = "<netduino>";
            xmlBuilder += "<data timestamp='" + DateTime.Now.ToString() + "'>";

            xmlBuilder += "<temperature>";
            xmlBuilder += ProcessControl.Instance.Temperature.ToString("F2");
            xmlBuilder += "</temperature>";
            xmlBuilder += "<targettemperature>";
            xmlBuilder += ProcessControl.Instance.TargetTemperature.ToString("F2");
            xmlBuilder += "</targettemperature>";
            xmlBuilder += "<limitmaxtemperature>";
            xmlBuilder += ProcessControl.Instance.TemperatureMax.ToString("F2");
            xmlBuilder += "</limitmaxtemperature>";
            xmlBuilder += "<maxtemperaturereached>";
            xmlBuilder += ProcessControl.Instance.TemperatureMaxReached.ToString();
            xmlBuilder += "</maxtemperaturereached>";
            xmlBuilder += "<heatpower>";
            xmlBuilder += HeatingControl.Instance.HeatPower.ToString();
            xmlBuilder += "</heatpower>";

            xmlBuilder += "<relativehumidity>";
            xmlBuilder += ProcessControl.Instance.RelativeHumidity.ToString("F2");
            xmlBuilder += "</relativehumidity>";
            xmlBuilder += "<targetrelativehumidity>";
            xmlBuilder += ProcessControl.Instance.TargetRelativeHumidity.ToString("F2");
            xmlBuilder += "</targetrelativehumidity>";
            xmlBuilder += "<pumpstate>";
            xmlBuilder += PumpControl.Instance.PumpState.ToString();
            xmlBuilder += "</pumpstate>";
            xmlBuilder += "<pumpduration>";
            xmlBuilder += PumpControl.Instance.Duration.ToString();
            xmlBuilder += "</pumpduration>";

            xmlBuilder += "<co2>";
            xmlBuilder += ProcessControl.Instance.CO2.ToString();
            xmlBuilder += "</co2>";
            xmlBuilder += "<targetco2>";
            xmlBuilder += ProcessControl.Instance.TargetCO2.ToString();
            xmlBuilder += "</targetco2>";

            xmlBuilder += "<trapstate>";
            xmlBuilder += VentilationControl.Instance.TrapState.ToString();
            xmlBuilder += "</trapstate>";
            xmlBuilder += "<fanstate>";
            xmlBuilder += VentilationControl.Instance.FanState.ToString();
            xmlBuilder += "</fanstate>";
            xmlBuilder += "<ventilationduration>";
            xmlBuilder += VentilationControl.Instance.Duration.ToString();
            xmlBuilder += "</ventilationduration>";
            xmlBuilder += "<ventilationfanenabled>";
            xmlBuilder += VentilationControl.Instance.FanEnabled.ToString(); // Fan used
            xmlBuilder += "</ventilationfanenabled>";
            xmlBuilder += "<ventilationIntervaltarget>";
            xmlBuilder += VentilationControl.Instance.IntervalTargetMinutes.ToString(); //minutes
            xmlBuilder += "</ventilationIntervaltarget>";
            xmlBuilder += "<ventilationdurationtarget>";
            xmlBuilder += VentilationControl.Instance.DurationTargetSeconds.ToString(); // seconds
            xmlBuilder += "</ventilationdurationtarget>";
            xmlBuilder += "<ventilationdstate>";
            xmlBuilder += VentilationControl.Instance.State.ToString(); //Started or stopped
            xmlBuilder += "</ventilationdstate>";

            xmlBuilder += "<actuatormode>";
            xmlBuilder += ActuatorControl.Instance.Mode.ToString();
            xmlBuilder += "</actuatormode>";
            xmlBuilder += "<actuatorstate>";
            xmlBuilder += ActuatorControl.Instance.State.ToString();
            xmlBuilder += "</actuatorstate>";
            xmlBuilder += "<actuatorduration>";
            xmlBuilder += ActuatorControl.Instance.Duration.ToString();
            xmlBuilder += "</actuatorduration>";
            xmlBuilder += "</data>";
            xmlBuilder += "</netduino>";

            return xmlBuilder.ToString();
        }
        #endregion
    }
}
