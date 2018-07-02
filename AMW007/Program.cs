using System;
using System.Collections;
using System.Text;
using System.Threading;
using GHIElectronics.TinyCLR.Devices.SerialCommunication;
using GHIElectronics.TinyCLR.Pins;
using GHIElectronics.TinyCLR.Devices.Gpio;
using System.Diagnostics;

namespace AMW007 {
    class Program {
        static AMW007Interface wifi;
        static MQTT mqtt;
        private static GpioPin led;
        static StringBuilder builder = new StringBuilder();
        static string clientID = "FEZ";

        static void Main() {

            led = GpioController.GetDefault().OpenPin(FEZ.GpioPin.Led1);
            led.SetDriveMode(GpioPinDriveMode.Output);

            string host = "ghi-test-iot.azure-devices.net"; //"192.168.1.152" "ghi-test-iot.azure-devices.net" "a1uyw4e5ps7wof.iot.us-east-2.amazonaws.com"
            int port = 8883; // 1883 8883

            var serial = SerialDevice.FromId(FEZ.UartPort.Usart1);
            serial.BaudRate = 115200;
            serial.ReadTimeout = TimeSpan.Zero;

            wifi = new AMW007Interface(serial);

            mqtt = new MQTT();
            wifi.Run();

            //wifi.SetTlsServerRootCertificate("azure.pem"); //aws.pem

            mqtt.Connect(wifi, host, port, clientID, 60, true);
            //mqtt.Publish("devices/FEZ/messages/events/", "HELLO!"); //devices/FEZ/messages/events/    $aws/things/FEZ/shadow/update

            mqtt.Subscribe("devices/FEZ/messages/devicebound/#", "test"); // devices/FEZ/messages/devicebound/# $aws/things/FEZ/shadow/update 
            mqtt.listen();

            while (true) {
                wifi.ReadSocket(0, 500);
                Thread.Sleep(200);
            }
        }


    }
}

