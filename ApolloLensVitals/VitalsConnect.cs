using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using System.Net;
using System.Net.Sockets;
using HL7.Dotnetcore;


namespace ApolloLensVitals
{
    /// <summary>
    /// Encapsulates Vitals Listener.
    /// Uses MLLP and HL7 v2/v3.
    /// Inspired by https://saravanansubramanian.com/hl72xdotnetprogramming/
    /// </summary>
    public class VitalsListener
    {
        private TcpClient client = null;
        private TcpListener listener = null;
        private NetworkStream stream = null;

        private ThreadStart childRef;
        private Thread childThread;

        private static char END_OF_BLOCK = '\u001c';
        private static char START_OF_BLOCK = '\u000b';
        private static char CARRIAGE_RETURN = (char)13;

        /// <summary>
        /// Initializes Listener.
        /// </summary>
        public VitalsListener()
        {
            this.listener = new TcpListener(IPAddress.Any, 10001);
            this.childRef = new ThreadStart(this.Listen);
            this.childThread = new Thread(childRef);
        }

        /// <summary>
        /// Thread starter: public interface.
        /// </summary>
        public void Run()
        {
            this.childThread.Start();
        }

        /// <summary>
        /// Wraps the try/catch and initiates listening.
        /// </summary>
        private void Listen()
        {
            try
            {
                this.listener.Start();
                this.Log("Listening...");

                while(true)
                {
                    // refreshed per connection
                    this.client = null;
                    this.stream = null;

                    this.Log("Waiting for new downstream connection...");
                    client = this.listener.AcceptTcpClient();
                    //client = new TcpClient();
                    //client.Connect(new IPEndPoint(IPAddress.Loopback, 10001));
                    stream = client.GetStream();
                    this.Log("Downstream connection made.");

                    int bytesReceived = 0;
                    string hl7Data = "";
                    var buffer = new byte[200];


                    while ((bytesReceived = stream.Read(buffer, 0, buffer.Length)) > 0)
                    {

                        hl7Data += Encoding.UTF8.GetString(buffer, 0, bytesReceived);
                        //this.Log($"Currently received: {hl7Data}");
                        

                        // Find start of MLLP frame, a VT character ...
                        var startOfMllpEnvelope = hl7Data.IndexOf(START_OF_BLOCK);
                        if (startOfMllpEnvelope >= 0)
                        {
                            // Now look for the end of the frame, a FS character
                            var end = hl7Data.IndexOf(END_OF_BLOCK);
                            if (end >= startOfMllpEnvelope) //end of block received
                            {
                                //if both start and end of block are recognized in the data transmitted, then extract the entire message
                                // string hl7MessageData = hl7Data.Substring(startOfMllpEnvelope + 1, end - startOfMllpEnvelope);
                                this.Log("Full hl7 Message Buffer Received.");
                                var messages = MessageHelper.ExtractMessages(hl7Data);
                                // construct and process each message
                                foreach (var strMsg in messages)
                                {
                                    this.Log(strMsg);
                                    Message message = new Message(strMsg);

                                    //string ack = message.GetACK().ToString();
                                    //var sendBuffer = Encoding.UTF8.GetBytes(ack);

                                   // if (stream.CanWrite)
                                  //  {
                                    //    stream.Write(sendBuffer, 0, sendBuffer.Length);
                                 //       this.Log($"Sent ack message to the client: {ack}");
                                //    }
                                   
                                }


                                //create a HL7 acknowledgement message
                                //var ackMessage = GetSimpleAcknowledgementMessage(hl7MessageData);

                                // Console.WriteLine(ackMessage);

                                //echo the received data back to the client 
                                //    var sendbuffer = Encoding.UTF8.GetBytes(ackMessage);

                                //    if (stream.CanWrite)
                                //    {
                                //        stream.Write(sendbuffer, 0, sendbuffer.Length);

                                //        this.Log("Ack message was sent back to the client...");
                                //  }
                            }
                        }
                    }

                    this.Log("MLLP HL7-encoded message received.");

                }

            }
            catch(Exception e)
            {
                this.Log(e.Message);
                this.Listen();
            }
        }

        private string GetSimpleAcknowledgementMessage(string incomingHl7Message)
        {
            if (string.IsNullOrEmpty(incomingHl7Message))
                throw new ApplicationException("Invalid HL7 message for parsing operation. Please check your inputs");

            //retrieve the message control ID of the incoming HL7 message 
            var messageControlId = "1407511";

            //build an acknowledgement message and include the control ID with it
            var ackMessage = new StringBuilder();
            ackMessage = ackMessage.Append(START_OF_BLOCK)
                .Append("MSH|^~\\&|||||||ACK||P|2.2")
                .Append(CARRIAGE_RETURN)
                .Append("MSA|AA|")
                .Append(messageControlId)
                .Append(CARRIAGE_RETURN)
                .Append(END_OF_BLOCK)
                .Append(CARRIAGE_RETURN);

            return ackMessage.ToString();
        }


        /// <summary>
        /// Utility logger function. Uses pass by reference.
        /// </summary>
        /// <param name="s">object to log</param>
        private void RLog(ref Object s)
        {
            System.Diagnostics.Debug.WriteLine(s);
        }

        /// <summary>
        /// Utility logger function. No pass by reference.
        /// </summary>
        /// <param name="s">object to log</param>
        private void Log(Object s)
        {
            System.Diagnostics.Debug.WriteLine(s);
        }
    }


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
                System.Diagnostics.Debug.WriteLine("ArgumentNullException : {0}", ane.ToString());
            }
            catch (SocketException se)
            {
                System.Diagnostics.Debug.WriteLine("SocketException : {0}", se.ToString());
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Unexpected exception : {0}", e.ToString());
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
                //  this.ipAddr = IPAddress.Loopback;
                this.ipAddr = IPAddress.Parse("127.0.0.1");
                //     this.ipAddr = new IPAddress(51639306);//  3.19.244.10
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
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }
        }
    }

}