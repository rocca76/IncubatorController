using System;
using Sensirion.SHT11;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;
using System.Text;

namespace NetduinoPlus.Controler
{
    public sealed class ProcessControl
    {
        public static readonly int CO2_DISABLE = 9999;

        #region Private Variables
        private static readonly ProcessControl _instance = new ProcessControl();
        private static readonly object _lockObject = new object();
        private MovingAverage _temperatureAverage = new MovingAverage();
        private MovingAverage _relativeHumidityAverage = new MovingAverage();

        private double _currentTemperature = 0.0;
        private double _targetTemperature = 0.0;
        private double _limitMaxTemperature = 39.5;
        private int _heatPower = 0;
        private int _maxTemperatureLimitReached = 0;

        private double _currentRelativeHumidity = 0.0;
        private double _targetRelativeHumidity = 0.0;

        private int _currentCO2 = 0;
        private int _targetCO2 = CO2_DISABLE;

        private OutputPort out250W = new OutputPort(Pins.GPIO_PIN_D4, false);  //250W
        private OutputPort out500W = new OutputPort(Pins.GPIO_PIN_D5, false);  //500W
        #endregion
        
        #region Public Properties
        public static ProcessControl Instance
        {
          get { return _instance; }
        }

        public double CurrentTemperature
        {
            get { return _currentTemperature; }
            set { _currentTemperature = value; }
        }

        public double TargetTemperature
        {
            get { return _targetTemperature; }
            set { _targetTemperature = value; }
        }

        public double LimitMaxTemperature
        {
            get { return _limitMaxTemperature; }
            set { _limitMaxTemperature = value; }
        }

        public int HeatPower
        {
            get { return _heatPower; }
            set { _heatPower = value; }
        }

        public int MaxTemperatureLimitReached
        {
            get { return _maxTemperatureLimitReached; }
            set { _maxTemperatureLimitReached = value; }
        }

        public double CurrentRelativeHumidity
        {
            get { return _currentRelativeHumidity; }
            set { _currentRelativeHumidity = value; }
        }

        public double TargetRelativeHumidity
        {
            get { return _targetRelativeHumidity; }
            set { _targetRelativeHumidity = value; }
        }

        public int CurrentCO2
        {
            get { return _currentCO2; }
            set { _currentCO2 = value; }
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
            ListenerThread.CommandReceived += new ReceivedEventHandler(OnCommandReceived);
            SHT11Sensor.InitInstance();
        }
        #endregion

        #region Public Methods
        public void LoadConfiguration()
        {
            ConfigurationManager.Load();
        }

        public void ProcessData()
        {
          lock(_lockObject)
          {
            ReadSensor();

            ManageHeatingState();
            SetOutputPin();

            PumpControl.GetInstance().ManageState();
            VentilationControl.GetInstance().ManageState();
            ActuatorControl.GetInstance().ManageState();
          }

          NetworkCommunication.Instance.NotifySender();
        }

        public void SetActuatorMode(String mode)
        {
            ActuatorControl.GetInstance().SetMode(mode);
        }

        public void SetActuatorOpen(int open)
        {
            ActuatorControl.GetInstance().Open(open);
        }

        public void SetActuatorClose(int close)
        {
            ActuatorControl.GetInstance().Close(close);
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
                ProcessControl.Instance.LimitMaxTemperature = double.Parse(parts[1]);
            }
            else if (parts[0] == "TARGET_RELATIVE_HUMIDITY")
            {
                ProcessControl.Instance.TargetRelativeHumidity = double.Parse(parts[1]);
            }
            else if (parts[0] == "TARGET_VENTILATION")
            {
                VentilationControl.GetInstance().FanEnabled = int.Parse(parts[1]);
                VentilationControl.GetInstance().IntervalTargetMinutes = int.Parse(parts[2]);
                VentilationControl.GetInstance().DurationTargetSeconds = int.Parse(parts[3]);
                ProcessControl.Instance.TargetCO2 = int.Parse(parts[4]);
            }
            else if (parts[0] == "ACTUATOR_MODE")
            {
                ProcessControl.Instance.SetActuatorMode(parts[1]);
            }
            else if (parts[0] == "ACTUATOR_OPEN")
            {
                ProcessControl.Instance.SetActuatorOpen(int.Parse(parts[1]));
            }
            else if (parts[0] == "ACTUATOR_CLOSE")
            {
                ProcessControl.Instance.SetActuatorClose(int.Parse(parts[1]));
            }
        }

        private void ReadSensor()
        {
            ReadTemperature();
            ReadRelativeHumidity();
            ReadCO2();
        }

