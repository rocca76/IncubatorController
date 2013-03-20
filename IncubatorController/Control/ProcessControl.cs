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

        private static StringBuilder _dataOutput = new StringBuilder();
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

        public StringBuilder DataOutput
        {
          get { return _dataOutput; }
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
        #endregion

        #region Events
        #endregion

        #region Constructors
        public ProcessControl() 
        {
            ListenerThread.CommandReceived += new ReceivedEventHandler(OnParametersReceived);
        }
        #endregion

        #region Public Methods
        public void ReadSensor()
        {
            ReadTemperature();
            ReadRelativeHumidity();
            ReadCO2();

            LogFile.Application(_temperature.ToString("F2") + "; " + _relativeHumidity.ToString("F2") + "; " + _CO2.ToString());
        }

        //[MethodImpl(MethodImplOptions.Synchronized)]
        public void ProcessData()
        {
            HeatingControl.Instance.ManageState();
            PumpControl.Instance.ManageState();
            VentilationControl.Instance.ManageState();
            ActuatorControl.Instance.ManageState();

            if (NetworkCommunication.Instance.IsSenderRunning)
            {
              BuildDataOutput();      
              NetworkCommunication.Instance.NotifySenderThread();
            }
        }
        #endregion

        #region Private Methods
        private void OnParametersReceived(String parameters)
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
            }
            else if (parts[0] == "RELATIVE_HUMIDITY_PARAMETERS")
            {
              _targetRelativeHumidity = double.Parse(parts[1]);
              PumpControl.Instance.IntervalTargetMinutes = int.Parse(parts[2]);
              PumpControl.Instance.DurationTargetSeconds = int.Parse(parts[3]);
            }
            else if (parts[0] == "PUMP_ACTIVATE")
            {
                PumpControl.Instance.Activate(int.Parse(parts[1]));
            }
            else if (parts[0] == "VENTILATION_PARAMETERS")
            {
              _instance.TargetCO2 = int.Parse(parts[1]);
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

        public void BuildDataOutput()
        {
          lock (_dataOutput)
          {
            _dataOutput.Clear();

            _dataOutput.Append("<netduino>");
            _dataOutput.Append("<data timestamp='" + DateTime.Now.ToString() + "'>");

            _dataOutput.Append("<temperature>");
            _dataOutput.Append(_instance.Temperature.ToString("F2"));
            _dataOutput.Append("</temperature>");
            _dataOutput.Append("<targettemperature>");
            _dataOutput.Append(_instance.TargetTemperature.ToString("F2"));
            _dataOutput.Append("</targettemperature>");
            _dataOutput.Append("<limitmaxtemperature>");
            _dataOutput.Append(_instance.TemperatureMax.ToString("F2"));
            _dataOutput.Append("</limitmaxtemperature>");
            _dataOutput.Append("<maxtemperaturereached>");
            _dataOutput.Append(_instance.TemperatureMaxReached.ToString());
            _dataOutput.Append("</maxtemperaturereached>");
            _dataOutput.Append("<heatpower>");
            _dataOutput.Append(HeatingControl.Instance.HeatPower.ToString());
            _dataOutput.Append("</heatpower>");

            _dataOutput.Append("<relativehumidity>");
            _dataOutput.Append(_instance.RelativeHumidity.ToString("F2"));
            _dataOutput.Append("</relativehumidity>");
            _dataOutput.Append("<targetrelativehumidity>");
            _dataOutput.Append(_instance.TargetRelativeHumidity.ToString("F2"));
            _dataOutput.Append("</targetrelativehumidity>");
            _dataOutput.Append("<pumpstate>");
            _dataOutput.Append(PumpControl.Instance.PumpState.ToString());
            _dataOutput.Append("</pumpstate>");
            _dataOutput.Append("<pumpduration>");
            _dataOutput.Append(PumpControl.Instance.Duration.ToString());
            _dataOutput.Append("</pumpduration>");
            _dataOutput.Append("<pumpintervaltarget>");
            _dataOutput.Append(PumpControl.Instance.IntervalTargetMinutes.ToString());
            _dataOutput.Append("</pumpintervaltarget>");
            _dataOutput.Append("<pumpdurationtarget>");
            _dataOutput.Append(PumpControl.Instance.DurationTargetSeconds.ToString());
            _dataOutput.Append("</pumpdurationtarget>");

            _dataOutput.Append("<co2>");
            _dataOutput.Append(_instance.CO2.ToString());
            _dataOutput.Append("</co2>");
            _dataOutput.Append("<targetco2>");
            _dataOutput.Append(_instance.TargetCO2.ToString());
            _dataOutput.Append("</targetco2>");

            _dataOutput.Append("<trapstate>");
            _dataOutput.Append(VentilationControl.Instance.TrapState.ToString());
            _dataOutput.Append("</trapstate>");
            _dataOutput.Append("<fanstate>");
            _dataOutput.Append(VentilationControl.Instance.FanState.ToString());
            _dataOutput.Append("</fanstate>");
            _dataOutput.Append("<ventilationdstate>");
            _dataOutput.Append(VentilationControl.Instance.State.ToString());
            _dataOutput.Append("</ventilationdstate>");

            _dataOutput.Append("<actuatormode>");
            _dataOutput.Append(ActuatorControl.Instance.Mode.ToString());
            _dataOutput.Append("</actuatormode>");
            _dataOutput.Append("<actuatorstate>");
            _dataOutput.Append(ActuatorControl.Instance.State.ToString());
            _dataOutput.Append("</actuatorstate>");
            _dataOutput.Append("<actuatorduration>");
            _dataOutput.Append(ActuatorControl.Instance.Duration.ToString());
            _dataOutput.Append("</actuatorduration>");
            _dataOutput.Append("</data>");
            _dataOutput.Append("</netduino>");
          }
        }
        #endregion
    }
}
