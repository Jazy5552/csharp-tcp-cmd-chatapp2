using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace UDPServer
{
    class Program
    {
        static void Main(string[] args)
        {
            int recv;
            byte[] data = new byte[1024];
            Socket soc = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint local = new IPEndPoint(IPAddress.Any, 8001);
            EndPoint rePoint = (EndPoint)new IPEndPoint(IPAddress.Any, 0);
            soc.Bind((EndPoint)local);

            Console.WriteLine("Listening on port 8001.");

            while ((recv = soc.ReceiveFrom(data,ref rePoint)) > 0)
            {
                Console.WriteLine("Message from {0} with {2} bytes.\n{1}", rePoint.ToString(), Encoding.ASCII.GetString(data, 0, recv), recv);

            }

        }
    }
}
