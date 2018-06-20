using System;
using System.Collections;
using System.Text;
using System.Threading;
using GHIElectronics.TinyCLR.Storage.Streams;
using GHIElectronics.TinyCLR.Devices.SerialCommunication;
using GHIElectronics.TinyCLR.Pins;
using GHIElectronics.TinyCLR.Devices.Gpio;
using System.Diagnostics;

namespace AMW007
{
    class Program
    {
        static MQTT mqtt;
        static void Main() {

            mqtt = new MQTT();
            mqtt.Connect("FEZ", 30, false, "ghi-test-iot.azure-devices.net/FEZ", "SharedAccessSignature sr=ghi-test-iot.azure-devices.net%2Fdevices%2FFEZ&sig=9ZsecohsA3ZtlROML0BMAO%2BgTEBIigvy7Cj5q34RUFI%3D&se=1560446796");
            mqtt.Publish("devices/FEZ/messages/events/", "HELLO!");
            mqtt.Subscribe("devices/FEZ/messages/events/");
        }


    }
}
