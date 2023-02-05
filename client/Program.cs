using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using UDP_FTP.Error_Handling;
using UDP_FTP.Models;
using static UDP_FTP.Models.Enums;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            string student_1 = "Darren Siriram 0999506";
            string student_2 = "Ertugrul Karaduman 0997475";
            
            List<int> LostAck = new List<int> { 3, 6 };
            byte[] revmsg = new byte[2048];
            byte[] buffer = new byte[2048];
            byte[] data = new byte[2048];
            byte[] ackb = new byte[2048];
            byte[] close = new byte[2048];
            Socket sock;
            //TODO: Change the IP address to Any
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 32000); 
            EndPoint remoteEp = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 32000);
            
            HelloMSG h = new HelloMSG();
            RequestMSG r = new RequestMSG();
            DataMSG D = new DataMSG();
            AckMSG ack = new AckMSG();
            CloseMSG cls = new CloseMSG();
            ConSettings c = new ConSettings();

            Console.WriteLine("Enter the window size:");
            int windowSize = int.Parse(Console.ReadLine());
            Console.WriteLine("Enter the segment size:");
            int segmentSize = int.Parse(Console.ReadLine());
            Console.WriteLine("-------------------------");
            
            h.From = "client1";
            h.Type = Enums.Messages.HELLO;
            h.To = "MyServer";
            byte[] helloBytes = JsonSerializer.SerializeToUtf8Bytes(h);
            
            try
            {
                Console.WriteLine("[Client] Sending hello message...");
                sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                sock.SendTo(helloBytes, helloBytes.Length, SocketFlags.None, endPoint);

                sock.ReceiveFrom(buffer, ref remoteEp);
                var utf8Reader = new Utf8JsonReader(buffer);
                HelloMSG H = JsonSerializer.Deserialize<HelloMSG>(ref utf8Reader);
                Console.WriteLine("[Server] Hello reply...");
                r.From = H.To;
                r.To = H.From;
                r.ConID = H.ConID;
                r.Type = Enums.Messages.REQUEST;
                r.FileName = "test.txt";
                r.WindowSize = windowSize;
                r.SegmentSize = segmentSize;

                Console.WriteLine("[Client]Sending request...");
                
                byte[] requestByte = JsonSerializer.SerializeToUtf8Bytes(r);
                sock.SendTo(requestByte, requestByte.Length, SocketFlags.None, endPoint);
                
                sock.ReceiveFrom(revmsg, ref remoteEp);
                var utf8Reader2 = new Utf8JsonReader(revmsg);
                RequestMSG R = JsonSerializer.Deserialize<RequestMSG>(ref utf8Reader2);
                Console.WriteLine("[Server] Request accepted...");
                
                Console.WriteLine("[Server] Data is being send...");
                sock.ReceiveFrom(data, ref remoteEp);
                var utf8Reader3 = new Utf8JsonReader(data);
                DataMSG DMSG = JsonSerializer.Deserialize<DataMSG>(ref utf8Reader3);

                int count = 0;
                string message = "";
                message += Encoding.UTF8.GetString(DMSG.Data);
                List<int> seqList = new List<int> { };

                Console.WriteLine("[" + DMSG.Sequence + "]" + " " + DMSG.Data.Length + " bytes received | last packet: " + DMSG.More);
                Console.WriteLine(Encoding.UTF8.GetString(DMSG.Data));
                while(DMSG.More)
                {
                    if (LostAck.Contains(DMSG.Sequence))
                    {
                        LostAck.Remove(DMSG.Sequence);
                    }
                    else
                    {   
                        ack.Type = Messages.ACK;
                        ack.From = DMSG.To;
                        ack.To = DMSG.From;
                        ack.ConID = DMSG.ConID;
                        ack.Sequence = DMSG.Sequence;
                        ackb = JsonSerializer.SerializeToUtf8Bytes(ack);
                        sock.SendTo(ackb, ackb.Length, SocketFlags.None, endPoint);
                        seqList.Add(DMSG.Sequence);
                    }
                    count++;
                    
                    byte[] msg = new byte[2048];
                    sock.ReceiveFrom(msg, ref remoteEp);
                    var utf8Reader4 = new Utf8JsonReader(msg);
                    DMSG = JsonSerializer.Deserialize<DataMSG>(ref utf8Reader4);

                    if(seqList.Contains(DMSG.Sequence))
                    {
                        continue;
                    }
                    else
                    {
                        message += Encoding.UTF8.GetString(DMSG.Data);
                    }
                    Console.WriteLine("-------------------------");
                    Console.WriteLine("[" + DMSG.Sequence + "]" + " " + DMSG.Data.Length + " bytes received | last packet: " + DMSG.More);
                    Console.WriteLine(Encoding.UTF8.GetString(DMSG.Data));
                    Console.WriteLine("-------------------------");
                }
            Console.WriteLine("Message: {0}", message);

            sock.ReceiveFrom(close, ref remoteEp);
            var utf8Reader5 = new Utf8JsonReader(close);
            CloseMSG CLS = JsonSerializer.Deserialize<CloseMSG>(ref utf8Reader5);
            if (CLS.Type == Messages.CLOSE_REQUEST)
            {
                Console.WriteLine("[Server] Closing connection...");
                cls.Type = Messages.CLOSE_CONFIRM;
                cls.From = CLS.To;
                cls.To = CLS.From;
                cls.ConID = CLS.ConID;
                byte[] closeByte = JsonSerializer.SerializeToUtf8Bytes(cls);
                sock.SendTo(closeByte, closeByte.Length, SocketFlags.None, endPoint);

                Environment.Exit(0);
            }
            }
            catch (SocketException e)
            {
                Console.WriteLine("Error: An exception has occurred.");
            }
            
            Console.WriteLine("Download Complete!");
            Console.WriteLine("Group members: {0} | {1}", student_1, student_2);
        
        }
    }
}
