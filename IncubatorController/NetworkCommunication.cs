using System.Threading;
using System.Net.Sockets;
using System.Net;
using Microsoft.SPOT.Net.NetworkInformation;
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
        static readonly NetworkCommunication _instance = new NetworkCommunication();
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

                _listenerThread = new ListenerThread();
                _listenerThread.Start();

                _senderThread = new SenderThread();
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


    class ListenerThread
    {
        #region Private Variables
        private Thread _listenerThread = null;
        private int _commandReceivedCount = 0;
        private Socket _socketListener = null;
        #endregion


        #region Constructors
        #endregion


        #region Events
        public static event ReceivedEventHandler CommandReceived;
        #endregion


        #region Public Properties
        #endregion


        #region Public Methods
        public void Start()
        {
            NetworkInterface networkInterface = NetworkInterface.GetAllNetworkInterfaces()[0];

            do
            {
                LogFile.Network("Awaiting IP Address");
                Thread.Sleep(1000);
            }
            while (networkInterface.IPAddress == "0.0.0.0");

            LogFile.Network("IP Address Granted: " + networkInterface.IPAddress);

            _socketListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socketListener.Bind(new IPEndPoint(IPAddress.Any, 11000));
            _socketListener.Listen(1);

            _commandReceivedCount = 0;

            _listenerThread = new Thread(WaitForConnection);
            _listenerThread.Start();
        }

        public void Stop()
        {
            if (_socketListener != null)
            {
                using (_socketListener)
                {
                    _socketListener.Close();
                }

                _socketListener = null; ////?????
            }

            if (_listenerThread != null)
            {
                if (_listenerThread.IsAlive)
                {
                    _listenerThread.Abort();
                }

                _listenerThread = null;
            }
        }
        #endregion


        #region Private Methods
        private void WaitForConnection()
        {
            try
            {
                while (true)
                {
                    LogFile.Network("Waiting for connection...");

                    Socket clientSocket = _socketListener.Accept();

                    string[] remoteEndPoint = clientSocket.RemoteEndPoint.ToString().Split(':');
                    NetworkCommunication.Instance.RemoteAddress = remoteEndPoint[0];

                    LogFile.Network("Connection Accepted from " + NetworkCommunication.Instance.RemoteAddress);

                    ProcessClientRequest(clientSocket);
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

        private void ProcessClientRequest(Socket clientSocket)
        {
            using (clientSocket)
            {
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

                    clientSocket.Close();
                }
            }
        }

        private bool SocketConnected(Socket clientSocket)
        {
            return !(clientSocket.Poll(5000000, SelectMode.SelectRead) & (clientSocket.Available == 0));
        }
        #endregion
    }

    /// <summary>
    /// 
    /// </summary>

    class SenderThread
    {
        #region Private Variables
        private int _dataSentCount = 0;
        private Thread _currentThread = null;
        private static ManualResetEvent _manualResetEvent = new ManualResetEvent(false);
        private SimpleSocket _clientSocket = null;
        private bool _cancelSender = false;
        #endregion


        #region Constructors
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
                String remoteAddress = NetworkCommunication.Instance.RemoteAddress;
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
