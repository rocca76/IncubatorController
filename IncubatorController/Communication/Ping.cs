using System;
using Microsoft.SPOT;
using System.Net;
using System.Net.Sockets;
using NetduinoPlus.Controler;

namespace MyNetduino.ICMP
{
    /// <summary>
    ///	 The Main Ping Class
    /// </summary>
    public static class Ping
    {
        //Declare some Constant Variables
        const int SOCKET_ERROR = -1;
        const int ICMP_ECHO = 8;

        /// <summary>
        ///	 This method takes the "hostname" of the server
        ///	 and then it ping's it and shows the response time
        /// </summary>
        public static bool PingHost(string clientAddress)
        {
            //Declare the IPHostEntry 
            IPEndPoint ipepServer, ipepLocal;
            int nBytes = 0;
            long dwStart = 0;

            LogFile.Network("Ping Address: " + clientAddress);

            try
            {
                //Initilize a Socket of the Type ICMP
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp))
                {
                    // Get the server endpoint
                    ipepServer = new IPEndPoint(IPAddress.Parse(Microsoft.SPOT.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()[0].GatewayAddress), 0);

                    EndPoint epServer = (ipepServer);

                    // Set the receiving endpoint to the client machine
                    ipepLocal = new IPEndPoint(IPAddress.Parse(Microsoft.SPOT.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()[0].IPAddress), 0);
                    EndPoint epLocal = (ipepLocal);

                    int PacketSize = 0;
                    IcmpPacket packet = new IcmpPacket();
                    // Construct the packet to send
                    packet.Type = ICMP_ECHO; //8
                    packet.SubCode = 0;
                    packet.CheckSum = UInt16.Parse("0");
                    packet.Identifier = UInt16.Parse("45");
                    packet.SequenceNumber = UInt16.Parse("0");
                    int PingData = 32; // sizeof(IcmpPacket) – 8;
                    packet.Data = new Byte[PingData];
                    //Initilize the Packet.Data
                    for (int i = 0; i < PingData; i++)
                    {
                        packet.Data[i] = (byte)'#';
                    }

                    //Variable to hold the total Packet size
                    PacketSize = PingData + 8;
                    Byte[] icmp_pkt_buffer = new Byte[PacketSize];
                    Int32 Index = 0;
                    //Call a Method Serialize which counts
                    //The total number of Bytes in the Packet
                    Index = Serialize(
                    packet,
                    icmp_pkt_buffer,
                    PacketSize,
                    PingData);
                    //Error in Packet Size
                    if (Index == -1)
                    {
                        return false;
                    }

                    // now get this critter into a UInt16 array

                    //Get the Half size of the Packet
                    Double double_length = (double)Index;
                    Double dtemp = System.Math.Ceiling(double_length / 2);
                    int cksum_buffer_length = (int)dtemp;
                    //Create a Byte Array
                    UInt16[] cksum_buffer = new UInt16[cksum_buffer_length];
                    //Code to initialize the Uint16 array 
                    int icmp_header_buffer_index = 0;
                    for (int i = 0; i < cksum_buffer_length; i++)
                    {
                        cksum_buffer[i] =
                        BitConverter.ToUInt16(icmp_pkt_buffer, icmp_header_buffer_index);
                        icmp_header_buffer_index += 2;
                    }
                    //Call a method which will return a checksum 
                    UInt16 u_cksum = checksum(cksum_buffer, cksum_buffer_length);
                    //Save the checksum to the Packet
                    packet.CheckSum = u_cksum;

                    // Now that we have the checksum, serialize the packet again
                    Byte[] sendbuf = new Byte[PacketSize];
                    //again check the packet size
                    Index = Serialize(packet, sendbuf, PacketSize, PingData);
                    //if there is a error report it
                    if (Index == -1)
                    {
                        return false;
                    }

                    dwStart = DateTime.Now.Ticks; // Start timing and send the Pack over the socket
                    socket.SendTimeout = 5000;
                    if ((nBytes = socket.SendTo(sendbuf, PacketSize, 0, epServer)) == SOCKET_ERROR)
                    {
                        return false;
                    }

                    // Initialize the buffers. The receive buffer is the size of the
                    // ICMP header plus the IP header (20 bytes)
                    Byte[] ReceiveBuffer = new Byte[256];
                    nBytes = 0;
                    long timeout = 0;

                    socket.ReceiveTimeout = 5000;

                    while (true)
                    {
                        nBytes = socket.ReceiveFrom(ReceiveBuffer, 256, 0, ref epLocal);
                        if (nBytes == SOCKET_ERROR)
                        {
                            return false;
                        }
                        else if (nBytes > 0)
                        {
                            return true;
                        }

                        timeout = DateTime.Now.Ticks - dwStart;
                        if (timeout > 1000)
                        {
                            return false;
                        }
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// This method get the Packet and calculates the total size 
        /// of the Pack by converting it to byte array
        /// </summary>
        public static Int32 Serialize(IcmpPacket packet, Byte[] Buffer,
        Int32 PacketSize, Int32 PingData)
        {
            Int32 cbReturn = 0;
            // serialize the struct into the array
            int Index = 0;

            Byte[] b_type = new Byte[1];
            b_type[0] = (packet.Type);

            Byte[] b_code = new Byte[1];
            b_code[0] = (packet.SubCode);

            Byte[] b_cksum = BitConverter.GetBytes(packet.CheckSum);
            Byte[] b_id = BitConverter.GetBytes(packet.Identifier);
            Byte[] b_seq = BitConverter.GetBytes(packet.SequenceNumber);

            // Console.WriteLine("Serialize type ");
            Array.Copy(b_type, 0, Buffer, Index, b_type.Length);
            Index += b_type.Length;

            // Console.WriteLine("Serialize code ");
            Array.Copy(b_code, 0, Buffer, Index, b_code.Length);
            Index += b_code.Length;

            // Console.WriteLine("Serialize cksum ");
            Array.Copy(b_cksum, 0, Buffer, Index, b_cksum.Length);
            Index += b_cksum.Length;

            // Console.WriteLine("Serialize id ");
            Array.Copy(b_id, 0, Buffer, Index, b_id.Length);
            Index += b_id.Length;

            Array.Copy(b_seq, 0, Buffer, Index, b_seq.Length);
            Index += b_seq.Length;

            // copy the data	
            Array.Copy(packet.Data, 0, Buffer, Index, PingData);
            Index += PingData;
            if (Index != PacketSize/* sizeof(IcmpPacket) */)
            {
                cbReturn = -1;
                return cbReturn;
            }

            cbReturn = Index;
            return cbReturn;
        }

        /// <summary>
        ///	 This Method has the algorithm to make a checksum 
        /// </summary>
        public static UInt16 checksum(UInt16[] buffer, int size)
        {
            Int32 cksum = 0;
            int counter;
            counter = 0;

            while (size > 0)
            {
                UInt16 val = buffer[counter];

                cksum += (int)buffer[counter];
                counter += 1;
                size -= 1;
            }

            cksum = (cksum >> 16) + (cksum & 0xffff);
            cksum += (cksum >> 16);
            return (UInt16)(~cksum);
        }
    }
}