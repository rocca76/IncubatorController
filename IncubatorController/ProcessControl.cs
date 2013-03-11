using System;
using Sensirion.SHT11;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;
using System.Net.Sockets;
using System.Text;

namespace NetduinoPlus.Controler
{
    class ProcessControl
    {
        #region Private Variables
        private static ProcessControl _instance = null;
        private static readonly object LockObject = new object();
        private MovingAverage _temperatureAverage = new MovingAverage();
        private MovingAverage _relativeHumidityAverage = new MovingAverage();

        private double _currentTemperature = 0.0;
        private double _targetTemperature = 0.0;
        private double _limitMaxTemperature = 39.5;
        private int _heatPower = 0;
        private int _maxTemperatureLimitReached = 0;

        private double _currentRelativeHumidity = 0.0;
        private double _targetRelativeHumidity = 0.0;

        private OutputPort out250W = new OutputPort(Pins.GPIO_PIN_D4, false);  //250W
        private OutputPort out500W = new OutputPort(Pins.GPIO_PIN_D5, false);  //500W
        #endregion
        
        #region Public Properties
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
        #endregion

        #region Events
        #endregion

        #region Constructors
        public ProcessControl() 
        {
            ListenerThread.EventHandlerMessageReceived += new ReceivedEventHandler(OnMessageReceived);
            SHT11Sensor.InitInstance();
        }
        #endregion

        #region Public Methods
        public static ProcessControl GetInstance()
        {
            lock (LockObject)
            {
                if (_instance == null)
                {
                    _instance = new ProcessControl();
                }

                return _instance;
            }
        }

        public void LoadConfiguration()
        {
            ConfigurationManager.Load();
        }

        public void ProcessData()
        {
            lock (LockObject)
            {
                ReadSensor();

                ManageHeatingState();
                SetOutputPin();

                PumpControl.GetInstance().ManageState();
                VentilationControl.GetInstance().ManageState();
                ActuatorControl.GetInstance().ManageState();
            }
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
        private void OnMessageReceived(Socket clientSocket, String message)
        {
            string[] parts = message.Split(' ');

            if (parts[0] == "TIME")
            {
                DateTime presentTime = new DateTime(int.Parse(parts[1]), int.Parse(parts[2]), int.Parse(parts[3]), int.Parse(parts[4]), int.Parse(parts[5]), int.Parse(parts[6]), int.Parse(parts[7]));
                Utility.SetLocalTime(presentTime);
            }
            else if (parts[0] == "GET_DATA")
            {
                String dataOutput = BuildDataOutput();
                int timeout = clientSocket.SendTimeout;
                int sent = clientSocket.Send(Encoding.UTF8.GetBytes(dataOutput));
                sent = 0;
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
                ProcessControl.GetInstance().SetActuatorOpen(int.Parse(parts[1]));
            }
            else if (parts[0] == "ACTUATOR_CLOSE")
            {
                ProcessControl.GetInstance().SetActuatorClose(int.Parse(parts[1]));
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
            VentilationControl.GetInstance().ReadCO2();
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

        public String BuildDataOutput()
        {
            StringBuilder xmlBuilder = new StringBuilder();

            lock (LockObject)
            {
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
            }

            return xmlBuilder.ToString();
        }
        #endregion
    }
}
