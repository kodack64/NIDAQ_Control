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
using Microsoft.Win32;
using System.IO;

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
		private TextBox Text_virtualName;
		public string virtualName {
			get {
				return Text_virtualName.Text;
			}
		}

		//チャンネルのデバイス
		private ComboBox Combo_channelName;
		public string channelName {
			get {
				return Combo_channelName.Text;
			}
			set {
				Combo_channelName.Text=value;
			}
		}
		public string deviceName {
			get{
				try {
					return channelName.Split('/')[0];
				} catch (Exception) {
					return "";
				}
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
				try {
					return double.Parse(Text_minVoltage.Text);
				} catch (Exception) {
					return 0;
				}
			}
			set {
				Text_minVoltage.Text = value.ToString();
			}
		}

		//最大電圧
		private TextBox Text_maxVoltage;
		public double maxVoltage {
			get {
				try{
					return double.Parse(Text_maxVoltage.Text);
				} catch (Exception) {
					return 0;
				}
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
				repaintValueBoxes();
			}
		}

		//右クリック時選択中の列
		private int clickedColumn;

		//ノード
		public List<Node> nodes {get;private set;}

		//コンパイル時の区間ごとの配列
		private List<double[]> samples = new List<double[]>();

		//AnalogInput時のwavearray
		public double[] inputWaveArray = null;

		//コンストラクタ
		public Channel(Sequence _parent, int divisionCount) {
			parent = _parent;
			nodes = new List<Node>();
			panel = new StackPanel() { };

			Text_rowIndex = new TextBlock() { Background=Brushes.Black , Foreground=Brushes.White };
			Text_virtualName = new TextBox() { Text = "IO " + uniqueId, Background = Brushes.LightGray, ContextMenu = new ContextMenu() };
			Text_rowIndex.ContextMenuOpening += (object sender, ContextMenuEventArgs arg) => CheckContextMenuOfLabel();
			uniqueId++;
			ADCombo = new ComboBox() { };
			ADCombo.Items.Add("Analog"); ADCombo.Items.Add("Digital"); ADCombo.SelectedIndex = 0;
			ADCombo.SelectionChanged += ((s, e) => updateItemList());
			IOCombo = new ComboBox() { };
			IOCombo.Items.Add("Out"); IOCombo.Items.Add("In"); IOCombo.SelectedIndex = 0;
			IOCombo.SelectionChanged += ((s, e) => updateItemList());
//			IOCombo.IsEnabled = false;
			Combo_channelName = new ComboBox() { };
			foreach (string str in TaskManager.GetInstance().getAnalogOutputList()) {
				Combo_channelName.Items.Add(str);
			}
			Text_minVoltage = new TextBox() { Text = "-1" , Width=40};
			Text_minVoltage.TextChanged += (s, e) => repaint();
			Text_maxVoltage = new TextBox() { Text = "1", Width=40 };
			Text_maxVoltage.TextChanged += (s, e) => repaint();

			panel.Children.Add(Text_rowIndex);
			panel.Children.Add(Text_virtualName);
			{
				StackPanel miniStack = new StackPanel() { Orientation = Orientation.Horizontal };
				miniStack.Children.Add(ADCombo);
				miniStack.Children.Add(IOCombo);
				panel.Children.Add(miniStack);
			}
			panel.Children.Add(Combo_channelName);
			{
				StackPanel miniStack = new StackPanel() { Orientation = Orientation.Horizontal };
				miniStack.Children.Add(Text_minVoltage);
				miniStack.Children.Add(new Label() { Content = "V ～ " });
				miniStack.Children.Add(Text_maxVoltage);
				miniStack.Children.Add(new Label() { Content = "V" });
				panel.Children.Add(miniStack);
			}

			for (int i = 0; i < divisionCount - 1; i++) {
				nodes.Add(new Node(this) { index = i, value = 0, type = NodeType.Hold, isEnd = false });
			}
			nodes.Add(new Node(this) { index = divisionCount-1, value = 0, type = NodeType.Hold, isEnd = true });

			canvas = new Canvas() { Background = Brushes.White, ContextMenu = new ContextMenu() };
			canvas.SetValue(Grid.ColumnSpanProperty, divisionCount);
			canvas.ContextMenuOpening += (object sender, ContextMenuEventArgs e) => CheckContextMenuOfCanvas();
			repaintValueBoxes();
		}

		public void updateItemList() {
			Combo_channelName.Items.Clear();
			if (ADCombo.SelectedIndex == 0) {
				if (IOCombo.SelectedIndex==0) {
					foreach (string str in TaskManager.GetInstance().getAnalogOutputList()) {
						Combo_channelName.Items.Add(str);
					}
				} else {
					foreach (string str in TaskManager.GetInstance().getAnalogInputList()) {
						Combo_channelName.Items.Add(str);
					}
				}
				minVoltage = -1;
				maxVoltage = 1;
				Text_minVoltage.IsEnabled = true;
				Text_maxVoltage.IsEnabled = true;
			} else {
				if (IOCombo.SelectedIndex == 0) {
					foreach (string str in TaskManager.GetInstance().getDigitalOutputList()) {
						Combo_channelName.Items.Add(str);
					}
				} else {
					foreach (string str in TaskManager.GetInstance().getDigitalInputList()) {
						Combo_channelName.Items.Add(str);
					}
				}
				minVoltage = 0;
				maxVoltage = 1;
				Text_minVoltage.IsEnabled = false;
				Text_maxVoltage.IsEnabled = false;
			}
			repaint();
		}


		//チャンネルラベルのコンテキストメニュー表示
		public void CheckContextMenuOfLabel() {
			Text_rowIndex.ContextMenu.Items.Clear();
			Text_rowIndex.ContextMenu.Items.Add(new MenuItem() { Header = String.Format("{0} {1} - {2}",ADCombo.Text,IOCombo.Text,channelName) , IsEnabled=false});
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
			item = new MenuItem() { Header =virtualName + " - " + parent.getDivisionName(clickedColumn) , IsEnabled=false};
			canvas.ContextMenu.Items.Add(item);

			/*
			canvas.ContextMenu.Items.Add(new Separator());
			item = new MenuItem() { Header="Edit Value"};
			item.Click += ((object s, RoutedEventArgs arg) => this.editNode(clickedColumn));
			canvas.ContextMenu.Items.Add(item);
			*/
			item = new MenuItem() { Header = "Save WaveForm" };
			item.Click += ((object s, RoutedEventArgs arg) => this.saveWaveForm());
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

		//波形の保存
		public void saveWaveForm() {
			if (inputWaveArray == null) {
				MessageBox.Show("このチャンネルの保持する入力波形がありません。","エラー");
			} else {
				SaveFileDialog dialog = new SaveFileDialog() {
					Filter = "波形データ|*.txt",
					FileName = channelName.Replace("/","_")+".txt"
				};
				bool? result = dialog.ShowDialog();
				if (result.HasValue) {
					if (result.Value) {
						double dt = 1.0 / parent.sampleRate;
						var sw = File.CreateText(dialog.FileName);
						for(int i=0;i<inputWaveArray.Length;i++){
							sw.WriteLine("{0} {1}",dt*i,inputWaveArray[i]);
						}
						sw.Close();
						DebugWindow.WriteLine(String.Format("{0}に保存しました。", dialog.FileName));
					}
				}
			}
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
			EditIOWindow window = new EditIOWindow(isAnalog,isOutput,channelName,minVoltage,maxVoltage);
			window.ShowDialog();
			if (window.isOk) {
				isAnalog = window.resultIsAnalog;
				isOutput = window.resultIsOutput;
				channelName = window.resultBindedName;
				minVoltage = window.resultMinVoltage;
				maxVoltage = window.resultMaxVoltage;
			}
			DebugWindow.WriteLine("チャンネルの情報を更新");
			repaint();
		}

		//ノードを挿入
		public void insertNode(int index, double value) {
			nodes.Insert(index, new Node(this) { index = index, value = value, type = NodeType.Hold, isEnd = false });
			for (int i = index + 1;i<nodes.Count ; i++) {
				nodes[i].index = i;
			}
			span = nodes.Count;
		}
		//ノードを削除
		public void removePlot(int index) {
			nodes.RemoveAt(index);
			for (int i = index; i < nodes.Count; i++) {
				nodes[i].index = i;
			}
			span = nodes.Count;
		}

		private const string separator = ",";
		//保存
		public string toSeq() {
			string str="";
			str += ADCombo.SelectedIndex + separator;
			str += IOCombo.SelectedIndex + separator;
			str += Combo_channelName.Text + separator;
			str += Text_virtualName.Text + separator;
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
			Combo_channelName.Text = strs[2];
			Text_virtualName.Text = strs[3];
			Text_rowIndex.Text = strs[4];
			Text_minVoltage.Text = strs[5];
			Text_maxVoltage.Text = strs[6];
			int tempPlotsCount = int.Parse(strs[7]);
			nodes.Clear();
			for (int i = 0; i < tempPlotsCount; i++) {
				Node Node = new Node(this) { index=i};
				Node.fromSeq(strs[8 + i]);
				nodes.Add(Node);
			}
		}
		//キャンバス中でのノードの高さを計算
		private double plotHeightInCanvas(double voltage,double minVol,double maxVol) {
			if (minVol == maxVol) return height/2;
			double plotheight = (1.0 - 1.0 * (voltage - minVol) / (maxVol - minVol)) * height;
			if (plotheight < 0) plotheight = 0;
			if (plotheight > height) plotheight =height;
			return plotheight;
		}
		//再描画
		public void repaint() {
			repaintPlotLines();
		}
		private void repaintValueBoxes (){
			canvas.Children.Clear();
			for (int i = 0; i < nodes.Count; i++) {
				canvas.Children.Add(nodes[i].Text_value);

				Label lb = new Label() { Content = "V" };
				Canvas.SetLeft(lb, Division.width * i + 40.0);
				canvas.Children.Add(lb);

				canvas.Children.Add(nodes[i].Combo_type);
			}
			repaint();
		}
		public void repaintPlotLines(){

			List<UIElement> deleted=new List<UIElement>();
			foreach (UIElement uie in canvas.Children) {
				if (uie is Line || uie is Ellipse)	deleted.Add(uie);
			}
			foreach (UIElement uie in deleted) {
				canvas.Children.Remove(uie);
			}

			double minVol = minVoltage;
			double maxVol = maxVoltage;
			double circleSize = 4.0;
			int sampleOffset = 0;
			for (int i = 0; i < nodes.Count; i++) {

				Line gridline;
				gridline = new Line() { Stroke = Brushes.Black, StrokeThickness = 2 };
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

				if (isOutput) {
					Ellipse Node = new Ellipse() { Fill = Brushes.Black, StrokeThickness = 2, Width = 8, Height = 8 };
					Canvas.SetLeft(Node, Division.width * i - circleSize);
					if (isAnalog) Canvas.SetTop(Node, plotHeightInCanvas(nodes[i].value, minVol, maxVol) - circleSize);
					else Canvas.SetTop(Node, height / 2 + (nodes[i].value == 0 ? height / 4 : -height / 4) - circleSize);
					canvas.Children.Add(Node);
				}
				Line line;
				int next = i + 1;
				if (next == nodes.Count) continue;

				if (isOutput) {
					// digital out
					if (!isAnalog) {
						line = new Line() { Stroke = Brushes.LightGray, StrokeThickness = 2 };
						line.X1 = Division.width * i;
						line.X2 = Division.width * next;
						line.Y1 = line.Y2 = nodes[i].value == 0 ? height / 2 + height / 4 : height / 2 - height / 4;
						canvas.Children.Add(line);
						if ((nodes[i].value == 0) != (nodes[next].value == 0)) {
							line = new Line() { Stroke = Brushes.LightGray, StrokeThickness = 2 };
							line.X1 = Division.width * next;
							line.X2 = Division.width * next;
							line.Y1 = nodes[i].value == 0 ? height / 2 + height / 4 : height / 2 - height / 4;
							line.Y2 = nodes[next].value == 0 ? height / 2 + height / 4 : height / 2 - height / 4;
							canvas.Children.Add(line);
						}
					}
					// analog out
					else{
						if (nodes[i].type == NodeType.Hold) {
							line = new Line() { Stroke = Brushes.LightGray, StrokeThickness = 2 };
							line.X1 = Division.width * i;
							line.X2 = Division.width * next;
							line.Y1 = plotHeightInCanvas(nodes[i].value, minVol, maxVol);
							line.Y2 = line.Y1;
							canvas.Children.Add(line);
							line = new Line() { Stroke = Brushes.LightGray, StrokeThickness = 2 };
							line.X1 = Division.width * next;
							line.X2 = Division.width * next;
							line.Y1 = plotHeightInCanvas(nodes[i].value, minVol, maxVol);
							line.Y2 = plotHeightInCanvas(nodes[next].value, minVol, maxVol);
							canvas.Children.Add(line);
						} else if (nodes[i].type == NodeType.Linear) {
							line = new Line() { Stroke = Brushes.LightGray, StrokeThickness = 2 };
							line.X1 = Division.width * i;
							line.X2 = Division.width * next;
							line.Y1 = plotHeightInCanvas(nodes[i].value, minVol, maxVol);
							line.Y2 = plotHeightInCanvas(nodes[next].value, minVol, maxVol);
							canvas.Children.Add(line);
						}
					}
				} else {
					//analog in
					if (isAnalog) {
						if (inputWaveArray != null) {
							int divisionSample = parent.getDivisionSampleCount(i);
							int pixStep = Division.width/70;
							int sampleStep = (int)(1.0 * pixStep * divisionSample / Division.width);
							for (int pix = 0; pix < Division.width; pix+=pixStep) {
								int cur = (int)(1.0 * divisionSample * pix / Division.width);
								if (sampleOffset + cur + sampleStep < inputWaveArray.Length) {
									line = new Line() { Stroke = Brushes.Black, StrokeThickness = 1 };
									line.X1 = Division.width * i + pix;
									line.X2 = Math.Min(Division.width * i + pix + pixStep, Division.width*next);
									line.Y1 = plotHeightInCanvas(inputWaveArray[sampleOffset + cur], minVol, maxVol);
									line.Y2 = plotHeightInCanvas(inputWaveArray[sampleOffset + Math.Min(cur + sampleStep,divisionSample-1)], minVol, maxVol);
									canvas.Children.Add(line);
								} else break;
							}
							sampleOffset += divisionSample;
						}
					}
				}
			}
		}
	}
}
