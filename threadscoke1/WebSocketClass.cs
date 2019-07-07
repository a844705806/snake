using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace new_scoket
{
	public class WebSocketClass
	{
		//websocket协议
		public static byte[] PackHandShakeData(byte[] r)
		{
			Encoding.ASCII.GetString(r, 0, r.Count());

			var req_SecWebSocketKey = GetSecKeyAccetp(r);

			SHA1 sha1 = new SHA1CryptoServiceProvider();
			byte[] bytes_sha1_in = Encoding.UTF8.GetBytes(req_SecWebSocketKey + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11");
			byte[] bytes_sha1_out = sha1.ComputeHash(bytes_sha1_in);
			string str_sha1_out = Convert.ToBase64String(bytes_sha1_out);


			var responseBuilder = new StringBuilder();
			responseBuilder.Append("HTTP/1.1 101 Switching Protocol" + Environment.NewLine);
			responseBuilder.Append("Upgrade: WebSocket" + Environment.NewLine);
			responseBuilder.Append("Connection: Upgrade" + Environment.NewLine);
			responseBuilder.Append("Sec-WebSocket-Accept: " + str_sha1_out + Environment.NewLine + Environment.NewLine);

            return Encoding.UTF8.GetBytes(responseBuilder.ToString());

        }

		/// <summary>
		/// 生成Sec-WebSocket-Accept
		/// </summary>
		/// <param name="handShakeText">客户端握手信息</param>
		/// <returns>Sec-WebSocket-Accept</returns>
		public static string GetSecKeyAccetp(byte[] handShakeBytes)
		{
			string handShakeText = Encoding.UTF8.GetString(handShakeBytes, 0, handShakeBytes.Length);
			string key = string.Empty;
			Regex r = new Regex(@"Sec\-WebSocket\-Key:(.*?)\r\n");
			Match m = r.Match(handShakeText);
			if (m.Groups.Count != 0)
			{
				key = Regex.Replace(m.Value, @"Sec\-WebSocket\-Key:(.*?)\r\n", "$1").Trim();
			}
			return key;
		}

		/// <summary>
		/// 打包服务器数据
		/// </summary>
		/// <param name="message">数据</param>
		/// <returns>数据包</returns>
		public static byte[] PackData(string message)
		{
			byte[] contentBytes = null;
			byte[] temp = Encoding.UTF8.GetBytes(message);

			if (temp.Length < 126)
			{
				contentBytes = new byte[temp.Length + 2];
				contentBytes[0] = 0x81;
				contentBytes[1] = (byte)temp.Length;
				Array.Copy(temp, 0, contentBytes, 2, temp.Length);
			}
			else if (temp.Length < 0xFFFF)
			{
                byte[] ushortlen = BitConverter.GetBytes((short)temp.Length);


                //0-3是标识
                contentBytes = new byte[temp.Length + 4];
                contentBytes[0] = 0x81;//结束针,
                contentBytes[1] = 126;
                contentBytes[2] = ushortlen[1];
                contentBytes[3] = ushortlen[0];
                //之前用这种方法,但是不行
                //contentBytes[2] = (byte)(temp.Length & 0xFF);
                //contentBytes[3] = (byte)(temp.Length >> 8 & 0xFF);
                Array.Copy(temp, 0, contentBytes, 4, temp.Length);
            }
			else
			{
				// 暂不处理超长内容  
			}

			return contentBytes;
		}

        /// <summary>
        /// 解析客户端数据包
        /// </summary>
        /// <param name="recBytes">服务器接收的数据包</param>
        /// <param name="recByteLength">有效数据长度</param>
        /// <returns></returns>
        public static string AnalyticData(byte[] recBytes, int recByteLength)
        {
            if (recByteLength < 2) { return string.Empty; }

            bool fin = (recBytes[0] & 0x80) == 0x80; // 1bit，1表示最后一帧  
            if (!fin)
            {
                return string.Empty;// 超过一帧暂不处理 
            }

            bool mask_flag = (recBytes[1] & 0x80) == 0x80; // 是否包含掩码  
            if (!mask_flag)
            {
                return string.Empty;// 不包含掩码的暂不处理
            }

            int payload_len = recBytes[1] & 0x7F; // 数据长度  

            byte[] masks = new byte[4];
            byte[] payload_data;

            if (payload_len == 126)
            {
                Array.Copy(recBytes, 4, masks, 0, 4);
                payload_len = (UInt16)(recBytes[2] << 8 | recBytes[3]);
                payload_data = new byte[payload_len];
                Array.Copy(recBytes, 8, payload_data, 0, payload_len);

            }
            else if (payload_len == 127)
            {
                Array.Copy(recBytes, 10, masks, 0, 4);
                byte[] uInt64Bytes = new byte[8];
                for (int i = 0; i < 8; i++)
                {
                    uInt64Bytes[i] = recBytes[9 - i];
                }
                UInt64 len = BitConverter.ToUInt64(uInt64Bytes, 0);

                payload_data = new byte[len];
                for (UInt64 i = 0; i < len; i++)
                {
                    payload_data[i] = recBytes[i + 14];
                }
            }
            else
            {
                Array.Copy(recBytes, 2, masks, 0, 4);
                payload_data = new byte[payload_len];
                Array.Copy(recBytes, 6, payload_data, 0, payload_len);

            }

            for (var i = 0; i < payload_len; i++)
            {
                payload_data[i] = (byte)(payload_data[i] ^ masks[i % 4]);
            }

            return Encoding.UTF8.GetString(payload_data);
        }
    }
}
