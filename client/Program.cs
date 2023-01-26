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
            
            int port = 32000;
            IPAddress serverIP = IPAddress.Parse("127.0.0.1");
            IPEndPoint serverEndPoint = new IPEndPoint(serverIP, port);
            UdpClient client = new UdpClient(); 

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

            h.ConID = -1;
            h.From = "client1";
            h.Type = Enums.Messages.HELLO;
            h.To = "MyServer";
            byte[] helloBytes = JsonSerializer.SerializeToUtf8Bytes(h);
            
            try
            {
                client.Send(helloBytes, helloBytes.Length, serverEndPoint);
                // TODO: Receive and verify a HelloMSG 
                byte[] receivedBytes = client.Receive(ref serverEndPoint);
                HelloMSG receivedHello = JsonSerializer.Deserialize<HelloMSG>(receivedBytes);
                if (ErrorHandler.VerifyGreeting(receivedHello, c) == ErrorType.NOERROR)
                {
                    Console.WriteLine("Error: Not a Hello message");
                    return;
                }
                // TODO: Send the RequestMSG message requesting to download a file name
                // In this message you need to send window and sequence sizes

                r.From = h.From;
                r.ConID = h.ConID;
                r.Type = Enums.Messages.REQUEST;
                r.To = h.To;
                r.FileName = "test.txt";
                r.WindowsSize = windowSize;
                r.SegmentSize = segmentSize;
                
                byte[] requestBytes = JsonSerializer.SerializeToUtf8Bytes(r);
                client.Send(requestBytes, requestBytes.Length, serverEndPoint);

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
