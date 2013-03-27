using System.Threading;
using System.Net.Sockets;
using System.Net;
using Microsoft.SPOT.Net.NetworkInformation;
using System;
using System.Text;
using MyNetduino.ICMP;

namespace NetduinoPlus.Controler
{
    public delegate void ReceivedEventHandler(String command);

    public sealed class NetworkCommunication
    {
        #region Private Variables
        private static readonly NetworkCommunication _instance = new NetworkCommunication();
        private ListenerThread _listenerThread = null;
        private SenderThread _senderThread = null;
        private String _remoteAddress = "";
        private static bool _networkIsAvailable = false;
        private int _port = 0;
        #endregion


        #region Constructors
        private NetworkCommunication()
        {
          NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;
        }
        #endregion


        #region Events
        #endregion


        #region Public Properties
        public static NetworkCommunication Instance
        {
            get { return _instance; }
        }

        public bool NetworkIsAvailable
        {
            get { return _networkIsAvailable; }
            set { _networkIsAvailable = value; }
        }

        public String RemoteAddress
        {
            get { return _remoteAddress; }
            set { _remoteAddress = value; }
        }

        public int Port
        {
            get { return _port; }
        }

        public bool IsSenderRunning
        {
            get { return _senderThread != null; }
        }
        #endregion


        #region Public Methods
        public void DetectAvailability()
        {
            NetworkInterface networkInterface = NetworkInterface.GetAllNetworkInterfaces()[0];

            if (networkInterface.IsDhcpEnabled)
            {
                while (networkInterface.IPAddress == "0.0.0.0")
                {
                    LogFile.Network("Awaiting IP Address");
                    Thread.Sleep(1000);
                }
            }

            string[] parts = networkInterface.IPAddress.Split('.');

            if (parts[3] == "200")
            {
                _port = 11000;
            }
            else if (parts[3] == "201")
            {
                _port = 11001;
            }

            LogFile.Network("Local Address: " + networkInterface.IPAddress + ", " + _port.ToString());

            //_networkIsAvailable = Ping.PingHost("192.168.10.100");
            //NTPTime.SetLocalTime();
            _networkIsAvailable = true;
            NetworkAvailability(_networkIsAvailable);
        }

        public void NetworkAvailability(bool IsAvailable)
        {
            try
            {
                _networkIsAvailable = IsAvailable;

                if (_networkIsAvailable)
                {
                    LogFile.Network("Network Available.");
                    StartListener();
                }
                else
                {
                    LogFile.Network("Network Unavailable.");

                    if (_senderThread != null)
                    {
                        _senderThread.Stop();
                        _senderThread = null;
                    }

                    if (_listenerThread != null)
                    {
                        _listenerThread.Stop();
                        _listenerThread = null;
                    }
                }
            }
            catch (SocketException se)
            {
                LogFile.Network(se.ToString());
            }
            catch (Exception ex)
            {
                LogFile.Network(ex.ToString());
            }
        }

        public void StartSender()
        {
          _senderThread = new SenderThread();
          _senderThread.Start();
        }

        public void StartListener()
        {
          _listenerThread = new ListenerThread();
          _listenerThread.Start();
        }

        public void NotifySenderThread()
        {
            if (_senderThread != null)
            {
                _senderThread.ResetEvent.Set();
            }
        }
        #endregion


        #region Private Methods
        private void NetworkChange_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs network)
        {
            NetworkAvailability(network.IsAvailable);
        }
        #endregion
    }
}
