using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfTest {
	namespace NIDaq {
		public class Plot {
			public int index;
			public double value;
			public PlotType type;
			public bool isEnd;
			public string toText() {
				string str;
				str = String.Format("{0} {1} {2} {3}", index, value, Enum.GetName(typeof(PlotType), type), isEnd);
				return str;
			}
			public void fromText(string str) {
				try {
					string[] strs = str.Split(' ');
					index = int.Parse(strs[0]);
					value = double.Parse(strs[1]);
					type = (PlotType)Enum.Parse(typeof(PlotType), strs[2]);
					isEnd = bool.Parse(strs[3]);
				} catch (Exception) {
					// load fail
				}
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
