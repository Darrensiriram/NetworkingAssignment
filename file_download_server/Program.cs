using System.Linq.Expressions;
using System;
using System.Net;
using System.Net.Sockets;
using UDP_FTP.File_Handler;
using static UDP_FTP.Models.Enums;

namespace UDP_FTP
{
    class Program
    {
        static void Main(string[] args)
        {
            string student_1 = "Darren Siriram 0999506" ;
            string student_2 = "Ertugrul Karaduman 0997475";

            
            Console.WriteLine("Server is waiting for new request!");
            Communicate FileShare = new Communicate();
            Console.WriteLine("The file download request terminated with code {0}.", FileShare.StartDownload());

            Console.WriteLine("Group members: {0} , {1}", student_1, student_2);
        }
    }
}
