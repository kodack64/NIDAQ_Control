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


namespace NIDaqController {
	public class Division {

		//自身の幅
		public static readonly int width = 140;
		//生成インデックス
		private static int uniqueId = 0;

		//自身の属するシーケンス
		private Sequence parent;

		//自身のパネル
		public StackPanel panel { get; private set; }

		//自身の列
		private TextBlock Text_columnIndex;
		public int columnIndex {
			get {
				return int.Parse(Text_columnIndex.Text);
			}
			set {
				Text_columnIndex.Text = value.ToString();
				Grid.SetRow(panel, 0);
				Grid.SetColumn(panel, value + 1);
			}
		}

		//名前
		private TextBox Text_name;
		public string name {
			get {
				return Text_name.Text;
			}
			set {
				Text_name.Text = value;
			}
		}

		//時間
		private TextBox Text_time;
		public double timeValue{
			get {
				return double.Parse(Text_time.Text);
			}
			set {
				Text_time.Text = value.ToString();
			}
		}

		//時間のユニット
		private ComboBox Combo_timeUnit;
		public TimeUnit timeUnit {
			get {
				return ((TimeUnit)Enum.Parse(typeof(TimeUnit), Combo_timeUnit.Text));
			}
			set {
				Combo_timeUnit.Text = value.ToString();
			}
		}
		public double time {
			get{
				return timeValue*timeUnit.getTime();
			}
		}

		//自分が最後かどうか
		private bool isLastFlag=false;
		public bool isLast {
			get {
				return isLastFlag;
			}
			set {
				isLastFlag = value;
				if (value) {
					name = "Last";
					timeValue = 0;
					Text_name.IsEnabled = false;
					Text_time.IsEnabled = false;
					Combo_timeUnit.IsEnabled = false;
				}
			}
		}

		//コンストラクタ
		public Division(Sequence _parent) {
			parent = _parent;
			panel = new StackPanel() { Orientation = Orientation.Vertical, ContextMenu = new ContextMenu() };
			{
				Text_columnIndex = new TextBlock() { Text = uniqueId.ToString() ,ContextMenu = null };
				Text_columnIndex.ContextMenuOpening += ((object sender, ContextMenuEventArgs arg) => CheckContextMenu());
				panel.Children.Add(Text_columnIndex);
			}
			{
				Text_name = new TextBox() { Text = "Div " + uniqueId, Background = Brushes.LightGray, ContextMenu = null };
				Text_name.ContextMenuOpening += ((object sender, ContextMenuEventArgs arg) => CheckContextMenu());
				panel.Children.Add(Text_name);
			}
			{
				StackPanel miniStack = new StackPanel() { Orientation = Orientation.Horizontal };
				Text_time = new TextBox() { Text = "1", Background = Brushes.LightGray, ContextMenu = null, Width = Division.width / 2 };
				Text_time.ContextMenuOpening += ((object sender, ContextMenuEventArgs arg) => CheckContextMenu());
				Combo_timeUnit = new ComboBox() { ContextMenu = null ,Width=Division.width/2};
				foreach (String tu in Enum.GetNames(typeof(TimeUnit))) {
					Combo_timeUnit.Items.Add(tu);
				}
				Combo_timeUnit.ContextMenuOpening += ((object sender, ContextMenuEventArgs arg) => CheckContextMenu());
				Combo_timeUnit.SelectedIndex = 0;
				miniStack.Children.Add(Text_time);
				miniStack.Children.Add(Combo_timeUnit);
				panel.Children.Add(miniStack);
			}
			uniqueId++;
		}

		private const string separator = ",";
		//保存
		public string toSeq() {
			string str = "";
			str += Text_name.Text + separator;
			str += Text_time.Text + separator;
			str += Combo_timeUnit.Text + separator;
			return str; 
		}
		//読み込み
		public void fromSeq(string s) {
			string[] strs = s.Trim().Split(separator.ToCharArray());
			Text_name.Text = strs[0];
			Text_time.Text = strs[1];
			Combo_timeUnit.Text = strs[2];
		}
		//コンテキストメニューの作成
		public void CheckContextMenu() {
			panel.ContextMenu.Items.Clear();
			panel.ContextMenu.Items.Add(new MenuItem() { Header = String.Format("time = {0} {1}", Text_time.Text, Combo_timeUnit.Text), IsEnabled = false });
			MenuItem item;
			item = new MenuItem() { Header = "Edit Division" , IsEnabled=!isLast};
			item.Click += (object sender, RoutedEventArgs arg) => editDivision();
			panel.ContextMenu.Items.Add(item);
			item = new MenuItem() { Header = "Remove This Division", IsEnabled = !isLast };
			item.Click += (object sender, RoutedEventArgs arg) => parent.removeDivision(int.Parse((string)Text_columnIndex.Text));
			panel.ContextMenu.Items.Add(item);
		}
		//divisionの編集
		public void editDivision() {
			EditDivisionWindow window = new EditDivisionWindow(timeValue,timeUnit);
			window.ShowDialog();
			if (window.isOk) {
				timeValue = window.resultTimeValue;
				timeUnit = window.resultTimeUnit;
			}
			DebugWindow.WriteLine("Divisionの情報を更新");
		}
		//列番号を移動
/*		public void setPosition(int i) {
			Text_columnIndex.Text  = i.ToString();
			Grid.SetRow(panel,0);
			Grid.SetColumn(panel, i + 1);
		}*/
	}
}
