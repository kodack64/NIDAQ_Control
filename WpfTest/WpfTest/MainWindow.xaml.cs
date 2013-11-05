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
using System.Threading;
using System.Diagnostics;
using Microsoft.Win32;

namespace WpfTest {


	/// <summary>
	/// MainWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class MainWindow : Window {

	
		// debug log window
		DebugWindow debugWindow;

		// worker thread
		Thread workerThread;

		// worker object
		NIDaqCommunicator communicator;

		// signal sequence
		NIDaqSequence seq;
		
		// sequence label array
		List<Label> sequenceLabelList = new List<Label>();
		private int uniqueSequenceId = 0;
		// IO label array
		List<Label> IOLabelList = new List<Label>();
		private int uniqueIOLabelId = 0;
		// canvas array
		List<Canvas> canvasList = new List<Canvas>();

		// constructor
		public MainWindow() {
			InitializeComponent();
			debugWindow = new DebugWindow();
			debugWindow.Show();

			seq = NIDaqSequence.getEmptyInstance();
			communicator = new NIDaqCommunicator(seq);
			workerThread = new Thread(communicator.Run);

			this.MouseLeftButtonDown += (sender, e) => this.DragMove();

			this.InsertColumn(0);
		}

		public class UIElementFactory {
			public static Label createIOLabel(int id) {
				return null;
			}
			public static Label createSequenceLabel(int id) {
				return null;
			}
			public static Canvas createCanvas(int id) {
				return null;
			}
		}

		// callback insert column to sequence
		private void Callback_InsertColumn(object sender, RoutedEventArgs e) {
			// if event invoker is canvas grid cell , insert left of cell column
			if(e.Source is MenuItem){
				Canvas canvas = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget as Canvas;
				if (canvas != null) {
					int? column = canvas.GetValue(Grid.ColumnProperty) as int?;
					if (column.HasValue) {
						this.InsertColumn(column.Value);
					}
				}
			} else {
				// if event invoker is button , insert right most
				this.InsertColumn(sequenceLabelList.Count);
			}
		}

		// callback insert row to sequence
		private void Callback_InsertRow(object sender, RoutedEventArgs e) {
			// if event invoker is canvas grid cell , insert bottom of cell row
			if (e.Source is MenuItem) {
				Canvas canvas = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget as Canvas;
				if (canvas != null) {
					int? row = canvas.GetValue(Grid.RowProperty) as int?;
					if (row.HasValue) {
						this.InsertRow(row.Value);
					}
				}
			} else {
				// if event invoker is button , insert lower most
				this.InsertRow(IOLabelList.Count);
			}
		}

		// callback erase column to sequence
		private void Callback_EraseColumn(object sender, RoutedEventArgs e) {
			// if event invoker is canvas grid cell , erase this column
			if (e.Source is MenuItem) {
				Canvas canvas = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget as Canvas;
				if (canvas != null) {
					int? column = canvas.GetValue(Grid.ColumnProperty) as int?;
					if (column.HasValue) {
						this.EraseColumn(column.Value);
					}
				}
			}
		}

		// callback insert row to sequence
		private void Callback_EraseRow(object sender, RoutedEventArgs e) {
			// if event invoker is canvas grid cell , erase this row
			if (e.Source is MenuItem) {
				Canvas canvas = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget as Canvas;
				if (canvas != null) {
					int? row = canvas.GetValue(Grid.RowProperty) as int?;
					if (row.HasValue) {
						this.EraseRow(row.Value);
					}
				}
			}
		}

