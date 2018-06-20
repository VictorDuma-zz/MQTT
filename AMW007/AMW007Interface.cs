using System;
using System.Collections;
using System.Text;
using System.Threading;
using GHIElectronics.TinyCLR.Storage.Streams;
using GHIElectronics.TinyCLR.Devices.SerialCommunication;
using GHIElectronics.TinyCLR.Pins;
using System.Diagnostics;

namespace AMW007 {
    public class AMW007Interface : IDisposable {
        private readonly SerialDevice serial;
        private DataWriter serWriter;
        private DataReader serReader;
        private byte[] errorCode;
        private bool connected;

        public AMW007Interface(SerialDevice serial) {
            this.serial = serial;

         }

        public void Run() {
            serReader = new DataReader(serial.InputStream);
            serWriter = new DataWriter(serial.OutputStream);
            Thread reader = new Thread(this.ReadSerial);
            reader.Start(); 
        }

        ~AMW007Interface() => this.Dispose(false);

        public void Dispose() {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                this.serial.Dispose();
            }
        }

        private void Write(string command) {
            if (command == null) {
                throw new ArgumentNullException("command");
            }

            serWriter.WriteString(command + "\r\n");
            serWriter.Store();
            Debug.WriteLine("Sent: " + command);
        }

        public void WriteData(string command, byte[] data) {
            if (command == null) {
                throw new ArgumentNullException("command");
            }

            serWriter.WriteString(command + "\r\n");
            Thread.Sleep(100);
            serWriter.WriteBytes(data);

            var written = 0U;

            while (serWriter.UnstoredBufferLength > 0)
                written = serWriter.Store();

        }

        public void OpenSocket(string host, int port) {
            this.Write("close all");
            Thread.Sleep(100);
            this.Write("tlsc " + host + " " + port); 
            while (connected == false) ;
            Thread.Sleep(3000);
            Debug.WriteLine("Connected");
        }

        public void CloseSocket(int socket) {
            this.Write("close all");
        }

        public void WriteSocket(int socket, byte[] data, int count) {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (count < 0) throw new ArgumentOutOfRangeException();
            this.WriteData("write 0 " + count.ToString(), data);
            Thread.Sleep(200);
        }

        public void ReadSocket(int socket, int count) {

            this.Write("read " + socket + " " + count);
            Thread.Sleep(500);
            //ReadSerial();
        }

        private void ReadSerial()
        {
            connected = false;

            while (true) {
                Thread.Sleep(10);
                var i = serReader.Load(1024);
                if (i == 0) continue;
                var response = serReader.ReadString(i);
                response.ToString();
                Debug.WriteLine(response);
                if (response.IndexOf("R000003") != -1) {
                    this.connected = true;
                }

            }
        }

        public void ReadBytes()
        {

            var builder = new StringBuilder();
            const int length = 10;

            byte[] buffer = new byte[length];

            var i = serReader.Load(length);

            for (var j = 0; j < i; j++)
            {

                buffer[j] = serReader.ReadByte();
                if (buffer[j] != 0)
                {
                    char result = (char)buffer[j];
                    builder.Append(result);
                    Array.Clear(buffer, 0, j);
                }

            }

            Debug.WriteLine(builder.ToString());
        }

        private byte[] ReadErrorCode() {
            errorCode = new byte[8];

            serReader.Load(8);

            for (var j = 0; j < errorCode.Length; j++) {
                errorCode[j] = serReader.ReadByte();
            }
            BufferToString(errorCode);

            return errorCode;
        }

        private void BufferToString(byte[] data) {

            var builder = new StringBuilder();

            for (var j = 0; j < data.Length; j++) {
                char result = (char)data[j];
                builder.Append(result);
            }

            Debug.WriteLine(builder.ToString());
        }

        public void JoinNetwork(string ssid, string password) {
            this.Write("set wlan.ssid " + ssid);
            this.Write("set wlan.passkey " + password);
            this.Write("set wlan.auto_join.enabled 1");
            this.Write("set system.print_level 1");
            this.Write("set system.cmd.header_enabled 1");
            this.Write("set system.cmd.prompt_enabled 1");
            this.Write("set system.cmd.echo on");
            this.Write("save");
            this.Write("reboot");
        }

        public void ClearTlsServerRootCertificate() {
          
        }

        public void SetTlsServerRootCertificate(byte[] certificate)
        {
 
        }

    }
}
