using System;
using System.Collections;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace AMW007 {

    public abstract class Constants {
        internal const int MQTTPROTOCOLVERSION = 4;
        internal const int MAXLENGTH = 10240; // 10K
        internal const int MAX_TOPIC_LENGTH = 256;
        internal const int MIN_TOPIC_LENGTH = 1;

        internal const byte MESSAGE_ID_SIZE = 2;
        internal const byte CONNECT_FLAG_BITS = 0x00;
        internal const byte SUBSCRIBE_FLAG_BITS = 0x02;
 
        internal const byte TYPE_MASK = 0xF0;
        internal const byte TYPE_OFFSET = 0x04;
        internal const byte TYPE_SIZE = 0x04;

        internal const byte CONNECT = 0x01;
        internal const byte SUBSCRIBE = 0x08;
        internal const byte PUBLISH = 0x30;
        internal const string PROTOCOL_NAME = "MQTT";

        // variable header fields
        internal const byte PROTOCOL_NAME_LEN_SIZE = 2;
        internal const byte PROTOCOL_NAME_SIZE = 4; // [v.3.1.1]
        internal const byte PROTOCOL_VERSION_SIZE = 1;
        internal const byte CONNECT_FLAGS_SIZE = 1;
        internal const byte KEEP_ALIVE_TIME_SIZE = 2;

        internal const byte PROTOCOL_VERSION_V3_1_1 = 0x04; 
        internal const ushort KEEP_ALIVE_PERIOD_DEFAULT = 60; 
        internal const ushort MAX_KEEP_ALIVE = 65535; 

        // connect flags
        internal const byte USERNAME_FLAG_OFFSET = 0x07;
        internal const byte PASSWORD_FLAG_OFFSET = 0x06;
        internal const byte CLEAN_SESSION_FLAG_OFFSET = 0x01;

        internal const int CONNACK = 32;
        internal const int PUBACK = 64;
        internal const int SUBACK = 144;
    }

    class MQTT {
        private AMW007Interface wifi;
        public string clientID;
        public string messageID;
        private bool connack = false;
        private bool suback = false;
        private bool puback = false;

        public void Connect(AMW007Interface wifi, string host, int port, string clientID, int keepAlive, bool cleanSession, string username = null, string password = null) {
            int fixedHeaderSize = 0;
            int varHeaderSize = 0;
            int payloadSize = 0;
            int remainingLength = 0;
            byte[] MQTTbuffer;
            int index = 0;
            this.wifi = wifi;
            this.clientID = clientID;

            wifi.OpenSocket(host, port); 

            byte[] clientIdUtf8 = Encoding.UTF8.GetBytes(clientID);
            byte[] usernameUtf8 = ((username != null) && (username.Length > 0)) ? Encoding.UTF8.GetBytes(username) : null;
            byte[] passwordUtf8 = ((password != null) && (password.Length > 0)) ? Encoding.UTF8.GetBytes(password) : null;

            varHeaderSize += (Constants.PROTOCOL_NAME_LEN_SIZE + Constants.PROTOCOL_NAME_SIZE);

            varHeaderSize += Constants.PROTOCOL_VERSION_SIZE;
            varHeaderSize += Constants.CONNECT_FLAGS_SIZE;
            varHeaderSize += Constants.KEEP_ALIVE_TIME_SIZE;

            payloadSize += clientIdUtf8.Length + 2;
            payloadSize += (usernameUtf8 != null) ? (usernameUtf8.Length + 2) : 0;
            payloadSize += (passwordUtf8 != null) ? (passwordUtf8.Length + 2) : 0;
            remainingLength += (varHeaderSize + payloadSize);

            fixedHeaderSize = 1;

            int temp = remainingLength;
            // increase fixed header size based on remaining length 
            // (each remaining length byte can encode until 128) 
            do {
                fixedHeaderSize++;
                temp = temp / 128;
            } while (temp > 0);

            MQTTbuffer = new byte[fixedHeaderSize + varHeaderSize + payloadSize];
            MQTTbuffer[index++] = (Constants.CONNECT << Constants.TYPE_OFFSET) | Constants.CONNECT_FLAG_BITS;
            index = this.encodeRemainingLength(remainingLength, MQTTbuffer, index);

            MQTTbuffer[index++] = 0; // MSB protocol name size 
            MQTTbuffer[index++] = Constants.PROTOCOL_NAME_SIZE; // LSB protocol name size 
            Array.Copy(Encoding.UTF8.GetBytes(Constants.PROTOCOL_NAME), 0, MQTTbuffer, index, Constants.PROTOCOL_NAME_SIZE);
            index += Constants.PROTOCOL_NAME_SIZE;
            MQTTbuffer[index++] = Constants.PROTOCOL_VERSION_V3_1_1;

            byte connectFlags = 0x00;
            connectFlags |= (usernameUtf8 != null) ? (byte)(1 << Constants.USERNAME_FLAG_OFFSET) : (byte)0x00;
            connectFlags |= (passwordUtf8 != null) ? (byte)(1 << Constants.PASSWORD_FLAG_OFFSET) : (byte)0x00;
            connectFlags |= (cleanSession) ? (byte)(1 << Constants.CLEAN_SESSION_FLAG_OFFSET) : (byte)0x00;
            MQTTbuffer[index++] = connectFlags;

            MQTTbuffer[index++] = (byte)(keepAlive / 256); // MSB 
            MQTTbuffer[index++] = (byte)(keepAlive % 256); // LSB 

            MQTTbuffer[index++] = (byte)((clientIdUtf8.Length >> 8) & 0x00FF); // MSB 
            MQTTbuffer[index++] = (byte)(clientIdUtf8.Length & 0x00FF); // LSB 
            Array.Copy(clientIdUtf8, 0, MQTTbuffer, index, clientIdUtf8.Length);
            index += clientIdUtf8.Length;

            if (usernameUtf8 != null) {
                MQTTbuffer[index++] = (byte)(usernameUtf8.Length / 256); // MSB 
                MQTTbuffer[index++] = (byte)(usernameUtf8.Length % 256); // LSB 
                Array.Copy(usernameUtf8, 0, MQTTbuffer, index, usernameUtf8.Length);
                index += usernameUtf8.Length;
            }

            if (passwordUtf8 != null) {
                MQTTbuffer[index++] = (byte)(passwordUtf8.Length / 256); // MSB 
                MQTTbuffer[index++] = (byte)(passwordUtf8.Length % 256); // LSB 
                Array.Copy(passwordUtf8, 0, MQTTbuffer, index, passwordUtf8.Length);
                index += passwordUtf8.Length;
            }

            wifi.WriteSocket(0, MQTTbuffer, MQTTbuffer.Length);
            Thread.Sleep(200);
            wifi.ReadSocket(0, 1000);
            Thread.Sleep(1000);
            //while (connack == false);
            // TODO implement ConnackHandler()
        }

        public void Publish(String topic, String message) {
            int index = 0;
            int tmp = 0;
            int fixedHeader = 0;
            int varHeader = 0;
            int payload = 0;
            int remainingLength = 0;
            byte[] MQTTbuffer = null;

            byte[] utf8Topic = Encoding.UTF8.GetBytes(topic);

             if ((topic.IndexOf('#') != -1) || (topic.IndexOf('+') != -1))
                throw new ArgumentException("Topic wildcards error");

            if ((utf8Topic.Length > Constants.MAX_TOPIC_LENGTH) || (utf8Topic.Length < Constants.MIN_TOPIC_LENGTH))
                throw new ArgumentException("Topic length error");

            varHeader += 2; 
            varHeader += utf8Topic.Length; 

            fixedHeader++; 
            payload = message.Length;
            remainingLength = varHeader + payload;

            if (remainingLength > Constants.MAXLENGTH)
                throw new ArgumentException("Message length error"); 

            // Add space for each byte we need in the fixed header to store the length
            tmp = remainingLength;
            while (tmp > 0) {
                fixedHeader++;
                tmp = tmp / 128;
            };

            MQTTbuffer = new byte[fixedHeader + varHeader + payload];
            MQTTbuffer[index++] = Constants.PUBLISH;

            index = encodeRemainingLength(remainingLength, MQTTbuffer, index);

            MQTTbuffer[index++] = (byte)(utf8Topic.Length / 256); 
            MQTTbuffer[index++] = (byte)(utf8Topic.Length % 256); 

            for (var i = 0; i < utf8Topic.Length; i++) {
                MQTTbuffer[index++] = utf8Topic[i];
            }

            for (var i = 0; i < message.Length; i++) {
                MQTTbuffer[index++] = (byte)message[i];
            }

            wifi.WriteSocket(0, MQTTbuffer, MQTTbuffer.Length);
            Thread.Sleep(1000);
            wifi.ReadSocket(0, 1000);
            //while (puback == false) ;
            // TODO implement PubackHandler()
        }

        public void Subscribe(string topic, string messageID = null) {
            int fixedHeaderSize = 0;
            int varHeaderSize = 0;
            int payloadSize = 0;
            int remainingLength = 0;
            byte[] MQTTbuffer;
            int index = 0;
            int qosLevel = 0x01;
            this.messageID = messageID;

            if ((topic == null) || (topic.Length == 0))
                throw new ArgumentException("Topic error");

            varHeaderSize += Constants.MESSAGE_ID_SIZE;

            byte[] topicsUtf8 = new byte[topic.Length];

            if ((topic.Length < Constants.MIN_TOPIC_LENGTH) || (topic.Length > Constants.MAX_TOPIC_LENGTH))
                throw new ArgumentException("Topic length error");

            topicsUtf8 = Encoding.UTF8.GetBytes(topic);
            payloadSize += 2; // topic size (MSB, LSB)
            payloadSize += topicsUtf8.Length;
            payloadSize++; // byte for QoS

            remainingLength += (varHeaderSize + payloadSize);
            fixedHeaderSize = 1;

            int temp = remainingLength;

            do {
                fixedHeaderSize++;
                temp = temp / 128;
            } while (temp > 0);

            MQTTbuffer = new byte[fixedHeaderSize + varHeaderSize + payloadSize];
            MQTTbuffer[index++] = (Constants.SUBSCRIBE << Constants.TYPE_OFFSET) | Constants.SUBSCRIBE_FLAG_BITS; 

            index = this.encodeRemainingLength(remainingLength, MQTTbuffer, index);

            int messageId = 1;
            MQTTbuffer[index++] = (byte)(messageId / 256);
            MQTTbuffer[index++] = (byte)(messageId % 256);

            MQTTbuffer[index++] = (byte)(topicsUtf8.Length / 256);
            MQTTbuffer[index++] = (byte)(topicsUtf8.Length % 256);
            Array.Copy(topicsUtf8, 0, MQTTbuffer, index, topicsUtf8.Length);
            index += topicsUtf8.Length;

            MQTTbuffer[index++] = (byte)qosLevel;

            wifi.WriteSocket(0, MQTTbuffer, MQTTbuffer.Length);
            //while (suback == false) ;
            // TODO implement SubackHandler()
        }

        private void ResponseHandler(byte[] buffer, int indexOffset) {
            indexOffset += 9; //response from module R000006 and 3-d byte int the buffer 
            int response = buffer[indexOffset];
            switch (response) {
                case Constants.CONNACK:
                    this.connack = true;
                    break;
                case Constants.SUBACK:
                    this.suback = true;
                    break;
                case Constants.PUBACK:
                    this.puback = true;
                    break;
                default:
                    break;
            }
        }

        private void SubackHandler() {

        }

        private void PubackHandler() {

        }
        public void listen() {
            Thread reader = new Thread(ReadStream);
            reader.Start();
        }

        private void ReadStream() {
            wifi.DataReceived += listen;
        }

        private void listen(object sender, byte[] buffer, int length, int indexOffset) {
            lock (sender) {
                string expectedBytes = "";
                int messageLength = 0;

                expectedBytes += Convert.ToChar(buffer[indexOffset + 2]);
                expectedBytes += Convert.ToChar(buffer[indexOffset + 3]);
                expectedBytes += Convert.ToChar(buffer[indexOffset + 4]);
                expectedBytes += Convert.ToChar(buffer[indexOffset + 5]);
                expectedBytes += Convert.ToChar(buffer[indexOffset + 6]);
                Int32.TryParse(expectedBytes.ToString(), out messageLength);

                if (messageLength > 50)
                    ReadMessage(buffer, messageLength, this.clientID, this.messageID);
                else
                    ResponseHandler(buffer, indexOffset);
            }
        }

        public string ReadMessage(byte [] data, int messageLength, string clientID, string messageID = null) {
            StringBuilder builder = new StringBuilder();

            int serviceMessage = 0;

            int clientIDlength = Encoding.UTF8.GetBytes(clientID).Length;
            int messageIDlength = (messageID != null)? Encoding.UTF8.GetBytes(messageID).Length : Encoding.UTF8.GetBytes("3c82d2d6-3417-4c43-bb0a-69aed1bfe7ac").Length;
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
                if (data[k] != 0) {
                    char result = (char)data[k];
                    builder.Append(result);
                }
            }
            Debug.WriteLine(builder.ToString());
            builder.Clear();

            return builder.ToString();
        }

        protected int encodeRemainingLength(int remainingLength, byte[] buffer, int index) {
            int digit = 0;
            do {
                digit = remainingLength % 128;
                remainingLength /= 128;
                if (remainingLength > 0)
                    digit = digit | 0x80;
                buffer[index++] = (byte)digit;
            } while (remainingLength > 0);
            return index;
        }

    }
}
