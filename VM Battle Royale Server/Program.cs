using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace VM_Battle_Royale
{
    class Program
    {
        static List<Socket> _clientSockets = new List<Socket>();
        static Dictionary<string, Socket> usernames = new Dictionary<string, Socket>();
        static Socket _serversocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
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
            }
            byte[] tempbuffer = new byte[recieved];
            Array.Copy(_buffer, tempbuffer, recieved);
            string text = Encoding.ASCII.GetString(tempbuffer);
            string command = VMBRFormatHandler.GetValue(text, "command");
            if(command == "dc")
            {
                Disconnect(socket);
            }

            if(command == "username")
            {
                string username = VMBRFormatHandler.GetValue(text, "playername");
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
                            string convertedvmbr = VMBRFormatHandler.CreateVMBRFormat(vmbrconvert);
                            socket.Send(Encoding.ASCII.GetBytes(convertedvmbr));
                        }
                        else if (v == username)
                        {
                            Dictionary<string, string> vmbrconvert = new Dictionary<string, string>();
                            vmbrconvert.Add("command", command);
                            vmbrconvert.Add("response", "Sorry! That username already exists!");
                            string convertedvmbr = VMBRFormatHandler.CreateVMBRFormat(vmbrconvert);
                            socket.Send(Encoding.ASCII.GetBytes(convertedvmbr));
                        }
                        else
                        {
                            usernames.Add(username, socket);
                            Dictionary<string, string> vmbrconvert = new Dictionary<string, string>();
                            vmbrconvert.Add("command", command);
                            vmbrconvert.Add("response", "Username set to " + username + "!");
                            string convertedvmbr = VMBRFormatHandler.CreateVMBRFormat(vmbrconvert);
                            socket.Send(Encoding.ASCII.GetBytes(convertedvmbr));
                        }
                    }
                }
                if(usernames.Count == 0)
                {
                    usernames.Add(username, socket);
                    Dictionary<string, string> vmbrconvert = new Dictionary<string, string>();
                    vmbrconvert.Add("command", command);
                    vmbrconvert.Add("response", "Username set to " + username + "!");
                    string convertedvmbr = VMBRFormatHandler.CreateVMBRFormat(vmbrconvert);
                    socket.Send(Encoding.ASCII.GetBytes(convertedvmbr));
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
                string convertedvmbr = VMBRFormatHandler.CreateVMBRFormat(vmbrconvert);
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
                string vmbr = VMBRFormatHandler.CreateVMBRFormat(dict);
                socket.Send(Encoding.ASCII.GetBytes(vmbr));
            }

            if(command == "hackperson")
            {
                Dictionary<string, string> vmbrconvert = new Dictionary<string, string>();
                vmbrconvert.Add("command", "hackperson");
                vmbrconvert.Add("response", "You got hacked!");
                string hackperson = VMBRFormatHandler.GetValue(text, "persontobehacked");
                string responsesend = VMBRFormatHandler.CreateVMBRFormat(vmbrconvert);
                Socket sockettohack;
                usernames.TryGetValue(hackperson, out sockettohack);
                 sockettohack.Send(Encoding.ASCII.GetBytes(responsesend));
                Dictionary<string, string> vmbrconvert2 = new Dictionary<string, string>();
                vmbrconvert2.Add("command", "hackedperson");
                vmbrconvert2.Add("username", hackperson);
                IPEndPoint ip = (IPEndPoint)sockettohack.RemoteEndPoint;
                vmbrconvert2.Add("ip", ip.Address.ToString());
                string responsesend2 = VMBRFormatHandler.CreateVMBRFormat(vmbrconvert2);
                socket.Send(Encoding.ASCII.GetBytes(responsesend2));
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
    }
}
