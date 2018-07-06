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
        static string messageID = "test";

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
            mqtt.Publish("devices/FEZ/messages/events/", "HELLO!"); //devices/FEZ/messages/events/    $aws/things/FEZ/shadow/update

            mqtt.Subscribe("devices/FEZ/messages/devicebound/#", "test"); // devices/FEZ/messages/devicebound/# $aws/things/FEZ/shadow/update 

            Thread reader = new Thread(ReadStream);
            reader.Start();

            while (true) {
                wifi.ReadSocket(0, 500);
                Thread.Sleep(200);
            }
        }

        static void ReadStream() {
            wifi.DataReceived += listen;
        }

        static void listen(object sender, byte[] buffer, int length, int indexOffset) {
            lock (sender) {
                StringBuilder builder = new StringBuilder();

                int serviceMessage = 0;
                string expectedBytes = "";
                int messageLength = 0;

                expectedBytes += Convert.ToChar(buffer[indexOffset + 2]);
                expectedBytes += Convert.ToChar(buffer[indexOffset + 3]);
                expectedBytes += Convert.ToChar(buffer[indexOffset + 4]);
                expectedBytes += Convert.ToChar(buffer[indexOffset + 5]);
                expectedBytes += Convert.ToChar(buffer[indexOffset + 6]);
                Int32.TryParse(expectedBytes.ToString(), out messageLength);

                int clientIDlength = Encoding.UTF8.GetBytes(clientID).Length;
                int messageIDlength = (messageID != null) ? Encoding.UTF8.GetBytes(messageID).Length : Encoding.UTF8.GetBytes("3c82d2d6-3417-4c43-bb0a-69aed1bfe7ac").Length;
                messageLength += 6; //Magic bytes. Need to investigate

                serviceMessage += Encoding.UTF8.GetBytes("2 ").Length; //Sequence number
                serviceMessage += clientIDlength;
                serviceMessage += Encoding.UTF8.GetBytes("devices//messages/devicebound/").Length; // topic name
                serviceMessage += messageIDlength; // length of message ID. Can spicify or use the default like Encoding.UTF8.GetBytes("3c82d2d6-3417-4c43-bb0a-69aed1bfe7ac").Length;
                serviceMessage += Encoding.UTF8.GetBytes("%24.mid=&%24.to=%2Fdevices%2F").Length;
                serviceMessage += clientIDlength;
                serviceMessage += Encoding.UTF8.GetBytes("%2Fmessages%2FdeviceBound&iothub-ack=full").Length;
                serviceMessage += 13; //Magic bytes. Need to investigate

                for (var k = serviceMessage; k <= messageLength; k++) {
                    if (buffer[k] != 0) {
                        char result = (char)buffer[k];
                        builder.Append(result);
                    }
                }
                if (builder.ToString().IndexOf("on") != -1)
                    led.Write(GpioPinValue.High);

                if (builder.ToString().IndexOf("off") != -1)
                    led.Write(GpioPinValue.Low);

                Debug.WriteLine(builder.ToString());
                builder.Clear();
            }
        }

    }
}

