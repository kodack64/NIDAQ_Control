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
			public Sequence parent;
			public bool isAnalog { get; private set; }
			public bool isOutput { get; private set; }
			public string bindedName;

			public Canvas channelCanvas;
			public TextBox channelLabel;
			private int currentTargetColumn;
			private int myRow;

			protected List<Plot> plots = new List<Plot>();
			public Channel(Sequence _parent,int divisionCount) {
				parent = _parent;
				isAnalog = true;
				isOutput = true;
				bindedName = "";
				for (int i = 0; i < divisionCount - 1; i++) {
					plots.Add(new Plot() { index = 0, value = 0, type = PlotType.Hold, isEnd = false });
				}
				Border border = new Border();
				plots.Add(new Plot() { index = 0, value = 0, type = PlotType.Hold, isEnd = true });
				channelLabel = new TextBox() { Text="new IO",Background=Brushes.LightGray , ContextMenu = new ContextMenu()};
				channelLabel.ContextMenuOpening += (object sender, ContextMenuEventArgs arg) => CheckContextMenuOfLabel();
				channelCanvas = new Canvas() { Background = Brushes.White , ContextMenu = new ContextMenu()};
				channelCanvas.SetValue(Grid.ColumnSpanProperty, divisionCount);
				channelCanvas.ContextMenuOpening += (object sender, ContextMenuEventArgs e)=>CheckContextMenuOfPlots();
			}
			public void CheckContextMenuOfLabel() {
				channelLabel.ContextMenu.Items.Clear();
				channelLabel.ContextMenu.Items.Add(new MenuItem() { Header = String.Format("{0} {1} - {2}",isAnalog?"Analog":"Digital",isOutput?"Output":"Input",bindedName==""?"None":bindedName) , IsEnabled=false});
				MenuItem item;
				item = new MenuItem() { Header="Edit"};
				item.Click += (object sender, RoutedEventArgs arg) => editChannel();
				channelLabel.ContextMenu.Items.Add(item);
			}
			// キャンバス上でメニューがクリックされたらキャンバス上の情報に応じてコンテキストメニューを作成
			public void CheckContextMenuOfPlots() {

				Point p = Mouse.GetPosition(channelCanvas);
				currentTargetColumn = (int)(p.X / 80);

				int maxColumn = (int)channelCanvas.GetValue(Grid.ColumnSpanProperty);
				bool isLast = (maxColumn - 1 == currentTargetColumn);

				channelCanvas.ContextMenu.Items.Clear();
				MenuItem item;
				item = new MenuItem();
				item.Header = "IO:" + myRow + "	Sequence" + currentTargetColumn;
				item.IsEnabled = false;
				channelCanvas.ContextMenu.Items.Add(item);

				channelCanvas.ContextMenu.Items.Add(new Separator());

				item = new MenuItem();
				item.Header = "Edit Value";
				item.Click += ((object s, RoutedEventArgs arg) => this.editValue(currentTargetColumn));
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
			public void editValue(int targetColumn) {
				EditValueWindow window = new EditValueWindow(plots[targetColumn].value,plots[targetColumn].type);
				window.ShowDialog();
				if (window.isOk) {
					plots[targetColumn].type = window.resultType;
					plots[targetColumn].value = window.resultValue;
				}
				repaint();
			}
			public void editChannel() {
				EditIOWindow window = new EditIOWindow(isAnalog,isOutput,bindedName);
				window.ShowDialog();
				if (window.isOk) {
					isAnalog = window.resultIsAnalog;
					isOutput = window.resultIsOutput;
					bindedName = window.resultBindedName;
				}
			}
			public void setPosition(int row) {
				myRow = row;
				channelLabel.SetValue(Grid.RowProperty, row + 1);
				channelLabel.SetValue(Grid.ColumnProperty, 0);
				channelCanvas.SetValue(Grid.RowProperty, row + 1);
				channelCanvas.SetValue(Grid.ColumnProperty, 1);
			}
			public void setSpan(int span) {
				channelCanvas.SetValue(Grid.ColumnSpanProperty, span);
			}
			public void insertPlot(int index, double value) {
				plots.Insert(index, new Plot() { index = index, value = value, type = PlotType.Hold, isEnd = false });
			}
			public void removePlot(int index) {
				plots.RemoveAt(index);
			}
			public void setPlotValue(int index, double value) {
				plots[index].value = value;
			}
			public void setPlotType(int index, PlotType type) {
				plots[index].type = type;
			}

			public string toText() {
				string str;
				str = String.Format("{0},{1},{2},{3}", isAnalog, isOutput, bindedName, plots.Count);
				for (int i = 0; i < plots.Count; i++) {
					str += "," + plots[i].toText();
				}
				return str;
			}
			public void fromText(string str) {
				try {
					plots.Clear();
					string[] strs = str.Split(',');
					isAnalog = bool.Parse(strs[0]);
					isOutput = bool.Parse(strs[1]);
					bindedName = strs[2];
					int plotCount = int.Parse(strs[3]);
					for (int i = 0; i < plotCount; i++) {
						Plot plot = new Plot();
						plot.fromText(strs[4 + i]);
						plots.Add(plot);
					}
				} catch (Exception) {
					// load fail
				}
			}
			public void repaint() {
				channelCanvas.Children.Clear();
				for (int i = 0; i < plots.Count; i++) {
					Ellipse plot = new Ellipse();
					plot.Fill = Brushes.Black;
					plot.StrokeThickness = 2;
					plot.SetValue(Canvas.LeftProperty, 80.0 * i - 4.0);
					plot.SetValue(Canvas.TopProperty, -plots[i].value + 40.0 - 4.0);
					plot.Width = 8;
					plot.Height = 8;
					channelCanvas.Children.Add(plot);
					if (i+1 < plots.Count) {
						if (plots[i].type != PlotType.Through) {
							int next;

							for (next = i+1; next<plots.Count; next++ ){
								if (plots[next].type != PlotType.Through) {
									break;
								}
							}
							if (next < plots.Count) {
								if (plots[i].type == PlotType.Hold) {
									Line line;
									line = new Line();
									line.Stroke = Brushes.Gray;
									line.StrokeThickness = 2;
									line.X1 = 80 * i;
									line.X2 = 80 * next;
									line.Y1 = -plots[i].value + 40;
									line.Y2 = -plots[i].value + 40;
									channelCanvas.Children.Add(line);
									line = new Line();
									line.Stroke = Brushes.Gray;
									line.StrokeThickness = 2;
									line.X1 = 80 * next;
									line.X2 = 80 * next;
									line.Y1 = -plots[i].value + 40;
									line.Y2 = -plots[next].value + 40;
									channelCanvas.Children.Add(line);
								} else if (plots[i].type == PlotType.Linear) {
									Line line = new Line();
									line.Stroke = Brushes.Gray;
									line.StrokeThickness = 2;
									line.X1 = 80 * i;
									line.X2 = 80 * next;
									line.Y1 = -plots[i].value + 40;
									line.Y2 = -plots[next].value + 40;
									channelCanvas.Children.Add(line);
								}
							}
						}
					}
				}
			}
		}
	}
}
