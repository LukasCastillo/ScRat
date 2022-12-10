using System;
using System.Linq;

namespace ScRat.net
{
    public enum PacketType : byte
    {
        User = 0,
        Shell = 1,
        Exit = 3,
        Download = 4,
        Error = 5,
        Screenshot = 6,
        Reconnect = 7,
        Upload = 8
    }
    class Packet
    {
        public PacketType type;
        public byte[] data;
        public Packet(PacketType type, byte[] data)
        {
            this.type = type;
            this.data = data;
        }
        public Packet(byte[] d)
        {
            if (d == null || d.Length == 0)
            {
                this.type = PacketType.Reconnect;
                this.data = new byte[0] { };
                return;
            }

            byte[] bytes = Helper.decrypt(d);
            this.type = (PacketType)bytes[0];
            this.data = bytes.Skip(1).ToArray();
        }
        public byte[] ToBytes()
        {
            byte[] newData = new byte[data.Length + 1];
            newData[0] = (Byte)type;
            Array.Copy(data, 0, newData, 1, data.Length);
            return Helper.encrypt(newData);
        }
    }
}
