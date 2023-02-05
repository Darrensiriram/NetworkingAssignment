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
        byte[] close;
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
            close = new byte[2048];

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
                filelocation = receivedRequestMessage.FileName;
            }
            else if (OperatingSystem.IsMacOS())
            {
                filelocation = "../../../" + receivedRequestMessage.FileName;
            }
            else
            {
                return ErrorType.BADREQUEST;
            }
           
            byte[] fileBytes = File.ReadAllBytes(filelocation); //Convert tekst file into bytes
            
            int seq = 0;
            int index = 0;
            List<int> seqListNotAck = new List<int> { };
            data.More = true;

            while(data.More)
            {
                for (int i = 0; i < receivedRequestMessage.WindowSize; i++)
                {
                    if (data.More == false)
                    {
                        break;
                    }
                    //Send data
                    data.Type = Messages.DATA;
                    data.From = receivedRequestMessage.To;
                    data.To = receivedRequestMessage.From;
                    data.ConID = receivedRequestMessage.ConID;
                    data.Size = receivedRequestMessage.SegmentSize;
                    data.More = true;
                    data.Sequence = seq;
                    data.Data = new byte[data.Size];
                    
                    for (int j = 0; j < data.Size; j++)
                    {
                        if (index >= fileBytes.Length)
                        {
                            data.More = false;
                            break;
                        } else {
                            data.Data[j] = fileBytes[index];
                            index++;
                        }
                    }

                    byte[] dataBytes = JsonSerializer.SerializeToUtf8Bytes(data);
                    socket.SendTo(dataBytes, dataBytes.Length, SocketFlags.None, remoteEP);
                    seq++;
                    //Acknowledge
                    try{
                        int ackReceived = socket.ReceiveFrom(dbuffer, ref remoteEP);
                        var utf8Reader3 = new Utf8JsonReader(dbuffer);
                        AckMSG receivedAckMessage = JsonSerializer.Deserialize<AckMSG>(ref utf8Reader3);
                        if (ErrorHandler.VerifyAck(receivedAckMessage, C) != ErrorType.NOERROR)
                        {
                            Console.WriteLine("Error: Wrong message type withing the ack phase");
                            return ErrorType.BADREQUEST;
                        }
                        Console.WriteLine("Data sequence: " + data.Sequence + " | More: " + data.More);
                        C.Sequence = receivedAckMessage.Sequence;
                    }catch (Exception e){
                        Console.WriteLine("Data sequence: " + data.Sequence + " | More: " + data.More + " | Not delivered");
                        seqListNotAck.Add(C.Sequence + 1);
                        continue;
                    }  

                }
                Console.WriteLine("---------------end of window-----------------");
                if(seqListNotAck.Count > 0)
                {
                    seq = seqListNotAck[0];
                    seqListNotAck.Remove(seq);
                    index = data.Size * seq;
                    C.Sequence = seq;
                }
            }

            cls.Type = Messages.CLOSE_REQUEST;
            cls.From = receivedRequestMessage.To;
            cls.To = receivedRequestMessage.From;
            cls.ConID = receivedRequestMessage.ConID;
            byte[] closeByte = JsonSerializer.SerializeToUtf8Bytes(cls);
            socket.SendTo(closeByte, closeByte.Length, SocketFlags.None, remoteEP);
            
            socket.ReceiveFrom(close, ref remoteEP);
            var utf8Reader5 = new Utf8JsonReader(close);
            CloseMSG CLS = JsonSerializer.Deserialize<CloseMSG>(ref utf8Reader5);
            if (CLS.Type == Messages.CLOSE_CONFIRM)
            {
                Console.WriteLine("[Server] Closing connection...");
                return ErrorType.NOERROR;
            }
            else
            {
                Console.WriteLine("[Server] Error: Wrong message type withing the close phase");
                return ErrorType.BADREQUEST;
            }
        }
    }
}
