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


namespace WpfTest {
	namespace NIDaq {
		public class Division {

			//自身の幅
			public static readonly int width = 80;
			//生成インデックス
			private static int uniqueId = 0;

			//自身の属するシーケンス
			private Sequence parent;

			//自身のパネル
			private StackPanel stackPanel;

			//名前
			private TextBox label;

			//時間
			private double time;
			//時間のユニット
			private TimeUnit units;

			//自身の列番号
			private int myColumn;

			//コンストラクタ
			public Division(Sequence _parent) {
				parent = _parent;
				label = new TextBox() { Text = "Div " + uniqueId, Background = Brushes.LightGray , ContextMenu=null};
				label.ContextMenuOpening += ((object sender, ContextMenuEventArgs arg) => CheckContextMenu());
				stackPanel = new StackPanel() { Orientation = Orientation.Vertical, ContextMenu = new ContextMenu() };
				stackPanel.Children.Add(label);
				time = 1;
				units = TimeUnit.s;
				uniqueId++;
			}

			private const string separator = ",";
			//保存
			public string toSeq() {
				string str = "";
				str += label.Text + separator;
				str += time.ToString() + separator;
				str += units.ToString() + separator;
				return str; 
			}
			//読み込み
			public void fromSeq(string s) {
				string[] strs = s.Trim().Split(separator.ToCharArray());
				label.Text = strs[0];
				time = double.Parse(strs[1]);
				units = (TimeUnit)Enum.Parse(typeof(TimeUnit), strs[2]);
			}
			//コンテキストメニューの作成
			public void CheckContextMenu() {
				stackPanel.ContextMenu.Items.Clear();
				stackPanel.ContextMenu.Items.Add(new MenuItem() { Header = String.Format("time = {0} {1}", time, units.ToString()), IsEnabled = false });
				MenuItem item;
				item = new MenuItem() { Header = "Edit Division"};
				item.Click += (object sender, RoutedEventArgs arg) => editDivision();
				stackPanel.ContextMenu.Items.Add(item);
				item = new MenuItem() { Header = "Remove This Division" };
				item.Click += (object sender, RoutedEventArgs arg) => parent.removeDivision(myColumn);
				stackPanel.ContextMenu.Items.Add(item);
			}
			//divisionの編集
			public void editDivision() {
				EditDivisionWindow window = new EditDivisionWindow(time, units);
				window.ShowDialog();
				if (window.isOk) {
					time = window.resultTimeValue;
					units = window.resultTimeUnit;
				}
				DebugWindow.WriteLine("Divisionの情報を更新");
			}
			//列番号を移動
			public void setPosition(int i) {
				myColumn = i;
				stackPanel.SetValue(Grid.RowProperty, 0);
				stackPanel.SetValue(Grid.ColumnProperty, i + 1);
			}
			//時間を取得
			public double getTime() {
				return time * units.getTime();
			}
			//名前を取得
			public string getName() {
				return label.Text;
			}
			//名前を設定
			public void setName(string str) {
				label.Text = str;
			}
			//パネルを取得
			public UIElement getPanel(){
				return stackPanel;
			}
			//最後に設定
			public void setLast() {
				label.Text = "Last";
				time = 0;
			}
		}
	}
}
