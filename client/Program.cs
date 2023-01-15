using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
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
            
            int port = 32000;
            IPAddress serverIP = IPAddress.Parse("127.0.0.1");
            IPEndPoint serverEndPoint = new IPEndPoint(serverIP, port);
            UdpClient client = new UdpClient();
            
            HelloMSG h = new HelloMSG();
            RequestMSG r = new RequestMSG();
            DataMSG D = new DataMSG();
            AckMSG ack = new AckMSG();
            CloseMSG cls = new CloseMSG();
            
            Console.WriteLine("Enter the window size:");
            int windowSize = int.Parse(Console.ReadLine());
            Console.WriteLine("Enter the segment size:");
            int segmentSize = int.Parse(Console.ReadLine());
            
            try
            {
                
                h.Type = Messages.HELLO;
                byte[] helloBytes = JsonSerializer.SerializeToUtf8Bytes(h);
                client.Send(helloBytes, helloBytes.Length, serverEndPoint);
                
                // TODO: Receive and verify a HelloMSG 
                byte[] receivedBytes = client.Receive(ref serverEndPoint);
                HelloMSG receivedHello = JsonSerializer.Deserialize<HelloMSG>(receivedBytes);
                if (receivedHello.Type != Messages.HELLO)
                {
                    Console.WriteLine("Error: Not a Hello message");
                    return;
                }
              
                r.Type = Messages.REQUEST;
                r.FileName = "example.txt";
                // r.WindowSize = windowSize;
                // r.SegmentSize = segmentSize;
                byte[] requestBytes = JsonSerializer.SerializeToUtf8Bytes(r);
                client.Send(requestBytes, requestBytes.Length, serverEndPoint);
                
                byte[] receivedRequestBytes = client.Receive(ref serverEndPoint);
                RequestMSG receivedRequest = JsonSerializer.Deserialize<RequestMSG>(receivedRequestBytes);
                if (receivedRequest.Type != Messages.REQUEST)
                {
                    Console.WriteLine("Error: Not a Request message");
                    return;
                }
                
                int nextSeqNum = 0;
                while(nextSeqNum < windowSize)
                {
                    byte[] receivedDataBytes = client.Receive(ref serverEndPoint);
                    DataMSG receivedData = JsonSerializer.Deserialize<DataMSG>(receivedDataBytes);
                    if (receivedData.Type != Messages.DATA)
                    {
                        Console.WriteLine("Error: Not a Data message");
                        return;
                    }
                    
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
                        client.Send(ackBytes, ackBytes.Length, serverEndPoint);
                    }
                    else
                    {
                        Console.WriteLine("Error: Unexpected sequence number");
                    }
                }
                
                byte[] receivedCloseBytes = client.Receive(ref serverEndPoint);
                CloseMSG receivedClose = JsonSerializer.Deserialize<CloseMSG>(receivedCloseBytes);
                if (receivedClose.Type != Messages.CLOSE_CONFIRM)
                {
                    Console.WriteLine("Error: Not a Close message");
                    return;
                }
                
                cls.Type = Messages.CLOSE_REQUEST;
                byte[] closeBytes = JsonSerializer.SerializeToUtf8Bytes(cls);
                client.Send(closeBytes, 1);
                client.Close();
            }
            catch
            {
                Console.WriteLine("Error: An exception has occurred.");
            }

            Console.WriteLine("Download Complete!");
            Console.WriteLine("Group members: {0} | {1}", student_1, student_2);
        
        }
    }
}