        private void ManageHeatingState()
        {
            if (_currentTemperature > 0)
            {
                if (_currentTemperature < (TargetTemperature - 0.5))
                {
                    HeatPower = 750;
                }
                else if (_currentTemperature >= (TargetTemperature - 0.5) && _currentTemperature < (TargetTemperature - 0.25))
                {
                    HeatPower = 500;
                }
                else if (_currentTemperature >= (TargetTemperature - 0.25) && _currentTemperature < TargetTemperature)
                {
                    HeatPower = 250;
                }
                else if (_currentTemperature >= TargetTemperature)
                {
                    HeatPower = 0;
                }
            }
            else
            {
                HeatPower = 0;
            }

            if (_currentTemperature > LimitMaxTemperature)
            {
                MaxTemperatureLimitReached = 1;
                HeatPower = 0;
            }
            else
            {
                MaxTemperatureLimitReached = 0;
            }
        }

        private void ReadTemperature()
        {
            double temperature = SHT11Sensor.ReadTemperature();
            _temperatureAverage.Push(temperature);
            _currentTemperature = _temperatureAverage.Average;

            LogFile.Application("Temperature: RAW = " + temperature.ToString("F2") + "  Average = " + _currentTemperature.ToString("F2"));
        }

        private void ReadRelativeHumidity()
        {
            double relativeHumidity = SHT11Sensor.ReadRelativeHumidity();
            _relativeHumidityAverage.Push(relativeHumidity);
            _currentRelativeHumidity = _relativeHumidityAverage.Average;

            LogFile.Application("HR: RAW = " + relativeHumidity.ToString("F2") + "  Average = " + _currentRelativeHumidity.ToString("F2"));
        }

        private void ReadCO2()
        {
            int co2Data = 0;
            K30Sensor.ECO2Result result = K30Sensor.Instance.ReadCO2(ref co2Data);
            LogFile.Application("CO2: " + co2Data.ToString());

            if (result == K30Sensor.ECO2Result.ValidResult)
            {
                _currentCO2 = co2Data;
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

        private void SetOutputPin()
        {
            switch (HeatPower)
            {
                case 0:
                    {
                        out250W.Write(false);
                        out500W.Write(false);
                    }
                    break;
                case 250:
                    {
                        out250W.Write(true);
                        out500W.Write(false);
                    }
                    break;
                case 500:
                    {
                        out250W.Write(false);
                        out500W.Write(true);
                    }
                    break;
                case 750:
                    {
                        out250W.Write(true);
                        out500W.Write(true);
                    }
                    break;
            }
        }

        public String BuildStateOutput()
        {
            StringBuilder xmlBuilder = new StringBuilder();

            
            xmlBuilder.Append("<netduino>");
            xmlBuilder.Append("<data timestamp='" + DateTime.Now.ToString() + "'>");

            xmlBuilder.Append("<temperature>");
            xmlBuilder.Append(ProcessControl.Instance.CurrentTemperature.ToString("F2"));
            xmlBuilder.Append("</temperature>");
            xmlBuilder.Append("<targettemperature>");
            xmlBuilder.Append(ProcessControl.Instance.TargetTemperature.ToString("F2"));
            xmlBuilder.Append("</targettemperature>");
            xmlBuilder.Append("<limitmaxtemperature>");
            xmlBuilder.Append(ProcessControl.Instance.LimitMaxTemperature.ToString("F2"));
            xmlBuilder.Append("</limitmaxtemperature>");
            xmlBuilder.Append("<maxtemperaturereached>");
            xmlBuilder.Append(ProcessControl.Instance.MaxTemperatureLimitReached.ToString());
            xmlBuilder.Append("</maxtemperaturereached>");
            xmlBuilder.Append("<heatpower>");
            xmlBuilder.Append(ProcessControl.Instance.HeatPower.ToString());
            xmlBuilder.Append("</heatpower>");

            xmlBuilder.Append("<relativehumidity>");
            xmlBuilder.Append(ProcessControl.Instance.CurrentRelativeHumidity.ToString("F2"));
            xmlBuilder.Append("</relativehumidity>");
            xmlBuilder.Append("<targetrelativehumidity>");
            xmlBuilder.Append(ProcessControl.Instance.TargetRelativeHumidity.ToString("F2"));
            xmlBuilder.Append("</targetrelativehumidity>");
            xmlBuilder.Append("<pumpstate>");
            xmlBuilder.Append(PumpControl.GetInstance().PumpState.ToString());
            xmlBuilder.Append("</pumpstate>");
            xmlBuilder.Append("<pumpduration>");
            xmlBuilder.Append(PumpControl.GetInstance().Duration.ToString());
            xmlBuilder.Append("</pumpduration>");

            xmlBuilder.Append("<co2>");
            xmlBuilder.Append(ProcessControl.Instance.CurrentCO2.ToString());
            xmlBuilder.Append("</co2>");
            xmlBuilder.Append("<targetco2>");
            xmlBuilder.Append(ProcessControl.Instance.TargetCO2.ToString());
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


            return xmlBuilder.ToString();
        }
        #endregion
    }
}
