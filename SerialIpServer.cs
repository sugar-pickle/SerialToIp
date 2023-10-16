using System;
using System.Collections.Concurrent;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SerialToIp
{
    public class SerialIpServer
    {
        private SerialPort serialPort;
        private NetworkStream connectedStream;
        private readonly ConcurrentQueue<byte[]> disconnectedQueue = new();
        public async Task RunServer(Port port)
        {
            Console.WriteLine($"Starting Serial IP Server - Serial port: {port.SerialPort} IP Port: {port.IpPort}");
            serialPort = new SerialPort(port.SerialPort, ConfigParser.Config.BaudRate);
            serialPort.Open();
            var tcpListener = new TcpListener(IPAddress.Any, port.IpPort);
            tcpListener.Start();
            serialPort.DataReceived += SerialPortOnDataReceived;
            while (serialPort.IsOpen)
            {
                try
                {
                    var socket = await tcpListener.AcceptTcpClientAsync();
                    connectedStream = socket.GetStream();
                    Console.WriteLine(
                        $"Client connected ({port.SerialPort}:{port.IpPort}) - {socket.Client.RemoteEndPoint}");
                    if (!disconnectedQueue.IsEmpty) _ = Task.Run(ClearDisconnectedQueue);
                    while (connectedStream != null)
                    {
                        if (socket.Available < 1)
                        {
                            await Task.Delay(500);
                            continue;
                        }
                        var buffer = new byte[socket.Available];
                        var read = await connectedStream.ReadAsync(buffer);
                        serialPort.Write(buffer, 0, read);
                    }
                    Console.WriteLine(
                        $"Client disconnected ({port.SerialPort}:{port.IpPort}) - {socket.Client.RemoteEndPoint}");
                }
                catch (Exception ex)
                {
                    await Console.Error.WriteLineAsync($"Socket closed with exception {ex.Message}");
                }
                finally
                {
                    connectedStream = null;
                }
            }
            
            tcpListener.Stop();
        }

        private async Task ClearDisconnectedQueue()
        {
            Console.WriteLine($"Clearing queue with {disconnectedQueue.Count} items");
            while (disconnectedQueue.TryDequeue(out var line))
            {
                await connectedStream.WriteAsync(line);
            }
            Console.WriteLine("Finished clearing queue");
        }
        
        private async void SerialPortOnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                var bfr = new byte[serialPort.BytesToRead];
                serialPort.Read(bfr, 0, bfr.Length);
                switch (connectedStream)
                {
                    case null when ConfigParser.Config.StoreWhenDisconnected:
                    case not null when !disconnectedQueue.IsEmpty:
                        disconnectedQueue.Enqueue(bfr);
                        break;
                    case not null:
                        await connectedStream.WriteAsync(bfr);
                        break;
                }
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Failed to send data to client{ex.Message}");
                connectedStream?.Close();
                connectedStream = null;
            }
        }
    }
}
