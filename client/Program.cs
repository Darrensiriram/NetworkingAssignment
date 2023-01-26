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
            
            List<int> LostAck = new List<int> { 2, 5, 7 };
            byte[] revmsg = new byte[1024];
            byte[] buffer = new byte[2048];
            Socket sock;
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 32000);
            EndPoint remoteEp = new IPEndPoint(IPAddress.Any, 32000);
            

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
        
            h.ConID = 123;
            h.From = "client1";
            h.Type = Enums.Messages.HELLO;
            h.To = "MyServer";
            byte[] helloBytes = JsonSerializer.SerializeToUtf8Bytes(h);
            
            
            try
            {
                sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                sock.SendTo(helloBytes, helloBytes.Length, SocketFlags.None, endPoint);

                int b = sock.ReceiveFrom(buffer, ref remoteEp);
                var utf8Reader = new Utf8JsonReader(buffer);
                HelloMSG H = JsonSerializer.Deserialize<HelloMSG>(ref utf8Reader);
                
                r.From = h.From;
                r.To = h.To;
                r.ConID = h.ConID;
                r.Type = Enums.Messages.REQUEST;
                r.FileName = "test.txt";
                r.WindowsSize = windowSize;
                r.SegmentSize = segmentSize;

                byte[] requestByte = JsonSerializer.SerializeToUtf8Bytes(r);
                sock.SendTo(requestByte, requestByte.Length, SocketFlags.None, endPoint);
                
                int reqmsg = sock.ReceiveFrom(buffer, ref remoteEp);
                RequestMSG R = JsonSerializer.Deserialize<RequestMSG>(ref utf8Reader);
                
                // TODO:  Check why R is not giving any reponse, also use error handler VerifyRequest to check response.
                
                int dataMsg = sock.ReceiveFrom(buffer, ref remoteEp);
                DataMSG DMSG = JsonSerializer.Deserialize<DataMSG>(ref utf8Reader);

                int nextSeqNum = 0;
                while(nextSeqNum < windowSize)
                {
                    int receivedDataBytes = sock.ReceiveFrom(buffer, ref remoteEp);
                    DataMSG receivedData = JsonSerializer.Deserialize<DataMSG>(receivedDataBytes);
                    
                    if (receivedData.Sequence == nextSeqNum)
                    {
                        nextSeqNum++;
                        
                        if (LostAck.Contains(receivedData.Sequence))
                        {
                            LostAck.Remove(receivedData.Sequence);
                            continue;
                        }
                        
                        ack.Type = Messages.ACK;
                        ack.Sequence = receivedData.Sequence;
                        byte[] ackBytes = JsonSerializer.SerializeToUtf8Bytes(ack);
                        sock.SendTo(ackBytes, ackBytes.Length, SocketFlags.None, endPoint);
                    }
                    else
                    {
                        Console.WriteLine("Error: Unexpected sequence number");
                    }
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
