﻿using System;
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
	//シーケンスのうち単一の入出力ライン
	public class Channel {
		//描画時の高さ
		public static readonly int height=120;
		//ユニークID
		private static int uniqueId = 0;
		//自身の所属
		public Sequence parent;

		//チャンネル情報のパネル
		public StackPanel panel{get;private set;}

		//チャンネルの行表示
		private TextBlock Text_rowIndex;
		public int rowIndex {
			get {
				return int.Parse(Text_rowIndex.Text);
			}
			set {
				Text_rowIndex.Text = value.ToString();
				Grid.SetRow(panel, value + 1);
				Grid.SetColumn(panel, 0);
				Grid.SetRow(canvas, value + 1);
				Grid.SetColumn(canvas, 1);
			}
		}
	
		//チャンネルの名前
		private TextBox Text_name;
		public string name {
			get {
				return Text_name.Text;
			}
		}

		//チャンネルのデバイス
		private ComboBox Combo_deviceName;
		public string deviceName {
			get {
				return Combo_deviceName.Text;
			}
			set {
				Combo_deviceName.Text=value;
			}
		}

		//チャンネルのAD選択
		private ComboBox ADCombo;
		public bool isAnalog {
			get {
				return ADCombo.SelectedIndex == 0;
			}
			set {
				ADCombo.SelectedIndex = value?0:1;
			}
		}

		//チャンネルのIO選択
		private ComboBox IOCombo;
		public bool isOutput {
			get {
				return IOCombo.SelectedIndex == 0;
			}
			set {
				IOCombo.SelectedIndex = value ? 0 : 1;
			}
		}

		//最小電圧
		private TextBox Text_minVoltage;
		public double minVoltage {
			get {
				return double.Parse(Text_minVoltage.Text);
			}
			set {
				Text_minVoltage.Text = value.ToString();
			}
		}

		//最大電圧
		private TextBox Text_maxVoltage;
		public double maxVoltage {
			get {
				return double.Parse(Text_maxVoltage.Text);
			}
			set {
				Text_maxVoltage.Text = value.ToString();
			}
		}

		//チャンネルの時系列を表示するキャンバス
		public Canvas canvas { get; private set; }
		public int span {
			get {
				return Grid.GetColumnSpan(canvas);
			}
			set {
				Grid.SetColumnSpan(canvas, value);
			}
		}

		//右クリック時選択中の列
		private int clickedColumn;

		//ノード
		public List<Node> nodes {get;private set;}

		//コンパイル時の区間ごとの配列
		private List<double[]> samples = new List<double[]>();

		//コンストラクタ
		public Channel(Sequence _parent, int divisionCount) {
			parent = _parent;
			nodes = new List<Node>();
			panel = new StackPanel() { };

			Text_rowIndex = new TextBlock() { Background=Brushes.Black , Foreground=Brushes.White };
			Text_name = new TextBox() { Text = "IO " + uniqueId, Background = Brushes.LightGray, ContextMenu = new ContextMenu() };
//			Text_rowIndex.ContextMenuOpening += (object sender, ContextMenuEventArgs arg) => CheckContextMenuOfLabel();
			uniqueId++;
			ADCombo = new ComboBox() { };
			ADCombo.Items.Add("Analog"); ADCombo.Items.Add("Digital");
			IOCombo = new ComboBox() { };
			IOCombo.Items.Add("Out"); IOCombo.Items.Add("In");
			Combo_deviceName = new ComboBox() { };
			foreach (string str in NIDaqTaskManager.GetInstance().getAnalogOutputList()) {
				Combo_deviceName.Items.Add(str);
			}
			Text_minVoltage = new TextBox() { Text = "-1" , Width=20};
			Text_maxVoltage = new TextBox() { Text = "1", Width=20 };

			panel.Children.Add(Text_rowIndex);
			panel.Children.Add(Text_name);
			{
				StackPanel miniStack = new StackPanel() { Orientation = Orientation.Horizontal };
				miniStack.Children.Add(ADCombo);
				miniStack.Children.Add(IOCombo);
				panel.Children.Add(miniStack);
			}
			panel.Children.Add(Combo_deviceName);
			{
				StackPanel miniStack = new StackPanel() { Orientation = Orientation.Horizontal };
				miniStack.Children.Add(Text_minVoltage);
				miniStack.Children.Add(new Label() { Content = "V - " });
				miniStack.Children.Add(Text_maxVoltage);
				miniStack.Children.Add(new Label() { Content = "V" });
				panel.Children.Add(miniStack);
			}

			for (int i = 0; i < divisionCount - 1; i++) {
				nodes.Add(new Node() { index = 0, value = 0, type = NodeType.Hold, isEnd = false });
			}
			nodes.Add(new Node() { index = 0, value = 0, type = NodeType.Hold, isEnd = true });

			canvas = new Canvas() { Background = Brushes.White, ContextMenu = new ContextMenu() };
			canvas.SetValue(Grid.ColumnSpanProperty, divisionCount);
			canvas.ContextMenuOpening += (object sender, ContextMenuEventArgs e) => CheckContextMenuOfCanvas();
		}

		//チャンネルラベルのコンテキストメニュー表示
		public void CheckContextMenuOfLabel() {
			Text_rowIndex.ContextMenu.Items.Clear();
			Text_rowIndex.ContextMenu.Items.Add(new MenuItem() { Header = String.Format("{0} {1} - {2}",ADCombo.Text,IOCombo.Text,deviceName) , IsEnabled=false});
			Text_rowIndex.ContextMenu.Items.Add(new MenuItem() { Header = String.Format("Voltage {0}V-{1}V", minVoltage,maxVoltage), IsEnabled = false });
			int myRow = rowIndex;

			MenuItem item;
			item = new MenuItem() { Header="Edit Channel"};
			item.Click += (object sender, RoutedEventArgs arg) => editChannel();
			Text_rowIndex.ContextMenu.Items.Add(item);
			item = new MenuItem() { Header = "Move Up" };
			item.Click += (object sender, RoutedEventArgs arg) => parent.moveUp(myRow);
			if (myRow == 0) item.IsEnabled = false;
			Text_rowIndex.ContextMenu.Items.Add(item);
			item = new MenuItem() { Header = "Move Down" };
			item.Click += (object sender, RoutedEventArgs arg) => parent.moveDown(myRow);
			if (myRow + 1 == parent.getChannelCount()) item.IsEnabled = false;
			Text_rowIndex.ContextMenu.Items.Add(item);
			item = new MenuItem() { Header = "Remove This Channel" };
			item.Click += (object sender, RoutedEventArgs arg) => parent.removeChannel(myRow);
			Text_rowIndex.ContextMenu.Items.Add(item);
		}

		//キャンバスのコンテキストメニュー表示
		public void CheckContextMenuOfCanvas() {

			int maxColumn = Grid.GetColumnSpan(canvas);
			bool isLast = (maxColumn - 1 == clickedColumn);
			int myRow = rowIndex;
			MenuItem item;

			clickedColumn = (int)(Mouse.GetPosition(canvas).X / Division.width);

			canvas.ContextMenu.Items.Clear();
			item = new MenuItem() { Header =name + " - " + parent.getDivisionName(clickedColumn) , IsEnabled=false};
			canvas.ContextMenu.Items.Add(item);

			canvas.ContextMenu.Items.Add(new Separator());

			item = new MenuItem() { Header="Edit Value"};
			item.Click += ((object s, RoutedEventArgs arg) => this.editNode(clickedColumn));
			canvas.ContextMenu.Items.Add(item);

			canvas.ContextMenu.Items.Add(new Separator());

			MenuItem itemHead;
			itemHead = new MenuItem() { Header="Edit Sequence"};
			{
				item = new MenuItem() { Header = "Insert Division to ←" };
				item.Click += ((object s,RoutedEventArgs arg) => parent.insertDivision(clickedColumn));
				itemHead.Items.Add(item);
				item = new MenuItem() { Header="Inert Channel to ↓"};
				item.Click += ((object s, RoutedEventArgs arg) => parent.insertChannel(myRow));
				itemHead.Items.Add(item);
				item = new MenuItem() { Header="Remove This Division" , IsEnabled=!isLast};
				item.Click += ((object s, RoutedEventArgs arg) => parent.removeDivision(clickedColumn));
				itemHead.Items.Add(item);
				item = new MenuItem() { Header="Remove This Channel"};
				item.Click += ((object s, RoutedEventArgs arg) => parent.removeChannel(myRow));
				itemHead.Items.Add(item);
			}
			canvas.ContextMenu.Items.Add(itemHead);
		}

		//ノードの編集
		public void editNode(int targetColumn) {
			EditValueWindow window = new EditValueWindow(nodes[targetColumn].value,nodes[targetColumn].type);
			window.ShowDialog();
			if (window.isOk) {
				nodes[targetColumn].type = window.resultType;
				nodes[targetColumn].value = window.resultValue;
			}
			DebugWindow.WriteLine("セルの情報を更新");
			repaint();
		}
		//チャンネル情報の編集
		public void editChannel() {
			EditIOWindow window = new EditIOWindow(isAnalog,isOutput,deviceName,minVoltage,maxVoltage);
			window.ShowDialog();
			if (window.isOk) {
				isAnalog = window.resultIsAnalog;
				isOutput = window.resultIsOutput;
				deviceName = window.resultBindedName;
				minVoltage = window.resultMinVoltage;
				maxVoltage = window.resultMaxVoltage;
			}
			DebugWindow.WriteLine("チャンネルの情報を更新");
			repaint();
		}
		//自身の行を変更
/*		public void setPosition(int row) {
			Text_rowIndex.Text = row.ToString();
			Grid.SetRow(panel, row + 1);
			Grid.SetColumn(panel,0);
			Grid.SetRow(canvas, row + 1);
			Grid.SetColumn(canvas, 1);
		}*/
		//divisionの数を更新
/*		public void setSpan(int span) {
			Grid.SetColumnSpan(canvas, span);
		}*/

		//ノードを挿入
		public void insertNode(int index, double value) {
			nodes.Insert(index, new Node() { index = index, value = value, type = NodeType.Hold, isEnd = false });
			Grid.SetColumnSpan(canvas, nodes.Count);
		}
		//ノードを削除
		public void removePlot(int index) {
			nodes.RemoveAt(index);
			Grid.SetColumnSpan(canvas, nodes.Count);
		}

		private const string separator = ",";
		//保存
		public string toSeq() {
			string str="";
			str += ADCombo.SelectedIndex + separator;
			str += IOCombo.SelectedIndex + separator;
			str += Combo_deviceName.Text + separator;
			str += Text_name.Text + separator;
			str += Text_rowIndex.Text + separator;
			str += Text_minVoltage.Text + separator;
			str += Text_maxVoltage.Text + separator;
			str += nodes.Count + separator;
			for (int i = 0; i < nodes.Count; i++) {
				str += nodes[i].toSeq() + separator;
			}
			return str;
		}
		//書き出し
		public void fromSeq(string str) {
			string[] strs = str.Trim().Split(separator.ToCharArray());
			ADCombo.SelectedIndex = int.Parse(strs[0]);
			IOCombo.SelectedIndex = int.Parse(strs[1]);
			Combo_deviceName.Text = strs[2];
			Text_name.Text = strs[3];
			Text_rowIndex.Text = strs[4];
			Text_minVoltage.Text = strs[5];
			Text_maxVoltage.Text = strs[6];
			int tempPlotsCount = int.Parse(strs[7]);
			nodes.Clear();
			for (int i = 0; i < tempPlotsCount; i++) {
				Node Node = new Node();
				Node.fromSeq(strs[8 + i]);
				nodes.Add(Node);
			}
		}
		//キャンバス中でのノードの高さを計算
		private double canvasHeight(double voltage,double minVol,double maxVol) {
			return (1.0 - 1.0 * (voltage - minVol) / (maxVol - minVol)) * height;
		}
		//再描画
		public void repaint() {
			canvas.Children.Clear();
			double minVol = minVoltage;
			double maxVol = maxVoltage;
			double circleSize = 4.0;
			for (int i = 0; i < nodes.Count; i++) {
				Ellipse Node = new Ellipse() { Fill=Brushes.Black,StrokeThickness=2,Width=8,Height=8};
				Canvas.SetLeft(Node, Division.width * i - circleSize);
				Canvas.SetTop(Node, canvasHeight(nodes[i].value, minVol, maxVol) - circleSize);
				canvas.Children.Add(Node);
					
				Line gridline;
				gridline = new Line() { Stroke=Brushes.Black, StrokeThickness=2};
				gridline.X1 = 0;
				gridline.X2 = Division.width * nodes.Count;
				gridline.Y1 = 0;
				gridline.Y2 = 0;
				canvas.Children.Add(gridline);
				gridline = new Line() { Stroke = Brushes.Black, StrokeThickness = 2 };
				gridline.X1 = 0;
				gridline.X2 = Division.width * nodes.Count;
				gridline.Y1 = height;
				gridline.Y2 = height;
				canvas.Children.Add(gridline);

				TextBox tb = new TextBox() { Width=40 , Text=nodes[i].value.ToString() , Background=Brushes.LightGray};
				Canvas.SetLeft(tb, Division.width * i);
				canvas.Children.Add(tb);

				Label lb = new Label() { Content = "V" };
				Canvas.SetLeft(lb, Division.width * i + 40.0);
				canvas.Children.Add(lb);

				if (i + 1 < nodes.Count && nodes[i].type != NodeType.Through) {
					int next;
					Line line;
					for (next = i + 1; next < nodes.Count; next++) {
						if (nodes[next].type != NodeType.Through) {
							break;
						}
					}
					if (next < nodes.Count) {
						if (nodes[i].type == NodeType.Hold) {
							line = new Line() { Stroke=Brushes.LightGray,StrokeThickness=2};
							line.X1 = Division.width * i;
							line.X2 = Division.width * next;
							line.Y1 = canvasHeight(nodes[i].value,minVol,maxVol);
							line.Y2 = line.Y1;
							canvas.Children.Add(line);
							line = new Line() { Stroke = Brushes.LightGray, StrokeThickness = 2 };
							line.X1 = Division.width * next;
							line.X2 = Division.width * next;
							line.Y1 = canvasHeight(nodes[i].value, minVol, maxVol);
							line.Y2 = canvasHeight(nodes[next].value, minVol, maxVol);
							canvas.Children.Add(line);
						} else if (nodes[i].type == NodeType.Linear) {
							line = new Line() { Stroke = Brushes.LightGray, StrokeThickness = 2 };
							line.X1 = Division.width * i;
							line.X2 = Division.width * next;
							line.Y1 = canvasHeight(nodes[i].value, minVol, maxVol);
							line.Y2 = canvasHeight(nodes[next].value, minVol, maxVol);
							canvas.Children.Add(line);
						}
					}
				}
			}
		}
	}
}
