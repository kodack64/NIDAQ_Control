using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfTest {
	namespace NIDaq {
		public class Plot {
			private string separator = ":";
			public int index;
			public double value;
			public PlotType type;
			public bool isEnd;
			public string toSeq() {
				string str="";
				str += index + separator;
				str += value + separator;
				str += type.ToString() + separator;
				str += isEnd+separator;
				return str;
			}
			public void fromSeq(string str) {
				string[] strs = str.Trim().Split(separator.ToCharArray());
				index = int.Parse(strs[0]);
				value = double.Parse(strs[1]);
				type = (PlotType)Enum.Parse(typeof(PlotType),strs[2]);
				isEnd = bool.Parse(strs[3]);
			}
		}
		public enum PlotType {
			Hold,
			Linear,
			Through
		}
		public enum TimeUnit {
			s,
			ms,
			us
		}
	}
}
