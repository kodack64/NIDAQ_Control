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
		//シーケンスのうち単一の入出力ライン
		public class Channel {
			private const string separator = ",";
			public static readonly int height=80;
			private static int uniqueId = 0;
			public Sequence parent;
			public bool isAnalog { get; private set; }
			public bool isOutput { get; private set; }
			public string bindedName;

			public Canvas channelCanvas;
			public TextBox channelLabel;
			private int currentTargetColumn;
			private int myRow;
			public double minVoltage;
			public double maxVoltage;

			public List<Node> nodes = new List<Node>();
			private List<double[]> samples = new List<double[]>();

			//コンストラクタ
			public Channel(Sequence _parent,int divisionCount) {
				parent = _parent;
				isAnalog = true;
				isOutput = true;
				bindedName = "";
				minVoltage = 0;
				maxVoltage = 10;
				for (int i = 0; i < divisionCount - 1; i++) {
					nodes.Add(new Node() { index = 0, value = 0, type = NodeType.Hold, isEnd = false });
				}
				Border border = new Border();
				nodes.Add(new Node() { index = 0, value = 0, type = NodeType.Hold, isEnd = true });
				channelLabel = new TextBox() { Text="IO "+uniqueId,Background=Brushes.LightGray , ContextMenu = new ContextMenu()};
				channelLabel.ContextMenuOpening += (object sender, ContextMenuEventArgs arg) => CheckContextMenuOfLabel();
				channelCanvas = new Canvas() { Background = Brushes.White , ContextMenu = new ContextMenu()};
				channelCanvas.SetValue(Grid.ColumnSpanProperty, divisionCount);
				channelCanvas.ContextMenuOpening += (object sender, ContextMenuEventArgs e)=>CheckContextMenuOfCanvas();
				uniqueId++;
			}
			//チャンネルラベルのコンテキストメニュー表示
			public void CheckContextMenuOfLabel() {
				channelLabel.ContextMenu.Items.Clear();
				channelLabel.ContextMenu.Items.Add(new MenuItem() { Header = String.Format("{0} {1} - {2}",isAnalog?"Analog":"Digital",isOutput?"Output":"Input",bindedName==""?"None":bindedName) , IsEnabled=false});
				channelLabel.ContextMenu.Items.Add(new MenuItem() { Header = String.Format("Voltage {0}V-{1}V", minVoltage,maxVoltage), IsEnabled = false });
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
				currentTargetColumn = (int)(p.X / height);

				int maxColumn = (int)channelCanvas.GetValue(Grid.ColumnSpanProperty);
				bool isLast = (maxColumn - 1 == currentTargetColumn);

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
				EditIOWindow window = new EditIOWindow(isAnalog,isOutput,bindedName,minVoltage,maxVoltage);
				window.ShowDialog();
				if (window.isOk) {
					isAnalog = window.resultIsAnalog;
					isOutput = window.resultIsOutput;
					bindedName = window.resultBindedName;
					minVoltage = window.resultMinVoltage;
					maxVoltage = window.resultMaxVoltage;
				}
				DebugWindow.WriteLine("チャンネルの情報を更新");
				repaint();
			}
			//自身の行を変更
			public void setPosition(int row) {
				myRow = row;
				channelLabel.SetValue(Grid.RowProperty, row + 1);
				channelLabel.SetValue(Grid.ColumnProperty, 0);
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

			//保存
			public string toSeq() {
				string str="";
				str += isAnalog.ToString() + separator;
				str += isOutput.ToString() + separator;
				str += bindedName + separator;
				str += channelLabel.Text + separator;
				str += myRow + separator;
				str += minVoltage.ToString() + separator;
				str += maxVoltage.ToString() + separator;
				str += nodes.Count + separator;
				for (int i = 0; i < nodes.Count; i++) {
					str += nodes[i].toSeq() + separator;
				}
				return str;
			}
			//書き出し
			public void fromSeq(string str) {
				string[] strs = str.Trim().Split(separator.ToCharArray());
				isAnalog = bool.Parse(strs[0]);
				isOutput = bool.Parse(strs[1]);
				bindedName = strs[2];
				channelLabel.Text = strs[3];
				myRow = int.Parse(strs[4]);
				minVoltage = double.Parse(strs[5]);
				maxVoltage = double.Parse(strs[6]);
				int tempPlotsCount = int.Parse(strs[7]);
				nodes.Clear();
				for (int i = 0; i < tempPlotsCount; i++) {
					Node Node = new Node();
					Node.fromSeq(strs[8 + i]);
					nodes.Add(Node);
				}
			}
			//名前取得
			public string getName() {
				return channelLabel.Text;
			}
			//キャンバス中でのノードの高さを計算
			private double canvasHeight(double voltage) {
				return (1.0 - 1.0 * (voltage - minVoltage) / (maxVoltage - minVoltage)) * height;
			}
			//再描画
			public void repaint() {
				channelCanvas.Children.Clear();
				double circleSize = 4.0;
				for (int i = 0; i < nodes.Count; i++) {
					Ellipse Node = new Ellipse();
					Node.Fill = Brushes.Black;
					Node.StrokeThickness = 2;
					Node.SetValue(Canvas.LeftProperty, height * i - circleSize);
					Node.SetValue(Canvas.TopProperty, canvasHeight(nodes[i].value)-circleSize);
					Node.Width = 8;
					Node.Height = 8;
					channelCanvas.Children.Add(Node);
					if (i+1 < nodes.Count && nodes[i].type!=NodeType.Through) {
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
								line.Stroke = Brushes.Gray;
								line.StrokeThickness = 2;
								line.X1 = Division.width * i;
								line.X2 = Division.width * next;
								line.Y1 = canvasHeight(nodes[i].value);
								line.Y2 = line.Y1;
								channelCanvas.Children.Add(line);
								line = new Line();
								line.Stroke = Brushes.Gray;
								line.StrokeThickness = 2;
								line.X1 = Division.width * next;
								line.X2 = Division.width * next;
								line.Y1 = canvasHeight(nodes[i].value);
								line.Y2 = canvasHeight(nodes[next].value);
								channelCanvas.Children.Add(line);
							} else if (nodes[i].type == NodeType.Linear) {
								Line line = new Line();
								line.Stroke = Brushes.Gray;
								line.StrokeThickness = 2;
								line.X1 = Division.width * i;
								line.X2 = Division.width * next;
								line.Y1 = canvasHeight(nodes[i].value);
								line.Y2 = canvasHeight(nodes[next].value);
								channelCanvas.Children.Add(line);
							}
						}
					}
				}
			}
		}
	}
}
