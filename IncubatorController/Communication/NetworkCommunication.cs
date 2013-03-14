using System.Threading;
using System.Net.Sockets;
using System.Net;
using Microsoft.SPOT.Net.NetworkInformation;
using System;
using System.Text;
using Toolbox.NETMF.NET;

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
        private bool _networkIsAvailable = true;
        #endregion


        #region Constructors
        private NetworkCommunication()
        {
            try
            {
                NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;

                _listenerThread = new ListenerThread();
                _listenerThread.Start();
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
        public void StartSender()
        {
            _senderThread = new SenderThread();
            _senderThread.Start();
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
            try
            {
                _networkIsAvailable = network.IsAvailable;

                if (_networkIsAvailable)
                {
                    LogFile.Network("Network Available.");

                    _listenerThread.Start();
                }
                else
                {
                    LogFile.Network("Network Unavailable.");

                    _senderThread.Stop();
                    _listenerThread.Stop();
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
        #endregion
    }
}
