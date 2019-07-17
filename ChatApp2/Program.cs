using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Net.NetworkInformation;
using System.IO;

/* 
 * Commandline startup    appname ([hostname/ip to connect to] (port to connect to default 8000) (server port default 8000)]
 * 
 * () Optional   [] Required
 * /connected
 * /ping (hostname)
 * /connect [hostname]
 * /disconnect
 * /quit
 * /restart
 * /filetext [file path]
 * 
 * MAX OF 254 Characters! to pass more characters use a txt file! (Hard Limit at 250)
 * */

namespace ChatApp2
{
    class Program
    {
        Socket mainSsocket;
        Socket mainCsocket;
        bool isJapp = false;

        public Program(string[] args, bool setPort)
        {
            Console.Title = "Jazy's Chat App";

            MyConsole.colorLine(ConsoleColor.White, "Jazy Chat App. v1.0  Contact: Jazysapplepie@gmail.com\nType /help for a list of commands.\n");
            int port = 8000;
            try
            {
                if (args != null && args.Length > 0)
                {
                    int remotePort = 8000;
                    if (args[0].Contains(':'))
                        remotePort = int.Parse(args[0].Split(':')[1]);
                    else if (args.Length > 1)
                        remotePort = int.Parse(args[1]);

                    if (args.Length > 2)
                        port = int.Parse(args[2]);
                    MyConsole.colorLine(ConsoleColor.White, "Connecting you to {0}:{1}", args[0], args[1]);
                    connectTo(args[0], remotePort);
                }
                else if (setPort)
                {
                    MyConsole.write("Please enter a port number to start listening on: ");
                    port = int.Parse(Console.ReadLine());
                }
                //mainCsocket = null;
                mainSsocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                mainSsocket.Bind(new IPEndPoint(IPAddress.Any, port));
                mainSsocket.Listen(4);

                mainSsocket.BeginAccept(new AsyncCallback(onAccept), null);
                MyConsole.writeLine("Listening for incoming connections on port {0}", port);
            }
            catch (FormatException fe)
            {
                MyConsole.errorLine("Error: {0}... Restarting...", fe.Message);
                Debug.log(fe.ToString());
                Restart();
            }
            catch (Exception e)
            {
                MyConsole.errorLine("Error starting app: {0}\nRestarting...", e.Message);
                Debug.log(e.ToString());
                Restart();
            }

            string input = "";
            do
            {
                input = Console.ReadLine();
                if (input.StartsWith("/ping"))
                {
                    if (input.StartsWith("/ping "))
                    {
                        //They have an address so lets do a proper ping
                        pingAdd(input.Substring(6));
                    }
                    else if (mainCsocket != null && mainCsocket.Connected)
                    {
                        //They are ping the connected person.
                        pingAdd(mainCsocket.RemoteEndPoint.ToString().Split(':')[0]);
                    }
                    else
                    {
                        MyConsole.errorLine("Cannot ping.");
                    }
                }
                else if (input == "/help")
                {
                    MyConsole.colorLine(ConsoleColor.Green,
                        "Commandline startup --> appname ([hostname/ip to connect to] (port to connect to default 8000) (server port default 8000)]\n\n" +
                        "() Optional   [] Required\n" +
                        "/connected -- Returns wether you are currently connected or not.\n" +
                        "/ping (hostname) -- If (hostname) is supplied pings (hostname) else pings remote connection.\n" +
                        "/connect [hostname] -- Attempts to connect to [hostname] or ip address.\n" +
                        "/disconnect -- Closes the current connection.\n" +
                        "/restart -- Restarts the app. (Closes any connections in the process).\n" +
                        "/filetext [file path] -- Used to read text from a file and send it in one big chunk.\n" +
                        "/quit -- Exits the application.\n\n" +
                        "MAX OF 250 Characters! To pass more characters use the /filetext command!"
                        );
                }
                else if (input.StartsWith("/connect "))
                {
                    try
                    {
                        input = input.Replace("/connect ", "");
                        if (mainCsocket != null)
                            mainCsocket.Close();
                        int p = 8000;
                        isJapp = false;
                        if (input.Contains(':'))
                        {
                            p = int.Parse(input.Split(':')[1]);
                            input = input.Substring(0, input.IndexOf(':'));
                        }
                        else if (setPort)
                        {
                            MyConsole.write("Enter port number to connect to: ");
                            p = int.Parse(Console.ReadLine());
                        }

                        connectTo(input, p);
                    }
                    catch (Exception e)
                    {
                        MyConsole.errorLine("Error: {0}", e.Message);
                    }
                }
                else if (input == "/connected")
                {
                    if (mainCsocket != null && mainCsocket.Connected == true)
                        MyConsole.colorLine(ConsoleColor.Cyan, "You are connected to {0}", mainCsocket.RemoteEndPoint.ToString());
                    else
                        MyConsole.colorLine(ConsoleColor.Cyan, "You are not connected.");
                }
                else if (input.StartsWith("/dis"))
                {
                    try
                    {
                        mainCsocket.Shutdown(SocketShutdown.Both);
                    }
                    catch (Exception e)
                    {
                        Debug.log(e.ToString());
                    }
                    finally
                    {
                        mainCsocket = null;
                        MyConsole.colorLine(ConsoleColor.Cyan, "Disconnected");
                    }
                }
                else if (input == "/restart")
                {
                    Restart();
                }
                else if (input.StartsWith("/filetext "))
                {
                    input = input.Substring(10);
                    sendFileText(input);
                }
                else if (input == "/debug")
                {
                    Close();
                    new Program(args, true);
                }
                else if (Encoding.UTF8.GetByteCount(input) > 250)
                {
                    MyConsole.errorLine("You have passed the 250 char limit! Use /filetext to send big chunks of text. Your message was not sent.");
                }
                else
                {
                    sendString(input);
                }
            } while (input != "/quit");
            Close();
        }

