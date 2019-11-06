using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;



// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409



namespace JsonExample.Serialization
{
    public interface ISerializer
    {
        string Serialize<TEntity>(TEntity entity)
            where TEntity : class, new();

        TEntity Deserialize<TEntity>(string entity)
            where TEntity : class, new();
    }
}

namespace JsonExample.Serialization
{
    public class JsonSerializer : ISerializer
    {
        public string Serialize<TEntity>(TEntity entity)
            where TEntity : class, new()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(TEntity));
                ser.WriteObject(ms, entity);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        public TEntity Deserialize<TEntity>(string entity)
            where TEntity : class, new()
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(TEntity));
            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(entity)))
            {
                return ser.ReadObject(stream) as TEntity;
            }
        }
    }
}





namespace ApolloLensVitals
{
    /// <summary>
    /// Vitals Main Page.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

        }

        //private VitalsConnect vitals;
        private VitalsListener vitals;
        private VitalsConnect vitalsOld;

        private void button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void textBlock_SelectionChanged(object sender, RoutedEventArgs e)
        {

        }

        protected override async void OnNavigatedTo(NavigationEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine("ApolloLensVitals booted.");
            //this.vitalsOld = new VitalsConnect(ChangeText);
            //this.vitalsOld.Run();
            this.vitals = new VitalsListener();
            this.vitals.Run();
            //ProgramX x = new ProgramX();

        }

        public delegate void ChangeTextDelegate(string bp);


        public async void ChangeText(string bp)
        {
            /////// var stream1 = new MemoryStream();
            // var ser = new DataContractJsonSerializer(typeof(Vitals));
            //stream1.Position = 0;
            // var p2 = (Vitals)ser.ReadObject(stream1);
            //Vitals p2 = JsonExample.Serialization.JsonSerializer.Deserialize<Vitals>(bp);

            //// JsonExample.Serialization.JsonSerializer x = new JsonExample.Serialization.JsonSerializer();
            //       Vitals p2 = x.Deserialize<Vitals>(bp);
            bp = bp.Replace("\0", string.Empty);

            Vitals p2 = JsonConvert.DeserializeObject<Vitals>(bp);

            
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                this.BloodPressureText.Text = p2.blood_pressure_systolic + " | " + p2.blood_pressure_diastolic + " mmHg";
                this.HeartRateText.Text = p2.heart_rate + " bpm";
                this.RespirationRateText.Text = p2.respiration_rate + " bpm";
                //System.Diagnostics.Debug.WriteLine(bp);


               //// this.BloodPressureText.Text = bp;
            //    this.HeartRateText.Text = bp;
             //   this.RespirationRateText.Text = bp;
            });
           // this.BloodPressureText.Text = bp;
          //  this.UpdateLayout();
        }
    }

    //DataContrac
    public class Vitals
    {
        ////[DataMember(Name = "heart_rate", Order = 0)]
        public string heart_rate { get; set; }

//[DataMember(Name = "blood_pressure_systolic", Order = 1)]
        public string blood_pressure_systolic { get; set; }

        //DataMember(Name = "blood_pressure_diastolic", Order = 2)]
        public string blood_pressure_diastolic { get; set; }

//[DataMember(Name = "respiration_rate", Order = 3)]
        public string respiration_rate { get; set; }
    }
}