		// insert new column to UI
		private void InsertColumn(int index) {

			// extends grid
			SequenceGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(80) });

			// add new label instance
			Label label = new Label() { Content = String.Format("Seq{0}", uniqueSequenceId), HorizontalAlignment = HorizontalAlignment.Left, Name = String.Format("SeqLabel{0}", index) };
			label.SetValue(Grid.ColumnProperty, index+1);
			label.SetValue(Grid.RowProperty, 0);
			SequenceGrid.Children.Add(label);
			sequenceLabelList.Insert(index,label);
			uniqueSequenceId++;

			// expands column span of all canvases
			for (int row = 0; row < canvasList.Count; row++) {
				canvasList[row].SetValue(Grid.ColumnSpanProperty, sequenceLabelList.Count + 1);
			}

			// re-label right sequence label cell index
			for (int column = index+1 ; column < sequenceLabelList.Count ; column++) {
				sequenceLabelList[column].SetValue(Grid.ColumnProperty, column + 1);
			}
		}

		// insert new row to UI
		private void InsertRow(int index) {

			// extend grid
			SequenceGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(80) });

			// add new IO label instance
			Label label = new Label() { Content = String.Format("IO{0}", uniqueIOLabelId), HorizontalAlignment = HorizontalAlignment.Left };
			label.SetValue(Grid.ColumnProperty, 0);
			label.SetValue(Grid.RowProperty, index+1);
			SequenceGrid.Children.Add(label);
			IOLabelList.Insert(index, label);
			uniqueIOLabelId++;

			// add new canvas
			Canvas canvas = new Canvas() { Background = Brushes.White };
			canvas.SetValue(Grid.ColumnProperty, 1);
			canvas.SetValue(Grid.RowProperty, index+1);
			canvas.SetValue(Grid.ColumnSpanProperty, sequenceLabelList.Count);
			canvas.ContextMenu = new ContextMenu();
			MenuItem item = new MenuItem();
			item.Header = "Insert Column to →";
			item.Click += Callback_InsertColumn;
			canvas.ContextMenu.Items.Add(item);
			item = new MenuItem();
			item.Header = "Insert Row to ↓";
			item.Click += Callback_InsertRow;
			canvas.ContextMenu.Items.Add(item);
			item = new MenuItem();
			item.Header = "Erase This Row";
			item.Click += Callback_EraseRow;
			canvas.ContextMenu.Items.Add(item);
			item = new MenuItem();
			item.Header = "Erase This Column";
			item.Click += Callback_EraseColumn;
			canvas.ContextMenu.Items.Add(item);
	
			SequenceGrid.Children.Add(canvas);
			canvasList.Insert(index, canvas);


			// re-label IO label and canvas
			for (int row = index + 1; row < canvasList.Count; row++) {
				canvasList[row].SetValue(Grid.RowProperty, row + 1);
				IOLabelList[row].SetValue(Grid.RowProperty, row + 1);
			}
		}

		// erase column
		private void EraseColumn(int index) {

			// remove label instance
			SequenceGrid.Children.Remove(sequenceLabelList[index-1]);
			sequenceLabelList.RemoveAt(index-1);

			// shrink column span of all canvases
			for (int row = 0; row < canvasList.Count; row++) {
				canvasList[row].SetValue(Grid.ColumnSpanProperty, sequenceLabelList.Count);
			}

			// re-label right sequence label cell index
			for (int column = index-1 ; column < sequenceLabelList.Count; column++) {
				sequenceLabelList[column].SetValue(Grid.ColumnProperty, column +1);
			}

			// remove column
			SequenceGrid.ColumnDefinitions.RemoveAt(SequenceGrid.ColumnDefinitions.Count-1);

		}


		// erase row
		private void EraseRow(int index) {

			// remove label and canvasn instance
			SequenceGrid.Children.Remove(IOLabelList[index-1]);
			SequenceGrid.Children.Remove(canvasList[index-1]);
			IOLabelList.RemoveAt(index-1);
			canvasList.RemoveAt(index-1);

			// re-label IO label and canvas
			for (int row = index-1 ; row < canvasList.Count; row++) {
				canvasList[row].SetValue(Grid.RowProperty, row +1);
				IOLabelList[row].SetValue(Grid.RowProperty, row +1);
			}

			// remove row
			SequenceGrid.RowDefinitions.RemoveAt(SequenceGrid.RowDefinitions.Count - 1);

		}


		// maximize window
		private void WindowMinimize(object sender, RoutedEventArgs e) {
			this.WindowState = WindowState.Minimized;
		}
		// minimize window
		private void WindowMaximize(object sender, RoutedEventArgs e) {
			this.WindowState = WindowState.Maximized;
			ToggleFullscreen.Content = "2";
			ToggleFullscreen.Click -= this.WindowMaximize;
			ToggleFullscreen.Click += this.WindowRestore;
		}
		// restore window
		private void WindowRestore(object sender, RoutedEventArgs e) {
			this.WindowState = WindowState.Normal;
			ToggleFullscreen.Content = "1";
			ToggleFullscreen.Click += this.WindowMaximize;
			ToggleFullscreen.Click -= this.WindowRestore;
		}
		// close window
		private void WindowClose(object sender, RoutedEventArgs e) {
			if (workerThread.IsAlive) {
				communicator.Stop();
				debugWindow.WriteLine("Request stop communicator");
				workerThread.Join();
				debugWindow.WriteLine("Communicator is terminated");
			}
			debugWindow.Close();
			this.Close();
		}

		// run button toggled
		private void SystemRun(object sender, RoutedEventArgs e) {
			ToggleButton element = sender as ToggleButton;
			if (element.IsChecked.HasValue && element.IsChecked.Value) {
				if (!workerThread.IsAlive) {
					workerThread = new Thread(communicator.Run);
					workerThread.Start();
					communicator.debugWindow = debugWindow;
					Button_Run.Content = "Stop Sequence";
					debugWindow.WriteLine("Run communicator");
				} else {
					debugWindow.WriteLine("Error : Try to running communicator but communicator is running now");
				}
			} else {
				communicator.Stop();
				debugWindow.WriteLine("Request communicator to stop...");
				workerThread.Join();
				debugWindow.WriteLine("Communicator is terminated");
				Button_Run.Content = "Run Sequence";
			}
			debugWindow.scroleToEnd();
		}

		// load sequence from file
		private void LoadSequence(object sender, RoutedEventArgs e) {
			OpenFileDialog dialog = new OpenFileDialog() {
				Multiselect=false,
				Filter="seqファイル|*.seq"
			};
			bool? result = dialog.ShowDialog();
			if (result.HasValue) {
				if(result.Value){
					string fileName = dialog.FileName;
					debugWindow.WriteLine(String.Format("Open sequence file {0}",fileName));
				}
			}
		}

		// save sequence to file
		private void SaveSequence(object sender, RoutedEventArgs e) {
			SaveFileDialog dialog = new SaveFileDialog() {
				Filter="seqファイル|*.seq"
			};
			bool? result = dialog.ShowDialog();
			if (result.HasValue) {
				if (result.Value) {
					string fileName = dialog.FileName;
					debugWindow.WriteLine(String.Format("Save sequence file {0}",fileName));
				}
			}
		}
	}

	public class SequencePoint {
		int index;
		double position;
		double span;
		double value;
		bool isEnd;
		int type;
	}
	public class NIDaqSequence {
		private NIDaqSequence(){}
		private List<SequencePoint> value = new List<SequencePoint>();

		// only accessed from UI thread
		static public NIDaqSequence getEmptyInstance() {
			return new NIDaqSequence();
		}
		static public NIDaqSequence fromString() {
			return new NIDaqSequence();
		}
		public string toString() {
			return "";
		}
	
		// only accessed from worker thread
		public double getAnalogValue(int id, double time) {
			return 0;
		}
		public bool getDigitalValue(int id,double time) {
			return false;
		}
		public void setAnalogValue(int id, double time , double value) {
		}
		public void setDigitalValue(int id, double time , bool value) {
		}
	}

	public class NIDaqCommunicator {
		public DebugWindow debugWindow=null;
		private NIDaqSequence seq;
		private int maxAnalogInput;
		private int maxAnalogOutput;
		private int maxDigitalInput;
		private int maxDigitalOutput;
		private double frequency;
		private volatile bool runningFlag;

		public NIDaqCommunicator(NIDaqSequence _seq) {
			seq = _seq;
			maxAnalogInput = 10;
			maxAnalogOutput = 10;
			maxDigitalInput = 10;
			maxDigitalOutput = 10;
			frequency = 1e6;
			runningFlag = false;
		}
		public void changeFrequence(int freq) {
			frequency = freq;
		}
		public void Run() {
			runningFlag = true;
			long loopCount;
			Stopwatch sw = new Stopwatch();
			double fps = 1.0 / frequency;
			double currentTime,nextTime,difTime,worstDifTime;
			double analogValue;
			bool digitalValue;


			nextTime = 0;
			difTime = 0;
			loopCount = 0;
			worstDifTime = 0;
			sw.Start();
			while (runningFlag) {
				currentTime = (double)sw.ElapsedTicks / Stopwatch.Frequency;
				if (currentTime < nextTime) {
					continue;
				} else {
					difTime += Math.Abs(nextTime - currentTime);
					if (Math.Abs(nextTime - currentTime) > worstDifTime) {
						worstDifTime = Math.Abs(nextTime - currentTime);
					}
					nextTime += fps;
					loopCount++;
				}

				for (int i = 0; i < maxAnalogOutput; i++) {
					analogValue = seq.getAnalogValue(i,currentTime);
					// to daq
				}
				for (int i = 0; i < maxDigitalOutput; i++) {
					digitalValue = seq.getDigitalValue(i,currentTime);
					// to daq
				}
				for (int i = 0; i < maxAnalogInput; i++) {
					// from daq
					analogValue = 0;
					seq.setAnalogValue(i, currentTime,analogValue);
				}
				for (int i = 0; i < maxDigitalInput; i++) {
					// from daq
					digitalValue = false;
					seq.setDigitalValue(i, currentTime, digitalValue);
				}
			}
			sw.Stop();

			if (debugWindow != null) {
				debugWindow.WriteLineAsyc(String.Format("Communicator thread stops"));
				debugWindow.WriteLineAsyc(String.Format("*** Running Result ***"));
				debugWindow.WriteLineAsyc(String.Format(" Highresolution timer = {0}", Stopwatch.IsHighResolution));
				debugWindow.WriteLineAsyc(String.Format(" Running Time = {0} sec", sw.ElapsedMilliseconds*1e-3));
				debugWindow.WriteLineAsyc(String.Format(" I/O Ideal Update Count = {0}", sw.ElapsedMilliseconds * 1e-3 * frequency));
				debugWindow.WriteLineAsyc(String.Format(" I/O Update Count = {0}", loopCount));
				debugWindow.WriteLineAsyc(String.Format(" Ideal Update Frequency = {0} Hz", frequency));
				debugWindow.WriteLineAsyc(String.Format(" Update Frequency = {0} Hz", (double)1e3*loopCount / sw.ElapsedMilliseconds));
				debugWindow.WriteLineAsyc(String.Format(" I/O Average Precision = {0} sec", difTime / loopCount));
				debugWindow.WriteLineAsyc(String.Format(" I/O Worst Precision = {0} sec", worstDifTime));
				debugWindow.WriteLineAsyc(String.Format("***"));
			}
		}
		public void Stop() {
			runningFlag = false;
		}
	}
}
