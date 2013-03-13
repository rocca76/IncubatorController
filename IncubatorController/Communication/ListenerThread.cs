using System;
using System.Threading;
using Microsoft.SPOT.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net;
using System.Text;

namespace NetduinoPlus.Controler
{
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
}
