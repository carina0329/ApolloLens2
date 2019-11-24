using System;
using Windows.UI.Xaml.Controls;
using System.Xml;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml;
using Windows.UI.Core;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using Windows.UI.Xaml.Media;

namespace Vitals
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public class TextboxText
    {
        public string Textdata { get; set; }
    }
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        // Upper and lower alert ranges for temperature, diastolic blood pressure, systolic blood pressure, heart rate, Sp02
        public static int temp_alert_lower_val = 90;
        public static int temp_alert_upper_val = 105;
        public static int bp_diast_alert_lower_val = 60;
        public static int bp_diast_alert_upper_val = 110;
        public static int bp_syst_alert_lower_val = 80;
        public static int bp_syst_alert_upper_val = 160;
        public static int hr_alert_lower_val = 40;
        public static int hr_alert_upper_val = 150;
        public static int sp02_alert_lower_val = 90;

        // Current vital readings
        private String _systolic_blood_pressure_val;
        private String _diastolic_blood_pressure_val;
        private String _heartrate_val;
        private String _oxygen_val;
        private String _temperature_val;
        private String _blood_pressure_val;
        private String _current_time_val;

        // Colors for UI display
        public static String alert_color = "White";
        private String _heartrate_color = "HotPink";
        private String _oxygen_color = "#FF2ED813";
        private String _temperature_color = "#FF0CA5DE";
        private String _blood_pressure_color = "#FFF39320";
        public String current_time_val
        {
            get { return _current_time_val; }
            set
            {
                _current_time_val = value;
                OnPropertyChanged();
            }
        }
        public String systolic_blood_pressure_val {
            get { return _systolic_blood_pressure_val; }
            set
            {
                _systolic_blood_pressure_val = value;
                OnPropertyChanged();
            }
        }
        public String diastolic_blood_pressure_val
        {
            get { return _diastolic_blood_pressure_val; }
            set
            {
                _diastolic_blood_pressure_val = value;
                OnPropertyChanged();
            }
        }
        public String heartrate_val
        {
            get { return _heartrate_val; }
            set
            {
                _heartrate_val = value;
                OnPropertyChanged();
            }
        }
        public String oxygen_val
        {
            get { return _oxygen_val; }
            set
            {
                _oxygen_val = value;
                OnPropertyChanged();
            }
        }
        public String temperature_val
        {
            get { return _temperature_val; }
            set
            {
                _temperature_val = value;
                OnPropertyChanged();
            }
        }
        public String blood_pressure_val
        {
            get { return _blood_pressure_val; }
            set
            {
                _blood_pressure_val = value;
                OnPropertyChanged();
            }
        }

        public String heartrate_color
        {
            get { return _heartrate_color; }
            set
            {
                _heartrate_color = value;
                OnPropertyChanged();
            }
        }
        public String oxygen_color
        {
            get { return _oxygen_color; }
            set
            {
                _oxygen_color = value;
                OnPropertyChanged();
            }
        }
        public String temperature_color
        {
            get { return _temperature_color; }
            set
            {
                _temperature_color = value;
                OnPropertyChanged();
            }
        }

        public String blood_pressure_color
        {
            get { return _blood_pressure_color; }
            set
            {
                _blood_pressure_color = value;
                OnPropertyChanged();
            }
        }
        public async Task OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            // Raise the PropertyChanged event, passing the name of the property whose value has changed.
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
            );
        }


        public MainPage()
        {
            this.InitializeComponent();
            Debug.WriteLine("Hello");
            systolic_blood_pressure_val = diastolic_blood_pressure_val = heartrate_val = oxygen_val = temperature_val = blood_pressure_val = "0";
            //DataContext = vitalValues;

            Thread t = new Thread(SimulateServer);
            Thread t2 = new Thread(Clock);
            t2.Start();
            t.Start();
        }

        public void UpdateUI()
        {
            blood_pressure_val = systolic_blood_pressure_val + "/" + diastolic_blood_pressure_val;
            //DataContext = vitalValues;
            Debug.WriteLine("Updated UI.....heartrate " + heartrate_val);
        }

        public void Clock()
        {
            while (true)
            {
                current_time_val = DateTime.Now.ToString("h:mm:ss tt");
            }
        }

        void ClickRevert(object sender, RoutedEventArgs e)
        {
            heartrate_button.Height = double.NaN;
            heartrate_button.Width = double.NaN;

            blood_pressure_button.Height = double.NaN;
            blood_pressure_button.Width = double.NaN;

            oxygen_button.Height = double.NaN;
            oxygen_button.Width = double.NaN;

            temperature_button.Height = double.NaN;
            temperature_button.Width = double.NaN;
        }
        void OnClickHR(object sender, RoutedEventArgs e)
        {
            heartrate_button.Foreground = new SolidColorBrush(Windows.UI.Colors.Green);
            heartrate_button.Height = 0;
            heartrate_button.Width = 0;
        }
        void OnClickBP(object sender, RoutedEventArgs e)
        {
            blood_pressure_button.Foreground = new SolidColorBrush(Windows.UI.Colors.Green);
            blood_pressure_button.Height = 0;
            blood_pressure_button.Width = 0;
        }
        void OnClickO2(object sender, RoutedEventArgs e)
        {
            oxygen_button.Foreground = new SolidColorBrush(Windows.UI.Colors.Green);
            oxygen_button.Height = 0;
            oxygen_button.Width = 0;
        }
        void OnClickT(object sender, RoutedEventArgs e)
        {
            temperature_button.Foreground = new SolidColorBrush(Windows.UI.Colors.Green);
            temperature_button.Height = 0;
            temperature_button.Width = 0;
        }
        public void SimulateServer()
        {
            String[] docs = { "test1.xml", "test2.xml", "test3.xml", "test4.xml" };
            string data = "";
            int i = 0;
            while (true)
            {
                Debug.WriteLine("Next file" + i);
                ParseDataFromSocket(docs[i], 3);

                Thread.Sleep(5000);

                if (i == 3) i = 0;
                else i++;
            }
        }

        public void ExecuteServer()
        {
            // Establish the local endpoint
            // for the socket. Dns.GetHostName
            // returns the name of the host
            // running the application.
            int data_version = 3;
            IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddr = ipHost.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddr, 11111);

            // Creation TCP/IP Socket using
            // Socket Class Costructor
            Socket listener = new Socket(ipAddr.AddressFamily,
                        SocketType.Stream, ProtocolType.Tcp);
            try
            {

                // Using Bind() method we associate a
                // network address to the Server Socket
                // All client that will connect to this
                // Server Socket must know this network
                // Address
                listener.Bind(localEndPoint);

                // Using Listen() method we create
                // the Client list that will want
                // to connect to Server
                listener.Listen(10);

                while (true)
                {

                    Debug.WriteLine("Waiting connection ... ");

                    // Suspend while waiting for
                    // incoming connection Using
                    // Accept() method the server
                    // will accept connection of client
                    Socket clientSocket = listener.Accept();

                    // Data buffer
                    byte[] bytes = new Byte[1024];
                    string data = null;

                    while (true)
                    {

                        int numByte = clientSocket.Receive(bytes);

                        data += Encoding.UTF8.GetString(bytes,
                                                0, numByte);

                        if (numByte == 0)
                            break;
                    }

                    Console.WriteLine(data);
                    if (data[0] == 'M' && data[1] == 'S' && data[2] == 'H')
                        data_version = 2;

                    ParseDataFromSocket(data, data_version);

                    Debug.WriteLine("Parsed data");

                    // Close client Socket using the
                    // Close() method. After closing,
                    // we can use the closed Socket
                    // for a new Client Connection
                    clientSocket.Shutdown(SocketShutdown.Both);
                    clientSocket.Close();
                }
            }

            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }


        public void ParseDataFromSocket(string data, int version){
              if(version == 2){
                List<string> piped = new List<string>();
                string[] tokens = data.Split("OBX");

                foreach (var word in tokens){
                  if(word[0] == '|')
                    piped.Add(word);
                }

                foreach (var pipe in piped){
                  string[] fields = pipe.Split('|');
                  if(fields[2] == "NM"){
                    if(fields[3] == "3446"){
                      double fahrenheit = ((Double.Parse(fields[5]) * 9) / 5) + 32;
                      String result = string.Format("{0:0.0}", Math.Truncate(fahrenheit * 10) / 10);
                      temperature_val = result;
                      if(Convert.ToDouble(temperature_val) <= temp_alert_lower_val || Convert.ToDouble(temperature_val) >= temp_alert_upper_val)
                      {
                          temperature_color = alert_color;
                      }
                      else
                      {
                          temperature_color = "#FF0CA5DE";
                      }
                    }
                    else if(fields[3] == "19"){
                      heartrate_val = fields[5];
                      if(Convert.ToInt32(heartrate_val) <= hr_alert_lower_val || Convert.ToInt32(heartrate_val) >= hr_alert_upper_val)
                      {
                          heartrate_color = alert_color;
                      }
                      else
                      {
                          heartrate_color = "HotPink";
                      }
                    }
                    else if(fields[3] == "2"){
                      systolic_blood_pressure_val = fields[5];
                      if (Convert.ToInt32(diastolic_blood_pressure_val) >= bp_diast_alert_upper_val || Convert.ToInt32(diastolic_blood_pressure_val) <= bp_diast_alert_lower_val ||
                          Convert.ToInt32(systolic_blood_pressure_val) >= bp_syst_alert_upper_val || Convert.ToInt32(systolic_blood_pressure_val) <= bp_syst_alert_lower_val)
                      {
                          blood_pressure_color = alert_color;
                      }
                      else
                      {
                          blood_pressure_color = "#FFF39320";
                      }
                    }
                    else if(fields[3] == "3"){
                      diastolic_blood_pressure_val = fields[5];
                      if(Convert.ToInt32(diastolic_blood_pressure_val) >= bp_diast_alert_upper_val || Convert.ToInt32(diastolic_blood_pressure_val) <= bp_diast_alert_lower_val ||
                          Convert.ToInt32(systolic_blood_pressure_val) >= bp_syst_alert_upper_val || Convert.ToInt32(systolic_blood_pressure_val) <= bp_syst_alert_lower_val)
                      {
                          blood_pressure_color = alert_color;
                      }
                      else
                      {
                          blood_pressure_color = "#FFF39320";
                      }
                    }
                    else if(fields[3] == "4"){
                      Console.WriteLine("mean blood pressure");
                    }
                    else if(fields[3] == "14"){
                      oxygen_val = fields[5];
                      if(Convert.ToInt32(oxygen_val) <= sp02_alert_lower_val)
                      {
                          oxygen_color = alert_color;
                      }
                      else
                      {
                          oxygen_color = "#FF2ED813";
                      }
                    }
                  }
                  UpdateUI();
                }
              }
              else{
                XmlDocument doc = new XmlDocument();
                doc.Load(data);

                XmlNamespaceManager mgr = new XmlNamespaceManager(doc.NameTable);

                mgr.AddNamespace("hl7", "urn:hl7-org:v3");

                XmlNodeList vital_values = doc.DocumentElement.SelectNodes("//hl7:POLB_IN224200UV01/hl7:controlActProcess/hl7:subject/hl7:observationBattery/hl7:component1/hl7:observationEvent/hl7:value", mgr);
                XmlNodeList display_names = doc.DocumentElement.SelectNodes("//hl7:POLB_IN224200UV01/hl7:controlActProcess/hl7:subject/hl7:observationBattery/hl7:component1/hl7:observationEvent/hl7:code", mgr);

                for (int i = 0; i < vital_values.Count; ++i)
                {
                    String curr_val = vital_values[i].Attributes["value"].Value;
                    // Update individual values
                    if (display_names[i].Attributes["displayName"].Value == "Body temperature")
                    {
                        double fahrenheit = ((Double.Parse(curr_val) * 9) / 5) + 32;
                        String result = string.Format("{0:0.0}", Math.Truncate(fahrenheit * 10) / 10);
                        temperature_val = result;
                        if(Convert.ToDouble(temperature_val) <= temp_alert_lower_val || Convert.ToDouble(temperature_val) >= temp_alert_upper_val)
                        {
                            temperature_color = alert_color;
                        }
                        else
                        {
                            temperature_color = "#FF0CA5DE";
                        }
                    }
                    else if (display_names[i].Attributes["displayName"].Value == "Systolic blood pressure")
                    {
                        systolic_blood_pressure_val = curr_val;
                        if (Convert.ToInt32(diastolic_blood_pressure_val) >= bp_diast_alert_upper_val || Convert.ToInt32(diastolic_blood_pressure_val) <= bp_diast_alert_lower_val ||
                            Convert.ToInt32(systolic_blood_pressure_val) >= bp_syst_alert_upper_val || Convert.ToInt32(systolic_blood_pressure_val) <= bp_syst_alert_lower_val)
                        {
                            blood_pressure_color = alert_color;
                        }
                        else
                        {
                            blood_pressure_color = "#FFF39320";
                        }
                    }
                    else if (display_names[i].Attributes["displayName"].Value == "Diastolic blood pressure")
                    {
                        diastolic_blood_pressure_val = curr_val;
                        if(Convert.ToInt32(diastolic_blood_pressure_val) >= bp_diast_alert_upper_val || Convert.ToInt32(diastolic_blood_pressure_val) <= bp_diast_alert_lower_val ||
                            Convert.ToInt32(systolic_blood_pressure_val) >= bp_syst_alert_upper_val || Convert.ToInt32(systolic_blood_pressure_val) <= bp_syst_alert_lower_val)
                        {
                            blood_pressure_color = alert_color;
                        }
                        else
                        {
                            blood_pressure_color = "#FFF39320";
                        }
                    }
                    else if (display_names[i].Attributes["displayName"].Value == "Mean blood pressure")
                    {

                    }
                    else if (display_names[i].Attributes["displayName"].Value == "Pulse rate")
                    {
                        heartrate_val = curr_val;
                        if(Convert.ToInt32(heartrate_val) <= hr_alert_lower_val || Convert.ToInt32(heartrate_val) >= hr_alert_upper_val)
                        {
                            heartrate_color = alert_color;
                        }
                        else
                        {
                            heartrate_color = "HotPink";
                        }
                    }
                    else if (display_names[i].Attributes["displayName"].Value == "SpO2")
                    {
                        oxygen_val = curr_val;
                        if(Convert.ToInt32(oxygen_val) <= sp02_alert_lower_val)
                        {
                            oxygen_color = alert_color;
                        }
                        else
                        {
                            oxygen_color = "#FF2ED813";
                        }
                    }
                    UpdateUI();
                }
              }
            }
        }
}
