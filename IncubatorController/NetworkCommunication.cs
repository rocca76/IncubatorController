using System.Threading;
using System.Net.Sockets;
using System.Net;
using Microsoft.SPOT.Net.NetworkInformation;
using Microsoft.SPOT;
using System;
using System.Text;
using System.Diagnostics;
using Toolbox.NETMF.NET;

namespace NetduinoPlus.Controler
{
    public delegate void ReceivedEventHandler(String command);

    class NetworkCommunication
    {
        #region Private Variables
        private static NetworkCommunication _instance = null;
        private ListenerThread _listenerThread = null;
        private SenderThread _senderThread = null;
        private String _remoteAddress = "";
        #endregion


        #region Constructors
        private NetworkCommunication() 
        {
            NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;
            _listenerThread = new ListenerThread();
            _senderThread = new SenderThread();
        }
        #endregion


        #region Events
        #endregion


        #region Public Properties
        public String RemoteAddress
        {
            get { return _remoteAddress; }
            set { _remoteAddress = value; }
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

        public static NetworkCommunication GetInstance()
        {
          return _instance;
        }

        public void InitializeSender()
        {
          if (_senderThread != null)
          {
            _senderThread.Start();
          }
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


    class ListenerThread
    {
        #region Private Variables
        private Thread _currentThread = null;
        private int _commandReceivedCount = 0;
        #endregion


        #region Constructors
        public ListenerThread() 
        {
            Start();
        }
        #endregion


        #region Events
        public static event ReceivedEventHandler CommandReceived;
        #endregion


        #region Public Properties
        #endregion


        #region Public Methods
        public void Start()
        {
            LogFile.Network("Starting listener thread.");

            _commandReceivedCount = 0;

            _currentThread = new Thread(ListeningThread);
            _currentThread.Start();
        }

        public void Stop()
        {
            LogFile.Network("Stopping listener thread.");

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
                    socketListener.Bind(new IPEndPoint(IPAddress.Any, 11000));
                    socketListener.Listen(1);

                    while (true)
                    {
                        LogFile.Network("Listening for connection...");

                        using (Socket clientSocket = socketListener.Accept())
                        {
                            string[] remoteEndPoint = clientSocket.RemoteEndPoint.ToString().Split(':');
                            NetworkCommunication.GetInstance().RemoteAddress = remoteEndPoint[0];

                            LogFile.Network("Connection Accept from " + NetworkCommunication.GetInstance().RemoteAddress);

                            if (SocketConnected(clientSocket))
                            {
                                byte[] buffer = new byte[clientSocket.Available];

                                clientSocket.Receive(buffer);

                                if (buffer.Length > 0)
                                {
                                    _commandReceivedCount++;

                                    String command = new String(Encoding.UTF8.GetChars(buffer));

                                    LogFile.Network("Message Received: " + _commandReceivedCount.ToString() + ", Size: " + buffer.Length.ToString() + ", Value: " + command);

                                    if (CommandReceived != null)
                                    {
                                        CommandReceived(command);
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


    class SenderThread
    {
        #region Private Variables
        private Thread _currentThread = null;
        private int _dataSentCount = 0;
        private static ManualResetEvent _manualResetEvent = new ManualResetEvent(false);
        private SimpleSocket _clientSocket = null;

        private bool _cancelSender = false;
        #endregion


        #region Constructors
        public SenderThread() { }
        #endregion


        #region Events
        #endregion


        #region Public Properties
        public ManualResetEvent Notify
        {
            get { return _manualResetEvent; }
        }
        #endregion


        #region Public Methods
        public void Start()
        {
            if (_currentThread != null)
            {
                if (_currentThread.IsAlive)
                {
                    if (_clientSocket.IsConnected)
                    {
                        _cancelSender = true;
                        _manualResetEvent.Set();
                    }
                    else
                    {
                        if (_clientSocket != null)
                        {
                            _clientSocket.Close();
                            _clientSocket = null;
                        }
                    }
                }
            }

            _cancelSender = false;
            _dataSentCount = 0;

            _currentThread = new Thread(SendingThread);
            _currentThread.Start();
        }
        #endregion


        #region Private Methods
        private void SendingThread()
        {
            try
            {
                String remoteAddress = NetworkCommunication.GetInstance().RemoteAddress;
                LogFile.Network("Connecting to " + remoteAddress.ToString());

                _clientSocket = new IntegratedSocket(remoteAddress, 11000);
                _clientSocket.Connect();

                while (_cancelSender == false)
                {
                    Stopwatch stopwatch = Stopwatch.StartNew();

                    _manualResetEvent.WaitOne();

                    String stateOutput = ProcessControl.GetInstance().BuildStateOutput();

                    _clientSocket.Send(stateOutput);
                    _dataSentCount++;

                    _manualResetEvent.Reset();

                    stopwatch.Stop();

                    LogFile.Network("Message Sent: " + stopwatch.ElapsedMilliseconds.ToString()  + "ms, " + _dataSentCount.ToString() + ", Size: " + stateOutput.Length.ToString() + ", Value: " + stateOutput);
                }

                _clientSocket.Close();
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
