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

            LogFile.Network("Local IP Address: " + networkInterface.IPAddress);

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
                    StartSender();
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
