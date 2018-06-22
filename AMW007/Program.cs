using System;
using System.Collections;
using System.Text;
using System.Threading;
using GHIElectronics.TinyCLR.Devices.SerialCommunication;
using GHIElectronics.TinyCLR.Pins;


namespace AMW007
{
    class Program
    {
        static AMW007Interface wifi;
        SerialDevice serial;
        static MQTT mqtt;

        static void Main() {

            //"192.168.1.152", 1883  "ghi-test-iot.azure-devices.net", 8883 "a1uyw4e5ps7wof.iot.us-east-2.amazonaws.com", 8883
            string host = "ghi-test-iot.azure-devices.net";
            int port = 8883;
            var serial = SerialDevice.FromId(FEZ.UartPort.Usart1);
            serial.BaudRate = 115200;
            serial.ReadTimeout = TimeSpan.Zero;

            wifi = new AMW007Interface(serial);

            mqtt = new MQTT();
            wifi.Run();

            //wifi.SetTlsServerRootCertificate("azure.pem");

            mqtt.Connect(wifi, host, port, "FEZ", 60, true);
            mqtt.Publish("devices/FEZ/messages/events/", "HELLO!"); //devices/FEZ/messages/events/    $aws/things/FEZ/shadow/update

            mqtt.Subscribe("devices/FEZ/messages/devicebound/#"); // devices/FEZ/messages/devicebound/# $aws/things/FEZ/shadow/update 

        }
    }
}
