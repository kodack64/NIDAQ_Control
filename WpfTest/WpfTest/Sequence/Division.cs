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
		public static readonly int width = 100;
		//生成インデックス
		private static int uniqueId = 0;

		//自身の属するシーケンス
		private Sequence parent;

		//自身のパネル
		private StackPanel stackPanel;

		//自身の列
		private Label columnIndexLabel;

		//名前
		private TextBox nameLabel;

		//時間
		private TextBox timeLabel;

		//時間のユニット
		private ComboBox unitCombo;

		//自分が最後かどうか
		private bool isLast;

		//コンストラクタ
		public Division(Sequence _parent) {
			parent = _parent;
			stackPanel = new StackPanel() { Orientation = Orientation.Vertical, ContextMenu = new ContextMenu() };
			{
				columnIndexLabel = new Label() { Content = uniqueId,ContextMenu = null };
				columnIndexLabel.ContextMenuOpening += ((object sender, ContextMenuEventArgs arg) => CheckContextMenu());
				stackPanel.Children.Add(columnIndexLabel);
			}
			{
				nameLabel = new TextBox() { Text = "Div " + uniqueId, Background = Brushes.LightGray, ContextMenu = null };
				nameLabel.ContextMenuOpening += ((object sender, ContextMenuEventArgs arg) => CheckContextMenu());
				stackPanel.Children.Add(nameLabel);
			}
			{
				StackPanel miniStack = new StackPanel() { Orientation = Orientation.Horizontal };
				timeLabel = new TextBox() { Text = "1", Background = Brushes.LightGray, ContextMenu = null, Width = Division.width / 2 };
				timeLabel.ContextMenuOpening += ((object sender, ContextMenuEventArgs arg) => CheckContextMenu());
				unitCombo = new ComboBox() { ContextMenu = null ,Width=Division.width/2};
				foreach (String tu in Enum.GetNames(typeof(TimeUnit))) {
					unitCombo.Items.Add(tu);
				}
				unitCombo.ContextMenuOpening += ((object sender, ContextMenuEventArgs arg) => CheckContextMenu());
				unitCombo.SelectedIndex = 0;
				miniStack.Children.Add(timeLabel);
				miniStack.Children.Add(unitCombo);
				stackPanel.Children.Add(miniStack);
			}
			uniqueId++;
		}

		private const string separator = ",";
		//保存
		public string toSeq() {
			string str = "";
			str += nameLabel.Text + separator;
			str += timeLabel.Text + separator;
			str += unitCombo.Text + separator;
			return str; 
		}
		//読み込み
		public void fromSeq(string s) {
			string[] strs = s.Trim().Split(separator.ToCharArray());
			nameLabel.Text = strs[0];
			timeLabel.Text = strs[1];
			unitCombo.Text = strs[2];
		}
		//コンテキストメニューの作成
		public void CheckContextMenu() {
			stackPanel.ContextMenu.Items.Clear();
			stackPanel.ContextMenu.Items.Add(new MenuItem() { Header = String.Format("time = {0} {1}", timeLabel.Text, unitCombo.Text), IsEnabled = false });
			MenuItem item;
			item = new MenuItem() { Header = "Edit Division"};
			if (isLast) item.IsEnabled = false;
			item.Click += (object sender, RoutedEventArgs arg) => editDivision();
			stackPanel.ContextMenu.Items.Add(item);
			item = new MenuItem() { Header = "Remove This Division" };
			if (isLast) item.IsEnabled = false;
			item.Click += (object sender, RoutedEventArgs arg) => parent.removeDivision(int.Parse((string)columnIndexLabel.Content));
			stackPanel.ContextMenu.Items.Add(item);
		}
		//divisionの編集
		public void editDivision() {
			EditDivisionWindow window = new EditDivisionWindow(getTime(), (TimeUnit)Enum.Parse(typeof(TimeUnit),unitCombo.Text));
			window.ShowDialog();
			if (window.isOk) {
				timeLabel.Text = window.resultTimeValue.ToString();
				unitCombo.Text = window.resultTimeUnit.ToString();
			}
			DebugWindow.WriteLine("Divisionの情報を更新");
		}
		//列番号を移動
		public void setPosition(int i) {
			columnIndexLabel.Content  = i.ToString();
			stackPanel.SetValue(Grid.RowProperty, 0);
			stackPanel.SetValue(Grid.ColumnProperty, i + 1);
		}
		//時間を取得
		public double getTime() {
			return double.Parse(timeLabel.Text) * ((TimeUnit)Enum.Parse(typeof(TimeUnit),unitCombo.Text)).getTime();
		}
		//名前を取得
		public string getName() {
			return nameLabel.Text;
		}
		//名前を設定
		public void setName(string str) {
			nameLabel.Text = str;
		}
		//パネルを取得
		public UIElement getPanel(){
			return stackPanel;
		}
		//最後に設定
		public void setLast() {
			nameLabel.Text = "Last";
			nameLabel.IsEnabled = false;
			timeLabel.Text = "0";
			timeLabel.IsEnabled = false;
			unitCombo.IsEnabled = false;
			isLast = true;
		}
	}
}
