using System;
using System.Collections;
using System.Diagnostics;
using System.Text;
using System.Threading;
using GHIElectronics.TinyCLR.Devices.SerialCommunication;
using GHIElectronics.TinyCLR.Pins;

namespace AMW007 {

    public abstract class Constants {
        internal const int MQTTPROTOCOLVERSION = 4;
        internal const int MAXLENGTH = 10240; // 10K
        internal const int MAX_TOPIC_LENGTH = 256;
        internal const int MIN_TOPIC_LENGTH = 1;
        internal const byte MQTT_PUBLISH_TYPE = 0x30;
 
        internal const byte MESSAGE_ID_SIZE = 2;
        internal const byte MQTT_MSG_CONNECT_FLAG_BITS = 0x00;
        internal const byte MQTT_MSG_PUBLISH_FLAG_BITS = 0x00; // just defined as 0x00 but depends on publish props (dup, qos, retain) 
        internal const byte MQTT_MSG_SUBSCRIBE_FLAG_BITS = 0x02;
 
        internal const byte MSG_TYPE_MASK = 0xF0;
        internal const byte MSG_TYPE_OFFSET = 0x04;
        internal const byte MSG_TYPE_SIZE = 0x04;

        internal const byte QOS_LEVEL_MASK = 0x06;
        internal const byte QOS_LEVEL_OFFSET = 0x01;
        internal const byte QOS_LEVEL_SIZE = 0x02;
        internal const byte MQTT_MSG_CONNECT_TYPE = 0x01;

        internal const byte MQTT_MSG_SUBSCRIBE_TYPE = 0x08;
        internal const string PROTOCOL_NAME_V3_1_1 = "MQTT";

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
    }

    class MQTT {

        private AMW007Interface wifi;

