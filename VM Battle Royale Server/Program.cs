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
    {   //Variables
        static List<Socket> _clientSockets = new List<Socket>();
        static Dictionary<string, Socket> usernames = new Dictionary<string, Socket>();
        static Dictionary<IPAddress, VMAndPass> vmandpass = new Dictionary<IPAddress, VMAndPass>();
        static Socket _serversocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        static Socket _keepalive = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        static Thread thr = new Thread(KeepAliveStart);
        static int maxkeepalivepingsfailed = 10;
        static bool debug = true;
        static IPAddress ban = IPAddress.Parse("37.5.242.87");
        enum GameState { Start, Play, End, Grace };
        static GameState gameState = GameState.Start;
        static byte[] _buffer = new byte[1024];
        static byte[] _keepalivebuffer = new byte[1024];
        static void Main(string[] args)
        {
            thr.Start();
            SetupServer();
        }

        static void SetupServer()
        {
            Console.Write("Setting up VMBR server...");
            _serversocket.Bind(new IPEndPoint(IPAddress.Any, 13000));
            _serversocket.Listen(400);
            _serversocket.BeginAccept(new AsyncCallback(AcceptCallBack), null);
            Console.Write("Done.");
            Console.WriteLine("\nWaiting for a connection...");
            while (true) { }
        }

        private static void AcceptCallBack(IAsyncResult ar)
        {
            //How the server handles when a client connects.
            Socket socket = _serversocket.EndAccept(ar);
            Console.WriteLine("New client connected!");
            Console.WriteLine(_clientSockets.Count);
            _clientSockets.Add(socket);
            IPEndPoint end = (IPEndPoint)socket.RemoteEndPoint;
                /*if(end.Address == ban || end.Address.ToString() == "45.61.186.157")
                {
                Disconnect(socket);
                }*/
            try
            {
                socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(RecieveCallBack), socket);
            }
            catch (Exception e)
            {
                socket.Disconnect(false);
                _clientSockets.Remove(socket);
            }
            _serversocket.BeginAccept(new AsyncCallback(AcceptCallBack), null);

        }

        private static void RecieveCallBack(IAsyncResult ar)
            {
            //How the server recieves things.
            
            int recieved = new int();
            Socket socket = (Socket)ar.AsyncState;
            try
            {
                recieved = socket.EndReceive(ar);
            }
            catch (SocketException)
            {
                Disconnect(socket);
                return;
            }
            byte[] tempbuffer = new byte[recieved];
            Array.Copy(_buffer, tempbuffer, recieved);
            string text = Encoding.UTF8.GetString(tempbuffer);
            //Checks to see if you legit just sent nothing to the server.
            if (text == "")
            {
                Dictionary<string, string> dict = new Dictionary<string, string>();
                dict.Add("command", "message");
                dict.Add("response", "Invalid command.");
                try
                {
                    socket.Send(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(dict)));
                    }
                catch (SocketException)
                {
                    Disconnect(socket);
                    return;
                }
                return;
            }
            string command = "";
            try
            {
                command = JObject.Parse(text)["command"].ToString();
            }
            catch (JsonReaderException)
            {

            }
            if (command == "dc")
            {
                Disconnect(socket);
            }

            if (command == "vm")
            {
                VMAndPass convert;
                //This is sent when the vm monitor starts up.
                if (gameState == GameState.Start)
                {
                    IPEndPoint end = (IPEndPoint)socket.RemoteEndPoint;
                    try
                    {
                         convert = new VMAndPass
                        {
                            //Adding the ngrokurl and pass the client sends us into a class.
                            Ngrokurl = JObject.Parse(text)["ngrokurl"].ToString(),
                            Pass = JObject.Parse(text)["pass"].ToString()
                        };
                    } catch(NullReferenceException)
                    {
                         convert = new VMAndPass
                        {
                            Ngrokurl = "error",
                            Pass = "error"
                        };
                        
                    }
                    
                    //Check if the client disconnected. (please someone make this better holy crap)
                   Task.Run(() => CheckIfDisconnected(socket));
                    try
                    {
                        if(convert.Ngrokurl != "error")
                        {
                            vmandpass.Add(end.Address, convert);
                        } else
                        {
                            Disconnect(socket);
                        }
                        
                    }
                    catch
                    {

                    }
                }
                else if (gameState == GameState.Play)
                {
                    VMAndPass check = new VMAndPass();
                    IPEndPoint end = (IPEndPoint)socket.LocalEndPoint;
                    if (vmandpass.TryGetValue(end.Address, out check))
                    {
                        if (check.Disconnected == true)
                        {
                            //Check to see if the vm already existed at one point. If it did, allow it to reconnect.
                            vmandpass[end.Address].Disconnected = false;
                            vmandpass[end.Address].Ngrokurl = JObject.Parse(text)["ngrokurl"].ToString();
                        }
                        else if (check.Eliminated == true)
                        {
                            //If the vm failed to reconnect with in the time limit instead they will be eliminated.
                            try
                            {
                                socket.Send(Encoding.UTF8.GetBytes("ERROR: You've been eliminated from this game. Please wait until the game ends to rejoin!"));
                            }
                            catch
                            {

                            }
                            socket.Disconnect(false);
                            _clientSockets.Remove(socket);
                        }
                    }
                    else
                    {
                        //If the vm never existed in the first place.
                        try
                        {
                            socket.Send(Encoding.UTF8.GetBytes("ERROR: Sorry, the game has already started. You can't join right now. Wait for the end of the game!"));
                        }
                        catch
                        {

                        }
                        socket.Disconnect(false);
                        _clientSockets.Remove(socket);
                    }
                }
            }
            
            if (command == "username")
            {
                //Very basic username system, works by just taking the playername from the json and trying to add it to usernames.
                string username = JObject.Parse(text)["playername"].ToString();
                IPEndPoint ip = (IPEndPoint)socket.RemoteEndPoint;
                if (usernames.Count == 0)
                {
                    usernames.Add(username, socket);
                    Dictionary<string, string> vmbrconvert = new Dictionary<string, string>();
                    vmbrconvert.Add("command", command);
                    vmbrconvert.Add("response", "Username set to " + username + "!");
                    string convertedvmbr = JsonConvert.SerializeObject(vmbrconvert);
                    try
                    {
                        socket.Send(Encoding.UTF8.GetBytes(convertedvmbr));
                    }
                    catch
                    {
                        Disconnect(socket);
                    }

                }
                else
                {
                    if (usernames.TryGetValue(username, out _))
                    {
                        IPEndPoint tableip = (IPEndPoint)usernames[username].RemoteEndPoint;
                        if (tableip.Address == ip.Address)
                        {
                            usernames.Remove(username);
                            usernames.Add(username, socket);
                            Dictionary<string, string> vmbrconvert = new Dictionary<string, string>();
                            string response = "Username set to " + username + "!";
                            vmbrconvert.Add("command", command);
                            vmbrconvert.Add("response", response);
                            string convertedvmbr = JsonConvert.SerializeObject(vmbrconvert);
                            try
                            {
                                socket.Send(Encoding.UTF8.GetBytes(convertedvmbr));
                            }
                            catch
                            {
                                Disconnect(socket);
                            }
                        }
                        else if (usernames.ContainsKey(username))
                        {
                            //Username exists
                            Dictionary<string, string> vmbrconvert = new Dictionary<string, string>();
                            vmbrconvert.Add("command", command);
                            vmbrconvert.Add("response", "Sorry! That username already exists!");
                            string convertedvmbr = JsonConvert.SerializeObject(vmbrconvert);
                            try
                            {
                                socket.Send(Encoding.UTF8.GetBytes(convertedvmbr));
                            }
                            catch
                            {
                                Disconnect(socket);
                            }
                        }
                        if (username.Length > 16)
                        {
                            //Username is too long
                            Dictionary<string, string> vmbrconvert = new Dictionary<string, string>();
                            vmbrconvert.Add("command", command);
                            vmbrconvert.Add("response", "Sorry! That username is too long.");
                            string convertedvmbr = JsonConvert.SerializeObject(vmbrconvert);
                            try
                            {
                                socket.Send(Encoding.UTF8.GetBytes(convertedvmbr));
                            }
                            catch
                            {
                                Disconnect(socket);
                            }
                        }
                    }
                    else
                    {
                        //If no hosts connected to the server and set their username, this condition will occur.
                        usernames.Add(username, socket);
                        Dictionary<string, string> vmbrconvert = new Dictionary<string, string>();
                        vmbrconvert.Add("command", command);
                        vmbrconvert.Add("response", "Username set to " + username + "!");
                        string convertedvmbr = JsonConvert.SerializeObject(vmbrconvert);
                        try
                        {
                            socket.Send(Encoding.UTF8.GetBytes(convertedvmbr));
                        }
                        catch
                        {
                            Disconnect(socket);
                        }
                    }
                }
            }






            if (command == "startgame")
            {
                //Starts the game, pretty obvious.
                if(debug)
                {
                    Console.WriteLine("Debug mode enabled. Starting without checking conditions.");
                    gameState = GameState.Grace;
                    foreach (KeyValuePair<string, Socket> kvp in usernames)
                    {
                        Dictionary<string, string> dict = new Dictionary<string, string>();
                        dict.Add("command", "gamestatechange");
                        dict.Add("gamestate", "GRACE");
                        socket.Send(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(dict)));
                    }
                    Thread.Sleep(60000);
                    gameState = GameState.Play;
                    foreach (KeyValuePair<string, Socket> kvp in usernames)
                    {
                        Dictionary<string, string> dict = new Dictionary<string, string>();
                        dict.Add("command", "gamestatechange");
                        dict.Add("gamestate", "PLAY");
                        socket.Send(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(dict)));
                    }
                }
                else if (usernames.Count == vmandpass.Count && usernames.Count >= 2 && gameState == GameState.Start && usernames.ElementAt(0).Value == socket)
                {
                    gameState = GameState.Grace;
                    foreach (KeyValuePair<string, Socket> kvp in usernames)
                    {
                        Dictionary<string, string> dict = new Dictionary<string, string>();
                        dict.Add("command", "gamestatechange");
                        dict.Add("gamestate", "GRACE");
                        socket.Send(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(dict)));
                    }
                    Thread.Sleep(60000);
                    gameState = GameState.Play;
                    foreach (KeyValuePair<string, Socket> kvp in usernames)
                    {
                        Dictionary<string, string> dict = new Dictionary<string, string>();
                        dict.Add("command", "gamestatechange");
                        dict.Add("gamestate", "PLAY");
                        socket.Send(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(dict)));
                    }
                }
                else
                {
                    //Handles different errors for when the conditions aren't met.
                    Dictionary<string, string> dict = new Dictionary<string, string>();
                    dict.Add("command", "message");
                    if (usernames.Count < 2)
                    {
                        dict.Add("response", "Failed to start the game. Reason: There isn't enough players to start the game.");
                    }
                    else if (usernames.Count != vmandpass.Count)
                    {
                        //bool found = false;
                        dict.Add("response", "Failed to start the game. Reason: There aren't the same amount of VMS connected as interfaces. \n This means that some vm(s) or interface(s) have not been connected to the server. ");
                        foreach (KeyValuePair<string, Socket> kvp in usernames)
                        {
                            IPEndPoint end = (IPEndPoint)kvp.Value.RemoteEndPoint;
                            if (!vmandpass.TryGetValue(end.Address, out _))
                            {
                                dict["response"] = dict["response"] + "\n" + kvp.Key + "'s VM is not connected.";
                            }
                        }
                        /*
                        foreach(KeyValuePair<IPAddress,VMAndPass> kvp2 in vmandpass)
                        {
                            foreach (KeyValuePair<string, Socket> kvp in usernames)
                            {
                                IPEndPoint end = (IPEndPoint)kvp.Value.RemoteEndPoint;
                                if(kvp2.Key == end.Address)
                                {
                                    found = true; 
                                }
                            }
                            if(!found)
                            {

                            }
                        }*/
                    }
                    else if (gameState != GameState.Start)
                    {
                        dict.Add("response", "Failed to start the game. Reason: The game has either already started or ended.");
                    }
                    else if (usernames.ElementAt(0).Value != socket)
                    {
                        dict.Add("response", "Failed to start the game. Reason: You are not the host of this game!");
                    }

                    socket.Send(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(dict)));
                }
            }

            if (command == "showips")
            {
                //What the server does to show the HOST ips. Strictly for debugging.
                Dictionary<string, string> vmbrconvert = new Dictionary<string, string>();
                vmbrconvert.Add("command", command);
                int i = 1;
                foreach (KeyValuePair<string, Socket> v in usernames)
                {

                    IPEndPoint tableip = (IPEndPoint)v.Value.RemoteEndPoint;
                    vmbrconvert.Add("ip" + i, tableip + " ");
                    i++;
                }
                vmbrconvert.Add("amountofips", (vmbrconvert.Count - 1).ToString());
                string convertedvmbr = JsonConvert.SerializeObject(vmbrconvert);
                try
                {
                    socket.Send(Encoding.UTF8.GetBytes(convertedvmbr));
                }
                catch (SocketException)
                {
                    Disconnect(socket);
                }
            }

            if (command == "showusernames")
            {
                //We use this to show the users that are hackable and then send it to the host.
                if (gameState == GameState.Play)
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
                    socket.Send(Encoding.UTF8.GetBytes(vmbr));
                }
                //If the grace period is up, then we call this.
                else if (gameState == GameState.Grace)
                {
                    Dictionary<string, string> dict = new Dictionary<string, string>();
                    dict.Add("command", "message");
                    dict.Add("response", "ERROR: Cannot hack at the momment. Reason: The 1 minute grace period is still on...WHAT ARE YOU EVEN DOING. PROTECT YOUR VM!");
                    socket.Send(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(dict)));
                }
                //Game hasn't started yet, this is just waiting for it to start.
                else if (gameState == GameState.Start)
                {
                    Dictionary<string, string> dict = new Dictionary<string, string>();
                    dict.Add("command", "message");
                    dict.Add("response", "ERROR: The game hasn't even started yet.");
                    socket.Send(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(dict)));
                }
                //Game ended.
                else if (gameState == GameState.End)
                {
                    Dictionary<string, string> dict = new Dictionary<string, string>();
                    dict.Add("command", "message");
                    dict.Add("response", "ERROR: The game is over.");
                    socket.Send(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(dict)));

                }

            }

            if (command == "hackperson")
            {
                //This occurs when the user successfully hacks a person.
                if (gameState == GameState.Play)
                {
                    //Creating a message for the user that got hacked.
                    Dictionary<string, string> vmbrconvert = new Dictionary<string, string>();
                    vmbrconvert.Add("command", "hackperson");
                    vmbrconvert.Add("response", "You got hacked!");
                    string hackperson = JObject.Parse(text).Value<string>("persontobehacked");
                    string responsesend = JsonConvert.SerializeObject(vmbrconvert);
                    Socket sockettohack;
                    usernames.TryGetValue(hackperson, out sockettohack);
                    //Sends the message.
                    sockettohack.Send(Encoding.UTF8.GetBytes(responsesend));
                    //The rest of this just sends the important info used for hacking the user back to the client.
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
                    socket.Send(Encoding.UTF8.GetBytes(responsesend2));
                }

            }
            try
            {

            }
            catch (SocketException)
            {
                Disconnect(socket);
            }
            catch (NullReferenceException)
            {
                //Crash protection, VERY OLD code.
                socket.Send(Encoding.UTF8.GetBytes("Bro please fuck off"));
                Disconnect(socket);
            }
            try
            {
                //Try to start receiving from the client.
                socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, RecieveCallBack, socket);
            }
            catch(Exception e)
            {
                Disconnect(socket);
            }
        }
        //Disconnect function, is used a lot, if you wanna change what the server does when a client disconnects, use this.
        public static void Disconnect(Socket socket)
        {
            bool error = false;
            foreach (KeyValuePair<string, Socket> kvp in usernames)
            {
                if (kvp.Value == socket)
                {
                    usernames.Remove(kvp.Key);
                }
            }
            try
            {
                IPEndPoint end = (IPEndPoint)socket.RemoteEndPoint;
                vmandpass[end.Address].Disconnected = true;
                error = false;
            } catch
            {
                error = true;
            }
            if(error)
            {
                socket.Disconnect(false);
                _clientSockets.Remove(socket);
            } else
            {
                IPEndPoint end = (IPEndPoint)socket.RemoteEndPoint;
                vmandpass.Remove(end.Address);
                socket.Disconnect(false);
                _clientSockets.Remove(socket);
            }
            
        }

        public static void CheckIfDisconnected(Socket socket)
        {
            //Pretty bad way of checking if the user is disconnected. IDK if this works.
            while (true)
            {
                Thread.Sleep(30000);
                if (!socket.Connected)
                {
                    if(gameState == GameState.Start)
                    {
                        Disconnect(socket);
                        return;
                    } else
                    {
                        IPEndPoint end = (IPEndPoint)socket.RemoteEndPoint;
                        try
                        {
                            vmandpass[end.Address].Disconnected = true;
                        }
                        catch
                        {
                            VMAndPass e = new VMAndPass();
                            e.Disconnected = true;
                            vmandpass.Add(end.Address, e);
                        }
                        //2 minutes is the timeout, and then it checks if they were eliminated.
                        Thread.Sleep(120000);
                        CheckIfEliminated(socket);
                    }
                   
                }
            
            }
        }

        public static void CheckIfEliminated(Socket socket)
        {
            //When this runs the disconnected timer has ran out, if it reconnected before this function ran, it is not eliminated.
            if (!socket.Connected)
            {
                IPEndPoint end = (IPEndPoint)socket.RemoteEndPoint;
                vmandpass[end.Address].Eliminated = true;
                foreach (KeyValuePair<string, Socket> kvp in usernames)
                {
                    IPEndPoint end2 = (IPEndPoint)kvp.Value.RemoteEndPoint;
                    if (end2.Address == end.Address)
                    {
                        kvp.Value.Send(Encoding.UTF8.GetBytes("RIP! " + kvp.Key + "has been eliminated from VMBR!"));
                        //Win condition for when the player destroys all other vms.
                        foreach (KeyValuePair<IPAddress, VMAndPass> kvp2 in vmandpass)
                        {
                            if (!kvp2.Value.Eliminated)
                            {
                                foreach (KeyValuePair<string, Socket> kvp3 in usernames)
                                {
                                    IPEndPoint end3 = (IPEndPoint)socket.RemoteEndPoint;
                                    if (end3.Address == kvp2.Key)
                                    {
                                        Dictionary<string, string> dict = new Dictionary<string, string>();
                                        dict.Add("command", "message");
                                        dict.Add("response", "You have won VMBR! Congratulations!");
                                        kvp3.Value.Send(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(dict)));
                                    }
                                }
                            }

                        }
                    }

                }
            }
        }

        //Handles receiving keep alive pings.
        public static void KeepAlive(IAsyncResult ar)
        {
                int recieved = new int();
                Socket socket = (Socket)ar.AsyncState;
                try
                {
                    recieved = socket.EndReceive(ar);
                }
                catch (SocketException)
                {
                    Disconnect(socket);
                    return;
                }
                catch(NullReferenceException)
                {
                    Disconnect(socket);
                    return;
                }
                byte[] tempbuffer = new byte[recieved];
                Array.Copy(_buffer, tempbuffer, recieved);
                string text = Encoding.UTF8.GetString(tempbuffer);
                if (text == "")
                {
                    Dictionary<string, string> dict = new Dictionary<string, string>();
                    dict.Add("command", "message");
                    dict.Add("response", "Invalid command.");
                    try
                    {
                        socket.Send(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(dict)));
                    }
                    catch (SocketException)
                    {
                        Disconnect(socket);
                        return;
                    }
                    return;
                }
                string command = "";
                try
                {
                    command = JObject.Parse(text)["command"].ToString();
                }
                catch (JsonReaderException)
                {

                }
                if (command == "dc")
                {
                    Disconnect(socket);
                    return;
                }
                else if (command == "keepalive")
                {
                    string arg1 = JObject.Parse(text)["arg1"].ToString();
                    IPAddress ip;
                    if(IPAddress.TryParse(arg1, out ip))
                    {
                        vmandpass[ip].KeepAliveMet = true;
                        vmandpass[ip].Disconnected = false;
                    }
                }
                
            }
            


        

        //Handles clients connecting to the keepalive.
        public static void KeepAliveAccept(IAsyncResult ar)
        {
            Socket socket = _keepalive.EndAccept(ar);
            try
            {
                socket.BeginReceive(_keepalivebuffer, 0, _keepalivebuffer.Length, SocketFlags.None, new AsyncCallback(KeepAlive), socket);
            } catch(SocketException)
            {
                Disconnect(socket);
                return;
            }

        }

        //Actual check for KeepAlive.
        public static void KeepAliveCheck()
        {
            while(true)
            {
                Thread.Sleep(21000);

               foreach(KeyValuePair<IPAddress,VMAndPass> kvp in vmandpass)
                {
                    if(kvp.Value.KeepAliveMet == false && kvp.Value.KeepAlivePingsFailed != maxkeepalivepingsfailed)
                    {
                        kvp.Value.KeepAlivePingsFailed += 1;
                        kvp.Value.Disconnected = true;
                    } else if(kvp.Value.KeepAliveMet == true)
                    {
                        kvp.Value.KeepAlivePingsFailed = 0;
                        kvp.Value.KeepAliveMet = false;
                    } else if(kvp.Value.KeepAlivePingsFailed >= maxkeepalivepingsfailed)
                    {
                        kvp.Value.Eliminated = true;
                        kvp.Value.Disconnected = false;
                        kvp.Value.KeepAliveMet = false;
                    }
                }
            }
        }

        //This is launched on a new thread, gets the KeepAlive and KeepAliveAccept and KeepAliveCheck functions ready.
        public static void KeepAliveStart()
        {
            _keepalive.Bind(new IPEndPoint(IPAddress.Any, 13001));
            _keepalive.Listen(400);
            _keepalive.BeginAccept(new AsyncCallback(KeepAliveAccept), null);
            KeepAliveCheck();
        }
    }

    class VMAndPass
    {
        //Class that I wrote, all it does is keeps values for ngrok urls, and passes and other stuff. Pretty helpful though.
        public string Ngrokurl { get; set; }
        public string Pass { get; set; }
        public bool Eliminated { get; set; }

        public bool Disconnected { get; set; }

        public bool KeepAliveMet { get; set; }

        public int KeepAlivePingsFailed { get; set; }
    }
}

