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
        static MQTT mqtt;
        static void Main() {

            var serial = SerialDevice.FromId(FEZ.UartPort.Usart1);
            serial.BaudRate = 115200;
            serial.ReadTimeout = TimeSpan.Zero;

            mqtt = new MQTT();
            mqtt.Connect(serial, "FEZ", 60, true, "ghi-test-iot.azure-devices.net/FEZ", "SharedAccessSignature sr=ghi-test-iot.azure-devices.net%2Fdevices%2FFEZ&sig=9ZsecohsA3ZtlROML0BMAO%2BgTEBIigvy7Cj5q34RUFI%3D&se=1560446796"); //, "ghi-test-iot.azure-devices.net/FEZ", "SharedAccessSignature sr=ghi-test-iot.azure-devices.net%2Fdevices%2FFEZ&sig=9ZsecohsA3ZtlROML0BMAO%2BgTEBIigvy7Cj5q34RUFI%3D&se=1560446796"
            mqtt.Publish("devices/FEZ/messages/events/", "HELLO!"); //devices/FEZ/messages/events/

            mqtt.Subscribe("devices/FEZ/messages/devicebound/#"); // $aws/things/FEZ/shadow/update

        }
    }
}
