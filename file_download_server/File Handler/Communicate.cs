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
        byte[] dbuffer;
        private string file;
        ConSettings C;


        public Communicate()
        {
            remoteEndpoint = new IPEndPoint(IPAddress.Any, 32000);
            remoteEP = new IPEndPoint(IPAddress.Any, 32000);
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(remoteEndpoint);
            // socket.Listen(10);
            
            msg = new byte[2048];
            buffer = new byte[2048];
            dbuffer = new byte[2048];

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
            HelloMSG receivedHelloMessage = JsonSerializer.Deserialize<HelloMSG>(ref utf8Reader);
            Client = receivedHelloMessage.From;
            
            C.To = receivedHelloMessage.To;
            C.Type = receivedHelloMessage.Type;

            if (ErrorHandler.VerifyGreeting(receivedHelloMessage, C) != ErrorType.NOERROR)
            {
                Console.WriteLine("Error: Wrong message type withing the greeting phase");
                return ErrorType.BADREQUEST;
            }
            try
            {
                GreetBack.From = Server;
                GreetBack.To = Client;
                GreetBack.ConID = SessionID;
                GreetBack.Type = Messages.HELLO_REPLY;
                byte[] helloReplyBytes = JsonSerializer.SerializeToUtf8Bytes(GreetBack);
                socket.SendTo(helloReplyBytes, helloReplyBytes.Length, SocketFlags.None, remoteEP);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return ErrorType.BADREQUEST;
            }
            
            
            int requestReceived = socket.ReceiveFrom(msg, ref remoteEP);
            var utf8Reader2 = new Utf8JsonReader(msg);
            RequestMSG receivedRequestMessage = JsonSerializer.Deserialize<RequestMSG>(ref utf8Reader2);
            
            C.WindowSize = receivedRequestMessage.WindowSize;
            C.SegmentSize = receivedRequestMessage.SegmentSize;
            C.ConID = receivedRequestMessage.ConID;
            C.From = receivedRequestMessage.From;
            
            if (ErrorHandler.VerifyRequest(receivedRequestMessage, C) != ErrorType.NOERROR)
            {
                Console.WriteLine("Error: Wrong message type withing the request phase");
                return ErrorType.BADREQUEST;
            }

            try
            {
                rep.ConID = SessionID;
                rep.From = Server;
                req.To = Client;
                req.SegmentSize = C.SegmentSize;
                req.WindowSize = C.WindowSize;
                req.Type = Messages.REPLY;
                byte[] replyBytes = JsonSerializer.SerializeToUtf8Bytes(rep);
                socket.SendTo(replyBytes, replyBytes.Length, SocketFlags.None, remoteEP);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
            socket.ReceiveTimeout = 5000;
            string filelocation = "";
            if (OperatingSystem.IsWindows())
            {
                filelocation = req.FileName;
            }
            else if (OperatingSystem.IsMacOS())
            {
                filelocation = "../../../" + req.FileName;
            }
            else
            {
                return ErrorType.BADREQUEST;
            }
            
            
            
            
            
            

            return ErrorType.NOERROR;
        }
    }
}
