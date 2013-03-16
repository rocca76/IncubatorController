using System;
using System.Net;
using System.Net.Sockets;
using NetduinoPlus.Controler;

namespace IncubatorController.Utility
{
    public static class NTPTime
    {
        public static void SetLocalTime()
        {
            try
            {
                if (NetworkCommunication.Instance.NetworkIsAvailable)
                {
                    // Find endpoint for timeserver
                    IPEndPoint ep = new IPEndPoint(IPAddress.Parse("64.90.182.55"), 123);

                    // Connect to timeserver
                    Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    s.Connect(ep);

                    // Make send/receive buffer
                    byte[] ntpData = new byte[48];
                    Array.Clear(ntpData, 0, 48);

                    // Set protocol version
                    ntpData[0] = 0x1B;

                    // Send Request
                    s.Send(ntpData);

                    // Receive Time
                    s.Receive(ntpData);

                    byte offsetTransmitTime = 40;

                    ulong intpart = 0;
                    ulong fractpart = 0;

                    for (int i = 0; i <= 3; i++)
                        intpart = (intpart << 8) | ntpData[offsetTransmitTime + i];

                    for (int i = 4; i <= 7; i++)
                        fractpart = (fractpart << 8) | ntpData[offsetTransmitTime + i];

                    ulong milliseconds = (intpart * 1000 + (fractpart * 1000) / 0x100000000L);

                    s.Close();

                    TimeSpan timeSpan = TimeSpan.FromTicks((long)milliseconds * TimeSpan.TicksPerMillisecond);
                    DateTime dateTime = new DateTime(1900, 1, 1);
                    dateTime += timeSpan;

                    TimeSpan offsetAmount = TimeZone.CurrentTimeZone.GetUtcOffset(dateTime);
                    DateTime networkDateTime = (dateTime + offsetAmount);
                    networkDateTime.AddHours(8);

                    Microsoft.SPOT.Hardware.Utility.SetLocalTime(networkDateTime);
                }
            }
            catch (SocketException se)
            {
                LogFile.Network("SocketException Error Code: " + se.ErrorCode.ToString());
            }
            catch (Exception ex)
            {
                LogFile.Exception(ex.ToString());
            }
        }
    }
}