        private void onAccept(IAsyncResult iare)
        {
            try
            {
                Socket soc = mainSsocket.EndAccept(iare);
                if (mainCsocket == null || !mainCsocket.Connected)
                {
                    mainCsocket = soc;

                    /*
                    byte[] buf = Encoding.UTF8.GetBytes("Hello World\n");
                    mainWsocket.BeginSend(buf, 0, buf.Length, SocketFlags.None, null, null);
                    mainWsocket.Disconnect(true);
                    */

                    MyConsole.colorLine(ConsoleColor.Cyan, String.Format("{0} connected.", mainCsocket.RemoteEndPoint.ToString()));
                    //sendString("/jazyisawesome");

                    waitForData();
                }
                else
                    MyConsole.colorLine(ConsoleColor.DarkCyan, String.Format("{0} attempted to connect.", soc.RemoteEndPoint.ToString()));
            }
            catch (Exception e)
            {
                Debug.log(e.ToString());
            }
            finally
            {
                try
                {
                    mainSsocket.BeginAccept(new AsyncCallback(onAccept), null);
                }
                catch (Exception ee)
                {
                    Debug.log(ee.ToString());
                }
            }
        }

        private void onConnect(IAsyncResult iare)
        {
            try
            {
                if (mainCsocket.Connected)
                {
                    MyConsole.colorLine(ConsoleColor.Cyan, "Connected Successfully!");
                    isJapp = false;
                    sendString("/jazyisawesome");
                    waitForData();
                }
                else
                    MyConsole.errorLine("Connection failed...");
                mainCsocket.EndConnect(iare);
            }
            catch (Exception e)
            {
                Debug.log(e.ToString());
            }
        }

        private void connectTo(string ip, int port)
        {
            try
            {
                IPAddress ipAddress = null;
                IPAddress[] adds = Dns.GetHostAddresses(ip);
                foreach (IPAddress ipa in adds)
                {
                    if (ipa.AddressFamily == AddressFamily.InterNetwork)
                        ipAddress = ipa;
                }
                mainCsocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                mainCsocket.BeginConnect(new IPEndPoint(ipAddress, port), new AsyncCallback(onConnect), null);
            }
            catch (Exception e)
            {
                MyConsole.errorLine("Error connecting: {0}", e.Message);
                Debug.log(e.ToString());
            }
        }

