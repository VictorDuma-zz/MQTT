using System;
using System.Collections;
using System.Diagnostics;
using System.Text;
using System.Threading;
using GHIElectronics.TinyCLR.Devices.SerialCommunication;
using GHIElectronics.TinyCLR.Pins;

namespace AMW007 {

    static class Constants
    {

        // These have been scaled down to the hardware
        // Maximum values are commented out - you can 
        // adjust, but keep in mind the limits of the hardware

        public const int MQTTPROTOCOLVERSION = 4;
        //public const int MAXLENGTH = 268435455; // 256MB
        public const int MAXLENGTH = 10240; // 10K
        public const int MAX_CLIENTID = 23;
        public const int MIN_CLIENTID = 1;
        public const int MAX_KEEPALIVE = 65535;
        public const int MIN_KEEPALIVE = 0;
        //public const int MAX_USERNAME = 65535;
        //public const int MAX_PASSWORD = 65535;
        public const int MAX_USERNAME = 12;
        public const int MAX_PASSWORD = 12;
        //public const int MAX_TOPIC_LENGTH = 32767;
        public const int MAX_TOPIC_LENGTH = 256;
        public const int MIN_TOPIC_LENGTH = 1;
        public const int MAX_MESSAGEID = 65535;

        // Error Codes
        public const int CLIENTID_LENGTH_ERROR = 1;
        public const int KEEPALIVE_LENGTH_ERROR = 1;
        public const int MESSAGE_LENGTH_ERROR = 1;
        public const int TOPIC_LENGTH_ERROR = 1;
        public const int TOPIC_WILDCARD_ERROR = 1;
        public const int USERNAME_LENGTH_ERROR = 1;
        public const int PASSWORD_LENGTH_ERROR = 1;
        public const int CONNECTION_ERROR = 1;
        public const int ERROR = 1;
        public const int SUCCESS = 0;

        public const int CONNECTION_OK = 0;

        public const int CONNACK_LENGTH = 4;
        public const int PINGRESP_LENGTH = 2;

        public const byte MQTT_CONN_OK = 0x00;  // Connection Accepted
        public const byte MQTT_CONN_BAD_PROTOCOL_VERSION = 0x01;  // Connection Refused: unacceptable protocol version
        public const byte MQTT_CONN_BAD_IDENTIFIER = 0x02;  // Connection Refused: identifier rejected
        public const byte MQTT_CONN_SERVER_UNAVAILABLE = 0x03;  // Connection Refused: server unavailable
        public const byte MQTT_CONN_BAD_AUTH = 0x04;  //  Connection Refused: bad user name or password
        public const byte MQTT_CONN_NOT_AUTH = 0x05;  //  Connection Refused: not authorized

        // Message types
        public const byte MQTT_CONNECT_TYPE = 0x10;
        public const byte MQTT_CONNACK_TYPE = 0x20;
        public const byte MQTT_PUBLISH_TYPE = 0x30;
        public const byte MQTT_PING_REQ_TYPE = 0xc0;
        public const byte MQTT_PING_RESP_TYPE = 0xd0;
        public const byte MQTT_DISCONNECT_TYPE = 0xe0;
        public const byte MQTT_SUBSCRIBE_TYPE = 0x82;
        public const byte MQTT_UNSUBSCRIBE_TYPE = 0xa2;

