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
    public delegate void SentEventHandler(SenderThread requestSender);

    class NetworkCommunication
    {
        #region Private Variables
        private static Thread _listeningThread = null;
        private static NetworkCommunication _networkCommunication = null;
        private static SenderThread _senderThread = null;
        private static bool _networkIsAvailable = false;
        private static int _messageSentCount = 1;
        private static int _messageReceivedCount = 1;
        private bool _patchFirmware42 = false;
        private static String _remoteIP;
        #endregion


        #region Constructors
        private NetworkCommunication() {}
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
            if (_networkCommunication == null)
            {
                NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;
                _networkCommunication = new NetworkCommunication();
            }

            _networkCommunication.ListeningThread();
            
            _networkIsAvailable = true;
        }

        public static void Send(String message)
        {
            _networkCommunication.SendingThread(message);
        }
        #endregion


        #region Private Static Methods
        private static void SentEventHandler(SenderThread requestSender)
        {
            _senderThread = null;

            Debug.Print("Message Sent " + _messageSentCount.ToString() + " : [ " + requestSender.Message + " ]");
            _messageSentCount++;
        }

        private static void NetworkChange_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            Debug.Print(DateTime.UtcNow.ToString("u") + " " + (e.IsAvailable ? "Online" : "Offline"));

            if (e.IsAvailable)
            {
                InitInstance();
            }
            else
            {
                _networkIsAvailable = false;
                _messageSentCount = 1;
                _messageReceivedCount = 1;

                ShutdownSender();
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

        private void SendingThread(String message)
        {
            if (_networkIsAvailable && _patchFirmware42)
            {
                ShutdownSender();

                _senderThread = new SenderThread(SentEventHandler, _remoteIP, message);
                _senderThread.Start();
            }
        }

        private void ReceiveSocketsInListeningThread()
        {
            try
            {
                Debug.Print("Waiting for valid IP address...");

                NetworkInterface networkInterface = NetworkInterface.GetAllNetworkInterfaces()[0];

                while (networkInterface.IPAddress.ToString() == "0.0.0.0")
                {
                    Thread.Sleep(1000);
                }

                Debug.Print("IP address: " + networkInterface.IPAddress.ToString());

                using (Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    listener.Bind(new IPEndPoint(IPAddress.Any, 250));
                    listener.Listen(1);

                    while (true)
                    {
                        Debug.Print("Listening for connection...");

                        using (Socket socket = listener.Accept())
                        {
                            if (SocketConnected(socket))
                            {
                                string[] remoteEndPoint = socket.RemoteEndPoint.ToString().Split(':');
                                _remoteIP = remoteEndPoint[0];

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
                Debug.Print(se.ToString());
            }
        }

        private bool SocketConnected(Socket client)
        {
            return !(client.Poll(5000000, SelectMode.SelectRead) & (client.Available == 0));
        }

        private void RaiseMessageReceivedEvent(String message)
        {
            Debug.Print("Message Received " + _messageReceivedCount.ToString() + " : [ " + message + " ]");
            _messageReceivedCount++;

            _patchFirmware42 = true;

            if (message == "EXIT")
            {
                ShutdownSender();
                _patchFirmware42 = false;
            }

            if (EventHandlerMessageReceived != null)
            {
                EventHandlerMessageReceived(message);
            }
        }

        private static void ShutdownSender()
        {
            try
            {
                if (_senderThread != null)
                {
                    if (_senderThread.IsAlive)
                    {
                        _senderThread.Stop();
                    }

                    _senderThread = null;
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.ToString());
            }
        }

        private static void ShutdownListener()
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
                    _remoteIP = null;
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.ToString());
            }
        }
        #endregion
    }

    /// <summary>
    /// 
    /// </summary>

    public class SenderThread
    {
        #region Private Variables
        private Thread currentThread = null;
        private SentEventHandler _senderEventHandler = null;
        private String _message;
        private String _remoteIP = null;
        #endregion


        #region Constructors
        public SenderThread(SentEventHandler sendEventHandler, String remoteIP, String message)
        {
            _senderEventHandler = sendEventHandler;
            _remoteIP = remoteIP;
            _message = message;
        }
        #endregion


        #region Events
        #endregion


        #region Public Properties
        public String Message
        {
            get { return _message; }
        }

        public bool IsAlive
        {
            get { return currentThread.IsAlive; }
        }
        #endregion

        public void Start()
        {
            this.currentThread = new Thread(ThreadMain);
            currentThread.Start();
        }

        public void Stop()
        {
            Debug.Print("Stopping this thread.");
            this.currentThread.Abort();
        }

        public void Dispose()
        {
            Stop();
        }

        private void ThreadMain()
        {
            try
            {
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(_remoteIP), 250);

                    Debug.Print("Connecting to: " + endpoint.ToString());
                    socket.Connect(endpoint);
                    socket.Send(Encoding.UTF8.GetBytes(Message));
                    socket.Close();
                }
                
                _senderEventHandler(this);
            }
            catch (SocketException se)
            {
                Debug.Print("Unable to connect or send through socket");
                Debug.Print(se.ToString());
            }
            catch (Exception ex)
            {
                Debug.Print(ex.ToString());
            }
        }
    }
}
