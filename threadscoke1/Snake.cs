using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace new_scoket
{
    public class Snake : Role
    {
        public String Name = "";

        public double Direc;

        public int State = 1;

        public int Eaten;

        //public Snake(List<Dictionary<string, object>> position_list, int state, string color, int direc)
        //{

        //    this.Position_list = position_list;
        //    this.State = state;
        //    this.Color = color;
        //    this.Direc = direc;
        //}

        //初始化
        public void init_snake()
        {
            Random r = new Random();

            var x = Convert.ToInt16(Math.Floor(r.NextDouble() * 80));
            var y = Convert.ToInt16(Math.Floor(r.NextDouble() * 30));

            List<Dictionary<string, object>> snake_list = new List<Dictionary<string, object>>();

            for (int i = 0; i < 3; i++)
            {
                var dict = new Dictionary<string, object>();
                dict["x"] = x + i;
                dict["y"] = y;
                snake_list.Add(dict);
            }

            Position_list = snake_list;
            //this.Color = "#EE4000";

            //随机颜色
            this.Color = "RGBA(" + Convert.ToInt16(r.NextDouble() * 250) + "," + Convert.ToInt16(r.NextDouble() * 250) + "," + +Convert.ToInt16(r.NextDouble() * 250) + "," + 0.5 + ")";
            this.State = 1;
            this.Direc = Math.PI / 2;
        }

        public void direc_snake(double direc)
        {
            this.Direc = direc;
        }

        public void move_snake(Food food, List<AsyncUserToken> m_asyncSocketList)
        {
            bool eating = false;

            //int new_food = 0;

            var flag_food_list = new List<Dictionary<string, object>>();

            Dictionary<string, object> next = new Dictionary<string, object>();

            var position_mvoe = Position_list[0] as Dictionary<string, object>;

            //计算方位(精确到2位)
            next["x"] = decimal.Round(Convert.ToDecimal(Convert.ToDouble(position_mvoe["x"]) - Math.Cos(Direc)), 2);
            next["y"] = decimal.Round(Convert.ToDecimal(Convert.ToDouble(position_mvoe["y"]) - Math.Sin(Direc)), 2);


            //游戏界面范围
            //if (Convert.ToDecimal(next["x"]) < 0 || Convert.ToDecimal(next["x"]) >= 130 || Convert.ToDecimal(next["y"]) < 0 || Convert.ToDecimal(next["y"]) >= 60)
            //{
            //    this.State = 0;
            //    return;
            //}

            //吃到食物
            foreach (var f in food.Position_list)
            {
                if (Math.Abs(Convert.ToInt16(next["x"]) - Convert.ToInt16(f["x"])) < 5 && Math.Abs(Convert.ToInt16(next["y"]) - Convert.ToInt16(f["y"])) < 5)
                {
                    flag_food_list.Add(f);

                    //这里要锁
                    this.Eaten++;
                    eating = true;
                    //如果只吃一个食物的话就return
                    //return;
                }
            }

            //碰到玩家
            //foreach (var player in m_asyncSocketList)
            //{
            //    var posttion_falg = 0;
            //    foreach (var postion in player.snake.Position_list)
            //    {
            //        //无计，暂时只能用位置判断是否是主人
            //        if (postion["x"] != position_mvoe["x"] && postion["y"] != position_mvoe["y"] && posttion_falg != 0)
            //        {
            //            if (Convert.ToInt16(next["x"]) == Convert.ToInt16(postion["x"]) && Convert.ToInt16(next["y"]) == Convert.ToInt16(postion["y"]))
            //            {
            //                State = 0;
            //                return;
            //            }
            //        }
            //        posttion_falg = 1;
            //    }
            //}

            //移除已经吃掉的食物
            foreach (var flag_food in flag_food_list)
            {
                food.Position_list.Remove(flag_food);
                food.reform_food();
            }

            //吃掉N个食物增加M个position
            if (eating == true && this.Eaten % 2 == 0)
            {
                var new_position = new Dictionary<string, object>();
                new_position["x"] = Convert.ToInt16(next["x"]);
                new_position["y"] = Convert.ToInt16(next["y"]);

                Position_list.Add(new_position);
            }

            //跟踪上一个部位
            for (var i = Position_list.Count() - 1; i > 0; i--)
            {
                Position_list[i] = Position_list[i - 1];
            }


            //吃到自己就死
            //if (show(this.Position_list, Convert.ToInt16(next["x"]), Convert.ToInt16(next["y"])) == false)
            //{
            //	this.State = 0;
            //	return;
            //}
            Position_list[0] = next;
        }
    }
}
