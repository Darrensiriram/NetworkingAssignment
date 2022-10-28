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
            byte[] revMsg = new byte[1024];
            byte[] revReqMsg = new byte[1024];
            byte[] revackMsg = new byte[1024];
            byte[] revcloMsg = new byte[1024];
            
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
            h.Type = Messages.HELLO;

            r.From = student_1;
            r.To = "Server";
            r.FileName = "test.txt";
            r.Type = Messages.REQUEST;
            
            c.From = student_1;
            c.To = "Server";
            c.Sequence = 0;

            ack.From = student_1;
            ack.To = "Server";
            ack.Sequence = 1;
            ack.Type = Messages.ACK;
            
            cls.From = student_1;
            cls.To = "Server";
            cls.Type = Messages.CLOSE_CONFIRM;
           
            try
            {
                IPEndPoint endpoint = new IPEndPoint(ip, 5004);
                EndPoint remoteEP = new IPEndPoint(ip, 5004);
                msg = Encoding.ASCII.GetBytes(Messages.HELLO.ToString());
                sock.SendTo(msg, endpoint);

                int recv = sock.ReceiveFrom(revMsg, SocketFlags.None, ref remoteEP);
                Console.WriteLine("Messaged received from: {0} and the message is: {1}",h.From, Encoding.ASCII.GetString(revMsg,0,recv));
                int chosenId = Int32.Parse(Encoding.ASCII.GetString(revMsg, 0, recv).Split("|")[0]);

                h.ConID = chosenId;
                r.ConID = chosenId;
                D.ConID = chosenId;
                c.ConID = chosenId;
                ack.ConID = chosenId;
                cls.ConID = chosenId;
                
                string req = "Type of message:" + r.Type + " & " + " File name:" + r.FileName + "& " + " Id:" + c.ConID + " Message from: " + r.From + " ,Message sending to: " + r.To ;
                byte[] reqMSG = Encoding.ASCII.GetBytes(req);
                sock.SendTo(reqMSG, endpoint);
                
                if (ErrorHandler.VerifyRequest(r, c) == ErrorType.NOERROR )
                {
                    int x = sock.ReceiveFrom(revReqMsg, SocketFlags.None, ref remoteEP);
                    Console.WriteLine("Message received from {0} and the message is: {1}", r.From, Encoding.ASCII.GetString(revReqMsg, 0, x));
                }
                
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

                    if (ack.Sequence == (int)Params.WINDOW_SIZE)
                    {
                        ack.Sequence = 0;
                    }
                    string boolingValue = Encoding.ASCII.GetString(revackMsg, 0, x).Split("|")[2];
                    if(boolingValue == "False")
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
