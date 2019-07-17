using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;

namespace UDPClient
{
    class Program
    {
        static void Main(string[] args)
        {

            Socket soc = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint rem = new IPEndPoint(IPAddress.Loopback, 8080);
            string input = "";
            byte[] data;
            do
            {
                input = Console.ReadLine();
                data = Encoding.ASCII.GetBytes(input);
                soc.SendTo(data, rem);
            } while (true);

        }
    }
}