        private void sendString(string msg)
        {
            Debug.log("Sending String bytes: {0}", Encoding.UTF8.GetByteCount(msg));
            try
            {
                if (mainCsocket == null || mainCsocket.Connected == false)
                {
                    MyConsole.errorLine("Not connected.");
                }
                else if (msg.StartsWith("/"))
                {
                    byte[] buffer = Encoding.UTF8.GetBytes(msg + "\r\n");
                    mainCsocket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, null, null);
                }
                else if (msg.Trim() != "")
                {
                    byte[] buffer;
                    if (isJapp)
                        buffer = Encoding.UTF8.GetBytes("/jia" + msg + "\r\n");
                    else
                        buffer = Encoding.UTF8.GetBytes(msg + "\r\n");
                    mainCsocket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(stringSent), null);
                    //MyConsole.colorLine(ConsoleColor.Cyan, "Sent: " + msg);
                }
            }
            catch (Exception e)
            {
                Debug.log(e.ToString());
                MyConsole.errorLine("Failed to send message...");
                mainCsocket.Close();
                mainCsocket = null;
                isJapp = false;
            }
        }

        private void waitForData()
        {
            Debug.log("waiting for data...");
            try
            {
                if (mainCsocket.Connected)
                {
                    byte[] buffer = new byte[1024];
                    mainCsocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(dataRecieved), buffer);
                }
                else
                    Debug.log("Not connected???");
            }
            catch (Exception e)
            {
                Debug.log(e.ToString());
            }
        }

        private void dataRecieved(IAsyncResult iare)
        {
            Debug.log("Data recieved!");
            try
            {
                int bytesRead = mainCsocket.EndReceive(iare);
                if (bytesRead < 1)
                {
                    //They disconnected
                    MyConsole.errorLine("Connection has been closed by remote host.");
                    try
                    {
                        mainCsocket.Shutdown(SocketShutdown.Both);
                    }
                    catch (Exception e)
                    {
                        Debug.log(e.ToString());
                    }
                    mainCsocket = null;
                    return;
                }

                Debug.log("Bytes Read: " + bytesRead);
                byte[] buffer = (byte[])iare.AsyncState;

                string rec = new string(Encoding.UTF8.GetChars(buffer, 0, bytesRead));
                if (rec == "" || rec.TrimStart(' ') == "")
                {
                    //caca
                }
                else if (rec.StartsWith("/jazyisawesome")) //It's my sig so I know this is my app.
                {
                    if (!isJapp)
                    {
                        isJapp = true;
                        sendString("/jazyisawesome"); //Circlejerk!! :D
                    }
                    Debug.log("My app has connected to me.");
                }
                else if (rec.StartsWith("/jia"))
                {
                    MyConsole.color(ConsoleColor.Green, DateTime.Now.ToString("MMM d, h:m:s-"));
                    MyConsole.write(rec.Substring(4));
                }
                else
                {
                    MyConsole.write(rec);
                    //TODO send it back so they can see and check connectivity. NO
                }
                waitForData();
            }
            catch (SocketException se)
            {
                MyConsole.errorLine("Connection was forcibly closed!");
                //mainCsocket = null;
                Debug.log(se.ToString());
            }
            catch (Exception e)
            {
                Debug.log(e.ToString());
            }
        }

        private void sendFileText(string source)
        {
            try
            {
                string msg = File.ReadAllText(source);
                sendString(msg);
            }
            catch (Exception e)
            {
                Debug.log(e.ToString());
                MyConsole.errorLine("Error sending file's text: {0}", e.Message);
            }
        }

        private void pingAdd(string add)
        {
            Ping ping = new Ping();
            int count = 0;
            ping.PingCompleted += (send, eargs) =>
            {
                if (eargs.Cancelled)
                {
                    MyConsole.errorLine("Ping was cancelled.");
                }
                else if (eargs.Error != null)
                {
                    MyConsole.errorLine("Error: {0}", eargs.Error.Message);
                }
                else if (count < 5)
                {
                    MyConsole.colorLine(ConsoleColor.Yellow, (count + 1) + " | " + eargs.Reply.Address.ToString() + " | " + eargs.Reply.RoundtripTime + " ms");
                    ping.SendAsync(add, null);
                }
                count++;
            };
            try
            {
                ping.SendAsync(add, null);
            }
            catch (Exception e)
            {
                Debug.log(e.ToString());
            }
        }

        private void stringSent(IAsyncResult iare)
        {
            try
            {
                int i = mainCsocket.EndSend(iare);
                Debug.log("String Sent: {0}", i);
            }
            catch (Exception e)
            {
                Debug.log(e.ToString());
            }
        }

        public void Close()
        {
            try
            {
                if (mainSsocket != null && mainSsocket.Connected)
                    mainSsocket.Disconnect(false);
                mainSsocket.Close();
                if (mainCsocket != null && mainCsocket.Connected)
                    mainCsocket.Disconnect(false);
                mainCsocket.Close();
            }
            catch (Exception)
            { }
            mainCsocket = null;
            mainSsocket = null;
            isJapp = false;
        }

        public void Restart()
        {
            Close();
            new Program(null, false);
        }

        static void Main(string[] args)
        {
            /*
            Console.CancelKeyPress += (sender, obj) =>
                {
                    obj.Cancel = false;
                    mre.Set();
                };
             */
            new Program(args, false);
            //mre.WaitOne();
        }
    }

    class MyConsole
    {
        //static ConsoleColor OLD_COLOR = ConsoleColor.Gray;
        public static void write(string msg, params object[] objs)
        {
            try
            {
                msg = String.Format(msg, objs);
            }
            catch (Exception) { }
            Console.ResetColor();
            Console.Write(msg);
            log(msg);
        }

        public static void writeLine(string msg, params object[] objs)
        {
            msg = msg + "\n";
            write(msg, objs);
        }

        public static void error(string msg, params object[] objs)
        {
            color(ConsoleColor.Red, msg, objs);
            log(msg);
        }

        public static void errorLine(string msg, params object[] objs)
        {
            msg = msg + "\n";
            error(msg, objs);
        }

        public static void color(ConsoleColor foreground, string msg, params object[] objs)
        {
            try
            {
                msg = String.Format(msg, objs);
            }
            catch (Exception) { }
            Console.ForegroundColor = foreground;
            Console.Write(msg);
            Console.ResetColor();
        }

        public static void colorLine(ConsoleColor foreground, string msg, params object[] objs)
        {
            msg = msg + "\n";
            color(foreground, msg, objs);
        }

        public static void log(string msg)
        {
            try
            {
                File.AppendAllText(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData) + "/log.txt", msg + "\n", Encoding.UTF8);
            }
            catch (Exception e)
            {
                Debug.log(e.ToString());
            }
        }

    }

    class Debug
    {
        public static void log(string msg, params object[] objs)
        {
            msg = String.Format(msg, objs);
            System.Diagnostics.Debug.WriteLine(msg);
        }
    }
}
