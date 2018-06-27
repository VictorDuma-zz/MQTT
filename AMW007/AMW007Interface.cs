using System;
using System.Collections;
using System.Text;
using System.Threading;
using GHIElectronics.TinyCLR.Storage.Streams;
using GHIElectronics.TinyCLR.Devices.SerialCommunication;
using GHIElectronics.TinyCLR.Pins;
using System.Diagnostics;
using GHIElectronics.TinyCLR.Devices.Gpio;
using System.IO;

namespace AMW007 {
    public class AMW007Interface : IDisposable {
        private readonly SerialDevice serial;
        public MemoryStream stream;
        private DataWriter serWriter;
        private DataReader serReader;
        private bool connected;
 
        public AMW007Interface(SerialDevice serial) {
            this.serial = serial;
         }

        public void Run() {
            serReader = new DataReader(serial.InputStream);
            serWriter = new DataWriter(serial.OutputStream);
            this.stream = new MemoryStream();

            Thread reader = new Thread(this.ReadBytes);
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

        }

        public event MyHandler DataReceived;

        public delegate void MyHandler(object sender, byte[] data, int length, int index);

        public void ReadBytes()
        {
            var builder = new StringBuilder();
            const int length = 500;
            byte[] buffer = new byte[length];
            int index = 0;

            while (true)
            {
                Thread.Sleep(100);

                var i = serReader.Load(length);
                if (i != 0) {
                    for (var j = 0; j < i; j++) {
                        buffer[j] = serReader.ReadByte();

                        if (buffer[j] == 'R')
                            index = j;
                    }

                    //R000003 - means socket is open.
                    if (buffer[index + 6] == 51) {
                        this.connected = true;
                    }

                    // Amount of bytes
                    if (buffer[index + 5] != 48) {
                        DataReceived?.Invoke(this, buffer, length, index);
                        Array.Clear(buffer, 0, length);
                    }

                }
            }
        }

        public void JoinNetwork(string ssid, string password) {
            this.Write("set wlan.ssid " + ssid);
            this.Write("set wlan.passkey " + password);
            this.Write("set wlan.auto_join.enabled 1");
            this.Write("set system.print_level 1");
            this.Write("set system.cmd.header_enabled 1");
            this.Write("set system.cmd.prompt_enabled 0");
            this.Write("set system.cmd.echo off");
            this.Write("save");
            this.Write("reboot");
        }

        public void SetTlsServerRootCertificate(string cert) {
            this.Write("set network.tls.ca_cert " + cert);
            this.Write("save");
            Thread.Sleep(500);
        }

    }
}
