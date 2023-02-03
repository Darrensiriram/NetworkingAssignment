using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using UDP_FTP.Error_Handling;
using UDP_FTP.Models;
using static UDP_FTP.Models.Enums;

namespace UDP_FTP.File_Handler
{
    class Communicate
    {
        private const string Server = "MyServer";
        private string Client;
        private int SessionID;
        private Socket socket;
        private IPEndPoint remoteEndpoint;
        private EndPoint remoteEP;
        private ErrorType Status;
        private byte[] buffer;
        byte[] msg;
        private string file;
        ConSettings C;


        public Communicate()
        {
            remoteEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 32000);
            remoteEP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 32000);
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(remoteEndpoint);
            // socket.Listen(10);
            
            msg = new byte[2048];
            buffer = new byte[2048];
            
            Random id = new Random();
            SessionID = id.Next(1, 1000);
            
        }

        public ErrorType StartDownload()
        {
            HelloMSG GreetBack = new HelloMSG();
            RequestMSG req = new RequestMSG();
            ReplyMSG rep = new ReplyMSG();
            DataMSG data = new DataMSG();
            AckMSG ack = new AckMSG();
            CloseMSG cls = new CloseMSG();
            
            C = new ConSettings();

            int HelloReceived = socket.ReceiveFrom(buffer, ref remoteEP);
            var utf8Reader = new Utf8JsonReader(buffer);
            HelloMSG GREETBACK = JsonSerializer.Deserialize<HelloMSG>(ref utf8Reader);

           Console.WriteLine(GREETBACK.Type);
           return ErrorType.NOERROR;

        }
    }
}
