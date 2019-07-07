using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace new_scoket
{
	public class AsyncUserToken
	{
		public Socket Socket;

        //握手完成Handshake=2(0:正在握手；1：握手完成)
        public int Handshake = 0;

        //记录client的 WebSocket_Accep（还没用到）
        public string WebSocket_Accep;

        public Snake snake=new Snake();

        protected SocketAsyncEventArgs m_receiveEventArgs;
        public SocketAsyncEventArgs ReceiveEventArgs { get { return m_receiveEventArgs; } set { m_receiveEventArgs = value; } }
        protected byte[] m_asyncReceiveBuffer;
        protected SocketAsyncEventArgs m_sendEventArgs;
        public SocketAsyncEventArgs SendEventArgs { get { return m_sendEventArgs; } set { m_sendEventArgs = value; } }



        public AsyncUserToken(int asyncReceiveBufferSize)
        {
            m_receiveEventArgs = new SocketAsyncEventArgs();
            m_receiveEventArgs.UserToken = this;
            m_asyncReceiveBuffer = new byte[asyncReceiveBufferSize];
            m_receiveEventArgs.SetBuffer(m_asyncReceiveBuffer, 0, m_asyncReceiveBuffer.Length);
            m_sendEventArgs = new SocketAsyncEventArgs();
            m_sendEventArgs.UserToken = this;
        }
    }
}
