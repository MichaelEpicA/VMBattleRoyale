using System;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Web.Security;

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
            // Console.WriteLine("What would you like your password to be?");
            string input = Membership.GeneratePassword(10,2); //Console.ReadLine();
            ProcessStartInfo processStartInfo = new ProcessStartInfo()
            {
                FileName = "cmd",
                Arguments = @"/c echo """ + input + @""" | " + vncserver + " -weakpwd -type AdminPassword -service",
                Verb = "runas"
            };
            File.WriteAllText("vncpasssetup.txt", input);
            Process.Start(processStartInfo).WaitForExit();
        }
    }
}
