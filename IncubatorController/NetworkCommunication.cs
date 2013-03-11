using System.Threading;
using System.Net.Sockets;
using System.Net;
using Microsoft.SPOT.Net.NetworkInformation;
using Microsoft.SPOT;
using System;
using System.Text;

namespace NetduinoPlus.Controler
{
    public delegate void ReceivedEventHandler(Socket clientSocket, String message);

    class NetworkCommunication
    {
        #region Private Variables
        private static NetworkCommunication _instance = null;
        private ListenerThread _listenerThread = null;
        #endregion


        #region Constructors
        private NetworkCommunication() 
        {
            NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;
            _listenerThread = new ListenerThread(ProcessControl.GetInstance());
        }
        #endregion


        #region Events
        #endregion


        #region Public Properties
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

    class ListenerThread
    {
        #region Private Variables
        private ProcessControl _processControl = null;
        private Thread _currentThread = null;
        private int _messageReceivedCount = 0;
        #endregion


        #region Constructors
        public ListenerThread( ProcessControl processControl ) 
        {
            _processControl = processControl;
            Start();
        }
        #endregion


        #region Events
        public static event ReceivedEventHandler EventHandlerMessageReceived;
        #endregion


        #region Public Properties
        #endregion


        #region Public Methods
        public void Start()
        {
            LogFile.Network("Starting network thread.");

            _messageReceivedCount = 0;
            _currentThread = new Thread(ListeningThread);
            _currentThread.Start();
        }

        public void Stop()
        {
            LogFile.Network("Stopping network thread.");

            if (_currentThread != null)
            {
                if (_currentThread.IsAlive)
                {
                    _currentThread.Abort();
                }

                _currentThread = null;
            }
        }
        #endregion


        #region Private Methods
        private bool SocketConnected(Socket clientSocket)
        {
            return !(clientSocket.Poll(5000000, SelectMode.SelectRead) & (clientSocket.Available == 0));
        }

        private void ListeningThread()
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

                using (Socket socketListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    socketListener.Bind(new IPEndPoint(IPAddress.Any, 250));
                    socketListener.Listen(1);

                    while (true)
                    {
                        LogFile.Network("Listening for connection...");

                        using (Socket clientSocket = socketListener.Accept())
                        {
                            LogFile.Network("Connection Accept: " + clientSocket.RemoteEndPoint.ToString());

                            if (SocketConnected(clientSocket))
                            {
                                byte[] buffer = new byte[clientSocket.Available];

                                clientSocket.Receive(buffer);

                                if (buffer.Length > 0)
                                {
                                    String message = new String(Encoding.UTF8.GetChars(buffer));


                                    String dataOutput = ProcessControl.GetInstance().BuildDataOutput();


                                    _messageReceivedCount++;

                                    LogFile.Network("Message: Count=" + _messageReceivedCount.ToString() + ", Size=" + buffer.Length.ToString() + ", Value=" + message);

                                    if (EventHandlerMessageReceived != null)
                                    {
                                        EventHandlerMessageReceived(clientSocket, message);
                                    }
                                }
                            }
                        }
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
        #endregion
    }
}
