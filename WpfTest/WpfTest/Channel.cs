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
using NIDaqInterface;

namespace WpfTest {
	namespace NIDaq {
		//シーケンスのうち単一の入出力ライン
		public class Channel {
			//描画時の高さ
			public static readonly int height=130;
			//ユニークID
			private static int uniqueId = 0;
			//自身の所属
			public Sequence parent;

			//チャンネル情報のパネル
			public StackPanel stackPanelLabel;
			public UIElement getPanel() { return stackPanelLabel; }
			//チャンネルの行表示
			private Label channelLabel;
			public int getRow() { return int.Parse((string)channelLabel.Content); }
			//チャンネルの名前
			private TextBox channelName;
			public string getName() { return channelName.Text; }
			//チャンネルのデバイス
			private ComboBox channelDevice;
			public string getDevice() { return channelDevice.Text; }
			public void setDevice(string str) { channelDevice.Text=str; }
			//チャンネルのAD選択
			private ComboBox ADCombo;
			public bool isAnalog() { return ADCombo.SelectedIndex == 0; }
			public void setIsAnalog(bool analog) { ADCombo.SelectedIndex = analog ? 0 : 1; }
			//チャンネルのIO選択
			private ComboBox IOCombo;
			public bool isOutput() { return IOCombo.SelectedIndex == 0; }
			public void setIsOutput(bool output) { IOCombo.SelectedIndex = output ? 0 : 1; }
			//最小電圧
			private TextBox minVoltage;
			public double getMinVoltage() { return double.Parse(minVoltage.Text); }
			//最大電圧
			private TextBox maxVoltage;
			public double getMaxVoltage() { return double.Parse(maxVoltage.Text); }

			//チャンネルの時系列を表示するキャンバス
			public Canvas channelCanvas;

			//右クリック時選択中の列
			private int currentTargetColumn;

			//ノード
			public List<Node> nodes = new List<Node>();
			//コンパイル時の区間ごとの配列
			private List<double[]> samples = new List<double[]>();

			//コンストラクタ
			public Channel(Sequence _parent, int divisionCount) {
				parent = _parent;
				stackPanelLabel = new StackPanel() { };

				channelLabel = new Label() { Background=Brushes.Black , Foreground=Brushes.White };
				channelName = new TextBox() { Text = "IO " + uniqueId, Background = Brushes.LightGray, ContextMenu = new ContextMenu() };
//				channelLabel.ContextMenuOpening += (object sender, ContextMenuEventArgs arg) => CheckContextMenuOfLabel();
				uniqueId++;
				ADCombo = new ComboBox() { };
				ADCombo.Items.Add("Ana"); ADCombo.Items.Add("Dig");
				IOCombo = new ComboBox() { };
				IOCombo.Items.Add("Out"); IOCombo.Items.Add("In");
				channelDevice = new ComboBox() { };
				foreach (string str in NIDaqTaskManager.GetInstance().getAnalogOutputList()) {
					channelDevice.Items.Add(str);
				}
				minVoltage = new TextBox() { Text = "-1" };
				maxVoltage = new TextBox() { Text = "1" };

				stackPanelLabel.Children.Add(channelLabel);
				stackPanelLabel.Children.Add(channelName);
				{
					StackPanel miniStack = new StackPanel() { Orientation = Orientation.Horizontal };
					miniStack.Children.Add(ADCombo);
					miniStack.Children.Add(IOCombo);
					stackPanelLabel.Children.Add(miniStack);
				}
				stackPanelLabel.Children.Add(channelDevice);
				{
					StackPanel miniStack = new StackPanel() { Orientation = Orientation.Horizontal };
					miniStack.Children.Add(minVoltage);
					miniStack.Children.Add(new Label() { Content = "V - " });
					miniStack.Children.Add(maxVoltage);
					miniStack.Children.Add(new Label() { Content = "V" });
					stackPanelLabel.Children.Add(miniStack);
				}

				for (int i = 0; i < divisionCount - 1; i++) {
					nodes.Add(new Node() { index = 0, value = 0, type = NodeType.Hold, isEnd = false });
				}

				nodes.Add(new Node() { index = 0, value = 0, type = NodeType.Hold, isEnd = true });

				channelCanvas = new Canvas() { Background = Brushes.White, ContextMenu = new ContextMenu() };
				channelCanvas.SetValue(Grid.ColumnSpanProperty, divisionCount);
				channelCanvas.ContextMenuOpening += (object sender, ContextMenuEventArgs e) => CheckContextMenuOfCanvas();
			}
			//チャンネルラベルのコンテキストメニュー表示
			public void CheckContextMenuOfLabel() {
				channelLabel.ContextMenu.Items.Clear();
				channelLabel.ContextMenu.Items.Add(new MenuItem() { Header = String.Format("{0} {1} - {2}",ADCombo.Text,IOCombo.Text,channelDevice.Text) , IsEnabled=false});
				channelLabel.ContextMenu.Items.Add(new MenuItem() { Header = String.Format("Voltage {0}V-{1}V", minVoltage.Text,maxVoltage.Text), IsEnabled = false });
				int myRow = int.Parse((string)channelLabel.Content);
				MenuItem item;
				item = new MenuItem() { Header="Edit Channel"};
				item.Click += (object sender, RoutedEventArgs arg) => editChannel();
				channelLabel.ContextMenu.Items.Add(item);
				item = new MenuItem() { Header = "Move Up" };
				item.Click += (object sender, RoutedEventArgs arg) => parent.moveUp(myRow);
				if (myRow == 0) item.IsEnabled = false;
				channelLabel.ContextMenu.Items.Add(item);
				item = new MenuItem() { Header = "Move Down" };
				item.Click += (object sender, RoutedEventArgs arg) => parent.moveDown(myRow);
				if (myRow + 1 == parent.getChannelCount()) item.IsEnabled = false;
				channelLabel.ContextMenu.Items.Add(item);
				item = new MenuItem() { Header = "Remove This Channel" };
				item.Click += (object sender, RoutedEventArgs arg) => parent.removeChannel(myRow);
				channelLabel.ContextMenu.Items.Add(item);
			}

