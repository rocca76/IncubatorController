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
        #endregion


        #region Constructors
        private NetworkCommunication()
        {
            try
            {
                NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;

                //_listenerThread = new ListenerThread();
                //_listenerThread.Start();

                //_senderThread = new SenderThread();
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

        public String RemoteAddress
        {
            get { return _remoteAddress; }
            set { _remoteAddress = value; }
        }
        #endregion


        #region Public Static Methods
        public void StartSender()
        {
            _senderThread.Start();
        }

        public void NotifySender()
        {
          _senderThread.Notify.Set();
        }
        #endregion


        #region Private Static Methods
        private void NetworkChange_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs network)
        {
            try
            {
                if (network.IsAvailable)
                {
                    LogFile.Network("Network Available");

                    _listenerThread.Start();
                }
                else
                {
                    LogFile.Network("Network Unavailable");

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


        #region Private Methods
        #endregion
    }
}