        public void Connect(AMW007Interface wifi, string host, int port, string clientID, int keepAlive, bool cleanSession, string username = null, string password = null) {
            int fixedHeaderSize = 0;
            int varHeaderSize = 0;
            int payloadSize = 0;
            int remainingLength = 0;
            byte[] MQTTbuffer;
            int index = 0;
            this.wifi = wifi;

            wifi.OpenSocket(host, port); 

            byte[] clientIdUtf8 = Encoding.UTF8.GetBytes(clientID);
            byte[] usernameUtf8 = ((username != null) && (username.Length > 0)) ? Encoding.UTF8.GetBytes(username) : null;
            byte[] passwordUtf8 = ((password != null) && (password.Length > 0)) ? Encoding.UTF8.GetBytes(password) : null;

            varHeaderSize += (Constants.PROTOCOL_NAME_LEN_SIZE + Constants.PROTOCOL_NAME_SIZE);

            // protocol level field size 
            varHeaderSize += Constants.PROTOCOL_VERSION_SIZE;
            // connect flags field size 
            varHeaderSize += Constants.CONNECT_FLAGS_SIZE;
            // keep alive timer field size 
            varHeaderSize += Constants.KEEP_ALIVE_TIME_SIZE;

            // client identifier field size 
            payloadSize += clientIdUtf8.Length + 2;
            // username field size 
            payloadSize += (usernameUtf8 != null) ? (usernameUtf8.Length + 2) : 0;
            // password field size
            payloadSize += (passwordUtf8 != null) ? (passwordUtf8.Length + 2) : 0;

            remainingLength += (varHeaderSize + payloadSize);

            // first byte of fixed header 
            fixedHeaderSize = 1;

            int temp = remainingLength;
            // increase fixed header size based on remaining length 
            // (each remaining length byte can encode until 128) 
            do {
                fixedHeaderSize++;
                temp = temp / 128;
            } while (temp > 0);

            // allocate buffer for message 
            MQTTbuffer = new byte[fixedHeaderSize + varHeaderSize + payloadSize];

            // first fixed header byte 
            MQTTbuffer[index++] = (Constants.MQTT_MSG_CONNECT_TYPE << Constants.MSG_TYPE_OFFSET) | Constants.MQTT_MSG_CONNECT_FLAG_BITS; // [v.3.1.1] 

            // encode remaining length 
            index = this.encodeRemainingLength(remainingLength, MQTTbuffer, index);


            MQTTbuffer[index++] = 0; // MSB protocol name size 
            MQTTbuffer[index++] = Constants.PROTOCOL_NAME_SIZE; // LSB protocol name size 
            Array.Copy(Encoding.UTF8.GetBytes(Constants.PROTOCOL_NAME_V3_1_1), 0, MQTTbuffer, index, Constants.PROTOCOL_NAME_SIZE);
            index += Constants.PROTOCOL_NAME_SIZE;
            // protocol version 
            MQTTbuffer[index++] = Constants.PROTOCOL_VERSION_V3_1_1;

            // connect flags 
            byte connectFlags = 0x00;
            connectFlags |= (usernameUtf8 != null) ? (byte)(1 << Constants.USERNAME_FLAG_OFFSET) : (byte)0x00;
            connectFlags |= (passwordUtf8 != null) ? (byte)(1 << Constants.PASSWORD_FLAG_OFFSET) : (byte)0x00;
            connectFlags |= (cleanSession) ? (byte)(1 << Constants.CLEAN_SESSION_FLAG_OFFSET) : (byte)0x00;
            MQTTbuffer[index++] = connectFlags;

            // keep alive period 
            MQTTbuffer[index++] = (byte)(keepAlive / 256); // MSB 
            MQTTbuffer[index++] = (byte)(keepAlive % 256); // LSB 

            // client identifier 
            MQTTbuffer[index++] = (byte)((clientIdUtf8.Length >> 8) & 0x00FF); // MSB 
            MQTTbuffer[index++] = (byte)(clientIdUtf8.Length & 0x00FF); // LSB 
            Array.Copy(clientIdUtf8, 0, MQTTbuffer, index, clientIdUtf8.Length);
            index += clientIdUtf8.Length;

            // username 
            if (usernameUtf8 != null) {
                MQTTbuffer[index++] = (byte)(usernameUtf8.Length / 256); // MSB 
                MQTTbuffer[index++] = (byte)(usernameUtf8.Length % 256); // LSB 
                Array.Copy(usernameUtf8, 0, MQTTbuffer, index, usernameUtf8.Length);
                index += usernameUtf8.Length;
            }

            // password 
            if (passwordUtf8 != null) {
                MQTTbuffer[index++] = (byte)(passwordUtf8.Length / 256); // MSB 
                MQTTbuffer[index++] = (byte)(passwordUtf8.Length % 256); // LSB 
                Array.Copy(passwordUtf8, 0, MQTTbuffer, index, passwordUtf8.Length);
                index += passwordUtf8.Length;
            }

            wifi.WriteSocket(0, MQTTbuffer, MQTTbuffer.Length);
            Thread.Sleep(1000);
            wifi.ReadSocket(0, 1000);

        }

