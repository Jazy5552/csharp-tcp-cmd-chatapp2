using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace PacketCatcher
{
    class UDPCatcher
    {
        Socket _mainS;
        public string remIp = "";
        public int remPort = 0;
        EndPoint _lastRemote = null;
        bool _errormsg = false;

        public UDPCatcher(int port)
        {
            _mainS = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            try
            {
                EndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                _mainS.Bind(new IPEndPoint(IPAddress.Any, port));
                byte[] buffer = new byte[1024];
                _mainS.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref remote, onReceive, buffer);
                log("Listening on " + _mainS.LocalEndPoint.ToString());
            }
            catch (Exception e)
            {
                log("Error opening udp socket on port " + port);
                debug(e.ToString());
            }

        }

        public void Send(string msg)
        {
            SendTo(msg, remIp, remPort);
        }

        public void SendTo(string msg, string ipadd, int port)
        {
            IPAddress remIP;
            EndPoint rem;
            byte[] buffer = Encoding.ASCII.GetBytes(msg);
            if (!IPAddress.TryParse(ipadd, out remIP))
            {
                debug("Using last remote.");
                if (_lastRemote == null)
                {
                    debug("No last remote.");
                    if (!_errormsg)
                    {
                        log("Error, no ip address supplied or no one has made connection.");
                        _errormsg = true;
                    }
                    return;
                }
                else
                {
                    rem = _lastRemote;
                }
            }
            else
            {
                rem = new IPEndPoint(remIP, port);
            }

            try
            {
                _mainS.BeginSendTo(buffer, 0, buffer.Length, SocketFlags.None, rem, onSend, rem);
                _errormsg = false;
            }
            catch (Exception e)
            {
                log("Error sending msg to "+rem.ToString());
                debug(e.ToString());
                remIp = "";
                remPort = 0;
            }
        }

        void onSend(IAsyncResult ar)
        {
            IPEndPoint rem = (IPEndPoint)ar.AsyncState;
            int bytessent=0;
            try
            {
                bytessent = _mainS.EndSendTo(ar);
            }catch (Exception e)
            {
                log("Error sending msg to "+rem.ToString());
                debug(e.ToString());
            }
            debug("Sent " + bytessent + " bytes to " + rem.ToString());
        }

        void onReceive(IAsyncResult ar)
        {
            try
            {
                EndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                byte[] buffer = (byte[])ar.AsyncState;
                int bytesread = _mainS.EndReceiveFrom(ar, ref remote);
                debug("Received " + bytesread + " bytes.");
                String msg = Encoding.ASCII.GetString(buffer, 0, bytesread);
                log(msg);
                debug("From: " + remote.ToString());
                _lastRemote = remote;
                if (msg == "ping" || msg.Trim() == "")
                    SendTo("pong", remote.ToString().Split(':')[0], int.Parse(remote.ToString().Split(':')[1]));
                _mainS.BeginReceiveFrom(buffer, 0, 1024, SocketFlags.None, ref remote, onReceive, buffer);
            }
            catch (Exception e)
            {
                log("Error getting packets. Remote has disconnected.");
                debug(e.ToString());
                _lastRemote = null;
            }
        }

        public string GetLocalEndpoint()
        {
            return (_mainS==null) ? "NULL" : _mainS.LocalEndPoint.ToString();
        }
        public string GetLastRemote()
        {
            return (_lastRemote == null) ? "NULL" : _lastRemote.ToString();
        }
        public void Disconnect()
        {
            remPort = 0;
            remIp = "";
            _lastRemote = null;
        }

        void log(string msg)
        {
            Program.log("UDP:" + msg + "\n");
        }
        void debug(string msg)
        {
            Program.debug("UDP:" + msg + "\n");
        }
    }
}
