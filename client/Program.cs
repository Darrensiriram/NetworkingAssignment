﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UDP_FTP.Models;
using UDP_FTP.Error_Handling;
using static UDP_FTP.Models.Enums;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            string student_1 = "Darren Siriram 0999506";
            // string student_2 = ;
            
            byte[] buffer = new byte[1000];
            byte[] msg = new byte[100];
            // TODO: Initialise the socket/s as needed from the description of the assignment
            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPAddress broadcast = IPAddress.Parse("127.0.0.1");

            HelloMSG h = new HelloMSG();
            RequestMSG r = new RequestMSG();
            DataMSG D = new DataMSG();
            AckMSG ack = new AckMSG();
            CloseMSG cls = new CloseMSG();

            try
            {
                
                //sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                //IPAddress broadcast = IPAddress.Parse("127.0.0.1");
                
                //Console.WriteLine($"[Client] send: {h.From}");
                //IPEndPoint ep = new IPEndPoint(broadcast, clientPort);

                // TODO: Instantiate and initialize your socket 
                IPEndPoint endpoint = new IPEndPoint(broadcast, 5004);
                // TODO: Send hello mesg
                byte[] sendbuf = Encoding.ASCII.GetBytes("cap");
                //byte[] test = Encoding.ASCII.GetBytes("Hello");
                sock.SendTo(sendbuf, endpoint);
                
                // TODO: Receive and verify a HelloMSG 


                // TODO: Send the RequestMSG message requesting to download a file name
                
                // TODO: Receive a RequestMSG from remoteEndpoint
                // receive the message and verify if there are no errors


                // TODO: Check if there are more DataMSG messages to be received 
                // receive the message and verify if there are no errors

                // TODO: Send back AckMSG for each received DataMSG 


                // TODO: Receive close message
                // receive the message and verify if there are no errors

                // TODO: confirm close message
                Console.WriteLine("Message sent to broadcast address");
            }
            catch
            {
                Console.WriteLine("\n Socket Error. Terminating");
            }

            Console.WriteLine("Download Complete!");
           
        }
    }
}
