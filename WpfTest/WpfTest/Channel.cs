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
			public bool isInput { get; private set; }
			public bool isBinded { get; private set; }
			public string bindedName;

			public Canvas channelCanvas;
			public Label channelLabel;
			private int currentTargetColumn;
			private int myRow;

			protected List<Plot> plots = new List<Plot>();
			public Channel(Sequence _parent,int divisionCount) {
				parent = _parent;
				isAnalog = true;
				isInput = false;
				isBinded = false;
				bindedName = "";
				for (int i = 0; i < divisionCount - 1; i++) {
					plots.Add(new Plot() { index = 0, value = 0, type = PlotType.Hold, isEnd = false });
				}
				plots.Add(new Plot() { index = 0, value = 0, type = PlotType.Hold, isEnd = true });
				channelCanvas = new Canvas() {Background=Brushes.White };
				channelLabel = new Label() { Content="new IO",Background=Brushes.LightGray};
				channelCanvas.SetValue(Grid.ColumnSpanProperty, divisionCount);
				channelCanvas.ContextMenuOpening += new ContextMenuEventHandler(CheckContextMenu);
			}
			// キャンバス上でメニューがクリックされたらキャンバス上の情報に応じてコンテキストメニューを作成
			public void CheckContextMenu(object sender, ContextMenuEventArgs e) {

				Point p = Mouse.GetPosition(channelCanvas);
				currentTargetColumn = (int)(p.X / 80);

				int maxColumn = (int)channelCanvas.GetValue(Grid.ColumnSpanProperty);
				bool isLast = (maxColumn - 1 == currentTargetColumn);

				channelCanvas.ContextMenu = new ContextMenu();
				MenuItem item;
				item = new MenuItem();
				item.Header = "IO:" + myRow + "	Sequence" + currentTargetColumn;
				item.IsEnabled = false;
				channelCanvas.ContextMenu.Items.Add(item);

				channelCanvas.ContextMenu.Items.Add(new Separator());

				item = new MenuItem();
				item.Header = "Edit Value";
				item.Click += ((object s, RoutedEventArgs arg) => this.editValue());
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
			public void editValue() {
				MainWindow window = new MainWindow();
				window.Show();
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
				str = String.Format("{0},{1},{2},{3}", isAnalog, isInput, isBinded, bindedName, plots.Count);
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
					isInput = bool.Parse(strs[1]);
					isBinded = bool.Parse(strs[2]);
					bindedName = strs[3];
					int plotCount = int.Parse(strs[4]);
					for (int i = 0; i < plotCount; i++) {
						Plot plot = new Plot();
						plot.fromText(strs[5 + i]);
						plots.Add(plot);
					}
				} catch (Exception e) {
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
					plot.SetValue(Canvas.TopProperty, plots[i].value + 40.0 - 4.0);
					plot.Width = 8;
					plot.Height = 8;
					channelCanvas.Children.Add(plot);
					if (i != 0) {
						Line line = new Line();
						line.Stroke = Brushes.Gray;
						line.StrokeThickness = 2;
						line.X1 = 80 * (i - 1);
						line.X2 = 80 * i;
						line.Y1 = plots[i - 1].value + 40;
						line.Y2 = plots[i].value + 40;
						channelCanvas.Children.Add(line);
					}
				}
			}
		}
		public class DivisionLabel {
			private Sequence parent;
			public Label label;
			public int time;
			public TimeUnit units;
			public string toText() { return ""; }
			public void fromText(string s) { }
			public DivisionLabel(Sequence _parent) {
				parent = _parent;
				label = new Label() { Content = "new division" , Background = Brushes.LightGray};
				time = 1;
				units = TimeUnit.s;
			}
			public void setPosition(int i) {
				label.SetValue(Grid.RowProperty, 0);
				label.SetValue(Grid.ColumnProperty, i+1);
			}
		}

		public class Plot {
			public int index;
			public double value;
			public PlotType type;
			public bool isEnd;
			public string toText() {
				string str;
				str = String.Format("{0} {1} {2} {3}", index, value, Enum.GetName(typeof(PlotType), type), isEnd);
				return str;
			}
			public void fromText(string str) {
				try {
					string[] strs = str.Split(' ');
					index = int.Parse(strs[0]);
					value = double.Parse(strs[1]);
					type = (PlotType)Enum.Parse(typeof(PlotType), strs[2]);
					isEnd = bool.Parse(strs[3]);
				} catch (Exception e) {
					// load fail
				}
			}
		}
		public enum PlotType {
			Hold,
			Linear,
			Through
		}
		public enum TimeUnit {
			s,
			ms,
			us,
			ns
		}
	}
}
