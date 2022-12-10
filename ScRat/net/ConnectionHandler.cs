using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace ScRat.net
{
    class ConnectionHandler
    {
        public Socket socket;
        public ConnectionHandler(IPAddress ip, int port)
        {

            IPEndPoint localEndPoint = new IPEndPoint(ip, port);
            this.socket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            Trace.WriteLine("Connecting to: " + localEndPoint.ToString());
            socket.Connect(localEndPoint);
            Trace.WriteLine("Socket connected to -> {0} ", this.socket.RemoteEndPoint.ToString());
        }
        public void sendPacket(Packet packet)
        {
            byte[] data = packet.ToBytes();
            try
            {
                this.socket.Send(BitConverter.GetBytes(data.Length));
                this.socket.Send(data);
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.ToString());
            }
        }

        public Packet getPacket()
        {
            try
            {
                byte[] buffer;
                byte[] countBuffer = new byte[4];
                while (this.socket.Available < 0)
                {
                    if ((this.socket.Poll(1000, SelectMode.SelectRead) && (this.socket.Available == 0)) || !this.socket.Connected)
                        return new Packet(PacketType.Reconnect, new byte[1] { 0 });
                }
                socket.Receive(countBuffer, 4, SocketFlags.None);
                int count = BitConverter.ToInt32(countBuffer, 0);
                buffer = new byte[count];

                while (this.socket.Available < count)
                {
                    if ((this.socket.Poll(1000, SelectMode.SelectRead) && (this.socket.Available == 0)) || !this.socket.Connected)
                        return new Packet(PacketType.Reconnect, new byte[1] { 0 });
                }
                socket.Receive(buffer, count, SocketFlags.None);

                return new Packet(buffer);
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.ToString());
                return null;
            }
        }
    }
}
