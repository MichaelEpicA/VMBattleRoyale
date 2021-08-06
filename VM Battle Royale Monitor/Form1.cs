using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VM_Battle_Royale
{
    public partial class Form1 : Form
    {
        private static Socket _keepalive = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        public Form1()
        {
            InitializeComponent();
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length != 1)
            {
                if (args[1] != null && args[1] == "-setupfinished")
                {
                    ReconnectToServer();
                }
            }
            else
            {
                if (!File.Exists("vncpasssetup.txt"))
                {
                    MessageBox.Show("Failed to find password. Please run the VM Setup program before this. Thank you.", "VM Battle Royale Monitor", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(-1);
                }
                SetupMonitor();
            }

        }



        private void ReconnectToServer()
        {
            string newngrokurl = ReRunPrograms();
            MessageBox.Show("This is the ReconnectToServer function.");
            while (!socket.Connected)
            {
                socket.Connect(IPAddress.Parse("107.209.49.185"), 13000);
                _keepalive.Connect(IPAddress.Parse("107.209.49.185"), 13001);
            }
            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict.Add("command", "vm");
            dict.Add("ngrokurl", newngrokurl);
            string vmbrformat = JsonConvert.SerializeObject(dict);
            Thread th = new Thread(KeepAlive);
            IPEndPoint end = (IPEndPoint)socket.LocalEndPoint;
            th.Start(end.Address.ToString());
            socket.Send(Encoding.ASCII.GetBytes(vmbrformat));
            byte[] responsebuffer = { };
            socket.Receive(responsebuffer);
            string response = JObject.Parse(Encoding.ASCII.GetString(responsebuffer)).Value<string>("response");

        }

        private void SetupMonitor()
        {
            string ngrokurl = ReRunPrograms();
            while (!socket.Connected)
            {
                socket.Connect(IPAddress.Parse("107.209.49.185"), 13000);
                _keepalive.Connect(IPAddress.Parse("107.209.49.185"), 13001);
            }
            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict.Add("command", "vm");
            dict.Add("ngrokurl", ngrokurl);
            dict.Add("pass", File.ReadAllText("vncpasssetup.txt"));
            IPEndPoint endPoint = (IPEndPoint)socket.LocalEndPoint;
            dict.Add("ip", endPoint.Address.ToString() + endPoint.Port.ToString());
            socket.Send(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(dict)));
            File.Delete("vncpasssetup.txt");
            RegistryKey key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            RegistryKey startup = key.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            string test = '"' + Environment.CurrentDirectory + @"""VM Battle Royale Monitor.exe""" + @"-setupfinished";
            startup.SetValue("VMBR Monitor", test);
        }

        private string ReRunPrograms()
        {
            Process p = new Process();
            p.StartInfo.FileName = "C:\\Program Files\\VM Battle Royale\\ngrok.exe";
            p.StartInfo.Arguments = "tcp 5900";
            p.StartInfo.CreateNoWindow = false;
            p.Start();
            Thread.Sleep(15000);
            System.Net.WebClient webClient = new System.Net.WebClient();
            byte[] data = webClient.DownloadData("http://127.0.0.1:4040/api/tunnels");
            string webData = Encoding.ASCII.GetString(data);
            JObject obj = JObject.Parse(webData);
            JValue tunnels = obj.SelectToken("tunnels")[0].ToObject<JObject>().Value<JValue>("public_url");
            try
            {
                ServiceController service = new ServiceController("vncserver");
                service.Start();
            }
            catch
            {

            }
            return tunnels.Value.ToString();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (this.Visible == true)
            {
                if (this.WindowState == FormWindowState.Minimized)
                {
                    Hide();
                }
            }
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (this.Visible == true)
            {
                if (this.WindowState == FormWindowState.Minimized)
                {
                    Hide();
                }
            }
        }

        private static void KeepAlive(Object obj)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict.Add("command", "keepalive");
            dict.Add("arg1", (string)obj);
            while (true)
            {
                Thread.Sleep(10000);
                string JSONKeepAlive = JsonConvert.SerializeObject(dict);
                _keepalive.Send(Encoding.Unicode.GetBytes(JSONKeepAlive));
            }

        }
    }
}
