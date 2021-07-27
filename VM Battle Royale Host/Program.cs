    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;

namespace VM_Battle_Royale
{
    class Program
    {
        static byte[] _buffer = new byte[1024];
        private static Socket _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static string[] easywords = {   };
        private static List<string> usernames = new List<string>();
        public static int asyncrec = new int();
        static bool waiting;
        static void Main(string[] args)
        {
            LoopConnect();
            Console.ReadLine();
        }

        private static void LoopConnect()
        {
            int attempts = 0;
            while (!_clientSocket.Connected)
            {

                try
                {
                    attempts++;
                    _clientSocket.Connect(IPAddress.Loopback, 13000);
                }
                catch (SocketException)
                {
                    Console.Clear();
                    Console.WriteLine("Connection attempts: " + attempts.ToString());
                }
            }
            Console.Clear();
            Console.WriteLine("Successfully connected to the server!");
            _clientSocket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(RecieveCallBack), _clientSocket);
            Console.WriteLine("What username would you like to use?");
            string username = Console.ReadLine();
             Dictionary<string, string> dict = new Dictionary<string, string>();
             dict.Add("command", "username");
             dict.Add("playername", username);
             string vmbrusername = VMBRFormatHandler.CreateVMBRFormat(dict);
             _clientSocket.Send(Encoding.ASCII.GetBytes(vmbrusername));
              asyncrec = 1;
            SendLoop();
            Console.ReadLine();
        }

        private static void SendLoop()
        {
            _clientSocket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(RecieveCallBack), _clientSocket);
            while (true)
            {
                if (asyncrec == 0)
                {
                    Console.WriteLine("Enter a request.");
                    string input = Console.ReadLine();
                    Console.WriteLine("Available commands: username.");
                    if (input == "username")
                    {
                        Console.WriteLine("What username would you like to use?");
                        string username = Console.ReadLine();
                        Dictionary<string, string> dict = new Dictionary<string, string>();
                        dict.Add("command", input);
                        dict.Add("playername", username);
                        string vmbrusername = VMBRFormatHandler.CreateVMBRFormat(dict);
                        _clientSocket.Send(Encoding.ASCII.GetBytes(vmbrusername));
                        asyncrec = 1;
                    }
                    else if (input == "showips")
                    {
                        Console.WriteLine("Showing all vm ips....");
                        string vmbrcommand = VMBRFormatHandler.CreateVMBRFormat("command", "showips");
                        _clientSocket.Send(Encoding.ASCII.GetBytes(vmbrcommand));
                        asyncrec = 1;
                    }

                    else if (input == "hack")
                    {
                        HackFunction();
                    }
                    else
                    {
                        Console.WriteLine("Invalid request.");
                    }
                }
                else
                {

                }
                



            }
        }

        private static void RecieveCallBack(IAsyncResult ar)
        {
            asyncrec = 1;
            Socket socket = (Socket)ar.AsyncState;
                asyncrec = socket.EndReceive(ar);
            byte[] tempbuffer = new byte[asyncrec];
            Array.Copy(_buffer, tempbuffer, asyncrec);
            string text = Encoding.ASCII.GetString(tempbuffer);
           string value = VMBRFormatHandler.GetValue(text, "command");
            string response = VMBRFormatHandler.GetValue(text, "response");
            if (value == "username")
            {   
                Console.WriteLine(response);
                asyncrec = 0;
                _clientSocket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(RecieveCallBack), _clientSocket);
            } else if(value == "showips")
            {
                string numberofips = VMBRFormatHandler.GetValue(text,"amountofips");
                for(int i = 0; i <= Int32.Parse(numberofips); i++)
                {
                    if (i == 0)
                    {
                        i++;
                    }
                    string ip = VMBRFormatHandler.GetValue(text, "ip" + i);
                    Console.WriteLine("IP Address: " + ip);
                }
                asyncrec = 0;
                _clientSocket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(RecieveCallBack), _clientSocket);
            } else if(value == "hackshowuser")
            {
                string vmbrnumbers = VMBRFormatHandler.GetValue(Encoding.ASCII.GetString(tempbuffer), "amountofusers");
                for (int i = 0; i <= Int32.Parse(vmbrnumbers); i++)
                {
                    if (i == 0)
                    {
                        i++;
                    }
                    string vmbrusername = VMBRFormatHandler.GetValue(Encoding.ASCII.GetString(tempbuffer), "username" + i);
                    Console.WriteLine("Username: " + vmbrusername);
                    usernames.Add(vmbrusername);
                }
                string exists = WhoToHack();
                Dictionary<string, string> dict = new Dictionary<string, string>();
                dict.Add("command", "hackperson");
                dict.Add("persontobehacked", exists);
                HackTUI();
                _clientSocket.Send(Encoding.ASCII.GetBytes(VMBRFormatHandler.CreateVMBRFormat(dict)));
            } else if(value == "hackperson")
            {
                Console.WriteLine(response);
                asyncrec = 0;
                _clientSocket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(RecieveCallBack), _clientSocket);
            } else if(value == "hackedperson")
            {
                Console.WriteLine("Hacked " + VMBRFormatHandler.GetValue(tempbuffer, "username") + "!");
                Console.WriteLine("IP Address for " + VMBRFormatHandler.GetValue(tempbuffer, "username") + ": " + VMBRFormatHandler.GetValue(tempbuffer, "ip"));
            }
        }

        private static void HackFunction()
        {
            Console.WriteLine("Listing usernames....");
            string vmbrcommand = VMBRFormatHandler.CreateVMBRFormat("command", "showusernames");
            _clientSocket.Send(Encoding.ASCII.GetBytes(vmbrcommand));
            asyncrec = 1;
        }

        private static string WhoToHack()
        {
            Console.WriteLine("Who would you like to hack?");
            string hackperson = Console.ReadLine();
            foreach (string s in usernames)
            {
                if (s == hackperson)
                {
                    return hackperson;
                }
            }
            Console.WriteLine("Invalid input, please retry.");
            hackperson = WhoToHack();
            return hackperson;
        }

        private static byte[] Recieve()
        { 
            int rec = new int();
            byte[] tempbuffer = new byte[1024];
            rec = _clientSocket.Receive(tempbuffer);
            byte[] data = new byte[rec];
            Array.Copy(tempbuffer, data, rec);
            return data;
        }

        private static void HackTUI()
        {
            for(int i = 0; i <= 100; i += 10)
            {
                Random random = new Random();
                string wordchosen = easywords[random.Next(0, easywords.Length - 1)];
                Console.WriteLine("Type " + wordchosen + "!");
                string input = Console.ReadLine();
                if(input == wordchosen)
                {
                   Console.WriteLine("Hacking progress: " + i + "%");
                }
            }

        }
    }
}
