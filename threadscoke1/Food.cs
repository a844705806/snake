using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace new_scoket
{
	public class Food : Role
	{
		public void init_food()
		{
			for (int i = 0; i < 30; i++)
			{
				this.Position_list.Add(new_food());
				this.Color = "#000000";
			}
		}


		public void reform_food()
		{
			if (Position_list.Count() <= 30)
			{
				this.Position_list.Add(new_food());
				this.Color = "#000000";
			}
		}

		private Dictionary<string, object> new_food()
		{
			var x = 0;
			var y = 0;
			Random r = new Random();
			do
			{
				x = Convert.ToInt16(Math.Floor(r.NextDouble() * 130));
				y = Convert.ToInt16(Math.Floor(r.NextDouble() * 60));

			} while (show(this.Position_list, x, y) == false);


			Dictionary<string, object> dict = new Dictionary<string, object>();
			dict["x"] = x;
			dict["y"] = y;

			return dict;
		}
	}
}
