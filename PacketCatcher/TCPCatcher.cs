using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace PacketCatcher
{
    class TCPCatcher
    {
        public int currentClient = 0;
        Socket _mainServerS;
        List<Socket> _mainClientsS;
        bool _errormsg = false;
        int MAXCLIENTS = 10; //TODO Make dynamic to user choice

        public TCPCatcher(int port)
        {
            _mainServerS = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                _mainClientsS = new List<Socket>(10);
                _mainServerS.Bind(new IPEndPoint(IPAddress.Any, port));
                _mainServerS.Listen(4);
                log("Listening on " + _mainServerS.LocalEndPoint.ToString());

                _mainServerS.BeginAccept(OnAccept, null);
            }
            catch (Exception e)
            {
                debug(e.ToString());
                if (port != -1) //Exclude error if it was intentional.
                    log("Error openning socket on port " + port + " maybe another socket is already on there.");
                log("Listening service disabled.");
            }
        }

        void OnAccept(IAsyncResult iar)
        {
            try
            {
                Socket newClient = _mainServerS.EndAccept(iar);
                CheckClients();
                if (_mainClientsS.Count + 1 > MAXCLIENTS)
                {
                    Socket oldClient = _mainClientsS.First();
                    int oldClientIndex = _mainClientsS.IndexOf(oldClient);
                    log("Backlog full, shutting down first connection client #" + oldClientIndex);
                    DisconnectClient(oldClientIndex);
                }
                _mainClientsS.Add(newClient);
                debug("Client #" + _mainClientsS.IndexOf(newClient) + "::" + newClient.RemoteEndPoint.ToString() + " connected on " + newClient.LocalEndPoint.ToString());
                log("Client #" + _mainClientsS.IndexOf(newClient) + " : " + newClient.RemoteEndPoint.ToString() + " connected.");

                Packet pac = new Packet();
                pac.buffer = new byte[1024];
                pac.soc = newClient;
                pac.ip = (IPEndPoint)newClient.RemoteEndPoint;
                newClient.BeginReceive(pac.buffer, 0, pac.buffer.Length, SocketFlags.None, OnReceive, pac);

                _mainServerS.BeginAccept(OnAccept, null);
            }
            catch (Exception e)
            {
                //_mainServerS was most likely killed
                debug(e.ToString());
            }
        }

        void OnReceive(IAsyncResult iar)
        {
            Packet pac = (Packet)iar.AsyncState;
            try
            {
                int bytesRead = pac.soc.EndReceive(iar);
                string msg = new String(Encoding.ASCII.GetChars(pac.buffer, 0, bytesRead));
                if (msg != "ping" && msg.Trim() != "")
                {
                    log(pac.ip + " : " + msg);
                }
                else
                {
                    SendToClient(_mainClientsS.IndexOf(pac.soc), "pong");
                }
                debug("Received " + bytesRead + " from " + pac.ip);

                pac.buffer = new byte[1024];
                pac.soc.BeginReceive(pac.buffer, 0, pac.buffer.Length, SocketFlags.None, OnReceive, pac);
            }
            catch (Exception e)
            {
                log("Error receiveing bytes from " + pac.ip + " shutting down connection.");
                debug(e.ToString());
                if (_mainClientsS.Contains(pac.soc) || pac.soc.Connected) //If it hasn't been removed yet (I closed connection)
                    DisconnectClient(_mainClientsS.IndexOf(pac.soc));
            }
        }

        void DisconnectSocket(Socket client)
        {
            try
            {
                debug("Disconnected " + client.RemoteEndPoint.ToString());
            }
            catch (Exception) { }
            client.Close();
        }

        void CheckClients() //Maintenance
        {
            for (int i = _mainClientsS.Count - 1; i > -1; i--)
            {
                try
                {
                    Socket s = _mainClientsS[i];
                    bool pol = s.Poll(200, SelectMode.SelectRead); //Poll the socket to test connection
                    bool ava = (s.Available == 0); //If info is not available will set to true
                    if ((pol && ava) || !s.Connected)
                    {
                        DisconnectClient(i);
                    }
                }
                catch (Exception e)
                {
                    debug(e.ToString());
                    DisconnectClient(i);
                }
            }
        }

        void OnSend(IAsyncResult iar)
        {
            Packet pac = (Packet)iar.AsyncState;
            Socket client = pac.soc;
            try
            {
                string msg = new String(Encoding.ASCII.GetChars(pac.buffer));
                debug("Bytes sent to " + client.RemoteEndPoint.ToString() + " = " + client.EndSend(iar));
                if (msg != "pong")
                    specialLog("Sent!");
                //Success
            }
            catch(Exception e)
            {
                debug(e.ToString());
                log("Error sending packets to " + pac.ip.ToString() + " closing connection.");
                DisconnectClient(_mainClientsS.IndexOf(client));
            }
        }

        void OnConnect(IAsyncResult iar)
        {
            Packet pac = (Packet)iar.AsyncState;
            Socket client = pac.soc;
            try
            {
                client.EndConnect(iar);
                //Connected successfully
                debug("Connected to " + pac.ip.ToString() + " on local " + client.LocalEndPoint.ToString());
                log("Connected to " + pac.ip.ToString());

                CheckClients();
                if (_mainClientsS.Count + 1 > MAXCLIENTS)
                {
                    Socket oldClient = _mainClientsS.First();
                    DisconnectSocket(oldClient);
                }
                _mainClientsS.Add(client);
                //Added successfully

                pac.buffer = new byte[1024];
                client.BeginReceive(pac.buffer, 0, pac.buffer.Length, SocketFlags.None, OnReceive, pac);
            }
            catch (Exception e)
            {
                log("Error connecting to " + pac.ip.ToString() + " :" + e.Message);
                debug(e.ToString());
                DisconnectSocket(client);
            }
        }

        public void Send(string msg) //Send to currentClient
        {
            SendToClient(currentClient, msg);
        }

        public void SendToAll(string msg)
        {
            for (int i = 0; i < _mainClientsS.Count; i++)
            {
                Socket s = _mainClientsS[i];
                SendToClient(_mainClientsS.IndexOf(s), msg);
            }
        }

        public void SendToClient(int indexOf, string msg)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(msg); //Send return new line cariage
            try
            {
                Socket client = _mainClientsS.ElementAt(indexOf);
                Packet pac = new Packet();
                pac.buffer = buffer;
                pac.ip = (IPEndPoint)client.RemoteEndPoint;
                pac.soc = client;
                client.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, OnSend, pac);
                _errormsg = false;
            }
            catch (Exception e)
            {
                if (_mainClientsS.Count < 1)
                {
                    if (!_errormsg)
                    {
                        log("No tcp connections present");
                        _errormsg = true;
                    }
                }
                else
                {
                    log("Error sending to client #" + indexOf);
                }
                debug(e.ToString());
            }
        }

        public void ConnectTo(IPEndPoint ipe)
        {
            Socket newClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Packet pac = new Packet();
            pac.soc = newClient;
            pac.ip = ipe;
            try
            {
                newClient.Bind(new IPEndPoint(IPAddress.Any, 0));
                newClient.BeginConnect(ipe, OnConnect, pac);
            }
            catch (Exception e)
            {
                log("Error connecting: " + e.Message);
                debug(e.ToString());
                DisconnectSocket(newClient);
            }
        }

        public void Disconnect()
        {
            DisconnectAll();
            _mainServerS.Close();
            currentClient = 0;
        }

        public void DisconnectAll()
        {
            for (int i = _mainClientsS.Count-1; i > -1; i--) //Must be backwards because it shifts foward after removing 0
            {
                DisconnectClient(i);
            }
        }

        public void DisconnectClient(int indexOf)
        {
            try
            {
                DisconnectSocket(_mainClientsS.ElementAt(indexOf));
                _mainClientsS.RemoveAt(indexOf);
            }
            catch (Exception e)
            {
                //log(e.Message);
                debug(e.ToString());
            }
        }

        public string ListClients()
        {
            string msg = "";
            CheckClients();
            for (int i = 0; i < _mainClientsS.Count; i++)
            {
                Socket s = _mainClientsS[i];
                msg = msg + "#" + _mainClientsS.IndexOf(s) + " : " + s.RemoteEndPoint.ToString() + "\n";
                debug("#" + _mainClientsS.IndexOf(s) + " : " + s.RemoteEndPoint.ToString() + " on " + s.LocalEndPoint.ToString());
            }
            return msg;
        }

        void specialLog(string msg)
        {
            Program.SpecialLog("TCP:" + msg + "\n");
        }
        void log(string msg)
        {
            Program.log("TCP:" + msg + "\n");
        }
        void debug(string msg)
        {
            Program.debug("TCP:" + msg + "\n");
        }

        class Packet
        {
            public byte[] buffer;
            public Socket soc;
            public IPEndPoint ip;
        }

    }
}
