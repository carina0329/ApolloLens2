using System;
using System.Collections.Generic;
using System.Xml;

namespace org.doublecloud
{
    class XmlParsingDemo
    {
        static void Main(string[] args)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(@"data.xml");

            XmlNamespaceManager mgr = new XmlNamespaceManager(doc.NameTable);

            mgr.AddNamespace("hl7", "urn:hl7-org:v3");

            XmlNodeList vital_values = doc.DocumentElement.SelectNodes("//hl7:POLB_IN224200UV01/hl7:controlActProcess/hl7:subject/hl7:observationBattery/hl7:component1/hl7:observationEvent/hl7:value", mgr);
            XmlNodeList display_names = doc.DocumentElement.SelectNodes("//hl7:POLB_IN224200UV01/hl7:controlActProcess/hl7:subject/hl7:observationBattery/hl7:component1/hl7:observationEvent/hl7:code", mgr);

            IDictionary<string, string> dict = new Dictionary<string, string>();



            for(int i = 0; i < vital_values.Count; ++i) {

                if (display_names[i].Attributes["displayName"].Value == "Body temperature") {
                    double celcius = Double.Parse(vital_values[i].Attributes["value"].Value);
                    double temp = ((celcius * 9) / 5) + 32;
                    string strValue = temp.ToString("N2");

                    dict.Add(display_names[i].Attributes["displayName"].Value, strValue);

                }
                else {
                    dict.Add(display_names[i].Attributes["displayName"].Value, vital_values[i].Attributes["value"].Value);
                }
            }

            foreach(KeyValuePair<string, string> entry in dict) {
                Console.Write(entry.Key);
                Console.Write(" ");
                Console.Write(entry.Value);
                Console.WriteLine("");
            }
        }
    }
}