			//キャンバスのコンテキストメニュー表示
			public void CheckContextMenuOfCanvas() {

				Point p = Mouse.GetPosition(channelCanvas);
				currentTargetColumn = (int)(p.X / Division.width);

				int maxColumn = (int)channelCanvas.GetValue(Grid.ColumnSpanProperty);
				bool isLast = (maxColumn - 1 == currentTargetColumn);
				int myRow = int.Parse((string)channelLabel.Content);

				channelCanvas.ContextMenu.Items.Clear();
				MenuItem item;
				item = new MenuItem();
				item.Header = parent.getChannelName(myRow) + " - " + parent.getDivisionName(currentTargetColumn);
				item.IsEnabled = false;
				channelCanvas.ContextMenu.Items.Add(item);

				channelCanvas.ContextMenu.Items.Add(new Separator());

				item = new MenuItem();
				item.Header = "Edit Value";
				item.Click += ((object s, RoutedEventArgs arg) => this.editNode(currentTargetColumn));
				channelCanvas.ContextMenu.Items.Add(item);

				channelCanvas.ContextMenu.Items.Add(new Separator());

				MenuItem itemHead;
				itemHead = new MenuItem();
				itemHead.Header = "Edit Sequence";
				{
					item = new MenuItem();
					item.Header = "Insert Division to ←";
					item.Click += ((object s,RoutedEventArgs arg) => parent.insertDivision(currentTargetColumn));
					itemHead.Items.Add(item);
					item = new MenuItem();
					item.Header = "Insert Channel to ↓";
					item.Click += ((object s, RoutedEventArgs arg) => parent.insertChannel(myRow));
					itemHead.Items.Add(item);
					item = new MenuItem();
					item.Header = "Remove This Division";
					item.Click += ((object s, RoutedEventArgs arg) => parent.removeDivision(currentTargetColumn));
					if (isLast) item.IsEnabled = false;
					itemHead.Items.Add(item);
					item = new MenuItem();
					item.Header = "Remove This Channel";
					item.Click += ((object s, RoutedEventArgs arg) => parent.removeChannel(myRow));
					itemHead.Items.Add(item);
				}
				channelCanvas.ContextMenu.Items.Add(itemHead);
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
				EditIOWindow window = new EditIOWindow(ADCombo.SelectedIndex==0,IOCombo.SelectedIndex==0,channelDevice.Text,double.Parse(minVoltage.Text),double.Parse(maxVoltage.Text));
				window.ShowDialog();
				if (window.isOk) {
					ADCombo.SelectedIndex = window.resultIsAnalog?0:1;
					IOCombo.SelectedIndex = window.resultIsOutput?0:1;
					channelDevice.Text = window.resultBindedName;
					minVoltage.Text = window.resultMinVoltage.ToString();
					maxVoltage.Text = window.resultMaxVoltage.ToString();
				}
				DebugWindow.WriteLine("チャンネルの情報を更新");
				repaint();
			}
			//自身の行を変更
			public void setPosition(int row) {
				channelLabel.Content = row.ToString();
				stackPanelLabel.SetValue(Grid.RowProperty, row + 1);
				stackPanelLabel.SetValue(Grid.ColumnProperty, 0);
				channelCanvas.SetValue(Grid.RowProperty, row + 1);
				channelCanvas.SetValue(Grid.ColumnProperty, 1);
			}
			//divisionの数を更新
			public void setSpan(int span) {
				channelCanvas.SetValue(Grid.ColumnSpanProperty, span);
			}
			//ノードを挿入
			public void insertNode(int index, double value) {
				nodes.Insert(index, new Node() { index = index, value = value, type = NodeType.Hold, isEnd = false });
			}
			//ノードを削除
			public void removePlot(int index) {
				nodes.RemoveAt(index);
			}

