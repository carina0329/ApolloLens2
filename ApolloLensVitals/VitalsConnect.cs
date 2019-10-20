using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using System.Net;
using System.Net.Sockets;


namespace ApolloLensVitals
{
    /// <summary>
    /// Encapsulates connection with Vitals Source Server.
    /// In charge of receiving Vitals data.
    /// </summary>
    public class VitalsConnect
    {
        private Socket sock;
        private IPHostEntry ipHost;
        private IPAddress ipAddr;
        private IPEndPoint localEndPoint;
        private TcpClient tc;
        private MainPage.ChangeTextDelegate changeText;
        private int byteArrInd;

        /// <summary>
        /// Constructor. Calls InitClient.
        /// </summary>
        public VitalsConnect(MainPage.ChangeTextDelegate c)
        {
            this.changeText = c;
            this.InitClient();
        }

        public void Run()
        {
            ThreadStart childref = new ThreadStart(this.RunThread);
            Thread childThread = new Thread(childref);
            childThread.Start();
        }

        private void RunThread()
        {
            try
            {
                this.Connect();
                this.Listen();
            }
            catch (ArgumentNullException ane)
            {
                Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
            }
            catch (SocketException se)
            {
                System.Diagnostics.Debug.WriteLine("SocketException : {0}", se.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("Unexpected exception : {0}", e.ToString());
            }
        }

        private void Connect()
        {
            // connect socket
            this.sock.Connect(this.localEndPoint);
           // this.tc = new TcpClient();
          //  this.tc.Connect(this.ipAddr, 10000);
            byte[] verifyMessage = Encoding.UTF8.GetBytes("Test");
            this.sock.Send(verifyMessage);
        }

        private void Listen()
        {
            // listen for vitals from the source
            byteArrInd = 0;
            byte[] msgArr = new byte[2048];
            while (true)
            {
                // Data buffer 
                byte[] recvArr = new byte[1];
                int byteRecv = this.sock.Receive(recvArr);

                msgArr[byteArrInd] = recvArr[0];
                if (recvArr[0] == '}')
                {
                    
                    string jsonStr = Encoding.UTF8.GetString(msgArr);
                    this.changeText(jsonStr);
                    for (int clear = 0; clear <= byteArrInd; ++clear)
                    {
                        msgArr[clear] = default(byte);
                    }
                    byteArrInd = 0;
                    
                    
                }
                else
                {
                    ++byteArrInd;
                }


                
            }

        }


        /// <summary>
        /// Initializes the client by setting up the socket and
        /// sending bytes to confirm connection (need to change auth)
        /// </summary>
        public void InitClient()
        {
            try
            {
                // determine destination
                // this.ipHost = Dns.GetHostEntry(Dns.GetHostName());
                //this.ipHost = Dns.GetHostEntry("localhost");
                //this.ipAddr = this.ipHost.AddressList[0];
                // this.ipAddr = new IPAddress(Encoding.ASCII.GetBytes("127.0.0.1"));
                this.ipAddr = IPAddress.Loopback;
                this.localEndPoint = new IPEndPoint(this.ipAddr, 10000);

                // create socket
                this.sock = new Socket(
                    this.ipAddr.AddressFamily,
                    SocketType.Stream,
                    ProtocolType.Tcp
                );
                //this.sock.ReceiveTimeout = 2000;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }


    }
}
