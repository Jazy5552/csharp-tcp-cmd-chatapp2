using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Threading;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

namespace FTPClient
{
    class FileFTPClient
    {
        private static double version = 1.32;
        private static bool over = false;
        private const string SITEFILENAME = "ftp://dbprftp.state.fl.us/pub/llweb/file_download/ABT_DEL_CSH.pdf";
        private const string EMAIL = "osuarez@reyesholdings.com";
        public static bool deamon = false;

        //Used to hide the window
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                foreach (string ar in args)
                {
                    System.Diagnostics.Debug.WriteLine(ar);
                    if (ar.StartsWith("-h"))
                    {
                        DisplayHelp();
                        return;
                    }
                    else if (ar.StartsWith("-n"))
                    {
                        over = true;
                        DoShit(new Uri(SITEFILENAME));
                        return;
                    }
                    else if (ar.StartsWith("-d"))
                    {
                        deamon = true;
                    }
                }
            }
            if (!deamon)
                Console.WriteLine("Program made by Jazy that downloads a file from a ftp server." +
                "Made for Mom. \nHit q to quit, n to download now, and h to hide console window (CANNOT BE RECOVERED).\nV {0:F2}\nCommandline commands: -h -n -d", version);
            else
                Console.WriteLine("V {0:F2}\n", version);

            DateTime now = DateTime.Now;
            DateTime sevenAM = DateTime.Today.AddHours(7);  
            if (now > sevenAM)
                sevenAM = sevenAM.AddDays(1);

            Timer autoEvent = new Timer(
                DoShit,
                new Uri(SITEFILENAME),
                sevenAM.Subtract(DateTime.Now),
                TimeSpan.FromDays(1));
            //autoEvent.Change(sevenAM.Subtract(DateTime.Now),TimeSpan.FromDays(1)); //Run at 7:00am

            Console.WriteLine("Started at " + DateTime.Now.ToString() + "\nNext run in " + sevenAM.Subtract(DateTime.Now).ToString() + "\n");
            //Debug

            char inp;
            bool debug = false;
            do
            {
                inp = char.ToLower(Console.ReadKey(!debug).KeyChar);
                if (inp == 'n') //Run now!
                {
                    over = true;
                    Task.Factory.StartNew(() => { DoShit(new Uri(SITEFILENAME)); });
                }
                else if (inp == 'j') //Enable debug info
                {
                    debug = !debug;
                    Console.WriteLine("Debug: " + debug);
                }
                else if (inp == 'h') //Detach console and run as deamon
                {
                    deamon = true;
                    var handle = GetConsoleWindow();
                    ShowWindow(handle, SW_HIDE);
                }
                else if (inp == 0 || deamon) //NULL Character
                {
                    //Just keep it alive and tranquile.
                    deamon = true;
                    System.Diagnostics.Debug.WriteLine("Detected NULL input, maintaining alive but tranquile (NO FURTHER INPUT ACCEPTED)");
                    ManualResetEvent mre = new ManualResetEvent(false);
                    mre.WaitOne();
                }
            } while (inp != 'q');
            //Task.Factory.StartNew(() => { DoShit(new Uri(SITEFILENAME)); });

            //Console.WriteLine("Hit q to exit at anytime...\n");
            //ManualResetEvent mre = new ManualResetEvent(false);
            //mre.WaitOne();
        }

        //This is bull async
        public static void DoShit(object obj)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Starting process at {0}",DateTime.Now.ToString());
            Console.ResetColor();
            if ((DateTime.Now.DayOfWeek == DayOfWeek.Sunday || DateTime.Now.DayOfWeek == DayOfWeek.Monday) && !over)
            {
                Console.WriteLine("Cancelled, it's " + DateTime.Now.DayOfWeek.ToString());
                return;
            }
            over = false;

            Uri fileUri = (Uri)obj;
            if (!deamon) Console.WriteLine("Downloading data...");
            string re = DownloadFileFromServer(fileUri);
            int i = 3;
            while (re == null && i>0)
            {
                Console.WriteLine("Download failed... Retrying ({0})", i);
                Thread.Sleep(3000);
                re = DownloadFileFromServer(fileUri);
                i--;
            }
            if (re == null)
            {
                Console.WriteLine("Could not send email. Download failed...");
                return;
            }
            if (!deamon) Console.WriteLine("Download complete!");
            if (!deamon) Console.WriteLine("Sending email to {0}", EMAIL);
            if (SendEmail(EMAIL, re))
            {
                if (!deamon) Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Mail sent!\n");
                if (!deamon) Console.ResetColor();
            }
            else
            {
                Console.WriteLine("Sending mail failed!");
            }
        }

        public static bool SendEmail(string dest, string attachmentPath)
        {
            MailMessage mail = new MailMessage();
            SmtpClient smtpServer = new SmtpClient("smtp.gmail.com");
            try
            {
                mail.From = new MailAddress("jazysapplepie@gmail.com");
                mail.To.Add(dest);
                mail.Subject = "Release Report from Jazy " + DateTime.Now.ToString("M_d_yy");
                mail.Body = "The report is in the attachment as a .pdf\nThis email was sent from Jazy's Program!\n\n"+
                    "Sent at: " + DateTime.Now.ToString();

                Attachment attachment = new Attachment(attachmentPath);
                mail.Attachments.Add(attachment);

                smtpServer.Credentials = new NetworkCredential("jazysapplepie", Encrypter.Encrypter.Decrypt("#:SRBL:P>LHF>",55));
                smtpServer.Port = 587;
                smtpServer.EnableSsl = true;
                ServicePointManager.ServerCertificateValidationCallback =
                delegate(object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
                { return true; }; //Don't check certificate
                smtpServer.Send(mail);

                File.Delete(attachmentPath);

                smtpServer.Dispose();
                mail.Dispose();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                smtpServer.Dispose();
                mail.Dispose();
                return false;
            }
        }

        public static void DisplayHelp()
        {
            Console.WriteLine("Commands: -h -d -n\n\t-h\tDisplay this help\n\t-n\tDownload and send now\n\t-d\tRun as deamon (Changes output)");
        }

        public static string DownloadFileFromServer(Uri fileUri)
        {
            string filePath = null;
            if (fileUri.Scheme != Uri.UriSchemeFtp)
                return filePath;

            WebClient request = new WebClient();
            try
            {
                byte[] newFileData = request.DownloadData(fileUri.ToString());
                string fileName = "ABT_DEL_CSH_" + DateTime.Now.ToString("M_d_yy") + ".pdf";
                FileStream file = new FileStream(fileName, FileMode.Create);
                file.Write(newFileData, 0, newFileData.Length);
                file.Flush(); 
                file.Close();
                filePath = Path.GetFullPath(fileName);
            }
            catch (WebException e)
            {
                Console.WriteLine(e.ToString());
            }
            catch (Exception ee)
            {
                Console.WriteLine(ee.ToString());
            }
            return filePath;
        }
    }
}
