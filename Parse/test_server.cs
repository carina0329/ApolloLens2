// A C# Program for Server
using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Server {

class Program {

// Main Method
static void Main(string[] args)
{
	ExecuteServer();
}

public static void ExecuteServer()
{
	TcpListener server = null;
	try{
	  Int32 port = 11111;
	  IPAddress localAddr = IPAddress.Parse("35.2.162.213");

	  server = new TcpListener(localAddr, port);

	  server.Start();

	  Byte[] bytes = new Byte[1024];
	  String data = null;

	  while(true){
	    Console.Write("Waiting for a connection... ");

	    // Perform a blocking call to accept requests.
	    // You could also user server.AcceptSocket() here.
	    TcpClient client = server.AcceptTcpClient();
	    Console.WriteLine("Connected!");

	    data = null;

	    // Get a stream object for reading and writing
	    NetworkStream stream = client.GetStream();

	    int i;

	    // Loop to receive all the data sent by the client.
	    while((i = stream.Read(bytes, 0, bytes.Length))!=0){
	      // Translate data bytes to a ASCII string.
	      data += System.Text.Encoding.ASCII.GetString(bytes, 0, i);
	    }

			Console.WriteLine(data);

	    // Shutdown and end connection
	    client.Close();
	  }
	}
	catch(SocketException e){
	  Console.WriteLine("SocketException: {0}", e);
	}
	finally{
	  // Stop listening for new clients.
	  server.Stop();
	}

	Console.WriteLine("\nHit enter to continue...");
	Console.Read();
}

public static IDictionary<string, string> ParseData(XmlDocument doc){
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

	return dict;
}

}
}
