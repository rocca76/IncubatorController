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
        private static AutoResetEvent _resetEvent = new AutoResetEvent(false);
        private static Socket _clientSocket = null;
        #endregion


        #region Constructors
        #endregion


        #region Events
        #endregion


        #region Public Properties
        public AutoResetEvent ResetEvent
        {
            get { return _resetEvent; }
        }
        #endregion


        #region Public Methods
        public void Start()
        {
            if (_currentThread != null && _currentThread.IsAlive)
            {
                LogFile.Error("Can not start SendingThread");
            }
            else
            {
                _dataSentCount = 0;

                _currentThread = new Thread(SendingThread);
                _currentThread.Start();
            }
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
            }            
        }

        private void ProcessClient()
        {
            while (true)
            {
                _resetEvent.WaitOne();

                Stopwatch stopwatch = Stopwatch.StartNew();

                int size = _clientSocket.Send(Encoding.UTF8.GetBytes(ProcessControl.Instance.BuildDataOutput()));
                _dataSentCount++;

                stopwatch.Stop();

                //ogFile.Network("ProcessClient duration: " + stopwatch.ElapsedMilliseconds.ToString() + "ms");
                //LogFile.Network("Message Sent: " + stopwatch.ElapsedMilliseconds.ToString() + "ms, " + _dataSentCount.ToString() + ", Size: " + stateOutput.Length.ToString() + ", Value: " + stateOutput);
            }
        }
        #endregion
    }
}
