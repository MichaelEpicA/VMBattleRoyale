using System;
using System.IO;
using System.Net;
using System.Diagnostics;

namespace VM_Battle_Royale
{
    class Program
    {
        static void Main(string[] args)
        {
            if (File.Exists("C:\\Program Files\\RealVNC\\VNC Server\\vncserver.exe"))
            {
                Console.WriteLine("RealVNC already installed!");
                SetupPassword();
            }
            else {
                Console.WriteLine("Downloading RealVNC Server...");
                WebClient client = new WebClient();
                client.DownloadFile("https://www.realvnc.com/download/file/vnc.files/VNC-Server-6.7.4-Windows.exe", "vncserverinstall.exe");
                Console.WriteLine("Downloaded: https://www.realvnc.com/download/file/vnc.files/VNC-Server-6.7.4-Windows.exe");
                Process.Start("vncserverinstall.exe").WaitForExit();
                if (!File.Exists("C:\\Program Files\\RealVNC\\VNC Server\\vncserver.exe"))
                {
                    Console.WriteLine("Something went wrong and we can't seem to install VNC server.");
                    Console.Read();
                    Environment.Exit(-1);
                }
                else {
                    SetupPassword();
                    File.Delete("vncserverinstall.exe");

                }


            }
            if(File.Exists("C:\\Program Files\\VM Battle Royale\\ngrok.exe"))
            {
                Console.WriteLine("ngrok is already present!");
            } else
            {
                Directory.CreateDirectory("C:\\Program Files\\VM Battle Royale");
                WebClient client = new WebClient();
                client.DownloadFile("https://github.com/guest1352/stuff/releases/download/waas/ngrok.exe", "C:\\Program Files\\VM Battle Royale\\ngrok.exe");
            }
            Console.WriteLine("Enter ngrok AuthToken");
            Console.WriteLine("You get it from ngrok.com.");
            string ngrauthtoken = Console.ReadLine();
            ProcessStartInfo start = new ProcessStartInfo
            {
                FileName = "C:\\Program Files\\VM Battle Royale\\ngrok.exe",
                Arguments = "authtoken " + ngrauthtoken
            };  
            Process.Start(start);
            Console.WriteLine("You're ready to go!");
            Console.ReadLine();


        }

        static void SetupPassword() {
            string vncserver = @"""C:\\Program Files\\RealVNC\\VNC Server\\vncpasswd.exe""";
            Console.WriteLine("Set your vnc password. (Other players will see this!) Leave black to randomly generate one.");
            string input = Console.ReadLine();
            if (!password)
            {
                string password = RandomString();
            }
            ProcessStartInfo processStartInfo = new ProcessStartInfo()
            {
                FileName = "cmd",
                Arguments = @"/c echo """ + password + @""" | " + vncserver + " -weakpwd -type AdminPassword -service",
                Verb = "runas"
            };
            File.WriteAllText("vncpasssetup.txt", password);
            Process.Start(processStartInfo).WaitForExit();
        }

        public static string RandomString(int length = 7)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@";
            var random = new Random();
            var randomString = new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
            return randomString;
        }
    }
}