			private const string separator = ",";
			//保存
			public string toSeq() {
				string str="";
				str += ADCombo.SelectedIndex + separator;
				str += IOCombo.SelectedIndex + separator;
				str += channelDevice.Text + separator;
				str += channelName.Text + separator;
				str += channelLabel.Content + separator;
				str += minVoltage.Text + separator;
				str += maxVoltage.Text + separator;
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
				channelDevice.Text = strs[2];
				channelName.Text = strs[3];
				channelLabel.Content = strs[4];
				minVoltage.Text = strs[5];
				maxVoltage.Text = strs[6];
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
				channelCanvas.Children.Clear();
				double minVol = double.Parse(minVoltage.Text);
				double maxVol = double.Parse(maxVoltage.Text);
				double circleSize = 4.0;
				for (int i = 0; i < nodes.Count; i++) {
					Ellipse Node = new Ellipse();
					Node.Fill = Brushes.Black;
					Node.StrokeThickness = 2;
					Node.SetValue(Canvas.LeftProperty, Division.width * i - circleSize);
					Node.SetValue(Canvas.TopProperty, canvasHeight(nodes[i].value,minVol,maxVol)-circleSize);
					Node.Width = 8;
					Node.Height = 8;
					channelCanvas.Children.Add(Node);
					
					Line gridline;
					gridline = new Line();
					gridline.Stroke = Brushes.Black;
					gridline.StrokeThickness = 2;
					gridline.X1 = 0;
					gridline.X2 = Division.width * nodes.Count;
					gridline.Y1 = 0;
					gridline.Y2 = 0;
					channelCanvas.Children.Add(gridline);
					gridline = new Line();
					gridline.Stroke = Brushes.Black;
					gridline.StrokeThickness = 2;
					gridline.X1 = 0;
					gridline.X2 = Division.width * nodes.Count;
					gridline.Y1 = height;
					gridline.Y2 = height;
					channelCanvas.Children.Add(gridline);

					if (i + 1 < nodes.Count && nodes[i].type != NodeType.Through) {
						int next;
						for (next = i+1; next<nodes.Count; next++ ){
							if (nodes[next].type != NodeType.Through) {
								break;
							}
						}
						if (next < nodes.Count) {
							if (nodes[i].type == NodeType.Hold) {
								Line line;
								line = new Line();
								line.Stroke = Brushes.LightGray;
								line.StrokeThickness = 2;
								line.X1 = Division.width * i;
								line.X2 = Division.width * next;
								line.Y1 = canvasHeight(nodes[i].value,minVol,maxVol);
								line.Y2 = line.Y1;
								channelCanvas.Children.Add(line);
								line = new Line();
								line.Stroke = Brushes.LightGray;
								line.StrokeThickness = 2;
								line.X1 = Division.width * next;
								line.X2 = Division.width * next;
								line.Y1 = canvasHeight(nodes[i].value, minVol, maxVol);
								line.Y2 = canvasHeight(nodes[next].value, minVol, maxVol);
								channelCanvas.Children.Add(line);
							} else if (nodes[i].type == NodeType.Linear) {
								Line line = new Line();
								line.Stroke = Brushes.LightGray;
								line.StrokeThickness = 2;
								line.X1 = Division.width * i;
								line.X2 = Division.width * next;
								line.Y1 = canvasHeight(nodes[i].value, minVol, maxVol);
								line.Y2 = canvasHeight(nodes[next].value, minVol, maxVol);
								channelCanvas.Children.Add(line);
							}
						}
					}
				}
			}
		}
	}
}
