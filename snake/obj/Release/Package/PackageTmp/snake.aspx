<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="snake.aspx.cs" Inherits="snake.snake" %>

<!DOCTYPE html>
<html>
<head>
	<meta charset="utf-8">
	<meta name="viewport" content="width=device-width,initial-scale=1,minimum-scale=1,maximum-scale=1,user-scalable=no" />
	<title></title>
	<script src="http://libs.baidu.com/jquery/2.0.0/jquery.min.js" type="text/javascript" charset="utf-8"></script>
	<script src="js/jquery.touchSwipe.min.js" type="text/javascript" charset="utf-8"></script>
	<script src="js/nipplejs.js" charset="utf-8"></script>

	<script type="text/javascript">
		window.requestAnimationFrame = window.requestAnimationFrame || window.mozRequestAnimationFrame || window.webkitRequestAnimationFrame || window.msRequestAnimationFrame;


		var game = null;
		var canvas = null;
		var pen = null;
		var fpen = null;
		var ballpen = null;
		var width = 300;
		var height = 500;
		var lineGrad = null;
		var angle = 0;

		var time = null;
		var ff = 0;
		//
		var color = null;
		var state = 0;

		//服务器数据
		var service_data = null;

		var snake = [];

		var snake_player = [];

		var food = [];

		window.onload = function () {
			if (typeof(WebSocket) == "undefined") {
				alert("您的浏览器不支持WebSocket");
			}

			//连接服务器
			socket = new WebSocket("ws://192.168.0.185:4017");

			//成功
			socket.onopen = function() {
				//alert("连接服务器成功"); //连接成功,准备获取服务器中主人蛇的数据,准备获取其他玩家蛇的位置
				$("#state").text(socket.readyState);

			};

			//获得消息事件
			socket.onmessage = function(msg) {
				service_data = JSON.parse(msg.data);
				//清楚掉久的蛇
				clearSnake(snake_player);
				clearSnake(snake);

				if (service_data != null) {
					for (var data in service_data) {
						if (service_data[data].snake == null) {
							var food_data_color = service_data[data].color;
							var food_data_food = service_data[data].food;

							for (var position_food in food_data_food) {
								food[position_food] = { x: food_data_food[position_food].x, y: food_data_food[position_food].y };
							}
							drawFood(food, food_data_color);
						} else {
							var snake_data_color = service_data[data].color;
							var snake_data_snake = service_data[data].snake;

							for (var position in snake_data_snake) {
								snake_player[position] = { x: snake_data_snake[position].x, y: snake_data_snake[position].y };
							}
							drawSnake(snake_player, snake_data_color);
							requestAnimationFrame(eatFood(snake_player));
						}
					}
					$("#state").text(socket.readyState);
				}
			};
			//发生了错误事件
			socket.onerror = function() {
				//alert("连接失败,请刷新页面");
				$("#state").text(socket.readyState);
				return;
			};


			//初始化
			bindNipple();


			//连接成功,准备绘画游戏
			game = document.getElementById("game");
			canvas = document.createElement("canvas");
			game.appendChild(canvas);
			canvas.width = width;
			canvas.height = height;
			canvas.style.backgroundColor = "burlywood";

			//画蛇的画笔
			pen = canvas.getContext("2d");
			//画食物的画笔
			fpen = canvas.getContext("2d");
			ballpen = canvas.getContext("2d");

			//web的start
			socket.onopen = function() {
				//alert("连接服务器成功"); //连接成功,准备获取服务器中主人蛇的数据,准备获取其他玩家蛇的位置
				$("#state").text(socket.readyState);
				//连接web的end
			};

			//清除

			function clearSnake(snake_entity) {
				for (var i = 0; i < snake_entity.length; i++) {
					//pen.clearRect(snake_entity[i].x * 10 + 1, snake_entity[i].y * 10 + 1, 8, 8);
					clearCircle(pen, snake_entity[i].x * 10 + 1, snake_entity[i].y * 10 + 1, snake_entity.length * 0.4 + 10);
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
				for (var i = 0; i < snake_entity.length; i++) {
					pen.arc(snake_entity[i].x * 10 + 1, snake_entity[i].y * 10 + 1, snake_entity.length * 0.4 + 9, 0, Math.PI * 2);
					pen.fill();
					pen.closePath();

				}
			}

			function eatFood(snake_entity) {
				if (snake_entity != null) {
					if (snake_entity.length > 0) {
						ballpen.beginPath();
						ballpen.fillStyle = "#000000";
						for (var i = 0; i < snake_entity.length; i++) {
							if (i == 0) {
								if (food.length > 0) {
									for (var j = 0; j < food.length; j++) {
										var reduce_x = parseInt(snake_entity[i].x) - parseInt(food[j].x);
										var reduce_y = parseInt(snake_entity[i].y) - parseInt(food[j].y);
										if (Math.abs(reduce_x) < 5 && Math.abs(reduce_y) < 5) {
											pen.clearRect(food[j].x * 10 + 5, food[j].y * 10 + 5, 5, 5);
											var last_x = -100;
											var last_y = -100;

											for (var k = 1; k < 80; k += 1) {
												pen.clearRect(last_x, last_y, 9, 9);

												last_x = (food[j].x + reduce_x * (k / 100)) * 10 + 5;
												last_y = (food[j].y + reduce_y * (k / 100)) * 10 + 5;
												pen.fillRect(last_x, last_y, 8, 8);
												pen.fill();
												for (var e = 0; e < 100; e++) {
													pen.fillRect(last_x, last_y, 8, 8);
													pen.fill();
												}
											}

											pen.clearRect(last_x, last_y, 9, 9);
											food.splice(j, 1);
										}
									}
								}
							}
						}
					}
				}
			}

			//画食物

			function drawFood(snake_entity, style) {
				//画食物的每一点
				pen.beginPath();
				pen.fillStyle = style;
				for (var i = 0; i < snake_entity.length; i++) {
					pen.restore();
					pen.fillRect(snake_entity[i].x * 10 + 5, snake_entity[i].y * 10 + 5, 4, 4);
					//pen.arc(snake_entity[i].x * 10 + 5, snake_entity[i].y * 10 + 5, 2, 0, Math.PI * 2);
					pen.fill();
					pen.save();
					pen.closePath();
				}

			}
		}
	</script>
</head>
<body>
	<div id="left">
		<div class="nipple collection_0" id="nipple_0_0" style="position: absolute; opacity: 0.5; display: block; z-index: 999; transition: opacity 250ms; -webkit-transition: opacity 250ms; top: 50%; left: 20%;">
		</div>
		<!--分割-->
		<div id="game">
		</div>

		<div>
			<input type="button" id="btnConnection" value="连接" />
			<input type="button" id="btnClose" value="关闭" />
			<input type="button" id="btnSend" value="发送" />
			状态:<label id="state"></label>
		</div>
		</div>
</body>

<script type="text/javascript">
	var socket;

	if (typeof (WebSocket) == "undefined")
	{
		alert("您的浏览器不支持WebSocket");
	}

	$("#btnConnection").click(function ()
	{
		socket = new WebSocket("ws://192.168.0.185:4017");
		//打开事件
		socket.onopen = function ()
		{
			alert("Socket 已打开");
			$("#state").text(socket.readyState);
		};
		$("#state").text(socket.readyState);
		//获得消息事件
		socket.onmessage = function (msg)
		{
			alert(msg.data);
			$("#state").text(socket.readyState);
		};
		//关闭事件
		socket.onclose = function ()
		{
			alert("Socket已关闭");
			$("#state").text(socket.readyState);
		};
		//发生了错误事件
		socket.onerror = function ()
		{
			alert("发生了错误");
			$("#state").text(socket.readyState);
		};
	});

	//发送消息
	$("#btnSend").click(function ()
	{
		alert("q23213123");

		socket.send("这是来自客户端的消息" + location.href + new Date());
		socket.send("sfsdfasdfsadfasdfsda");
	});

	//关闭
	$("#btnClose").click(function ()
	{
		socket.close();
	});
</script>
	

<script type="text/javascript">
	var joystick = nipplejs.create({
		zone: document.getElementById('left'),
		mode: 'dynamic',
		color: 'green'
	});

	function bindNipple()
	{
		joystick.on('start end', function (evt, data)
		{

		}).on('move', function (evt, data)
		{
			//debug(data);
		}).on('dir:up plain:up dir:left plain:left dir:down ' +
			'plain:down dir:right plain:right',
			function (evt, data)
			{
				//if(data.distance>20) {
					if (Math.abs(parseFloat(data.angle.radian)-angle)>0.3)
					{
						angle = parseFloat(data.angle.radian);
						socket.send('{"direc":"' + angle + '"}');
					}
				//}
			}
		).on('pressure', function (evt, data)
		{
			//debug({ pressure: data });
		});
	}
</script>
</html>
