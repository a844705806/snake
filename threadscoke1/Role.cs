using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace new_scoket
{
	abstract public class Role
	{
		public List<Dictionary<string, object>> Position_list=new List<Dictionary<string, object>>();

		public string Color { get; set; }

		public bool show(List<Dictionary<string, object>> position_list, int x, int y)
		{
			if (position_list != null)
			{
				for (var i = 0; i < Position_list.Count; i++)
				{
					if (Convert.ToInt16((position_list[i])["x"]) == x && Convert.ToInt16((position_list[i])["y"]) == y)
					{
						return false;
					}
				}
			}
			return true;
		}
	}
}
