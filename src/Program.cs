//
// GStage: a modern C# rewrite of the Global Stage IRC server
// src/Program.cs: application entry point and command-line parser
//

using System;
using System.Net;
using System.Threading;

namespace GStage
{
    class Program
    {
        static void Main(string[] args)
        {
            int port = 6667;
            IPAddress bindAddress = IPAddress.Any;
            string serverName = "GStage";
            string networkName = "local";
            string superpass = "god";
            string motdPath = null;

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if ((arg == "-p" || arg == "--port") && i + 1 < args.Length)
                {
                    int p;
                    if (int.TryParse(args[++i], out p)) port = p;
                }
                else if ((arg == "-b" || arg == "--bind") && i + 1 < args.Length)
                {
                    IPAddress addr;
                    if (IPAddress.TryParse(args[++i], out addr)) bindAddress = addr;
                }
                else if ((arg == "-n" || arg == "--name") && i + 1 < args.Length)
                {
                    serverName = args[++i];
                }
                else if (arg == "--net" && i + 1 < args.Length)
                {
                    networkName = args[++i];
                }
                else if (arg == "--pass" && i + 1 < args.Length)
                {
                    superpass = args[++i];
                }
                else if (arg == "--motd" && i + 1 < args.Length)
                {
                    motdPath = args[++i];
                }
                else if (arg == "-h" || arg == "--help" || arg == "/?")
                {
                    PrintHelp();
                    return;
                }
            }

            try
            {
                Console.Title = string.Format("GStage IRC Server [{0}.{1}:{2}]", serverName, networkName, port);
            }
            catch { }

            Server server = new Server(serverName, networkName, port, bindAddress, superpass, motdPath);

            ManualResetEvent exitEvent = new ManualResetEvent(false);
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                server.Stop();
                exitEvent.Set();
            };

            var serverTask = server.StartAsync();

            exitEvent.WaitOne();
        }

        static void PrintHelp()
        {
            Console.WriteLine("GStage IRC Server - Native Windows 11 Edition");
            Console.WriteLine("Emulates Quarterdeck 'Global Stage' IRC Server (SciFi Channel #dominion)");
            Console.WriteLine();
            Console.WriteLine("Usage: GStage.exe [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -p, --port <port>       Port to listen on (default: 6667)");
            Console.WriteLine("  -b, --bind <ip>         IP address to bind to (default: 0.0.0.0)");
            Console.WriteLine("  -n, --name <name>       Server name (default: GStage)");
            Console.WriteLine("      --net <network>     Network name (default: local)");
            Console.WriteLine("      --pass <password>   Supervisor password (default: god)");
            Console.WriteLine("      --motd <file>       Custom MOTD file path");
            Console.WriteLine("  -h, --help              Show this help message");
            Console.WriteLine();
        }
    }
}