        public void Publish(String topic, String message) {
            int index = 0;
            int tmp = 0;
            int fixedHeader = 0;
            int varHeader = 0;
            int payload = 0;
            int remainingLength = 0;
            byte[] MQTTbuffer = null;

            // Encode the topic
            byte[] utf8Topic = Encoding.UTF8.GetBytes(topic);

            // Some error checking
            // Topic contains wildcards
            if ((topic.IndexOf('#') != -1) || (topic.IndexOf('+') != -1))
                throw new ArgumentException("Topic wildcards error");

            // Topic is too long or short
            if ((utf8Topic.Length > Constants.MAX_TOPIC_LENGTH) || (utf8Topic.Length < Constants.MIN_TOPIC_LENGTH))
                throw new ArgumentException("Topic length error");

            // Calculate the size of the var header
            varHeader += 2; // Topic Name Length (MSB, LSB)
            varHeader += utf8Topic.Length; // Length of the topic

            // Calculate the size of the fixed header
            fixedHeader++; // byte 1

            // Calculate the payload
            payload = message.Length;

            // Calculate the remaining size
            remainingLength = varHeader + payload;

            // Check that remaining length will fit into 4 encoded bytes
            if (remainingLength > Constants.MAXLENGTH)
                throw new ArgumentException("Message length error"); 

            // Add space for each byte we need in the fixed header to store the length
            tmp = remainingLength;
            while (tmp > 0) {
                fixedHeader++;
                tmp = tmp / 128;
            };
            // End of Fixed Header

            // Build buffer for message
            MQTTbuffer = new byte[fixedHeader + varHeader + payload];

            // Start of Fixed header
            // Publish (3.3)
            MQTTbuffer[index++] = Constants.MQTT_PUBLISH_TYPE;

            // Encode the fixed header remaining length
            // Add remaining length
            index = encodeRemainingLength(remainingLength, MQTTbuffer, index);
            // End Fixed Header

            // Start of Variable header
            // Length of topic name
            MQTTbuffer[index++] = (byte)(utf8Topic.Length / 256); // Length MSB
            MQTTbuffer[index++] = (byte)(utf8Topic.Length % 256); // Length LSB
            // Topic
            for (var i = 0; i < utf8Topic.Length; i++) {
                MQTTbuffer[index++] = utf8Topic[i];
            }
            // End of variable header

            // Start of Payload
            // Message (Length is accounted for in the fixed header)
            for (var i = 0; i < message.Length; i++) {
                MQTTbuffer[index++] = (byte)message[i];
            }
            // End of Payload

            wifi.WriteSocket(0, MQTTbuffer, MQTTbuffer.Length);
            Thread.Sleep(1000);
            wifi.ReadSocket(0, 1000);

        }

        public void Subscribe(String topic) {
            int fixedHeaderSize = 0;
            int varHeaderSize = 0;
            int payloadSize = 0;
            int remainingLength = 0;
            byte[] MQTTbuffer;
            int index = 0;
            int qosLevel = 0x01;

            // topics list empty
            if ((topic == null) || (topic.Length == 0))
                throw new ArgumentException("Topic error");

             // message identifier
            varHeaderSize += Constants.MESSAGE_ID_SIZE;

            byte[] topicsUtf8 = new byte[topic.Length];

            if ((topic.Length < Constants.MIN_TOPIC_LENGTH) || (topic.Length > Constants.MAX_TOPIC_LENGTH))
                throw new ArgumentException("Topic length error");

            topicsUtf8 = Encoding.UTF8.GetBytes(topic);
            payloadSize += 2; // topic size (MSB, LSB)
            payloadSize += topicsUtf8.Length;
            payloadSize++; // byte for QoS

            remainingLength += (varHeaderSize + payloadSize);

            // first byte of fixed header
            fixedHeaderSize = 1;

            int temp = remainingLength;
            // increase fixed header size based on remaining length
            // (each remaining length byte can encode until 128)
            do {
                fixedHeaderSize++;
                temp = temp / 128;
            } while (temp > 0);

            // allocate buffer for message
            MQTTbuffer = new byte[fixedHeaderSize + varHeaderSize + payloadSize];

            // first fixed header byte
            MQTTbuffer[index++] = (Constants.MQTT_MSG_SUBSCRIBE_TYPE << Constants.MSG_TYPE_OFFSET) | Constants.MQTT_MSG_SUBSCRIBE_FLAG_BITS; 

            // encode remaining length
            index = this.encodeRemainingLength(remainingLength, MQTTbuffer, index);

            // check message identifier assigned (SUBSCRIBE uses QoS Level 1, so message id is mandatory)
            int messageId = 1;
            MQTTbuffer[index++] = (byte)((messageId >> 8) & 0x00FF); // MSB
            MQTTbuffer[index++] = (byte)(messageId & 0x00FF); // LSB 

            MQTTbuffer[index++] = (byte)((topicsUtf8.Length >> 8) & 0x00FF); // MSB
            MQTTbuffer[index++] = (byte)(topicsUtf8.Length & 0x00FF); // LSB
            Array.Copy(topicsUtf8, 0, MQTTbuffer, index, topicsUtf8.Length);
            index += topicsUtf8.Length;

            // requested QoS
            MQTTbuffer[index++] = (byte)qosLevel;

            wifi.WriteSocket(0, MQTTbuffer, MQTTbuffer.Length);
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
