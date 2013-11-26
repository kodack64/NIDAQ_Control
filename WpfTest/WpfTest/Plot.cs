using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfTest {
	namespace NIDaq {
		//チャンネルの時系列におけるノード
		public class Node {
			//自身のインデックス
			public int index;
			//数値
			public double value;
			//ノードのタイプ
			public NodeType type;
			//自身が最後かどうか
			public bool isEnd;

			private string separator = ":";
			//書き出し
			public string toSeq() {
				string str="";
				str += index + separator;
				str += value + separator;
				str += type.ToString() + separator;
				str += isEnd+separator;
				return str;
			}
			//読み込み
			public void fromSeq(string str) {
				string[] strs = str.Trim().Split(separator.ToCharArray());
				index = int.Parse(strs[0]);
				value = double.Parse(strs[1]);
				type = (NodeType)Enum.Parse(typeof(NodeType),strs[2]);
				isEnd = bool.Parse(strs[3]);
			}
		}
		//ノードの種類
		public enum NodeType {
			Hold,
			Linear,
			Through
		}
		//時間の種類
		public enum TimeUnit {
			s,
			ms,
			us
		}
	}
}
