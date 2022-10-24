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

            remoteEP = new IPEndPoint(broadcast, 5004);
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(remoteEndpoint);
            
        }

        public ErrorType StartDownload()
        {
            // TODO: Instantiate and initialize different messages needed for the communication
            // required messages are: HelloMSG, RequestMSG, DataMSG, AckMSG, CloseMSG
            // Set attribute values for each class accordingly 
            HelloMSG GreetBack = new HelloMSG();
            RequestMSG req = new RequestMSG();
            DataMSG data = new DataMSG();
            AckMSG ack = new AckMSG();
            CloseMSG cls = new CloseMSG();
            ConSettings c = new ConSettings();
    
            GreetBack.From = Server;
            GreetBack.To = Client;
            GreetBack.ConID = SessionID;
            GreetBack.Type = Messages.HELLO;

            req.From = Server;
            req.To = Client;
            req.Type = Messages.REQUEST;

            c.To = Client;
            c.From = Server;
            c.Sequence = 0;

            cls.From = Server;
            cls.To = Client;
            cls.Type = Messages.CLOSE_CONFIRM;


            // TODO: Start the communication by receiving a HelloMSG message
            Console.WriteLine("Connection started: {0}", remoteEP);
            int recv = socket.ReceiveFrom(msg, SocketFlags.None, ref remoteEP);
            Console.WriteLine("Messaged received from: {0}. Content of the message: [{1}]", GreetBack.To, Encoding.ASCII.GetString(msg,0,recv));
           
            
            // TODO: If no error is found then HelloMSG will be sent back
            if (ErrorHandler.VerifyGreeting(GreetBack , c ) == ErrorType.NOERROR)
            {
                msg = Encoding.ASCII.GetBytes(Messages.HELLO_REPLY.ToString());
                socket.SendTo(msg, remoteEP);
            }
            
            // TODO: Receive the next message
            // Expected message is a download RequestMSG message containing the file name
            // Receive the message and verify if there are no errors
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


            // TODO: Send a RequestMSG of type REPLY message to remoteEndpoint verifying the status
            if (req.Status == ErrorType.NOERROR)
            {
                string statusMessage = $"Status of the message is: {req.Status} message type: {Messages.REPLY}";
                reqMSG = Encoding.ASCII.GetBytes(statusMessage);
                socket.SendTo(reqMSG, remoteEP);    
            }
            


            // TODO:  Start sending file data by setting first the socket ReceiveTimeout value
            socket.ReceiveTimeout = 5000;
            string text = "";
            if(OperatingSystem.IsWindows()){
                text = File.ReadAllText(req.FileName);
            }
            if(OperatingSystem.IsMacOS()){
                text = File.ReadAllText("../../../" + req.FileName);
            }
            
            byte[] fileBytes = File.ReadAllBytes(req.FileName); //Convert tekst file into bytes
            byte[][] chunk = new byte[(int)Params.WINDOW_SIZE][]; //prepare byte array with size of windows size

            //variables configuration
            int maxFileSize = 0;
            int maxChuckSize = 0;
            int fileIndex = 0;
            int checkWindowSize = 0;

            
            while(true)
            {
                for (int i = 0; i < chunk.Length; i++)
                {
                    chunk[i] = new byte[fileBytes.Length / (int)Params.SEGMENT_SIZE]; //Calculate bytes one chunk send and make array
                    for (int j = 0; j < chunk[i].Length; j++) //Loops calculated chunk untill it is full or ends
                    {
                        chunk[i][j] = fileBytes[fileIndex]; //Fill chunk of window size with value fileIndex | chunk = [windowsize][calculated byte from the message]
                        fileIndex++;
                        if(fileIndex == fileBytes.Length) //If index equals file.length. end loop
                        {
                            break;
                        }
                    }
                    socket.SendTo(chunk[i], remoteEP);
                    

                    checkWindowSize++;
                    if(checkWindowSize == (int)Params.WINDOW_SIZE)
                    {
                        socket.SendTimeout = 1000;
                        int max = 1;
                        while(true)
                        {
                            int x = socket.ReceiveFrom(revackMsg, SocketFlags.None, ref remoteEP);
                            Console.WriteLine("Message received from {0} and the message is: {1}", req.From, Encoding.ASCII.GetString(revackMsg, 0, x));
                            Console.WriteLine(max);
                            if(max == 5)
                            {
                                break;
                            }
                            max++;
                        }
                        break;
                    }
                    //Console.WriteLine(Encoding.ASCII.GetString(chunk[i]));
                    if(fileIndex == fileBytes.Length)
                    {
                        socket.SendTo(Encoding.ASCII.GetBytes(Messages.CLOSE_REQUEST.ToString()), remoteEP);
                        break;
                    }
                    
                }
                
                if(fileIndex == fileBytes.Length)
                {
                    break;
                }

                
                
                // if (checkWindowSize == (int)Params.WINDOW_SIZE)
                // {
                //     checkWindowSize = 0;
                //     for (int i = 0; i < (int)Params.WINDOW_SIZE; i++)
                //     {
                //         //send packages
                //     }
                // }

                // if (maxFileSize == fileBytes.Length)
                // {
                //     //Max size reached of array
                // }
            }
            
            int xd = socket.ReceiveFrom(revclsMsg, SocketFlags.None, ref remoteEP);
            Console.WriteLine("Message received from {0} and the message is: {1}", req.From, Encoding.ASCII.GetString(revclsMsg, 0, xd));
            cls.ConID = Int32.Parse(Encoding.ASCII.GetString(revclsMsg, 0, xd).Split("|")[1]);
            c.ConID = Int32.Parse(Encoding.ASCII.GetString(revclsMsg, 0, xd).Split("|")[1]);
            if(ErrorHandler.VerifyClose(cls, c) == ErrorType.NOERROR)
            {
                socket.Close();
            }
            
            // Console.WriteLine("--------------------------------");
            // foreach(var x in chunk)
            // {
            //     Console.WriteLine(Encoding.ASCII.GetString(x));
            // }
            // Console.WriteLine("--------------------------------");
            

            // TODO: Open and read the text-file first
            // Make sure to locate a path on windows and macos platforms
            



            // TODO: Sliding window with go-back-n implementation
            // Calculate the length of data to be sent
            // Send file-content as DataMSG message as long as there are still values to be sent
            // Consider the WINDOW_SIZE and SEGMENT_SIZE when sending a message  
            // Make sure to address the case if remaining bytes are less than WINDOW_SIZE
            //
            // Suggestion: while there are still bytes left to send,
            // first you send a full window of data
            // second you wait for the acks
            // then you start again.



            // TODO: Receive and verify the acknowledgements (AckMSG) of sent messages
            // Your client implementation should send an AckMSG message for each received DataMSG message   



            // TODO: Print each confirmed sequence in the console
            // receive the message and verify if there are no errors


            // TODO: Send a CloseMSG message to the client for the current session
            // Send close connection request

            // TODO: Receive and verify a CloseMSG message confirmation for the current session
            // Get close connection confirmation
            // Receive the message and verify if there are no errors


            //Console.WriteLine("Group members: {0} | {1}", student_1, student_2);
            return ErrorType.NOERROR;
        }
    }
}
