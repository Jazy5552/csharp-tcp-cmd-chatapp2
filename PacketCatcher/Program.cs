using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Net;
using System.Windows.Forms;

namespace PacketCatcher
{
    class Program
    {
        static bool deb = false;
        const int VERSION = 110;
        private static Object logLock = new Object();

        [STAThread]
        static void Main(string[] args)
        {
            if (JustHelpCommand(args)) //If the /h switch is used just give help and exit
                return;

            int udpport = -1;
            int tcpport = -1;
            IPEndPoint autoConnectTcp = null;
            IPEndPoint autoConnectUdp = null;

            #region Commandline switches code and excecution
            for (int i = 0; i < args.Length; i++)
            {
                args[i] = args[i].Replace('-', '/'); //Support for -command or /command
                if (args[i] == "/tp")
                {
                    //Set initial TCP port
                    Int32.TryParse(args[i + 1], out tcpport);
                }
                else if (args[i] == "/up")
                {
                    //Set initial UDP port
                    Int32.TryParse(args[i + 1], out udpport);
                }
                else if (args[i] == "/tc")
                {
                    //TCP auto connect switch
                    try
                    {
                        string ip = args[i + 1].Split(':')[0];
                        int port;
                        IPAddress ipa;
                        port = Int32.Parse(args[i + 1].Split(':')[1]);
                        ipa = GetIPAddress(ip);

                        autoConnectTcp = new IPEndPoint(ipa, port);
                    }
                    catch (Exception e)
                    {
                        debug(e.ToString() + "\n");
                    }
                }
                else if (args[i] == "/uc")
                {
                    //UDP auto connect switch
                    try
                    {
                        string ip = args[i + 1].Split(':')[0];
                        int port;
                        IPAddress ipa;
                        port = Int32.Parse(args[i + 1].Split(':')[1]);
                        ipa = GetIPAddress(ip);

                        autoConnectUdp = new IPEndPoint(ipa, port);
                    }
                    catch (Exception e)
                    {
                        debug(e.ToString() + "\n");
                    }
                }
            }
            #endregion


            if (!RunConsoled(args)) //START GUI
            {
                //TODO make GUI
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
            else
            {
                //Consoled application ( -console switch was supplied )
                //RUN HEADLESS WITH TCP AND UDP PORT
                NativeMethods.AllocConsole(); //Attach console


                ShowHelp(); //Show console commands

                UDPCatcher udpc = new UDPCatcher(udpport);
                TCPCatcher tcpc = new TCPCatcher(tcpport);


                //AutoConnection from commandline udp
                if (autoConnectUdp != null)
                {
                    udpc.remIp = autoConnectUdp.ToString().Split(':')[0];
                    udpc.remPort = Int32.Parse(autoConnectUdp.ToString().Split(':')[1]);
                }
                //AutoConnection from commandline tcp
                if (autoConnectTcp != null)
                {
                    tcpc.ConnectTo(autoConnectTcp);
                }

                string input;
                while (!(input = Console.ReadLine()).StartsWith("/q"))
                {
                    #region Input Listener
                    input = input.Replace('-', '/'); //Support for -command
                    if (input == "debug")
                    {
                        deb = (deb == true) ? false : true; //Toggle debug mode
                        log((deb == true) ? "Debug on.\n" : "Debug off\n");
                    }
                    else if (input.StartsWith("/h"))
                    {
                        ShowHelp();
                    }
                    else if (input.StartsWith("/resolvehost ")) //Get ip address from hostname
                    {
                        input = input.Replace("/resolvehost ", "");
                        try
                        {
                            IPAddress[] ips = Dns.GetHostAddresses(input);
                            foreach (IPAddress ip in ips)
                            {
                                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                                    log(input + " => " + ip.ToString() + "\n");
                            }
                        }
                        catch (Exception e)
                        {
                            debug(e.ToString() + "\n");
                            log("Error resolving hostname:" + e.Message + "\n");
                        }
                    }
                    else if (input.StartsWith("/tcpconnectto "))
                    {
                        input = input.Replace("/tcpconnectto ", "");
                        int port = 0;
                        string ip = input.Split(':')[0];
                        IPAddress ipa;
                        if (input.Contains(':'))
                        {
                            Int32.TryParse(input.Split(':')[1], out port);
                        }
                        else
                        {
                            log("Please enter a port:");
                            Int32.TryParse(Console.ReadLine(), out port);
                        }

                        if (port != 0 && (ipa = GetIPAddress(ip)) != null)
                        {
                            tcpc.ConnectTo(new IPEndPoint(ipa, port));
                        }
                        else
                            log("Invalid ip/port.\n");
                    }
                    else if (input.StartsWith("/tcpinfo"))
                    {
                        log("Selected client #" + tcpc.currentClient + "\nConnected clients:\n" + tcpc.ListClients());
                    }
                    else if (input.StartsWith("/tcpdisconnectall")) //Placed first so /tcpdisconnect doesnt take presidence
                    {
                        tcpc.DisconnectAll();
                    }
                    else if (input.StartsWith("/tcpdisconnect"))
                    {
                        input = input.Replace("/tcpdisconnect", "");
                        if (input.Trim() == "")
                        {
                            tcpc.DisconnectClient(tcpc.currentClient);
                        }
                        else
                        {
                            int cl = tcpc.currentClient;
                            Int32.TryParse(input.Trim(), out cl);
                            tcpc.DisconnectClient(cl);
                        }
                    }
                    else if (input.StartsWith("/tcpsendto ")) //Send to client number
                    {
                        input = input.Replace("/tcpsendto ", "");
                        int cl;
                        if (Int32.TryParse(input.Split(' ')[0], out cl))
                        {
                            tcpc.SendToClient(cl, input.Substring(input.IndexOf(' ') + 1));
                        }
                        else
                            log("Invalid command.\n");
                    }
                    else if (input.StartsWith("/tcpsendtoall ")) //Send to all clients
                    {
                        input = input.Replace("/tcpsendtoall ", "");
                        tcpc.SendToAll(input);
                    }
                    else if (input.StartsWith("/tcpselectclient ")) //Change currenttcpclient
                    {
                        input = input.Replace("/tcpselectclient ", "");
                        int cl;
                        if (Int32.TryParse(input, out cl))
                        {
                            tcpc.currentClient = cl;
                        }
                        else
                            log("Invalud command.\n");
                    }
                    else if (input.StartsWith("/tcprestart"))
                    {
                        int port;
                        tcpc.Disconnect();
                        do
                        {
                            Console.Write("\nEnter tcp port to listen on (0 for random):");
                        } while (!Int32.TryParse(Console.ReadLine(), out port));
                        tcpc = new TCPCatcher(port);
                    }
                    else if (input.StartsWith("/udpsendto "))
                    {
                        input = input.Replace("/udpsendto ", "");
                        try
                        {
                            if (!input.Contains(':'))
                            {
                                Console.Write("\nEnter port number to send to:");
                                Int32.TryParse(Console.ReadLine(), out udpc.remPort); //No error is shown...
                                udpc.remIp = GetIPAddress(input).ToString().Split(':')[0];
                            }
                            else
                            {
                                udpc.remIp = GetIPAddress(input.Split(':')[0]).ToString().Split(':')[0];
                                Int32.TryParse(input.Split(':')[1], out udpc.remPort);
                            }
                            log("Saved. Sending to " + udpc.remIp + ":" + udpc.remPort + "\n");
                        }
                        catch (Exception e)
                        {
                            log("Invalid ip/hostname.\n");
                            debug(e.ToString() + "\n");
                        }
                    }
                    else if (input.StartsWith("/udpdisconnect"))
                    {
                        udpc.Disconnect();
                    }
                    else if (input.StartsWith("/udpsendtolastremote"))
                    {
                        if (udpc.GetLastRemote() != "NULL")
                        {
                            udpc.remIp = udpc.GetLastRemote().Split(':')[0];
                            Int32.TryParse(udpc.GetLastRemote().Split(':')[1], out udpc.remPort);
                            //I didn't feel like placeing a try catch clause here... :D
                        }
                    }
                    else if (input.StartsWith("/udpinfo"))
                    {
                        log("Listening on " + udpc.GetLocalEndpoint() + "\n");
                        log("Sending to " + udpc.remIp + ":" + udpc.remPort + "\n");
                        log("Last remote message was from " + udpc.GetLastRemote() + "\n");
                    }
                    else if (input.StartsWith("/udprestart"))
                    {
                        int port;
                        udpc.Disconnect();
                        do
                        {
                            Console.Write("\nEnter udp port to listen on (0 for random):");
                        } while (Int32.TryParse(Console.ReadLine(), out port));
                        udpc = new UDPCatcher(port);
                    }
                    else if (input.StartsWith("/log"))
                    {
                        input = input.Replace("/log", "");
                        //TODO Log to file!
                    }
                    else if (input.Contains("\\n") || input.Contains("\\r")) //Handle specially... to not auto-escape them.
                    {
                        udpc.Send(input.Replace("\\n", "\n").Replace("\\r", "\r"));
                        tcpc.Send(input.Replace("\\n", "\n").Replace("\\r", "\r"));
                    }
                    else
                    {
                        udpc.Send(input);
                        tcpc.Send(input);
                    }
                    #endregion
                }
                udpc.Disconnect();
                tcpc.DisconnectAll();
            }
        }

        static void ShowHelp()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            log("\nMade by Jazy5552. Open source software, contact at jazy555@hotmail.com.\nVersion " + VERSION + "\nCommands [] = optional :\n" +
                "/help\n" +
                "/quit\n" +
                "/resolvehost (hostname)\n" +
                "/udpinfo\n" +
                "/udpsendto (ipaddress)[:port]\n" +
                "/udpdisconnect\n" +
                "/udprestart\n" +
                "/udpsendtolastremote\n" +
                "/tcpconnectto (ipaddress)[:port]\n" +
                "/tcpinfo\n" +
                "/tcprestart\n" +
                "/tcpselectclient (number)\n" +
                "/tcpsendto (client#) (message)\n" +
                "/tcpsendtoall (message)\n" +
                "/tcpdisconnect [client#]\n" +
                "/tcpdisconnectall\n" +
                "ping\n\n");
            Console.ResetColor();
        }
        public static void log(string msg) //TODO Change to work with GUI
        {
            lock (logLock)
            {
                Console.Write(msg);
            }
        }
        public static void debug(string msg) //TODO Change to work with GUI
        {
            System.Diagnostics.Debug.Write(msg);
            if (deb)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                log(msg);
                Console.ResetColor();
            }
        }

