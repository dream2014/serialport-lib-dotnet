using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;
using SerialPortLib;
using NLog;

namespace MetaLib
{
    public class MetaMessage
    {

        #region Private fields
        /// <summary>
        /// new fields start
        /// 消息格式：
        /// 包头(1) 目标地址(4) 命令(1)	数据长度_H(1) 数据长度_L(1) 数据(n...) 和校验(1) 附加和校验(1)
        /// </summary>

        public const int msgMinLength = 10;     //消息包最小长度
        public const int msgNoPos = 5;          //消息号位置
        public const int dataLengthHigh = 6; //高位
        public const int dataLengthLow = 7; //低位
        public const int dataStartPos = 8; //数据开始位置

        /// <summary>
        /// new fields end
        /// </summary>

        private const Int16 messageNo = 0xFF;
        internal ulong seqNumber = 0;

        #endregion

        #region Public Fields

        /// <summary>
        /// Max resend attempts.
        /// </summary>
        public const int ResendAttemptsMax = 2;
        /// <summary>
        /// The send message timeout in milliseconds.
        /// </summary>
        public const int SendMessageTimeoutMs = 10000;
        /// <summary>
        /// The Z-Wave message frame header.
        /// </summary>
        public FrameHeader Header;

        /// <summary>
        /// The raw message bytes data.
        /// </summary>
        public readonly byte[] RawData;

        /// <summary>
        /// The timestamp.
        /// </summary>
        public readonly DateTime Timestamp = DateTime.UtcNow;

        /// <summary>
        /// The command class.
        /// </summary>
        public readonly CommandClass CommandClass = CommandClass.NotSet;

        #endregion

        #region Public members

        /// <summary>
        /// Initializes a new instance of the <see cref="ZWaveLib.ZWaveMessage"/> class.
        /// </summary>
        /// <param name="message">Message.</param>
        /// <param name="direction">Direction.</param>
        /// <param name="generateCallback">If set to <c>true</c> generate callback.</param>
        public MetaMessage(byte[] message)
        {
            Header = (FrameHeader)message[0];
            RawData = message;

            if (Header == FrameHeader.SOF)
            {
                if (message.Length >= msgMinLength)
                {
                    //Enum.TryParse<MessageType>(message[2].ToString(), out Type);
                    //Enum.TryParse<ZWaveFunction>(message[msgNoPos].ToString(), out Function);
                    Utility.logger.Debug("MetaMessage length greater than {0}", msgMinLength);
                }
                else
                {
                    Utility.logger.Debug("MetaMessage length less than {0}", msgMinLength);
                }
            }

            var dataLength = BitConverter.ToInt16(message, dataLengthHigh);
            Utility.logger.Debug("MetaMessage (RawData={0})", BitConverter.ToString(RawData));
            Utility.logger.Debug("MetaMessage (Header={0}, msgNo={1}, datalength={2}, msgLength={3})",
                Header, message[5], dataLength, message.Length);
        }

        #endregion

        #region Public static utility functions

        /// <summary>
        /// Builds a SendData request message.
        /// </summary>
        /// <returns>The send data request.</returns>
        /// <param name="nodeId">Node identifier.</param>
        /// <param name="request">Request.</param>
        public static byte[] BuildSendDataRequest(byte msgNo, byte[] request)
        {
            //格式：
            //header:   包头(1) 目标地址(4) 命令(1)	数据长度_H(1) 数据长度_L(1)	
            //data:     数据(n...)	
            //footer:   和校验(1)	附加和校验(1)

            var tmpByte = BitConverter.GetBytes(request.Length);

            byte[] header = new byte[] {
                (byte)FrameHeader.SOF, /* Start Of Frame */
                (byte)0 /*address 4 byte */,
                (byte)0 /*address 4 byte */,
                (byte)0 /*address 4 byte */,
                (byte)0 /*address 4 byte */,
                (byte)msgNo /*msgNO */,
                (byte)tmpByte[0], /* data length height byte */
                (byte)tmpByte[1] /* data length low byte */
            };

            byte[] message = new byte[header.Length + request.Length + 2];

            System.Array.Copy(header, 0, message, 0, header.Length);
            System.Array.Copy(request, 0, message, header.Length, request.Length);

            var footer = GetChecksum(message);
            System.Array.Copy(footer, 0, message, message.Length - footer.Length, footer.Length);

            return message;
        }

        /// <summary>
        /// Verifies the checksum.
        /// </summary>
        /// <returns><c>true</c>, if checksum was verifyed, <c>false</c> otherwise.</returns>
        /// <param name="data">Data.</param>
        public static bool VerifyChecksum(byte[] data)
        {
            var checksum = GetChecksum(data);
            if (checksum[0] == data[data.Length - 2] && checksum[1] == data[data.Length - 1])
            {
                return true;
            }

            return false;
        }

        //根据数据返回校验值，其中数据包含了两个校验值的长度。
        //return： byte[0]==sumcheck,byte[1]=addcheck
        public static byte[] GetChecksum(byte[] data)
        {
            var result = new byte[2] { 0x00, 0x00 };
            Byte sumCheck = 0;
            Byte addCheck = 0;
            int length = data.Length - 2;
            for (Byte i = 0; i < length; i++)
            {
                sumCheck += data[i];
                addCheck += sumCheck;
            }
            result[0] = sumCheck;
            result[1] = addCheck;
            return result;
        }

        #endregion
    }
}
