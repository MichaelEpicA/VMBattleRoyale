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
            //Get the recieve function ready.
            _clientSocket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(RecieveCallBack), _clientSocket);
            while (true)
            {
                //Checks if the host is in progress of recieving something. If it is, wait until that is done, else continue on.
                if (asyncrec == 0)
                {
                    Console.WriteLine("Enter a request.");
                    string input = Console.ReadLine();
                    if (input == "username")
                    {
                        UsernameFunction("username");
                    }
                    //Basic checking, don't worry the server will also check as well.
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
                



            }
        }

        private static void RecieveCallBack(IAsyncResult ar)
        {
            //Sets the number to 1 to show that it is progress of recieving something.
            asyncrec = 1;
            Socket socket = (Socket)ar.AsyncState;
                asyncrec = socket.EndReceive(ar);
            byte[] tempbuffer = new byte[asyncrec];
            Array.Copy(_buffer, tempbuffer, asyncrec);
            string text = Encoding.Unicode.GetString(tempbuffer);
            string value = JObject.Parse(text)["command"].ToString();
            string response = JObject.Parse(text)["response"].ToString();
            if (value == "username")
            {   //If the username already exists, we reprompt the user.
                if(response.Contains("exists"))
                {
                    UsernameFunction("username");
                } else
                {
                    //If it doesn't already exist, we just write what the server told us to write.
                    Console.WriteLine(response);
                }
                //Sets the variable to 0, to show that the host is done recieving something.
                asyncrec = 0;
                _clientSocket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(RecieveCallBack), _clientSocket);
            } else if(value == "showips")
            {
                //Not used a lot, used for showing ips of the host users. (DEBUG)
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
                //Used for showing users that are hackable.
                string vmbrnumbers = JObject.Parse(text)["amountofusers"].ToString();
                for (int i = 0; i <= Int32.Parse(vmbrnumbers); i++)
                {
                    if (i == 0)
                    {
                        i++;
                    }
                    string vmbrusername = JObject.Parse(text)["username" + 1].ToString();
                    Console.WriteLine("Username: " + vmbrusername);
                    //Adds the usernames to a temporary list, mainly just used to check if the username actually exists in the WhoToHack function.
                    usernames.Add(vmbrusername);
                }
                string exists = WhoToHack();
                //This is how we build JSON, mainly because this is how the VMBR formatter worked.
                Dictionary<string, string> dict = new Dictionary<string, string>();
                dict.Add("command", "hackperson");
                dict.Add("persontobehacked", exists);
                //Calls the hack interface to pull it up.
                HackTUI();
                _clientSocket.Send(Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(dict)));

            } else if(value == "hackperson")
            {
                //This is used when the user got hacked, and alerts them.
                Console.WriteLine(response);
                asyncrec = 0;
                _clientSocket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(RecieveCallBack), _clientSocket);
            } else if(value == "hackedperson")
            {
                //When the user successfully hacks somebody, it sends back ip and pass, so here, we just display that info.
                Console.WriteLine("Hacked " + JObject.Parse(text)["username"] + "!");
                Console.WriteLine("IP Address for " + JObject.Parse(text)["username"] + ": " + JObject.Parse(text)["ip"]);
                Console.WriteLine("Password for " + JObject.Parse(text)["username"]+ ": " + JObject.Parse(text)["pass"]);
            } else if(value == "message")
            {
                //Used if I just wanna send a message to clients for some reason.
                Console.WriteLine(JObject.Parse(text)["response"]);
                asyncrec = 0;
                _clientSocket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(RecieveCallBack), _clientSocket);
            } else if(value == "gamestatechange")
            {
                //Changes gamestate, for example if the game just starts, this would be called to update it here.
                gameState = JObject.Parse(text)["gamestate"].ToString();
                asyncrec = 0;
                _clientSocket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(RecieveCallBack), _clientSocket);
            }
        }

        private static void HackFunction()
        {
            //Says that it's listing usernames, and then sends a request to get those usernames.
            Console.WriteLine("Listing usernames....");
            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict.Add("command", "showusername");
            string vmbrcommand = JsonConvert.SerializeObject(dict);
            _clientSocket.Send(Encoding.ASCII.GetBytes(vmbrcommand));
            asyncrec = 1;
        }

        private static string WhoToHack()
        {
            //This function gets called when the usernames are recieved, it just asks who would you like to hack, reprompting is a bit broken though.
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

        private static void HackTUI()
        {
            // The "interface" of the hacking, allows for really easy edits to the TUI.
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

        private static void UsernameFunction(string input)
        {
            //Sends the username to the server, and this was made a function just for reprompting.
            Console.WriteLine("What username would you like to use?");
            string username = Console.ReadLine();
            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict.Add("command", input);
            dict.Add("playername", username);
            string vmbrusername = JsonConvert.SerializeObject(dict);
            _clientSocket.Send(Encoding.Unicode.GetBytes(vmbrusername));
            asyncrec = 1;
        }
    }
}
