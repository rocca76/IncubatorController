using System;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Text;

namespace NetduinoPlus.Controler
{
    public sealed class SenderThread
    {
        #region Private Variables
        private int _dataSentCount = 0;
        private Thread _currentThread = null;
        private static ManualResetEvent _manualResetEvent = new ManualResetEvent(false);
        private static Socket _clientSocket = null;
        #endregion


        #region Constructors
        #endregion


        #region Events
        #endregion


        #region Public Properties
        public ManualResetEvent ResetEvent
        {
            get { return _manualResetEvent; }
        }
        #endregion


        #region Public Methods
        public void Start()
        {
            _dataSentCount = 0;

            _currentThread = new Thread(SendingThread);
            _currentThread.Start();
        }

        public void Stop()
        {
            if (_clientSocket != null)
            {
                _clientSocket.Close();
                _clientSocket = null;
            }
        }
        #endregion


        #region Private Methods
        private void SendingThread()
        {
            try
            {
                LogFile.Network("Connecting to " + NetworkCommunication.Instance.RemoteAddress);
                IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(NetworkCommunication.Instance.RemoteAddress), 11000);

                _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _clientSocket.Connect(endpoint);

                ProcessClient();
            }
            catch (SocketException se)
            {
                if (se.ErrorCode == 10054)
                {
                    LogFile.Network("Connection reset by peer.");
                }
                else
                {
                    LogFile.Network(se.ToString());
                }
            }
            catch (Exception ex)
            {
                LogFile.Network(ex.ToString());
            }
            finally
            {
                _clientSocket = null;
                _manualResetEvent.Reset();
            }            
        }

        private void ProcessClient()
        {
            while (true)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

                _manualResetEvent.WaitOne();

                String stateOutput = ProcessControl.Instance.DataOutput;

                int size = _clientSocket.Send(Encoding.UTF8.GetBytes(stateOutput));

                _dataSentCount++;

                _manualResetEvent.Reset();

                stopwatch.Stop();

                LogFile.Network("Message Sent: " + stopwatch.ElapsedMilliseconds.ToString() + "ms, " + _dataSentCount.ToString() + ", Size: " + stateOutput.Length.ToString() + ", Value: " + stateOutput);
            }
        }
        #endregion
    }
}
