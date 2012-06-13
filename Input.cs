using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using Reality.Config;

namespace Reality
{
    public static class Input
    {
        /// <summary>
        /// Uses invoking thread (usually main thread) as input listener.
        /// </summary>
        public static void Listen()
        {
            while (Program.Alive)
            {
                if (Console.ReadKey(true).Key == ConsoleKey.Enter)
                {
                    Console.Write("$" + Environment.UserName.ToLower() + "@Reality> ");
                    string Input = Console.ReadLine();

                    if (Input.Length > 0)
                    {
                        ProcessInput(Input.Split(' '));
                    }
                }
            }
        }

        /// <summary>
        /// Handles command line input.
        /// </summary>
        /// <param name="Args">Arguments split by space character</param>
        public static void ProcessInput(string[] Args)
        {
            switch (Args[0].ToLower())
            {
                case "delay": // Used to delay startup after a restart to allow current instance to shut down safely
                    {
                        int Delay = 12000;

                        if (Args.Length > 1)
                        {
                            int.TryParse(Args[1], out Delay);
                        }

                        Thread.Sleep(Delay);
                        return;
                    }
                case "restart":

                    if (System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe" == "Reality.exe")
                    {
                        Process.Start(Environment.CurrentDirectory + "\\Reality1.exe", "\"delay 1500\"");
                    }
                    else
                    {
                        Process.Start(Environment.CurrentDirectory + "\\Reality.exe", "\"delay 1500\"");
                    }
                    Program.Stop();
                    return;

                case "crash":

                    Environment.FailFast(string.Empty);
                    return;

                case "stop":

                    Program.Stop();
                    return;

                case "cls":

                    Output.ClearStream();
                    break;

                default:

                    Output.WriteLine("'" + Args[0].ToLower() + "' is not recognized as a command or internal operation.", OutputLevel.Warning);
                    break;
            }
        }
    }
}
