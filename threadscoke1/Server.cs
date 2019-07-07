using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace new_scoket
{
    class Server
    {
        private int m_numConnections;
        private int m_receiveBufferSize;
        public BufferManager m_bufferManager;
        const int opsToPreAlloc = 2;
        Socket listenSocket;

        public SocketAsyncUserTokenPool m_readWritePool;

        //用户Socketlist
        public List<AsyncUserToken> m_asyncSocketList = new List<AsyncUserToken>();
        int m_totalBytesRead;
        public int m_numConnectedSockets;
        Semaphore m_maxNumberAcceptedClients;


        public Server(int numConnections, int receiveBufferSize)
        {
            m_totalBytesRead = 0;
            m_numConnectedSockets = 0;
            m_numConnections = numConnections;
            m_receiveBufferSize = receiveBufferSize;

            m_bufferManager = new BufferManager(receiveBufferSize * numConnections * opsToPreAlloc,
                receiveBufferSize);

            m_readWritePool = new SocketAsyncUserTokenPool(numConnections);
            m_maxNumberAcceptedClients = new Semaphore(numConnections, numConnections);
        }


        public void Init()
        {
            m_bufferManager.InitBuffer();

            AsyncUserToken userToken;
            for (int i = 0; i < m_numConnections; i++)
            {
                userToken = new AsyncUserToken(m_receiveBufferSize);
                userToken.ReceiveEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IOReceive_Completed);
                userToken.SendEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IOSend_Completed);

                m_readWritePool.Push(userToken);
            }
        }

        public void Start(IPEndPoint localEndPoint)
        {
            // create the socket which listens for incoming connections
            listenSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(localEndPoint);

            Init();

            listenSocket.Listen(100);

            StartAccept(null);

            Console.WriteLine("Press any key to terminate the server process....");
            Console.ReadKey();
        }


        public void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            if (acceptEventArg == null)
            {
                acceptEventArg = new SocketAsyncEventArgs();
                acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptEventArg_Completed);
            }
            else
            {
                acceptEventArg.AcceptSocket = null;
            }

            m_maxNumberAcceptedClients.WaitOne();
            bool willRaiseEvent = listenSocket.AcceptAsync(acceptEventArg);
            if (!willRaiseEvent)
            {
                ProcessAccept(acceptEventArg);
            }


        }

        void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success) return;       //异步处理失败，不做处理
            Interlocked.Increment(ref m_numConnectedSockets);
            Console.WriteLine("Client connection accepted. There are {0} clients connected to the server",
                m_numConnectedSockets);


            AsyncUserToken token = m_readWritePool.Pop();
            ((AsyncUserToken)token).Socket = e.AcceptSocket;

            AsyncUserToken flag_usertoken = null;
            foreach (var m_asyncsocket in m_asyncSocketList)
            {
                if (((IPEndPoint)e.AcceptSocket.LocalEndPoint).Address.ToString() == ((IPEndPoint)m_asyncsocket.Socket.LocalEndPoint).Address.ToString())
                {
                    flag_usertoken = m_asyncsocket;
                }
            }

            if (flag_usertoken == null)
            {

                ((AsyncUserToken)token).snake.init_snake();

                //添加到usertokenlist
                m_asyncSocketList.Add(token);

                bool willRaiseEvent = e.AcceptSocket.ReceiveAsync(token.ReceiveEventArgs);
                if (!willRaiseEvent)
                {
                    lock (token)
                    {
                        ProcessReceive(token.ReceiveEventArgs);
                    }
                }

                StartAccept(e);

                var dic = new Dictionary<string, string>();
                dic["ip"] = ((IPEndPoint)token.Socket.LocalEndPoint).Address.ToString();
                PublicSend(token.SendEventArgs, WebSocketClass.PackData(Json.funcObj2JsonStr(dic)));
            }
            else
            {
                ((AsyncUserToken)token).snake= flag_usertoken.snake;

                //移除掉线的
                m_asyncSocketList.Remove(flag_usertoken);
                Interlocked.Decrement(ref m_numConnectedSockets);
                m_maxNumberAcceptedClients.Release();
                Console.WriteLine("A client has been disconnected from the server. There are {0} clients connected to the server", m_numConnectedSockets);
                //添加到usertokenlist
                m_asyncSocketList.Add(token);

                bool willRaiseEvent = e.AcceptSocket.ReceiveAsync(token.ReceiveEventArgs);
                if (!willRaiseEvent)
                {
                    lock (token)
                    {
                        ProcessReceive(token.ReceiveEventArgs);
                    }
                }

                StartAccept(e);
            }    
        }

        void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
                default:
                    throw new ArgumentException("The last operation completed on the socket was not a receive or send");
            }

        }

        void IOReceive_Completed(object sender, SocketAsyncEventArgs e)
        {
            AsyncUserToken userToken = e.UserToken as AsyncUserToken;
            lock (userToken)
            {
                ProcessReceive(e);
            }

        }

        void IOSend_Completed(object sender, SocketAsyncEventArgs e)
        {
            AsyncUserToken userToken = e.UserToken as AsyncUserToken;
            lock (userToken)
            {
                ProcessSend(e);
            }

        }

        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            // check if the remote host closed the connection
            AsyncUserToken token = (AsyncUserToken)e.UserToken;
            if (token.ReceiveEventArgs.BytesTransferred > 0 && token.ReceiveEventArgs.SocketError == SocketError.Success)
            {
                //如果是第一次链接的话就发送协议握手
                if (token.Handshake == 0)
                {
                    Console.WriteLine(WebSocketClass.AnalyticData(e.Buffer, e.Buffer.Length));
                    //e.Buffer:来自client的数据
                    var websocket_head = WebSocketClass.PackHandShakeData(e.Buffer);


                    //这个必须用ReceiveEventArgs发送数据
                    token.ReceiveEventArgs.SetBuffer(websocket_head, 0, websocket_head.Length);

                    token.Handshake++;

                    bool willRaiseEvent = token.Socket.SendAsync(token.ReceiveEventArgs);
                    if (!willRaiseEvent)
                    {
                        //ProcessSend(token.SendEventArgs);
                        // ProcessReceive(token.ReceiveEventArgs);
                    }
                }
                //握手完成
                else if (token.Handshake == 1 && e.SocketError == SocketError.Success)
                {
                    var websocket_head = WebSocketClass.PackHandShakeData(e.Buffer);

                    //这里应该要验证Sec-WebSocket-Accep(关闭时也要)
                    token.Handshake++;

                    //如果是握手完成的数据就没必要触发发送事件
                    bool willRaiseEvent = token.Socket.ReceiveAsync(token.ReceiveEventArgs);
                    if (!willRaiseEvent)
                    {
                        ProcessReceive(token.ReceiveEventArgs);
                    }
                }
                else
                {
                    var client_str = WebSocketClass.AnalyticData(e.Buffer, e.Buffer.Length);

                    var snake_info = client_str;

                    //判断是否是double
                    double judge = 0;
                    if (Double.TryParse(snake_info, out judge))
                    {
                        token.snake.direc_snake(judge);
                    }

                    //如果是接受的是普通数据就没必要触发发送事件
                    bool willRaiseEvent = token.Socket.ReceiveAsync(token.ReceiveEventArgs);
                    if (!willRaiseEvent)
                    {
                        ProcessReceive(token.ReceiveEventArgs);
                    }
                }
            }
            else
            {
                CloseClientSocket(e);
            }
        }

        public void PublicSend(SocketAsyncEventArgs e, byte[] buffer)
        {
            AsyncUserToken token = (AsyncUserToken)e.UserToken;
            if (e.SocketError == SocketError.Success)
            {
                try
                {
                    lock (e)
                    {
                        e.SetBuffer(buffer, 0, buffer.Length);

                        bool willRaiseEvent = token.Socket.SendAsync(e);

                        if (!willRaiseEvent)
                        {
                            ProcessSend(e);
                        }
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("发送失败");
                }


            }
        }

        private void ProcessSend(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = (AsyncUserToken)e.UserToken;
            if (e.SocketError == SocketError.Success)
            {
                //bool willRaiseEvent = token.Socket.ReceiveAsync(token.ReceiveEventArgs);
                // if (!willRaiseEvent)
                // {
                //ProcessReceive(token.ReceiveEventArgs);
                // }
                //ProcessSend(e);
            }
            else
            {
                CloseClientSocket(e);
            }
        }

        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            //移除掉线的。。后来增加了重连
            //AsyncUserToken token = e.UserToken as AsyncUserToken;

            //try
            //{
            //    token.Socket.Shutdown(SocketShutdown.Send);
            //}
            //catch (Exception) { }
            //token.Socket.Close();

            //Interlocked.Decrement(ref m_numConnectedSockets);
            //m_maxNumberAcceptedClients.Release();
            //Console.WriteLine("A client has been disconnected from the server. There are {0} clients connected to the server", m_numConnectedSockets);

            ////移除掉线的
            //token.snake.State = 0;

            //m_readWritePool.Push(token);
        }
    }
}
