using System;
using System.Threading;
using Toolbox.NETMF.NET;
using System.Net.Sockets;

namespace NetduinoPlus.Controler
{
  public sealed class SenderThread
  {
    #region Private Variables
    private int _dataSentCount = 0;
    private Thread _currentThread = null;
    private static ManualResetEvent _manualResetEvent = new ManualResetEvent(false);
    private SimpleSocket _clientSocket = null;
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
            String remoteAddress = NetworkCommunication.Instance.RemoteAddress;
            LogFile.Network("Connecting to " + remoteAddress.ToString());

            _clientSocket = new IntegratedSocket(remoteAddress, 11000);
            _clientSocket.Connect();

            while (_clientSocket.IsConnected)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

                _manualResetEvent.WaitOne();

                String stateOutput = ProcessControl.Instance.DataOutput;

                _clientSocket.Send(stateOutput);
                _dataSentCount++;

                _manualResetEvent.Reset();

                stopwatch.Stop();

                LogFile.Network("Message Sent: " + stopwatch.ElapsedMilliseconds.ToString() + "ms, " + _dataSentCount.ToString() + ", Size: " + stateOutput.Length.ToString() + ", Value: " + stateOutput);
            }

            _clientSocket.Close();
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
            _manualResetEvent.Reset();
        }
    }
    #endregion
  }
}