        static IPAddress GetIPAddress(string hostip) //IP helper to get ipaddress from ip or hostname
        {
            try
            {
                IPAddress[] ips = Dns.GetHostAddresses(hostip);
                foreach (IPAddress ip in ips)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        return ip;
                }
            }
            catch (Exception e)
            {
                debug(e.ToString() + "\n");
            }
            return null;
        }

        static bool JustHelpCommand(string[] args)
        {
            //Just show the help for command line and exit
            foreach (string s in args)
            {
                if (s.Replace('-', '/').StartsWith("/h"))
                {
                    //Help switch
                    NativeMethods.AllocConsole(); //Attach console
                    Console.Write("Commandline Commands:\n" +
                        "/console {Run in console mode}\n" +
                        "/tp port\n" +
                        "/up port\n" +
                        "/tc ip:port {Connect to ip:port on startup with tcp}\n" +
                        "/uc ip:port {Connect to ip:port on startup with udp}\n" +
                        "/h\n");
                    //Console.ReadKey(false);
                    return true;
                }
            }
            return false;
        }

        static bool RunConsoled(string[] args)
        {
            //If /console switch is passed return true
            foreach (string s in args)
            {
                if (s.Replace('-', '/').StartsWith("/console")) //Support for the -commanders
                    return true;
            }
            return false;
        }

        internal static void SpecialLog(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            log(msg);
            Console.ResetColor();
        }

        //Class to attach console
        internal static class NativeMethods
        {
            [DllImport("kernel32.dll")]
            internal static extern Boolean AllocConsole();
        }
    }
}
