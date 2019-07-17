using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace TotalListener
{
    class Program
    {
        static void Main(string[] args)
        {
            for (int i = 27005; i < 27006; i++)
            {
                Thread t = new Thread((obj) =>
                    {
                        int num = (int)obj;
                        Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                        try
                        {
                            EndPoint ip = new IPEndPoint(IPAddress.Any, 0);
                            s.Bind(new IPEndPoint(IPAddress.Any, num));
                            //s.Listen(1);
                            //Socket ss = s.Accept();
                            //s.Close();
                            byte[] buffer = new byte[1024];
                            int bytesread = 0;
                            while ((bytesread = s.ReceiveFrom(buffer, ref ip)) != 0)
                            {
                                string msg = new string(Encoding.UTF8.GetChars(buffer, 0, bytesread));
                                Console.WriteLine(s.RemoteEndPoint.ToString() + ":" + msg);
                            }
                        }
                        catch (Exception e)
                        {
                            System.Diagnostics.Debug.WriteLine(e.ToString());
                            Console.WriteLine("Port {0} failed:{1}", num, e.Message);
                        }
                        s.Close();
                    });
                t.Start(i);
                if (i%100 == 0)
                    System.Diagnostics.Debug.WriteLine("Number {0}",i);
            }
            Console.ReadKey();
        }
    }
}