        // Flags
        public const int CLEAN_SESSION_FLAG = 0x02;
        public const int USING_USERNAME_FLAG = 0x80;
        public const int USING_PASSWORD_FLAG = 0x40;
        public const int CONTINUATION_BIT = 0x80;
        /// <summary>
        /// //////////////////////////////////////////////////////////////////////
        /// </summary>
        /// 
        internal const byte MESSAGE_ID_SIZE = 2;
        internal const byte MQTT_MSG_CONNECT_FLAG_BITS = 0x00;
        internal const byte MQTT_MSG_CONNACK_FLAG_BITS = 0x00;
        internal const byte MQTT_MSG_PUBLISH_FLAG_BITS = 0x00; // just defined as 0x00 but depends on publish props (dup, qos, retain) 
        internal const byte MQTT_MSG_PUBACK_FLAG_BITS = 0x00;
        internal const byte MQTT_MSG_PUBREC_FLAG_BITS = 0x00;
        internal const byte MQTT_MSG_PUBREL_FLAG_BITS = 0x02;
        internal const byte MQTT_MSG_PUBCOMP_FLAG_BITS = 0x00;
        internal const byte MQTT_MSG_SUBSCRIBE_FLAG_BITS = 0x02;
        internal const byte MQTT_MSG_SUBACK_FLAG_BITS = 0x00;
        internal const byte MQTT_MSG_UNSUBSCRIBE_FLAG_BITS = 0x02;
        internal const byte MQTT_MSG_UNSUBACK_FLAG_BITS = 0x00;
        internal const byte MQTT_MSG_PINGREQ_FLAG_BITS = 0x00;
        internal const byte MQTT_MSG_PINGRESP_FLAG_BITS = 0x00;
        internal const byte MQTT_MSG_DISCONNECT_FLAG_BITS = 0x00;
        internal const byte MSG_TYPE_MASK = 0xF0;
        internal const byte MSG_TYPE_OFFSET = 0x04;
        internal const byte MSG_TYPE_SIZE = 0x04;
        internal const byte MSG_FLAG_BITS_MASK = 0x0F;      // [v3.1.1]
        internal const byte MSG_FLAG_BITS_OFFSET = 0x00;    // [v3.1.1]
        internal const byte MSG_FLAG_BITS_SIZE = 0x04;      // [v3.1.1]
        internal const byte DUP_FLAG_MASK = 0x08;
        internal const byte DUP_FLAG_OFFSET = 0x03;
        internal const byte DUP_FLAG_SIZE = 0x01;
        internal const byte QOS_LEVEL_MASK = 0x06;
        internal const byte QOS_LEVEL_OFFSET = 0x01;
        internal const byte QOS_LEVEL_SIZE = 0x02;
        internal const byte RETAIN_FLAG_MASK = 0x01;
        internal const byte RETAIN_FLAG_OFFSET = 0x00;
        internal const byte RETAIN_FLAG_SIZE = 0x01;
        internal const byte MQTT_MSG_CONNECT_TYPE = 0x01;
        internal const byte MQTT_MSG_CONNACK_TYPE = 0x02;
        internal const byte MQTT_MSG_PUBLISH_TYPE = 0x03;
        internal const byte MQTT_MSG_PUBACK_TYPE = 0x04;
        internal const byte MQTT_MSG_PUBREC_TYPE = 0x05;
        internal const byte MQTT_MSG_PUBREL_TYPE = 0x06;
        internal const byte MQTT_MSG_PUBCOMP_TYPE = 0x07;
        internal const byte MQTT_MSG_SUBSCRIBE_TYPE = 0x08;
        internal const byte MQTT_MSG_SUBACK_TYPE = 0x09;
        internal const byte MQTT_MSG_UNSUBSCRIBE_TYPE = 0x0A;
        internal const byte MQTT_MSG_UNSUBACK_TYPE = 0x0B;
        internal const byte MQTT_MSG_PINGREQ_TYPE = 0x0C;
        internal const byte MQTT_MSG_PINGRESP_TYPE = 0x0D;
        internal const byte MQTT_MSG_DISCONNECT_TYPE = 0x0E;
        internal const string PROTOCOL_NAME_V3_1_1 = "MQTT"; // [v.3.1.1]

        // max length for client id (removed in 3.1.1)
        internal const int CLIENT_ID_MAX_LENGTH = 23;

        // variable header fields
        internal const byte PROTOCOL_NAME_LEN_SIZE = 2;
        internal const byte PROTOCOL_NAME_V3_1_SIZE = 6;
        internal const byte PROTOCOL_NAME_V3_1_1_SIZE = 4; // [v.3.1.1]
        internal const byte PROTOCOL_VERSION_SIZE = 1;
        internal const byte CONNECT_FLAGS_SIZE = 1;
        internal const byte KEEP_ALIVE_TIME_SIZE = 2;

        internal const byte PROTOCOL_VERSION_V3_1 = 0x03;
        internal const byte PROTOCOL_VERSION_V3_1_1 = 0x04; // [v.3.1.1]
        internal const ushort KEEP_ALIVE_PERIOD_DEFAULT = 60; // seconds
        internal const ushort MAX_KEEP_ALIVE = 65535; // 16 bit

        // connect flags
        internal const byte USERNAME_FLAG_MASK = 0x80;
        internal const byte USERNAME_FLAG_OFFSET = 0x07;
        internal const byte USERNAME_FLAG_SIZE = 0x01;
        internal const byte PASSWORD_FLAG_MASK = 0x40;
        internal const byte PASSWORD_FLAG_OFFSET = 0x06;
        internal const byte PASSWORD_FLAG_SIZE = 0x01;
        internal const byte WILL_RETAIN_FLAG_MASK = 0x20;
        internal const byte WILL_RETAIN_FLAG_OFFSET = 0x05;
        internal const byte WILL_RETAIN_FLAG_SIZE = 0x01;
        internal const byte WILL_QOS_FLAG_MASK = 0x18;
        internal const byte WILL_QOS_FLAG_OFFSET = 0x03;
        internal const byte WILL_QOS_FLAG_SIZE = 0x02;
        internal const byte WILL_FLAG_MASK = 0x04;
        internal const byte WILL_FLAG_OFFSET = 0x02;
        internal const byte WILL_FLAG_SIZE = 0x01;
        internal const byte CLEAN_SESSION_FLAG_MASK = 0x02;
        internal const byte CLEAN_SESSION_FLAG_OFFSET = 0x01;
        internal const byte CLEAN_SESSION_FLAG_SIZE = 0x01;
        // [v.3.1.1] lsb (reserved) must be now 0
        internal const byte RESERVED_FLAG_MASK = 0x01;
        internal const byte RESERVED_FLAG_OFFSET = 0x00;
        internal const byte RESERVED_FLAG_SIZE = 0x01;
    }

