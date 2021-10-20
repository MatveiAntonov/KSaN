using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tracert
{
    public class ICMP
    {
        public int MessageSize { get; protected set; }
        public int PacketSize { get; protected set; }
        public byte type { get; protected set; } = 0x08;
        public byte code { get; protected set; } = 0x00;

        private UInt16 SequenceNumber = 107;
        public UInt16 Checksum = 0;
        private byte[] Data;
        public byte[] Message = new byte[1024];


        public ICMP(byte[] data)
        {
            this.Data = data;
            // размер сообщения: длина данных + длина заголовка(1 байт - тип, 1 байт - код, 2 байта - checksum)
            MessageSize = data.Length + 4;
            // полный размер пакета: identifier(2 байта) + sequenceNumber(2 байта)
            PacketSize = MessageSize + 4;
            Message = createPacket();
        }

        public UInt16 UpdateSequence()
        {
            SequenceNumber++;
            Message = createPacket();
                                                     
            return SequenceNumber;
        }

        private byte[] createPacket()
        {
            byte[] Packet = new byte[MessageSize + 8];
            // копирование 1 байта type в Packet
            Buffer.BlockCopy(BitConverter.GetBytes(type), 0, Packet, 0, 1);
            // копирование 1 байта code в Packet со смещением в байт
            Buffer.BlockCopy(BitConverter.GetBytes(code), 0, Packet, 1, 1);
            //0100
            Buffer.BlockCopy(BitConverter.GetBytes(1), 0, Packet, 4, 1);
            //2 байта для seauenceNumber
            Buffer.BlockCopy(BitConverter.GetBytes(SequenceNumber), 0, Packet, 6, 2);
            //данные
            Buffer.BlockCopy(Data, 0, Packet, 8, Data.Length);
            //checksum
            Buffer.BlockCopy(BitConverter.GetBytes(getChecksum(Packet)), 0, Packet, 2, 2);

             return Packet;
        }

        private UInt16 getChecksum(byte[] bytes)
        {
            UInt32 chcksm = 0;
            int packetsize = MessageSize + 8;

            for (int index = 0; index < packetsize; index += 2)
                chcksm += Convert.ToUInt32(BitConverter.ToUInt16(bytes, index));

            chcksm = (chcksm >> 16) + (chcksm & 0xffff);
            chcksm += (chcksm >> 16);

            return (UInt16)(~chcksm);
        }
    }
}
