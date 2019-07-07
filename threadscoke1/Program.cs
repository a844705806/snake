using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Collections;
using System.Net.NetworkInformation;
using System.Net.WebSockets;

namespace new_scoket
{
    class Program
    {
        public static Food food = new Food();

        private static int myProt = 4017;

        static int max_player = 40;

        //sever
        public static Server sever = new Server(max_player, 1024);

        static void Main(string[] args)
        {
            //IPAddress ip = IPAddress.Parse("192.168.0.185");

            IPAddress[] ipList = Dns.GetHostEntry(Environment.MachineName).AddressList;
            IPAddress ip = ipList[ipList.Length - 1];

            ip = IPAddress.Parse("127.0.0.1");
            //服务器
            //ip = IPAddress.Parse("172.27.0.12");

            IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] ipEndPoints = ipProperties.GetActiveTcpListeners();

            foreach (IPEndPoint endPoint in ipEndPoints)
            {
                if (endPoint.Port == myProt)
                {
                    Console.WriteLine(myProt + "这个端口已经被使用");
                    Console.ReadLine();
                    return;
                }
            }

            Console.WriteLine("本地地址为ip为" + ip);

            Thread Listen_start = new Thread(delegate () { sever.Start(new IPEndPoint(ip, myProt)); });
            Listen_start.Start();

            //初始化食物
            food.init_food();

            //发送玩家的信息
            Thread Send_thread = new Thread(wsSend_thread);
            Send_thread.Start();


            //移除状态为0的玩家
            Thread Remove_snake = new Thread(ws_remove_thread);
            Remove_snake.Start();

            //增加食物
            Thread Reform_thread = new Thread(ws_reform_thread);
            Reform_thread.Start();

            Console.WriteLine("等待");


        }

        //蛇的线程
        private static void wsSend_thread()
        {
            while (true)
            {
                if (sever.m_asyncSocketList.Count > 0)
                {
                    List<Dictionary<string, object>> snake_list = new List<Dictionary<string, object>>();
                    foreach (var socket in sever.m_asyncSocketList)
                    {
                        var snakeEntity = socket.snake;

                        if (snakeEntity != null)
                        {
                            if (snakeEntity.State == 1)
                            {
                                //这要线程执行
                                snakeEntity.move_snake(food, sever.m_asyncSocketList);

                                if (snakeEntity.Position_list.Count() >= 3)
                                {
                                    var snake_dict = new Dictionary<string, object>();
                                    snake_dict.Add("snake", snakeEntity.Position_list);
                                    snake_dict.Add("color", snakeEntity.Color);
                                    snake_dict.Add("ip", ((IPEndPoint)socket.Socket.LocalEndPoint).Address.ToString());
                                    snake_list.Add(snake_dict);
                                }
                            }
                        }
                    }

                    wsSend_snakes(Json.funcObj2JsonStr(snake_list));

                    Thread.Sleep(300);
                }
            }
        }

        //发送信息给蛇
        private static void wsSend_snakes(string str)
        {
            foreach (var socket in sever.m_asyncSocketList)
            {
                var snakeEntity = socket.snake;
                if (snakeEntity != null)
                {
                    if (snakeEntity.State == 1)
                    {
                        if (socket.SendEventArgs.SocketError == SocketError.Success)
                        {
                                sever.PublicSend(socket.SendEventArgs, WebSocketClass.PackData(str));
                        }
                    }
                }
            }
        }

        //移除状态为0的玩家
        private static void ws_remove_thread()
        {
            while (true)
            {
                if (sever.m_asyncSocketList.Count > 0)
                {
                    var new_m_asyncSocketList = new List<AsyncUserToken>();
                    foreach (var socket in sever.m_asyncSocketList)
                    {
                        var snakeEntity = socket.snake;
                        if (snakeEntity.State == 0)
                        {
                            socket.Socket.Close();
                            //sever.m_asyncSocketList.Remove(socket);
                            new_m_asyncSocketList.Add(socket);
                            //sever.m_readWritePool.Push(socket);
                        }
                    }

                    if (new_m_asyncSocketList.Count > 0)
                    {
                        foreach (var socket in new_m_asyncSocketList)
                        {
                            //移除掉线的
                            sever.m_asyncSocketList.Remove(socket);
                        }
                    }
                }
                Thread.Sleep(1000);
            }
        }

        //增加食物
        private static void ws_reform_thread()
        {
            while (true)
            {
                if (food.Position_list.Count < 30)
                {
                    food.reform_food();
                    // send_food_thread();
                }
                send_food_thread();
                Thread.Sleep(3000);
            }
        }

        //食物的线程
        private static void send_food_thread()
        {
            if (sever.m_asyncSocketList.Count > 0)
            {
                if (food.Position_list.Count > 0)
                {
                    var food_list = new List<Dictionary<string, object>>();
                    var food_dict = new Dictionary<string, object>();
                    food_dict.Add("food", food.Position_list);
                    food_dict.Add("color", food.Color);
                    food_dict.Add("quantity", sever.m_numConnectedSockets);
                    //food_dict.Add("maxplayer", max_player);
                    food_list.Add(food_dict);

                    wsSend_snakes(Json.funcObj2JsonStr(food_list));
                }
            }
        }


    }
}