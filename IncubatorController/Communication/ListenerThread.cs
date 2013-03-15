using System;
using System.Threading;
using Microsoft.SPOT.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net;
using System.Text;
using MyNetduino.ICMP;

namespace NetduinoPlus.Controler
{
  public sealed class ListenerThread
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
      try
      {
        NetworkInterface networkInterface = NetworkInterface.GetAllNetworkInterfaces()[0];

        if (networkInterface.IsDhcpEnabled)
        {
          do
          {
            LogFile.Network("Awaiting IP Address");
            Thread.Sleep(1000);
          }
          while (networkInterface.IPAddress == "0.0.0.0");
        }

        LogFile.Network("Local IP Address: " + networkInterface.IPAddress);

        if (NetworkCommunication.Instance.NetworkIsAvailable == false)
        {
          String clientAddress = "192.168.10.100";
          LogFile.Network("Ping Address: " + clientAddress);
          NetworkCommunication.Instance.NetworkIsAvailable = Ping.PingHost(clientAddress);
        }

        _socketListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _socketListener.Bind(new IPEndPoint(IPAddress.Any, 11000));
        _socketListener.Listen(1);

        _commandReceivedCount = 0;

        _listenerThread = new Thread(WaitForConnection);
        _listenerThread.Start();
      }
      catch (SocketException se)
      {
        if (se.ErrorCode == 10050)
        {
          NetworkCommunication.Instance.NetworkIsAvailable = false;
          LogFile.Network("Network is down.");
        }
        else if (se.ErrorCode == 10060)
        {
          NetworkCommunication.Instance.NetworkIsAvailable = false;
          LogFile.Network("Connection timed out.");
        }
        else
        {
          LogFile.Network("SocketException Error Code: " + se.ErrorCode.ToString());            
        }
      }
      catch (Exception ex)
      {
        LogFile.Exception(ex.ToString());
      }
    }

    public void Stop()
    {
      if (_socketListener != null)
      {
        _socketListener.Close();
        _socketListener = null;
      }
    }
    #endregion


    #region Private Methods
    private void WaitForConnection()
    {
        try
        {
            while (_socketListener != null)
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
            if (se.ErrorCode == 10050)
            {
                LogFile.Network("Network is down.");
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
          Stop();
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
