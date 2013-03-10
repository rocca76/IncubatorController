using System.Threading;
using System.Net.Sockets;
using System.Net;
using Microsoft.SPOT.Net.NetworkInformation;
using Microsoft.SPOT;
using System;
using System.Text;

namespace NetduinoPlus.Controler
{
    public delegate void ReceivedEventHandler(String message);

    class NetworkCommunication
    {
        #region Private Variables
        private static Thread _listeningThread = null;
        private static NetworkCommunication _instance = null;
        private static bool _networkIsAvailable = false;
        private static int _messageSentCount = 0;
        private static int _messageReceivedCount = 0;
        #endregion


        #region Constructors
        private NetworkCommunication() 
        {
            NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;
        }
        #endregion


        #region Events
        public static event ReceivedEventHandler EventHandlerMessageReceived;
        #endregion


        #region Public Properties
        public bool NetworkAvailable
        {
            get { return _networkIsAvailable; }
        }
        #endregion


        #region Public Static Methods
        public static void InitInstance()
        {
            if (_instance == null)
            {
                _instance = new NetworkCommunication();
            }
        }
        #endregion


        #region Private Static Methods
        private void NetworkChange_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs network)
        {
            _networkIsAvailable = network.IsAvailable;

            LogFile.Network(DateTime.UtcNow.ToString("u") + " " + (_networkIsAvailable ? "Online" : "Offline"));

            if (_networkIsAvailable)
            {
                _messageReceivedCount = 0;
                _instance.ListeningThread();
            }
            else
            {
                ShutdownListener();
            }
        }
        #endregion


        #region Private Methods
        private void ListeningThread()
        {
            _listeningThread = new Thread(new ThreadStart(ReceiveSocketsInListeningThread));
            _listeningThread.Start();
        }

        private void ReceiveSocketsInListeningThread()
        {
            try
            {
                LogFile.Network("Waiting for valid IP address...");

                NetworkInterface networkInterface = NetworkInterface.GetAllNetworkInterfaces()[0];

                while (networkInterface.IPAddress.ToString() == "0.0.0.0")
                {
                    Thread.Sleep(1000);
                }

                LogFile.Network("IP address: " + networkInterface.IPAddress.ToString());

                using (Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    listener.Bind(new IPEndPoint(IPAddress.Any, 250));
                    listener.Listen(1);

                    while (true)
                    {
                        LogFile.Network("Listening for connection...");

                        using (Socket socket = listener.Accept())
                        {
                            if (SocketConnected(socket))
                            {
                                byte[] buffer = new byte[socket.Available];

                                socket.Receive(buffer);

                                if (buffer.Length > 0)
                                {
                                    RaiseMessageReceivedEvent(new String(Encoding.UTF8.GetChars(buffer)));
                                }
                            }
                        }
                    }
                }
            }
            catch (SocketException se)
            {
                LogFile.Exception(se.ToString());
            }
        }

        private bool SocketConnected(Socket client)
        {
            return !(client.Poll(5000000, SelectMode.SelectRead) & (client.Available == 0));
        }

        private void RaiseMessageReceivedEvent(String message)
        {
            _messageReceivedCount++;
            LogFile.Network("Message Received " + _messageReceivedCount.ToString() + " : [ " + message + " ]");

            if (message == "EXIT")
            {
                //LogFile.Network("Message Sent " + _messageSentCount.ToString() + " : [ " + requestSender.Message + " ]");
                //_messageSentCount++;
                //socket.Send(Encoding.UTF8.GetBytes(Message));
            }

            if (EventHandlerMessageReceived != null)
            {
                EventHandlerMessageReceived(message);
            }
        }

        private void ShutdownListener()
        {
            try
            {
                if (_listeningThread != null)
                {
                    if (_listeningThread.IsAlive)
                    {
                        _listeningThread.Abort();
                    }

                    _listeningThread = null;
                }
            }
            catch (Exception ex)
            {
                LogFile.Exception(ex.ToString());
            }
        }
        #endregion
    }
}
