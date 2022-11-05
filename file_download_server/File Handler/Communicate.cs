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
        private const string Client = "Darren Siriram";
        private int SessionID;
        private Socket socket;
        private IPEndPoint remoteEndpoint;
        private EndPoint remoteEP;
        private ErrorType Status;
        private byte[] buffer;
        byte[] msg;
        byte[] reqMSG;
        byte[] revackMsg;
        byte[] revclsMsg;
        private string file;
        ConSettings C;


        public Communicate()
        {
            IPAddress broadcast = IPAddress.Parse("127.0.0.1");
            remoteEndpoint = new IPEndPoint(broadcast, 5004);

            buffer = new byte[(int)Params.BUFFER_SIZE];
            msg = new byte[1024];

            reqMSG = new byte[1024];
            revackMsg = new byte[1024];
            revclsMsg = new byte[1024];

            Random id = new Random();
            SessionID = id.Next(1, 40);

            remoteEP = new IPEndPoint(broadcast, 5010);
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(remoteEndpoint);
            
        }

        public ErrorType StartDownload()
        {
            HelloMSG GreetBack = new HelloMSG();
            RequestMSG req = new RequestMSG();
            DataMSG data = new DataMSG();
            AckMSG ack = new AckMSG();
            CloseMSG cls = new CloseMSG();
            ConSettings c = new ConSettings();
            Random id = new Random();
            var chosenId = id.Next(1, 40);
    
            GreetBack.From = Server;
            GreetBack.To = Client;
            GreetBack.ConID = SessionID;
            GreetBack.Type = Messages.HELLO;
            GreetBack.ConID = chosenId;

            req.From = Server;
            req.To = Client;
            req.Type = Messages.REQUEST;
            req.ConID = chosenId;

            c.To = Client;
            c.From = Server;
            c.Sequence = 0;
            c.ConID = chosenId;

            cls.From = Server;
            cls.To = Client;
            cls.Type = Messages.CLOSE_CONFIRM;
            cls.ConID = chosenId;

            data.Type = Messages.DATA;
            data.From = Server;
            data.To = Client;
            data.ConID = chosenId;
            data.Data = new byte[(int)Params.SEGMENT_SIZE];

            Console.WriteLine("Connection started: {0}", remoteEP);
            int recv = socket.ReceiveFrom(msg, SocketFlags.None, ref remoteEP);
            Console.WriteLine("Messaged received from: {0}. Content of the message: [{1}]", GreetBack.To, Encoding.ASCII.GetString(msg,0,recv));
           
            if (ErrorHandler.VerifyGreeting(GreetBack , c ) == ErrorType.NOERROR)
            {
                msg = Encoding.ASCII.GetBytes(GreetBack.ConID.ToString() + "|" + Messages.HELLO_REPLY.ToString());
                socket.SendTo(msg, remoteEP);
            }
        
            if (ErrorHandler.VerifyRequest(req, c) == ErrorType.NOERROR)
            {
                int x = socket.ReceiveFrom(reqMSG, SocketFlags.None, ref remoteEP);
                Console.WriteLine("Messaged received from: {0} and the message is: {1}",GreetBack.To, Encoding.ASCII.GetString(reqMSG,0, x ));
                string reqMessage = Encoding.ASCII.GetString(reqMSG, 0, x);
                var s = reqMessage.Split("&")[1];
                req.FileName = s.Split(":")[1];
                req.Status = ErrorType.NOERROR;
                
            }
            else
            {
                req.Status = ErrorType.BADREQUEST;
                Console.WriteLine(ErrorType.BADREQUEST.ToString());
            }

            if (req.Status == ErrorType.NOERROR)
            {
                string statusMessage = $"Status of the message is: {req.Status} message type: {Messages.REPLY}";
                reqMSG = Encoding.ASCII.GetBytes(statusMessage);
                socket.SendTo(reqMSG, remoteEP);    
            }
            
            socket.ReceiveTimeout = 5000;
            string filelocation = "";
            if(OperatingSystem.IsWindows()){
                filelocation = req.FileName;
            }
            if(OperatingSystem.IsMacOS()){
                filelocation = "../../../" + req.FileName;
            }
            
            byte[] fileBytes = File.ReadAllBytes(filelocation); //Convert tekst file into bytes
            byte[][] chunk = new byte[(int)Params.WINDOW_SIZE][]; //prepare byte array with size of windows size
            data.Size = fileBytes.Length / (int)Params.SEGMENT_SIZE + 1; // calculate how many byte can be send within a segmentSize
            data.More = true;
            var secondLast = 522;

            int fileIndex = 0;
            int checkWindowSize = 0;
            data.Sequence = 0;
      

            while(data.More)
            {
                
                for (int i = 0; i < chunk.Length; i++)
                {
                    chunk[i] = new byte[data.Size]; //Calculate bytes one chunk send and make array
                    for (int j = 0; j < chunk[i].Length; j++) //Loops calculated chunk until it is full or ends
                    {
                        if (fileIndex < fileBytes.Length)
                        {
                            chunk[i][j] = fileBytes[fileIndex]; //Fill chunk of window size with value fileIndex | chunk = [window size][calculated byte from the message]
                            fileIndex++;
                        }
                        else
                        {
                            data.More = false;
                        }
                    }

                    data.Data = chunk[i];
                    byte[] sendPacket = Encoding.ASCII.GetBytes(data.Sequence + "|" + Encoding.ASCII.GetString(data.Data) + "|" + data.More + "|" + data.Size);
                    socket.SendTo(sendPacket, remoteEP);
                    data.Sequence++;
                    c.Sequence += sendPacket.Length;
                    
                    // takes care of ACK message
                    checkWindowSize++;
                    if(checkWindowSize == (int)Params.WINDOW_SIZE)
                    {
                        checkWindowSize = 0;
                        socket.SendTimeout = 1000;
                        int max = 1;
                        bool confirm = true;
                        while(true)
                        {
                            int x = socket.ReceiveFrom(revackMsg, SocketFlags.None, ref remoteEP);
                            Console.WriteLine("Message received from {0} and the message is: {1}", req.From, Encoding.ASCII.GetString(revackMsg, 0, x));
                            if(Encoding.ASCII.GetString(revackMsg, 0, x) != Messages.ACK.ToString())
                            {
                                int packetNumber = Int32.Parse(Encoding.ASCII.GetString(revackMsg, 0, x)) - 1;
                                fileIndex = (packetNumber * data.Size) - data.Size; // 464
                                data.More = true;
                                break;
                            }
                            if(max == (int)Params.WINDOW_SIZE)
                            {
                                break;
                            }
                            max++;
                        }
                    }

                    if(data.More == false)
                    {
                        socket.SendTo(Encoding.ASCII.GetBytes(Messages.CLOSE_REQUEST.ToString()), remoteEP);
                        break;
                    }
                    
                }
            }
            
            
            int xd = socket.ReceiveFrom(revclsMsg, SocketFlags.None, ref remoteEP);
            Console.WriteLine("Message received from {0} and the message is: {1}", req.From, Encoding.ASCII.GetString(revclsMsg, 0, xd));
            if(ErrorHandler.VerifyClose(cls, c) == ErrorType.NOERROR)
            {
                socket.Close();
            }
            return ErrorType.NOERROR;
        }
    }
}
