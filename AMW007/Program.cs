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
        static byte[] streamBuffer = new byte[512];
        private static GpioPin led;
        static StringBuilder builder = new StringBuilder();

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

            mqtt.Connect(wifi, host, port, "FEZ", 60, true);
            mqtt.Publish("devices/FEZ/messages/events/", "HELLO!"); //devices/FEZ/messages/events/    $aws/things/FEZ/shadow/update

            mqtt.Subscribe("devices/FEZ/messages/devicebound/#"); // devices/FEZ/messages/devicebound/# $aws/things/FEZ/shadow/update 

            Thread reader = new Thread(ReadStream);
            reader.Start();

            while (true) {
                wifi.ReadSocket(0, 500);
                Thread.Sleep(500);
            }
        }

        static void ReadStream() {
            wifi.DataReceived += Wifi_DataReceived;
        }

        private static void Wifi_DataReceived(object sender, byte[] data, int length, int index) {

            lock (sender) {
                string getBytes = "";
                int messageLength = 0;

                getBytes += Convert.ToChar(data[index + 2]);
                getBytes += Convert.ToChar(data[index + 3]);
                getBytes += Convert.ToChar(data[index + 4]);
                getBytes += Convert.ToChar(data[index + 5]);
                getBytes += Convert.ToChar(data[index + 6]);

                Int32.TryParse(getBytes.ToString(), out messageLength);

                int serviceMessage = 158;
                for (var k = serviceMessage; k < messageLength + 8; k++)
                {
                    if (data[k] != 0)
                    {
                        char result = (char)data[k];
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