    class MQTT {

        static AMW007Interface netif;

        public void Connect(String clientID, int keepAlive, bool cleanSession, String username, String password)
        {
            int fixedHeaderSize = 0;
            int varHeaderSize = 0;
            int payloadSize = 0;
            int remainingLength = 0;
            byte[] MQTTbuffer;
            int index = 0;

            var serial = SerialDevice.FromId(FEZ.UartPort.Usart1);
            serial.BaudRate = 115200;
            serial.ReadTimeout = TimeSpan.Zero;

            netif = new AMW007Interface(serial);
            //netif.JoinNetwork("GHI", "ghi555wifi.");
            netif.Run();

            netif.OpenSocket("ghi-test-iot.azure-devices.net", 8883); //"192.168.1.152", 1883  "ghi-test-iot.azure-devices.net", 8883

            byte[] clientIdUtf8 = Encoding.UTF8.GetBytes(clientID);
            byte[] usernameUtf8 = Encoding.UTF8.GetBytes(username);
            byte[] passwordUtf8 = Encoding.UTF8.GetBytes(password);

            varHeaderSize += (Constants.PROTOCOL_NAME_LEN_SIZE + Constants.PROTOCOL_NAME_V3_1_1_SIZE);

            // protocol level field size 
            varHeaderSize += Constants.PROTOCOL_VERSION_SIZE;
            // connect flags field size 
            varHeaderSize += Constants.CONNECT_FLAGS_SIZE;
            // keep alive timer field size 
            varHeaderSize += Constants.KEEP_ALIVE_TIME_SIZE;

            // client identifier field size 
            payloadSize += clientIdUtf8.Length + 2;
            // username field size 
            payloadSize += usernameUtf8.Length + 2;
            // password field size 
            payloadSize += passwordUtf8.Length + 2;

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
            MQTTbuffer[index++] = Constants.PROTOCOL_NAME_V3_1_1_SIZE; // LSB protocol name size 
            Array.Copy(Encoding.UTF8.GetBytes(Constants.PROTOCOL_NAME_V3_1_1), 0, MQTTbuffer, index, Constants.PROTOCOL_NAME_V3_1_1_SIZE);
            index += Constants.PROTOCOL_NAME_V3_1_1_SIZE;
            // protocol version 
            MQTTbuffer[index++] = Constants.PROTOCOL_VERSION_V3_1_1;

            // connect flags 
            byte connectFlags = 0x00;
            connectFlags |= (usernameUtf8 != null) ? (byte)(1 << Constants.USERNAME_FLAG_OFFSET) : (byte)0x00;
            connectFlags |= (passwordUtf8 != null) ? (byte)(1 << Constants.PASSWORD_FLAG_OFFSET) : (byte)0x00;
            MQTTbuffer[index++] = connectFlags;

            // keep alive period 
            MQTTbuffer[index++] = (byte)((keepAlive >> 8) & 0x00FF); // MSB 
            MQTTbuffer[index++] = (byte)(keepAlive & 0x00FF); // LSB 

            // client identifier 
            MQTTbuffer[index++] = (byte)((clientIdUtf8.Length >> 8) & 0x00FF); // MSB 
            MQTTbuffer[index++] = (byte)(clientIdUtf8.Length & 0x00FF); // LSB 
            Array.Copy(clientIdUtf8, 0, MQTTbuffer, index, clientIdUtf8.Length);
            index += clientIdUtf8.Length;

            // username 
            if (usernameUtf8 != null) {
                MQTTbuffer[index++] = (byte)((usernameUtf8.Length >> 8) & 0x00FF); // MSB 
                MQTTbuffer[index++] = (byte)(usernameUtf8.Length & 0x00FF); // LSB 
                Array.Copy(usernameUtf8, 0, MQTTbuffer, index, usernameUtf8.Length);
                index += usernameUtf8.Length;
            }

            // password 
            if (passwordUtf8 != null) {
                MQTTbuffer[index++] = (byte)((passwordUtf8.Length >> 8) & 0x00FF); // MSB 
                MQTTbuffer[index++] = (byte)(passwordUtf8.Length & 0x00FF); // LSB 
                Array.Copy(passwordUtf8, 0, MQTTbuffer, index, passwordUtf8.Length);
                index += passwordUtf8.Length;
            }

            netif.WriteSocket(0, MQTTbuffer, MQTTbuffer.Length);
            Thread.Sleep(1000);
            netif.ReadSocket(0, 1000);
        }

