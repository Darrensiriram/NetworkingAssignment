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
            
            Console.WriteLine(receivedRequestMessage.FileName + receivedRequestMessage.Type);
           
            byte[] fileBytes = File.ReadAllBytes(filelocation); //Convert tekst file into bytes
            byte[][] chunk = new byte[receivedRequestMessage.WindowSize][]; //prepare byte array with size of windows size
            data.Size = fileBytes.Length / receivedRequestMessage.SegmentSize + 1; // calculate how many byte can be send within a segmentSize
            data.More = true;
            // var secondLast = 522;

            // int fileIndex = 0;  
            // int checkWindowSize = 0;
            // data.Sequence = 0;
            //

            // while(data.More)
            // {
            //     
            //     for (int i = 0; i < chunk.Length; i++)
            //     {
            //         chunk[i] = new byte[data.Size]; //Calculate bytes one chunk send and make array
            //         for (int j = 0; j < chunk[i].Length; j++) //Loops calculated chunk until it is full or ends
            //         {
            //             if (fileIndex < fileBytes.Length)
            //             {
            //                 chunk[i][j] = fileBytes[fileIndex]; //Fill chunk of window size with value fileIndex | chunk = [window size][calculated byte from the message]
            //                 fileIndex++;
            //             }
            //             else
            //             {
            //                 data.More = false;
            //             }
            //         }
            //
            //         data.Data = chunk[i];
            //         byte[] sendPacket = Encoding.ASCII.GetBytes(data.Sequence + "|" + Encoding.ASCII.GetString(data.Data) + "|" + data.More + "|" + data.Size);
            //         socket.SendTo(sendPacket, remoteEP);
            //         data.Sequence++;
            //         c.Sequence += sendPacket.Length;
            //         
            //         // takes care of ACK message
            //         checkWindowSize++;
            //         if(checkWindowSize == receivedRequestMessage.WindowSize)
            //         {
            //             checkWindowSize = 0;
            //             socket.SendTimeout = 1000;
            //             int max = 1;
            //             // bool confirm = true;
            //             while(true)
            //             {
            //                 int x = socket.ReceiveFrom(revackMsg, SocketFlags.None, ref remoteEP);
            //                 Console.WriteLine("Message received from {0} and the message is: {1}", req.From, Encoding.ASCII.GetString(revackMsg, 0, x));
            //                 if(Encoding.ASCII.GetString(revackMsg, 0, x) != Messages.ACK.ToString())
            //                 {
            //                     int packetNumber = Int32.Parse(Encoding.ASCII.GetString(revackMsg, 0, x)) - 1;
            //                     fileIndex = (packetNumber * data.Size) - data.Size; // 464
            //                     data.More = true;
            //                     break;
            //                 }
            //                 if(max == receivedRequestMessage.WindowSize)
            //                 {
            //                     break;
            //                 }
            //                 max++;
            //             }
            //         }
            //
            //         if(data.More == false)
            //         {
            //             socket.SendTo(Encoding.ASCII.GetBytes(Messages.CLOSE_REQUEST.ToString()), remoteEP);
            //             break;
            //         }
            //         
            // }
            // }
            
            
            
            
            
            

            return ErrorType.NOERROR;
        }
    }
}
