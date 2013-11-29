using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NIDaqInterfaceDummy;

namespace NIDaqController {
	//チャンネルの時系列におけるノード
	public class Node {
		private Channel parent;
		//自身のインデックス
		private int _index;
		public int index {
			get {
				return _index;
			}
			set {
				_index = value;
				Canvas.SetLeft(Text_value,Division.width*value);
				Canvas.SetLeft(Combo_type, Division.width * value+60);
			}
		}
		//数値
		public TextBox Text_value = new TextBox() { Width = 40, Text = "0", Background = Brushes.LightGray };
		public double value {
			get {
				try {
					return double.Parse(Text_value.Text);
				} catch (Exception) {
					return 0;
				}
			}
			set {
				Text_value.Text = value.ToString();
			}
		}

		//ノードのタイプ
		public ComboBox Combo_type = new ComboBox();
		public NodeType type {
			get {
				return (NodeType)Enum.Parse(typeof(NodeType), Combo_type.Text);
			}
			set {
				Combo_type.Text = value.ToString();
			}
		}


		//自身が最後かどうか
		private bool _isEnd;
		public bool isEnd {
			get {
				return _isEnd;
			}
			set {
				_isEnd = value;
				if (value) {
					Text_value.IsEnabled = false;
					Combo_type.IsEnabled = false;
				}
			}
		}

		public Node(Channel _parent) {
			parent = _parent;
			foreach (string str in Enum.GetNames(typeof(NodeType))) {
				Combo_type.Items.Add(str);
			}
			Combo_type.SelectedIndex = 0;
			Text_value.TextChanged += (s, e) => TextChanged();
			Combo_type.DropDownClosed += (s, e) => ComboChanged();
		}
		public void TextChanged() {
			double value;
			bool result = double.TryParse(Text_value.Text,out value);
			if (result) {
				Text_value.Background = Brushes.LightGray;
				parent.repaint();
			} else {
				Text_value.Background = Brushes.Red;
			}
		}
		public void ComboChanged() {
			parent.repaint();
		}

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
		Linear
	}
	public static class NodeTypeExt {
		public static double getFuncValue(this NodeType type, double left, double right,double pos) {
			if (type == NodeType.Hold) return left;
			if (type == NodeType.Linear) return left + (right - left) * pos;
			else return 0;
		}
	}
}