        public void Publish(String topic, String message) {
            int index = 0;
            int tmp = 0;
            int fixedHeader = 0;
            int varHeader = 0;
            int payload = 0;
            int remainingLength = 0;
            byte[] buffer = null;

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
            while (tmp > 0)
            {
                fixedHeader++;
                tmp = tmp / 128;
            };
            // End of Fixed Header

            // Build buffer for message
            buffer = new byte[fixedHeader + varHeader + payload];

            // Start of Fixed header
            // Publish (3.3)
            buffer[index++] = Constants.MQTT_PUBLISH_TYPE;

            // Encode the fixed header remaining length
            // Add remaining length
            index = encodeRemainingLength(remainingLength, buffer, index);
            // End Fixed Header

            // Start of Variable header
            // Length of topic name
            buffer[index++] = (byte)(utf8Topic.Length / 256); // Length MSB
            buffer[index++] = (byte)(utf8Topic.Length % 256); // Length LSB
            // Topic
            for (var i = 0; i < utf8Topic.Length; i++)
            {
                buffer[index++] = utf8Topic[i];
            }
            // End of variable header

            // Start of Payload
            // Message (Length is accounted for in the fixed header)
            for (var i = 0; i < message.Length; i++)
            {
                buffer[index++] = (byte)message[i];
            }
            // End of Payload

            netif.WriteSocket(0, buffer, buffer.Length);
            Thread.Sleep(1000);
            netif.ReadSocket(0, 1000);

        }

        public void Subscribe(String topic) {
            int fixedHeaderSize = 0;
            int varHeaderSize = 0;
            int payloadSize = 0;
            int remainingLength = 0;
            byte[] buffer;
            int index = 0;
            int qosLevel = 1;

            // topics list empty
            if ((topic == null) || (topic.Length == 0))
                throw new ArgumentException("Topic error");

             // message identifier
            varHeaderSize += Constants.MESSAGE_ID_SIZE;

            int topicIdx = 0;
            byte[][] topicsUtf8 = new byte[topic.Length][];

            for (topicIdx = 0; topicIdx < topic.Length; topicIdx++)
            {
                // check topic length
                if ((topic.Length < Constants.MIN_TOPIC_LENGTH) || (topic.Length > Constants.MAX_TOPIC_LENGTH))
                    throw new ArgumentException("Topic length error");

                topicsUtf8[topicIdx] = Encoding.UTF8.GetBytes(topic);
                payloadSize += 2; // topic size (MSB, LSB)
                payloadSize += topicsUtf8[topicIdx].Length;
                payloadSize++; // byte for QoS
            }

            remainingLength += (varHeaderSize + payloadSize);

            // first byte of fixed header
            fixedHeaderSize = 1;

            int temp = remainingLength;
            // increase fixed header size based on remaining length
            // (each remaining length byte can encode until 128)
            do
            {
                fixedHeaderSize++;
                temp = temp / 128;
            } while (temp > 0);

            // allocate buffer for message
            buffer = new byte[fixedHeaderSize + varHeaderSize + payloadSize];

            // first fixed header byte
            buffer[index++] = (Constants.MQTT_MSG_SUBSCRIBE_TYPE << Constants.MSG_TYPE_OFFSET) | Constants.MQTT_MSG_SUBSCRIBE_FLAG_BITS; // [v.3.1.1]

            // encode remaining length
            index = this.encodeRemainingLength(remainingLength, buffer, index);

            // check message identifier assigned (SUBSCRIBE uses QoS Level 1, so message id is mandatory)
            int messageId = 0;
            buffer[index++] = (byte)((messageId >> 8) & 0x00FF); // MSB
            buffer[index++] = (byte)(messageId & 0x00FF); // LSB 

            topicIdx = 0;
            for (topicIdx = 0; topicIdx < topic.Length; topicIdx++)
            {
                // topic name
                buffer[index++] = (byte)((topicsUtf8[topicIdx].Length >> 8) & 0x00FF); // MSB
                buffer[index++] = (byte)(topicsUtf8[topicIdx].Length & 0x00FF); // LSB
                Array.Copy(topicsUtf8[topicIdx], 0, buffer, index, topicsUtf8[topicIdx].Length);
                index += topicsUtf8[topicIdx].Length;

                // requested QoS
                buffer[index++] = (byte)qosLevel;
            }

            netif.WriteSocket(0, buffer, buffer.Length);
            Thread.Sleep(1000);
            netif.ReadSocket(0, 1000);

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
