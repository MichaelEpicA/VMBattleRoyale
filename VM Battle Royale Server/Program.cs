using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VM_Battle_Royale
{
    class Program
    {
        static List<Socket> _clientSockets = new List<Socket>();
        static Dictionary<string, Socket> usernames = new Dictionary<string, Socket>();
        static Dictionary<IPAddress, VMAndPass> vmandpass = new Dictionary<IPAddress, VMAndPass>();
        static Socket _serversocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        static string gameState = "START";
        static byte[] _buffer = new byte[1024];
        static void Main(string[] args)
        {
            SetupServer();
        }

        static void SetupServer()
        {
             Console.Write("Setting up test VMBR server...");
            _serversocket.Bind(new IPEndPoint(IPAddress.Any, 13000));
            _serversocket.Listen(5);
            _serversocket.BeginAccept(new AsyncCallback(AcceptCallBack), null);
            Console.Write("Done.");
            Console.WriteLine("\nWaiting for a connection...");
            Console.Read();
        }

        private static void AcceptCallBack(IAsyncResult ar)
         {
            Socket socket = _serversocket.EndAccept(ar);
            Console.WriteLine("New client connected!");
            Console.WriteLine(_clientSockets.Count);
            _clientSockets.Add(socket);
            try {
                socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(RecieveCallBack), socket);
            }
            catch(Exception)
            {
                socket.Disconnect(false);
                _clientSockets.Remove(socket);
            }
            _serversocket.BeginAccept(new AsyncCallback(AcceptCallBack), null);

        }

        private static void RecieveCallBack(IAsyncResult ar)
       {
            int recieved = new int();
            Socket socket = (Socket)ar.AsyncState;
            try 
            {
                recieved = socket.EndReceive(ar);
            } catch(SocketException)
            {
                Disconnect(socket);
                return;
            }
            byte[] tempbuffer = new byte[recieved];
            Array.Copy(_buffer, tempbuffer, recieved);
            string text = Encoding.Unicode.GetString(tempbuffer);
            if(text == "")
            {
                Dictionary<string, string> dict = new Dictionary<string, string>();
                dict.Add("command", "message");
                dict.Add("response", "Invalid command.");
                socket.Send(Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(dict)));
                return;
            }
            string command = JObject.Parse(text)["command"].ToString();
            if(command == "dc")
            {
                Disconnect(socket);
            }

            if(command == "vm")
            {
                if (gameState == "START")
                {
                    IPEndPoint end = (IPEndPoint)socket.LocalEndPoint;

                    VMAndPass convert = new VMAndPass
                    {
                        Ngrokurl = JObject.Parse(text)["ngrokurl"].ToString(),
                        Pass = JObject.Parse(text)["pass"].ToString()
                    };
                    Task.Run(() => CheckIfDisconnected(socket));
                    vmandpass.Add(end.Address, convert);
                }
                else if (gameState == "PLAY")
                {
                    VMAndPass check = new VMAndPass();
                    IPEndPoint end = (IPEndPoint)socket.LocalEndPoint;
                    if (vmandpass.TryGetValue(end.Address, out check))
                    {
                        if (check.Disconnected == true)
                        {
                            vmandpass[end.Address].Disconnected = false;
                            check.Ngrokurl = JObject.Parse(text).Value<string>( "ngrokurl");
                        }
                        else if (check.Eliminated == true)
                        {
                            socket.Send(Encoding.ASCII.GetBytes("ERROR: You've been eliminated from this game. Please wait until the game ends to rejoin!"));
                            socket.Disconnect(false);
                            _clientSockets.Remove(socket);
                        }
                    }
                    else
                    {
                        socket.Send(Encoding.ASCII.GetBytes("ERROR: Sorry, the game has already started. You can't join right now. Wait for the end of the game!"));
                        socket.Disconnect(false);
                        _clientSockets.Remove(socket);
                    }


                } else
                {
                    foreach(KeyValuePair<IPAddress, VMAndPass> kvp in vmandpass)
                    { if(!kvp.Value.Eliminated)
                        {
                            foreach(KeyValuePair<string,Socket> kvp2 in usernames)
                            {
                                IPEndPoint end = (IPEndPoint)socket.RemoteEndPoint;
                                if(end.Address == kvp.Key)
                                {
                                    Dictionary<string, string> dict = new Dictionary<string, string>();
                                    dict.Add("command", "message");
                                    dict.Add("response", "You have won VMBR! Congratulations!");    
                                    kvp2.Value.Send(Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(dict)));
                                }
                            }
                        }
                     
                    }
                }
            }

            if(command == "username")
            {
                string username = JObject.Parse(text)["playername"].ToString();
                IPEndPoint ip = (IPEndPoint)socket.RemoteEndPoint;
                foreach (string v in usernames.Keys.ToList())
                {
                    foreach(Socket s in usernames.Values.ToList())
                    {
                        IPEndPoint tableip = (IPEndPoint)s.RemoteEndPoint;
                        if (tableip.Address == ip.Address)
                        {
                            usernames.Remove(v);
                            usernames.Add(username, socket);
                            Dictionary<string, string> vmbrconvert = new Dictionary<string, string>();
                            string response = "Username set to " + username + "!";
                            vmbrconvert.Add("command", command);
                            vmbrconvert.Add("response", response);
                            string convertedvmbr = JsonConvert.SerializeObject(vmbrconvert);
                            socket.Send(Encoding.Unicode.GetBytes(convertedvmbr));
                            break;
                        }
                        else if (v == username)
                        {
                            Dictionary<string, string> vmbrconvert = new Dictionary<string, string>();
                            vmbrconvert.Add("command", command);
                            vmbrconvert.Add("response", "Sorry! That username already exists!");
                            string convertedvmbr = JsonConvert.SerializeObject(vmbrconvert);
                            socket.Send(Encoding.Unicode.GetBytes(convertedvmbr));
                            break;
                        }
                        else
                        {
                            usernames.Add(username, socket);
                            Dictionary<string, string> vmbrconvert = new Dictionary<string, string>();
                            vmbrconvert.Add("command", command);
                            vmbrconvert.Add("response", "Username set to " + username + "!");
                            string convertedvmbr = JsonConvert.SerializeObject(vmbrconvert);
                            socket.Send(Encoding.Unicode.GetBytes(convertedvmbr));
                            break;
                        }
                    }
                }
                if(usernames.Count == 0)
                {
                    usernames.Add(username, socket);
                    Dictionary<string, string> vmbrconvert = new Dictionary<string, string>();
                    vmbrconvert.Add("command", command);
                    vmbrconvert.Add("response", "Username set to " + username + "!");
                    string convertedvmbr = JsonConvert.SerializeObject(vmbrconvert);
                    socket.Send(Encoding.Unicode.GetBytes(convertedvmbr));
                }
            }


            
            if(command == "startgame")
            {
                if(usernames.Count == vmandpass.Count && usernames.Count > 2 && gameState == "START" && usernames.ElementAt(0).Value == socket)
                {
                    gameState = "GRACE";
                    foreach (KeyValuePair<string,Socket> kvp in usernames)
                    {
                        Dictionary<string, string> dict = new Dictionary<string, string>();
                        dict.Add("command", "gamestatechange");
                        dict.Add("gamestate", "graceperiod");
                        socket.Send(Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(dict)));
                    }
                } else
                {
                    Dictionary<string, string> dict = new Dictionary<string, string>();
                    dict.Add("command", "message");
                    if(usernames.Count <= 2)
                    {
                        dict.Add("response", "Failed to start the game. Reason: There isn't enough players to start the game.");
                    } else if(usernames.Count != vmandpass.Count)
                    {
                        dict.Add("response", "Failed to start the game. Reason: There aren't the same amount of VMS connected as interfaces. \n This means that some vm(s) or interface(s) have not been connected to the server.");
                    } else if(gameState != "START")
                    {
                        dict.Add("response", "Failed to start the game. Reason: The game has either already started or ended.");
                    } else if(usernames.ElementAt(0).Value != socket)
                    {
                        dict.Add("response", "Failed to start the game. Reason: You are not the host of this game!");
                    }
                    
                    socket.Send(Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(dict)));
                }
            }

            if(command == "showips")
            {
                Dictionary<string, string> vmbrconvert = new Dictionary<string, string>();
                vmbrconvert.Add("command", command);
                int i = 1;
                foreach (KeyValuePair<string,Socket> v in usernames)
                {
                     
                    IPEndPoint tableip = (IPEndPoint)v.Value.RemoteEndPoint;
                    vmbrconvert.Add("ip" + i, tableip + " ");
                    i++;
                }
                vmbrconvert.Add("amountofips", (vmbrconvert.Count - 1).ToString());
                string convertedvmbr = JsonConvert.SerializeObject(vmbrconvert);
                try
                {
                    socket.Send(Encoding.ASCII.GetBytes(convertedvmbr));
                }
                catch (SocketException)
                {
                    Disconnect(socket);
                }
            }
            
            if(command == "showusernames")
            {
                if(gameState == "PLAY")
                {
                    Dictionary<string, string> dict = new Dictionary<string, string>();
                    dict.Add("command", "hackshowuser");
                    dict.Add("amountofusers", usernames.Count.ToString());
                    int i = 1;
                    foreach (KeyValuePair<string, Socket> v in usernames)
                    {
                        IPEndPoint ip = (IPEndPoint)v.Value.RemoteEndPoint;
                        dict.Add("username" + i, v.Key);
                        i++;
                    }
                    string vmbr = JsonConvert.SerializeObject(dict);
                    socket.Send(Encoding.ASCII.GetBytes(vmbr));
                }
                else if (gameState == "GRACE")
                {
                    Dictionary<string, string> dict = new Dictionary<string, string>();
                    dict.Add("command", "message");
                    dict.Add("response", "ERROR: Cannot hack at the momment. Reason: The 1 minute grace period is still on...WHAT ARE YOU EVEN DOING. PROTECT YOUR VM!");
                    socket.Send(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(dict)));
                }
                else if (gameState == "START")
                {
                    Dictionary<string, string> dict = new Dictionary<string, string>();
                    dict.Add("command", "message");
                    dict.Add("response", "ERROR: The game hasn't even started yet.");
                    socket.Send(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(dict)));
                }
                else if (gameState == "END")
                {
                    Dictionary<string, string> dict = new Dictionary<string, string>();
                    dict.Add("command", "message");
                    dict.Add("response", "ERROR: The game is over.");
                    socket.Send(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(dict)));

                }

            }

            if(command == "hackperson")
            {
                if(gameState == "PLAY")
                {
                    Dictionary<string, string> vmbrconvert = new Dictionary<string, string>();
                    vmbrconvert.Add("command", "hackperson");
                    vmbrconvert.Add("response", "You got hacked!");
                    string hackperson = JObject.Parse(text).Value<string>( "persontobehacked");
                    string responsesend = JsonConvert.SerializeObject(vmbrconvert);
                    Socket sockettohack;
                    usernames.TryGetValue(hackperson, out sockettohack);
                    sockettohack.Send(Encoding.ASCII.GetBytes(responsesend));
                    Dictionary<string, string> vmbrconvert2 = new Dictionary<string, string>();
                    vmbrconvert2.Add("command", "hackedperson");
                    vmbrconvert2.Add("username", hackperson);
                    VMAndPass convert = new VMAndPass();
                    Console.WriteLine(vmandpass);
                    IPEndPoint end = (IPEndPoint)sockettohack.RemoteEndPoint;
                    vmandpass.TryGetValue(end.Address, out convert);
                    vmbrconvert2.Add("pass", convert.Pass);
                    vmbrconvert2.Add("ip", convert.Ngrokurl);
                    string responsesend2 = JsonConvert.SerializeObject(vmbrconvert2);
                    socket.Send(Encoding.ASCII.GetBytes(responsesend2));
                } 
                
            }
            
            try
            {
                
            } catch(SocketException)
            {
                Disconnect(socket);
            }
            catch(NullReferenceException)
            {
                socket.Send(Encoding.ASCII.GetBytes("Bro please fuck off"));
                Disconnect(socket);
            }
            try
            {
                socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, RecieveCallBack, socket);
            }
            catch
            {
                Disconnect(socket);
            }
        }

        public static void Disconnect(Socket socket)
        {
            foreach(KeyValuePair<string,Socket> kvp in usernames)
            {
             if(kvp.Value == socket)
                {
                    usernames.Remove(kvp.Key);
                } 
            }
            socket.Disconnect(false);
            _clientSockets.Remove(socket);
        }

        public static void CheckIfDisconnected(Socket socket)
        {
            while(true)
            {
                Thread.Sleep(30000);
                if (!socket.Connected)
                {
                    IPEndPoint end = (IPEndPoint)socket.RemoteEndPoint;
                    try
                    {
                        vmandpass[end.Address].Disconnected = true;
                    } catch
                    {
                        VMAndPass e = new VMAndPass();
                        e.Disconnected = true;
                        vmandpass.Add(end.Address, e);
                    }
                    Thread.Sleep(120000);
                    CheckIfEliminated(socket);
                }
            }
        }

        public static void CheckIfEliminated(Socket socket)
        {
            if(!socket.Connected)
            {
                IPEndPoint end = (IPEndPoint)socket.RemoteEndPoint;
                vmandpass[end.Address].Eliminated = true;
                foreach(KeyValuePair<string,Socket> kvp in usernames)
                {
                    IPEndPoint end2 = (IPEndPoint)kvp.Value.RemoteEndPoint;
                    if(end2.Address == end.Address)
                    {
                        kvp.Value.Send(Encoding.ASCII.GetBytes("RIP! " + kvp.Key + "has been eliminated from VMBR!"));
                    }
                    
                }
            }
        }
    }

    class VMAndPass
    {
        public string Ngrokurl { get; set; }
        public string Pass { get; set; }
        public bool Eliminated { get; set; }
        
        public bool Disconnected { get; set; }
    }
}
