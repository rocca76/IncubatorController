using System;
using Microsoft.SPOT;

namespace MyNetduino.ICMP
{
    // class ping
    /// <summary>
    ///	 Class that holds the Pack information
    /// </summary>
    public class IcmpPacket
    {
        public Byte Type; // type of message
        public Byte SubCode; // type of sub code
        public UInt16 CheckSum; // ones complement checksum of struct
        public UInt16 Identifier; // identifier
        public UInt16 SequenceNumber; // sequence number 
        public Byte[] Data;

    } // class IcmpPacket
}