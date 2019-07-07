<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="snake.aspx.cs" Inherits="snake.snake" %>

<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width,initial-scale=1,minimum-scale=1,maximum-scale=1,user-scalable=no" />
    <title></title>
    <script src="http://libs.baidu.com/jquery/2.0.0/jquery.min.js" type="text/javascript" charset="utf-8"></script>
    <script src="//cdn.bootcss.com/jquery-cookie/1.4.1/jquery.cookie.min.js"></script>
    <script src="js/nipplejs.js" charset="utf-8"></script>

    <style>
        body {
            padding: 0;
            margin: 0;
            overflow: hidden;
        }

        #cav {
            display: block;
            background-color: rgba(0,0,0,0);
            /*margin: auto;*/
            border: 1px solid;
        }
    </style>

    <script type="text/javascript">
        var game = null;
        var canvas = null;
        var pen = null;
        var fpen = null;
        var ballpen = null;
        var width = 1300;
        var height = 600;
        //角度
        var angle = 0;

        //防止重复执行eatfood方法
        var repeat_food = null;
        //
        var color = null;

        var ip = null;
        //连接状态：0未连接 1已连接 -1异常
        var state = 0;

        //服务器数据
        var service_data = null;

        var snake_player = [];

        var food = [];

        ballpen = function (x, y) {
            this.x = x;
            this.y = y;
            this.color = "#000000";
            this.cacheCanvas = document.createElement("canvas");
            this.cacheCtx = this.cacheCanvas.getContext("2d");
            this.cacheCanvas.width = 1300;
            this.cacheCanvas.height = 600;
            this.cacheCtx.save();
            this.cacheCtx.beginPath();
            this.cacheCtx.strokeStyle = this.color;
            //圆形的食物，后改为正方形
            //this.cacheCtx.arc(this.x, this.y, 5, 0, 2 * Math.PI);
            this.cacheCtx.fillRect(this.x, this.y, 4, 4);
            this.cacheCtx.fill();
            this.cacheCtx.stroke();
            this.cacheCtx.restore();
        };

        ballpen.prototype = {
            paint: function (pen) {
                pen.drawImage(this.cacheCanvas, this.x, this.y);
            }
        };

        var f = new ballpen(0, 0);

        //window.onload = function () {




        function init() {
            //socket对象
            //socket = new WebSocket("ws://192.168.0.103:4017");
            if (typeof (WebSocket) == "undefined") {
                alert("您的浏览器不支持WebSocket");
            }

            //socket对象
            socket = new WebSocket("ws://192.168.0.103:4017");
            //连接成功
            socket.onopen = function () {
                //alert("连接服务器成功"); //连接成功,准备获取服务器中主人蛇的数据,准备获取其他玩家蛇的位置
                state = 1;
                $("#state").html("正常");
            };

            //发生了错误事件
            socket.onerror = function () {
                //alert("连接失败,请刷新页面");
                $("#state").html("异常");
                state = -1;
                return;
            };

            //连接关闭的回调方法
            socket.onclose = function () {
                $("#state").html("断开");
                state = 0;
                return;
            }




            //初始化按钮
            bindNipple();

            //连接成功,准备绘画游戏
            canvas = document.getElementById("cav");
            canvas.width = width;
            canvas.height = height;
            canvas.style.backgroundColor = "burlywood";
            //画蛇的画笔
            pen = canvas.getContext("2d");
            //画食物的画笔
            fpen = canvas.getContext("2d");


            //获得消息事件
            socket.onmessage = function (msg) {
                service_data = JSON.parse(msg.data);

                if (service_data != null) {
                    //接受IP信息
                    if (service_data.ip != null) {
                        ip = service_data.ip;
                        $("#ip").html(ip);
                        $.cookie('snake_ip', ip, { expires: 30 });
                        return;
                    }
                    if (service_data[0].snake != null) {
                        clearSnake(snake_player);
                    }
                    for (var data in service_data) {
                        if (service_data[data].snake == null) {
                            var food_data_color = service_data[data].color;
                            var food_data_food = service_data[data].food;

                            //玩家数量
                            var game_data_quantity = service_data[data].quantity;
                            $("#quantity").html(game_data_quantity);

                            for (var position in food_data_food) {
                                //pen.drawImage(f.cacheCanvas, food_data_food[position].x * 10 + 5, food_data_food[position].y * 10 + 5);
                                // food.push(f);
                                food[position] = { x: food_data_food[position].x, y: food_data_food[position].y };
                            }
                            // drawFood(food, food_data_color);
                        } else {
                            //清楚前面那一帧的蛇
                            //clearSnake(snake_player);

                            var snake_data_color = service_data[data].color;
                            var snake_data_snake = service_data[data].snake;

                            //获取玩家分身：snake的长度-初始值
                            if (service_data[data].ip == ip) {
                                $("#grade").html(snake_data_snake.length - 3);
                            }

                            //获取当前玩家的颜色
                            if (color == null) {
                                if (service_data[data].ip == ip) {
                                    color = snake_data_color;
                                    $("#color").css("background", color);
                                }
                            }

                            for (var position in snake_data_snake) {
                                snake_player[position] = { x: snake_data_snake[position].x, y: snake_data_snake[position].y };
                            }
                            drawSnake(snake_player, snake_data_color);
                            eatFood(snake_player);

                            for (var position in food) {
                                fpen.drawImage(f.cacheCanvas, food[position].x * 10 + 5, food[position].y * 10 + 5);
                            }
                        }
                    }
                }
            };

            //清除
            function clearSnake(snake_entity) {
                for (var i = 0; i < snake_entity.length; i++) {
                    pen.clearRect(0, 0, 2000, 2000);
                    //clearCircle(pen, snake_entity[i].x * 10 + 1, snake_entity[i].y * 10 + 1, snake_entity.length * 0.4 + 10);
                }
            }

            //清除圆形 

            function clearCircle(pen_obj, x, y, r) {
                for (var i = 0; i < Math.round(Math.PI * r); i++) {
                    var angle = (i / Math.round(Math.PI * r)) * 360;
                    pen_obj.clearRect(x, y, Math.sin(angle * (Math.PI / 180)) * r, Math.cos(angle * (Math.PI / 180)) * r);
                }
            }

            //画蛇

            function drawSnake(snake_entity, style) {
                //画蛇的每一点
                pen.beginPath();
                pen.fillStyle = style;
                // pen.fillStyle = 'rgba(255,0,0,0.5)';
                for (var i = 0; i < snake_entity.length; i++) {
                    pen.arc(snake_entity[i].x * 10 + 1, snake_entity[i].y * 10 + 1, snake_entity.length * 0.4 + 9, 0, Math.PI * 2);
                    pen.fill();
                    pen.closePath();

                }
            }


            //接近食物，45度移动，不是太准确
            function eatFood(snake_entity) {
                if (snake_entity != null) {
                    if (snake_entity.length > 0) {
                        for (var i = 0; i < snake_entity.length; i++) {
                            if (i == 0) {
                                if (food.length > 0) {
                                    for (var j = 0; j < food.length; j++) {
                                        var reduce_x = parseInt(snake_entity[i].x) - parseInt(food[j].x);
                                        var reduce_y = parseInt(snake_entity[i].y) - parseInt(food[j].y);
                                        if (Math.abs(reduce_x) < 5 && Math.abs(reduce_y) < 5) {
                                            if (repeat_food != food[j]) {
                                                repeat_food = food[j];

                                                pen.clearRect(food[j].x * 10 + 5, food[j].y * 10 + 5, 10, 10);
                                                var last_x = -1;
                                                var last_y = -1;
                                                var k = 10;
                                                var foodx = food[j].x;
                                                var foody = food[j].y;
                                                var rx = reduce_x;
                                                var ry = reduce_y;
                                                food.splice(j, 1);

                                                var eat_interval = setInterval(function () {
                                                    pen.clearRect(last_x, last_y, 10, 10);
                                                    last_x = (foodx + rx * (k / 100)) * 10 + 5;
                                                    last_y = (foody + ry * (k / 100)) * 10 + 5;
                                                    pen.drawImage(f.cacheCanvas, last_x, last_y);

                                                    if (k >= 60) {
                                                        pen.clearRect(last_x, last_y, 10, 10);
                                                        clearInterval(eat_interval);
                                                    }
                                                    k += 10;
                                                }, 30);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    </script>
</head>
<body>
    <div id="left">
        <div class="nipple collection_0" id="nipple_0_0" style="position: absolute; opacity: 0.5; display: block; z-index: 999; transition: opacity 250ms; -webkit-transition: opacity 250ms; top: 50%; left: 20%;">
        </div>
        <div style="height: 50px; width: 100%">
            <div style="text-align: center; padding-top: 25px">
                <button style="width: 100px; height: 50px" id="start">开始</button>
            </div>

        </div>
        <div style="height: 50px; width: 100%;;display:none">
            <div>当前玩家IP:<label id="ip"></label></div>
            <div style="white-space: nowrap;"><span style="float:left;padding-right:5px">当前玩家:</span>
                <div style="width:20px;height:20px;float:left;padding-right:5px" id="color"></div>
            </div>
            <div>分数:<label id="grade" style="padding-right:5px"></label></div>
            <div>连接状态:<label id="state" style="padding-right:5px"></label></div>
            <div>当前玩家数量:<label id="quantity"></label></div>
        </div>
        <canvas id="cav" width="1300" height="600" style="margin-left: 100px; margin-top: 50px"></canvas>
    </div>
</body>
<script type="text/javascript">
    var joystick = null;
    $("#start").click(function () {
        joystick = nipplejs.create({
            zone: document.getElementById('left'),
            mode: 'dynamic',
            color: 'green'
        });

        //读取cookic中的ip
        if ($.cookie('snake_ip') != null) {
            ip = $.cookie('snake_ip');
            $("#ip").html($.cookie('snake_ip'));
        }

        var grandpa = $(this).parent().parent();
        //连接和初始化
        $("#state").html("连接中");
        init();
        grandpa.css("display", "none");
        grandpa.next().css("display", "block");

    })
</script>
<script type="text/javascript">
    //var joystick = nipplejs.create({
    //    zone: document.getElementById('left'),
    //    mode: 'dynamic',
    //    color: 'green'
    //});

    function bindNipple() {
        joystick.on('start end', function (evt, data) {

        }).on('move', function (evt, data) {
            //debug(data);
        }).on('dir:up plain:up dir:left plain:left dir:down ' +
            'plain:down dir:right plain:right',
            function (evt, data) {
                //if(data.distance>20) {
                if (Math.abs(parseFloat(data.angle.radian) - angle) > 0.3) {
                    angle = parseFloat(data.angle.radian);
                    socket.send(angle);
                }
                //}
            }
        ).on('pressure', function (evt, data) {
            //debug({ pressure: data });
        });
    }
</script>
</html>
