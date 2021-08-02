    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VM_Battle_Royale
{
    class Program
    {
        static byte[] _buffer = new byte[1024];
        private static Socket _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static string[] easywords = {"remove", "load", "signal", "right", "part", "url", "event", "stat", "call", "anon", "init", "dir", "add", "cookies", "handle", "ping", "ghost", "count", "loop", "temp","status","xml", "num", "bytes", "join", "intel", "reset", "info", "global", "size", "port", "get", "http", "emit", "delete", "buffer", "root", "file", "write", "socket", "bit", "key", "pass", "host", "val", "send", "list", "poly", "data", "log", "user", "upload", "set", "system", "com", "type", "add", "net", "client", "domain", "left", "point"};
        private static List<string> usernames = new List<string>();
        public static int asyncrec = new int();
        public static string gameState = "START";
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
                    _clientSocket.Connect(IPAddress.Parse("107.209.49.185"), 13000);
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
            string vmbrusername = JsonConvert.SerializeObject(dict);
             _clientSocket.Send(Encoding.Unicode.GetBytes(vmbrusername));
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
                    if (input == "username")
                    {
                        Console.WriteLine("What username would you like to use?");
                        string username = Console.ReadLine();
                        Dictionary<string, string> dict = new Dictionary<string, string>();
                        dict.Add("command", input);
                        dict.Add("playername", username);
                        string vmbrusername = JsonConvert.SerializeObject(dict);
                        _clientSocket.Send(Encoding.Unicode.GetBytes(vmbrusername));
                        asyncrec = 1;
                    }

                    else if (input == "hack")
                    {
                        if(gameState == "PLAY")
                        {
                            HackFunction();
                        } else if(gameState == "GRACE")
                        {
                            Console.WriteLine("ERROR: Unable to hack at this time. Reason: The grace period is still on.");
                        } else if(gameState == "START")
                        {
                            Console.WriteLine("ERROR: Unable to hack at this time. Reason: The game hasn't started yet.");
                        } else
                        {
                            Console.WriteLine("ERROR: Unable to hack at this time. Reason: The game is over.");
                        }
                    }
                    else if (input == "startgame")
                    {
                        Dictionary<string, string> dict = new Dictionary<string, string>();
                        dict.Add("command", input);
                        string vmbrstartgame = JsonConvert.SerializeObject(dict);
                        _clientSocket.Send(Encoding.Unicode.GetBytes(vmbrstartgame));
                        asyncrec = 1;
                    }
                    else
                    {
                        Console.WriteLine("Invalid request.");
                        Console.WriteLine("Available commands: username, hack, startgame");
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
            string text = Encoding.Unicode.GetString(tempbuffer);
            string value = JObject.Parse(text)["command"].ToString();
            string response = JObject.Parse(text)["response"].ToString();
            if (value == "username")
            {   
                Console.WriteLine(response);
                asyncrec = 0;
                _clientSocket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(RecieveCallBack), _clientSocket);
            } else if(value == "showips")
            {
                string numberofips = JObject.Parse(text)["amountofips"].ToString();
                for(int i = 0; i <= Int32.Parse(numberofips); i++)
                {
                    if (i == 0)
                    {
                        i++;
                    }
                    string ip = JObject.Parse(text)["ip" + 1].ToString();
                    Console.WriteLine("IP Address: " + ip);
                }
                asyncrec = 0;
                _clientSocket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(RecieveCallBack), _clientSocket);
            } else if(value == "hackshowuser")
            {
                string vmbrnumbers = JObject.Parse(text)["amountofusers"].ToString();
                for (int i = 0; i <= Int32.Parse(vmbrnumbers); i++)
                {
                    if (i == 0)
                    {
                        i++;
                    }
                    string vmbrusername = JObject.Parse(text)["username" + 1].ToString();
                    Console.WriteLine("Username: " + vmbrusername);
                    usernames.Add(vmbrusername);
                }
                string exists = WhoToHack();
                Dictionary<string, string> dict = new Dictionary<string, string>();
                dict.Add("command", "hackperson");
                dict.Add("persontobehacked", exists);
                HackTUI();
                _clientSocket.Send(Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(dict)));
            } else if(value == "hackperson")
            {
                Console.WriteLine(response);
                asyncrec = 0;
                _clientSocket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(RecieveCallBack), _clientSocket);
            } else if(value == "hackedperson")
            {
                Console.WriteLine("Hacked " + JObject.Parse(text)["username"] + "!");
                Console.WriteLine("IP Address for " + JObject.Parse(text)["username"] + ": " + JObject.Parse(text)["ip"]);
                Console.WriteLine("Password for " + JObject.Parse(text)["username"]+ ": " + JObject.Parse(text)["pass"]);
            } else if(value == "message")
            {
                Console.WriteLine(JObject.Parse(text)["response"]);
                asyncrec = 0;
                _clientSocket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(RecieveCallBack), _clientSocket);
            } else if(value == "gamestatechange")
            {
                gameState = JObject.Parse(text)["gamestate"].ToString();
                asyncrec = 0;
                _clientSocket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(RecieveCallBack), _clientSocket);
            }
        }

        private static void HackFunction()
        {
            Console.WriteLine("Listing usernames....");
            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict.Add("command", "showusername");
            string vmbrcommand = JsonConvert.SerializeObject(dict);
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
            int i = 0;
            while(i < 100)
            {
                Random random = new Random();
                string wordchosen = easywords[random.Next(0, easywords.Length - 1)];
                Console.WriteLine("Type " + wordchosen + "!");
                string input = Console.ReadLine();
                if(input == wordchosen)
                {
                   i += 10;
                   Console.WriteLine("Hacking progress: " + i + "%");
                } else
                {
                  Console.WriteLine("Incorrect! " + input + "does not equal " + wordchosen + "!");
                }
            }

        }
    }
}
