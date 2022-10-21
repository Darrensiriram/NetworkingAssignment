using System;
using System.Collections.Generic;
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
        private string Client;
        private int SessionID;
        private Socket socket;
        private IPEndPoint remoteEndpoint;
        private EndPoint remoteEP;
        private ErrorType Status;
        private byte[] buffer;
        byte[] msg;
        private string file;
        ConSettings C;


        public Communicate()
        {
            // TODO: Initializes another instance of the IPEndPoint for the remote host
            remoteEndpoint = new IPEndPoint(IPAddress.Any, 5004);

            // TODO: Specify the buffer size
            buffer = new byte[1000];
            msg = new byte[1024];
            
            // TODO: Get a random SessionID
            Random id = new Random();
            SessionID = id.Next(1, 400);
            
            // TODO: Create local IPEndpoints and a Socket to listen 
            remoteEP = new IPEndPoint(IPAddress.Any, 5004);
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            
            
            
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

            GreetBack.From = Server;
            GreetBack.To = Client;
            GreetBack.ConID = SessionID;
            GreetBack.Type = Messages.HELLO_REPLY;
                
            // TODO: Start the communication by receiving a HelloMSG message
            Console.WriteLine("Message received from: {0}", remoteEP);
            int recv = socket.ReceiveFrom(msg, SocketFlags.None, ref remoteEP);
            Console.WriteLine(Encoding.ASCII.GetString(msg,0,recv));
            // Receive and deserialize HelloMSG message 
            // Verify if there are no errors
            // Type must match one of the ConSettings' types and receiver address must be the server address
            
            // TODO: If no error is found then HelloMSG will be sent back


            // TODO: Receive the next message
            // Expected message is a download RequestMSG message containing the file name
            // Receive the message and verify if there are no errors



            // TODO: Send a RequestMSG of type REPLY message to remoteEndpoint verifying the status



            // TODO:  Start sending file data by setting first the socket ReceiveTimeout value



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
