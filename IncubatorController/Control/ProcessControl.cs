using System;
using Sensirion.SHT11;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;
using System.Text;
using System.Runtime.CompilerServices;
using System.Threading;

namespace NetduinoPlus.Controler
{
    public sealed class ProcessControl
    {
        private static readonly double TEMPERATURE_MAX = 39.5;

        #region Private Variables
        private static readonly ProcessControl _instance = new ProcessControl();
        private static readonly object _lockObject = new object();

        private MovingAverage _temperatureAverage = new MovingAverage();
        private MovingAverage _relativeHumidityAverage = new MovingAverage();

        private double _temperature = 0.0;
        private double _targetTemperature = 0.0;
        private double _temperatureMax = TEMPERATURE_MAX;
        private bool _temperatureMaxReached = false;


        private double _relativeHumidity = 0.0;
        private double _targetRelativeHumidity = 0.0;

        private int _CO2 = 0;
        private int _targetCO2 = 0;
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
        }

        public double TemperatureMax
        {
            get { return _temperatureMax; }
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
        #endregion

        #region Events
        #endregion

        #region Constructors
        public ProcessControl() 
        {
            InitFromConfigFile();
            ListenerThread.CommandReceived += new ReceivedEventHandler(OnParametersReceived);
        }
        #endregion

        #region Public Methods
        public void ProcessData()
        {
          lock (_lockObject)
          {
            ReadSensor();

            HeatingControl.Instance.ManageState();
            PumpControl.Instance.ManageState();
            VentilationControl.Instance.ManageState();
            ActuatorControl.Instance.ManageState();

            if (NetworkCommunication.Instance.IsSenderRunning)
            {
              NetworkCommunication.Instance.NotifySenderThread();
            }
          }
        }
        #endregion

        #region Private Methods
        public void ReadSensor()
        {
          ReadTemperature();
          ReadRelativeHumidity();
          ReadCO2();

          LogFile.Application(_temperature.ToString("F2") + "; " + _relativeHumidity.ToString("F2") + "; " + _CO2.ToString());
        }

        private void OnParametersReceived(String parameters)
        {
            lock (_lockObject)
            {
                string[] parts = parameters.Split(' ');

                if (parts[0] == "INIT")
                {
                    DateTime presentTime = new DateTime(int.Parse(parts[1]), int.Parse(parts[2]), int.Parse(parts[3]), int.Parse(parts[4]), int.Parse(parts[5]), int.Parse(parts[6]), int.Parse(parts[7]));
                    Utility.SetLocalTime(presentTime);

                    NetworkCommunication.Instance.StartSender();
                }
                else if (parts[0] == "TEMPERATURE_PARAMETERS")
                {
                    _targetTemperature = double.Parse(parts[1]);
                    _temperatureMax = double.Parse(parts[2]);

                    if (LogFile.SDCardAvailable)
                    {
                        ConfigFile.Instance.SetValue("Temperature", "Target", _targetTemperature);
                        ConfigFile.Instance.SetValue("Temperature", "Max", _targetTemperature);
                        ConfigFile.Instance.Save();
                    }
                }
                else if (parts[0] == "RELATIVE_HUMIDITY_PARAMETERS")
                {
                    _targetRelativeHumidity = double.Parse(parts[1]);
                    PumpControl.Instance.IntervalTargetMinutes = int.Parse(parts[2]);
                    PumpControl.Instance.DurationTargetSeconds = int.Parse(parts[3]);

                    if (LogFile.SDCardAvailable)
                    {
                        ConfigFile.Instance.SetValue("RelativeHumidity", "Target", _targetRelativeHumidity);
                        ConfigFile.Instance.SetValue("RelativeHumidity", "IntervalMinutes", PumpControl.Instance.IntervalTargetMinutes);
                        ConfigFile.Instance.SetValue("RelativeHumidity", "DurationSeconds", PumpControl.Instance.DurationTargetSeconds);
                        ConfigFile.Instance.Save();
                    }

                    PumpControl.Instance.Duration = TimeSpan.Zero;
                    PumpControl.Instance.PumpState = PumpControl.PumpStateEnum.Stopped;
                }
                else if (parts[0] == "PUMP_ACTIVATE")
                {
                    PumpControl.Instance.Activate(int.Parse(parts[1]));
                    PumpControl.Instance.Duration = TimeSpan.Zero;
                }
                else if (parts[0] == "VENTILATION_PARAMETERS")
                {
                    _targetCO2 = int.Parse(parts[1]);
                    VentilationControl.Instance.IntervalTargetMinutes = int.Parse(parts[2]);
                    VentilationControl.Instance.DurationTargetSeconds = int.Parse(parts[3]);

                    if (LogFile.SDCardAvailable)
                    {
                        ConfigFile.Instance.SetValue("CO2", "Target", _targetCO2);
                        ConfigFile.Instance.SetValue("CO2", "IntervalMinutes", VentilationControl.Instance.IntervalTargetMinutes);
                        ConfigFile.Instance.SetValue("CO2", "DurationSeconds", VentilationControl.Instance.DurationTargetSeconds);
                        ConfigFile.Instance.Save();
                    }

                }
                else if (parts[0] == "ACTUATOR_COMMAND")
                {
                    ActuatorControl.Instance.Command = (ActuatorControl.ActuatorCommand)int.Parse(parts[1]);
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

        public StringBuilder BuildDataOutput()
        {
          StringBuilder dataOutput = new StringBuilder();

          lock (_lockObject)
          {
            dataOutput.Append("<hatcher>");
            dataOutput.Append("<data timestamp='" + DateTime.Now.ToString() + "'>");

            dataOutput.Append("<temperature>");
            dataOutput.Append(_instance.Temperature.ToString("F2"));
            dataOutput.Append("</temperature>");
            dataOutput.Append("<maxtemperaturereached>");
            dataOutput.Append(_instance.TemperatureMaxReached.ToString());
            dataOutput.Append("</maxtemperaturereached>");
            dataOutput.Append("<heatpower>");
            dataOutput.Append(HeatingControl.Instance.HeatPower.ToString());
            dataOutput.Append("</heatpower>");

            dataOutput.Append("<relativehumidity>");
            dataOutput.Append(_instance.RelativeHumidity.ToString("F2"));
            dataOutput.Append("</relativehumidity>");
            dataOutput.Append("<pumpstate>");
            dataOutput.Append(PumpControl.Instance.PumpState.ToString());
            dataOutput.Append("</pumpstate>");
            dataOutput.Append("<pumpduration>");
            dataOutput.Append(PumpControl.Instance.Duration.ToString());
            dataOutput.Append("</pumpduration>");
            dataOutput.Append("<trapstate>");
            dataOutput.Append(VentilationControl.Instance.TrapState.ToString());
            dataOutput.Append("</trapstate>");
            dataOutput.Append("<fanstate>");
            dataOutput.Append(VentilationControl.Instance.FanState.ToString());
            dataOutput.Append("</fanstate>");

            dataOutput.Append("<co2>");
            dataOutput.Append(_instance.CO2.ToString());
            dataOutput.Append("</co2>");
            dataOutput.Append("<ventilationdstate>");
            dataOutput.Append(VentilationControl.Instance.State.ToString());
            dataOutput.Append("</ventilationdstate>");

            dataOutput.Append("<actuatorcommand>");
            dataOutput.Append(ActuatorControl.Instance.Command.ToString());
            dataOutput.Append("</actuatorcommand>");
            dataOutput.Append("<actuatorstate>");
            dataOutput.Append(ActuatorControl.Instance.State.ToString());
            dataOutput.Append("</actuatorstate>");
            dataOutput.Append("<actuatorduration>");
            dataOutput.Append(ActuatorControl.Instance.Duration.ToString());
            dataOutput.Append("</actuatorduration>");

            //////////////////////////////////////////////////////////////////////////

            dataOutput.Append("<targettemperature>");
            dataOutput.Append(_instance.TargetTemperature.ToString("F2"));
            dataOutput.Append("</targettemperature>");
            dataOutput.Append("<limitmaxtemperature>");
            dataOutput.Append(_instance.TemperatureMax.ToString("F2"));
            dataOutput.Append("</limitmaxtemperature>");

            dataOutput.Append("<targetrelativehumidity>");
            dataOutput.Append(_instance.TargetRelativeHumidity.ToString("F2"));
            dataOutput.Append("</targetrelativehumidity>");
            dataOutput.Append("<pumpintervaltarget>");
            dataOutput.Append(PumpControl.Instance.IntervalTargetMinutes.ToString());
            dataOutput.Append("</pumpintervaltarget>");
            dataOutput.Append("<pumpdurationtarget>");
            dataOutput.Append(PumpControl.Instance.DurationTargetSeconds.ToString());
            dataOutput.Append("</pumpdurationtarget>");

            dataOutput.Append("<targetco2>");
            dataOutput.Append(_instance.TargetCO2.ToString());
            dataOutput.Append("</targetco2>");
            dataOutput.Append("<ventilationintervaltarget>");
            dataOutput.Append(VentilationControl.Instance.IntervalTargetMinutes.ToString());
            dataOutput.Append("</ventilationintervaltarget>");
            dataOutput.Append("<ventilationdurationtarget>");
            dataOutput.Append(VentilationControl.Instance.DurationTargetSeconds.ToString());
            dataOutput.Append("</ventilationdurationtarget>");
            dataOutput.Append("<ventilationduration>");
            dataOutput.Append(VentilationControl.Instance.Duration.ToString());
            dataOutput.Append("</ventilationduration>");
            dataOutput.Append("<ventilationstandby>");
            dataOutput.Append(VentilationControl.Instance.Standby.ToString());
            dataOutput.Append("</ventilationstandby>");

            dataOutput.Append("</data>");
            dataOutput.Append("</hatcher>");
          }

          return dataOutput;
        }

        private void InitFromConfigFile()
        {
            if (LogFile.SDCardAvailable)
            {
                ConfigFile.Instance.Load(false);

                lock (_lockObject)
                {
                    _targetTemperature = ConfigFile.Instance.GetValue("Temperature", "Target", 0);
                    _temperatureMax = ConfigFile.Instance.GetValue("Temperature", "Max", 0);

                    _targetRelativeHumidity = ConfigFile.Instance.GetValue("RelativeHumidity", "Target", 0);
                    PumpControl.Instance.IntervalTargetMinutes = ConfigFile.Instance.GetValue("RelativeHumidity", "IntervalMinutes", 0);
                    PumpControl.Instance.DurationTargetSeconds = ConfigFile.Instance.GetValue("RelativeHumidity", "DurationSeconds", 0);

                    _targetCO2 = ConfigFile.Instance.GetValue("CO2", "Target", 0);
                    VentilationControl.Instance.IntervalTargetMinutes = ConfigFile.Instance.GetValue("CO2", "IntervalMinutes", 0);
                    VentilationControl.Instance.DurationTargetSeconds = ConfigFile.Instance.GetValue("CO2", "DurationSeconds", 0);
                }
            }
        }
        #endregion
    }
}
