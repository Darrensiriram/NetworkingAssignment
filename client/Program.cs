using System;
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

            byte[] buffer = new byte[1000];
            byte[] msg = new byte[100];
            // TODO: Initialise the socket/s as needed from the description of the assignment
            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPAddress ip = IPAddress.Parse("127.0.0.1");

            HelloMSG h = new HelloMSG();
            RequestMSG r = new RequestMSG();
            DataMSG D = new DataMSG();
            AckMSG ack = new AckMSG();
            CloseMSG cls = new CloseMSG();
            ConSettings c = new ConSettings();

            h.From = student_1;
            h.To = "Server";
            h.ConID = 1;
            h.Type = Messages.HELLO;

            r.From = student_1;
            r.To = "Server";
            r.FileName = "test.txt";
            r.Type = Messages.REQUEST;
            string req = "Type of message: " + r.Type + " File name:" + r.FileName; 

            try
            {
                IPEndPoint endpoint = new IPEndPoint(ip, 5004);
                EndPoint remoteEP = new IPEndPoint(ip, 5004);
                msg = Encoding.ASCII.GetBytes(Messages.HELLO.ToString());
                sock.SendTo(msg, endpoint);
                
                // TODO: Receive and verify a HelloMSG 
                int recv = sock.ReceiveFrom(msg, SocketFlags.None, ref remoteEP);
                Console.WriteLine("Messaged received from: {0} and the message is: {1}",h.From, Encoding.ASCII.GetString(msg,0,recv));

                // TODO: Send the RequestMSG message requesting to download a file name
                byte[] reqMSG = Encoding.ASCII.GetBytes(req);
                sock.SendTo(reqMSG, endpoint);
                
                // TODO: Receive a RequestMSG from remoteEndpoint
                // receive the message and verify if there are no errors
                if (ErrorHandler.VerifyRequest(r, c) == ErrorType.NOERROR )
                {
                    int x = sock.ReceiveFrom(msg, SocketFlags.None, ref remoteEP);
                    Console.WriteLine("Message received from {0} and the message is: {1}", r.From, Encoding.ASCII.GetString(msg, 0, x));
                }
                

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
