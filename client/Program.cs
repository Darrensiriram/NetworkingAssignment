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

            byte[] buffer = new byte[(int)Params.BUFFER_SIZE];
            byte[] msg = new byte[1024];
            byte[] revReqMsg = new byte[1024];
            byte[] revackMsg = new byte[1024];
            byte[] revcloMsg = new byte[1024];
            // TODO: Initialise the socket/s as needed from the description of the assignment
            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPAddress ip = IPAddress.Parse("127.0.0.1");

            HelloMSG h = new HelloMSG();
            RequestMSG r = new RequestMSG();
            DataMSG D = new DataMSG();
            AckMSG ack = new AckMSG();
            CloseMSG cls = new CloseMSG();
            ConSettings c = new ConSettings();
            Random id = new Random();
            var chosenId = id.Next(1, 40);

            h.From = student_1;
            h.To = "Server";
            h.ConID = chosenId;
            h.Type = Messages.HELLO;

            r.From = student_1;
            r.To = "Server";
            r.FileName = "test.txt";
            r.ConID = chosenId;
            r.Type = Messages.REQUEST;
            
            c.ConID = chosenId;
            c.From = student_1;
            c.To = "Server";
            c.Sequence = 0;

            ack.From = student_1;
            ack.To = "Server";
            ack.Sequence = 0;
            ack.ConID = chosenId;
            ack.Type = Messages.ACK;

            cls.ConID = chosenId;
            cls.From = student_1;
            cls.To = "Server";
            cls.Type = Messages.CLOSE_CONFIRM;
            
            string req = "Type of message:" + r.Type + " & " + " File name:" + r.FileName + "& " + " Id:" + c.ConID;

            try
            {
                IPEndPoint endpoint = new IPEndPoint(ip, 5004);
                EndPoint remoteEP = new IPEndPoint(ip, 5004);
                msg = Encoding.ASCII.GetBytes(Messages.HELLO.ToString());
                sock.SendTo(msg, endpoint);
                
                // TODO: Receive and verify a HelloMSG 
                //int recv = sock.ReceiveFrom(msg, SocketFlags.None, ref remoteEP);
                //Console.WriteLine("Messaged received from: {0} and the message is: {1}",h.From, Encoding.ASCII.GetString(msg,0,recv));

                // TODO: Send the RequestMSG message requesting to download a file name
                byte[] reqMSG = Encoding.ASCII.GetBytes(req);
                sock.SendTo(reqMSG, endpoint);
                
                // TODO: Receive a RequestMSG from remoteEndpoint
                // receive the message and verify if there are no errors
                if (ErrorHandler.VerifyRequest(r, c) == ErrorType.NOERROR )
                {
                    int x = sock.ReceiveFrom(revReqMsg, SocketFlags.None, ref remoteEP);
                    Console.WriteLine("Message received from {0} and the message is: {1}", r.From, Encoding.ASCII.GetString(revReqMsg, 0, x));
                }
                

                // TODO: Check if there are more DataMSG messages to be received 
                // receive the message and verify if there are no errors
                string fullText = "";
                while(true)
                {
                    int x = sock.ReceiveFrom(revackMsg, SocketFlags.None, ref remoteEP);
                    Console.WriteLine("Message received from {0} and the message is: {1}", r.From, Encoding.ASCII.GetString(revackMsg, 0, x));
                    string packetNumber = Encoding.ASCII.GetString(revackMsg, 0, x).Split("|")[0];
                    string split1 = Encoding.ASCII.GetString(revackMsg, 0, x).Split("|")[0];
                    fullText += Encoding.ASCII.GetString(revackMsg, 0, x);
                    ack.Sequence++;
                    if(ErrorHandler.VerifyAck(ack, c) == ErrorType.NOERROR)
                    {
                        //if message correct, send correct back
                        sock.SendTo(Encoding.ASCII.GetBytes(Messages.ACK.ToString()), remoteEP);
                    } else {
                        sock.SendTo(Encoding.ASCII.GetBytes(packetNumber.ToString()), remoteEP);
                    }
                    if((int)Params.WINDOW_SIZE == ack.Sequence)
                    {
                        break;
                    }
                }
                //Console.WriteLine(fullText);
                
                int xd = sock.ReceiveFrom(revcloMsg, SocketFlags.None, ref remoteEP);
                Console.WriteLine("Message received from {0} and the message is: {1}", r.From, Encoding.ASCII.GetString(revcloMsg, 0, xd));
                
                if(ErrorHandler.VerifyClose(cls, c) == ErrorType.NOERROR)
                {
                        sock.SendTo(Encoding.ASCII.GetBytes(Messages.CLOSE_CONFIRM.ToString()+"|"+cls.ConID), remoteEP);
                }

                

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
