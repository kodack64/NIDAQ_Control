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
		
		// Column array
		List<ColumnInfo> columnList = new List<ColumnInfo>();
		private int uniqueColumnId = 0;

		// Row array
		List<RowInfo> rowList = new List<RowInfo>();
		private int uniqueRowId = 0;

		static MainWindow myInstance;

		class ColumnInfo {
			public Label sequenceLabel { get; private set; }
			public string myName { get; set; }
			public int myIndex { get; set; }
			public int timespan { get; set; }
			public TimeUnit timeUnit { get; set; }
			public enum TimeUnit {
				s,
				ms,
				us
			}
			public ColumnInfo() {
				sequenceLabel = new Label() { Background=Brushes.Gray};
				sequenceLabel.SetValue(Grid.RowProperty,0);
			}
			public void SetName(string name) {
				myName = name;
				sequenceLabel.Content = myName;
			}
			public void SetColumn(int index) {
				sequenceLabel.SetValue(Grid.ColumnProperty,index+1);
			}
		}
		class RowInfo {
			public Label IOLabel{get;private set;}
			public Canvas canvas { get; private set; }
			public string myName { get; set; }
			public int myBindingId { get; set; }
			public bool isAnalog { get; set; }
			public List<double> positionArray { get; set; }
			public RowInfo() {
				IOLabel = new Label() { Background=Brushes.Gray};
				canvas = new Canvas() { Background=Brushes.White};
				canvas.ContextMenuOpening += new ContextMenuEventHandler(CheckContextMenu);
				positionArray = new List<double>();
				IOLabel.SetValue(Grid.ColumnProperty,0);
				canvas.SetValue(Grid.ColumnProperty, 1);
			}
			public void CheckContextMenu(object sender,ContextMenuEventArgs e){

			}
			public void SetName(string name) {
				myName = name;
				IOLabel.Content = myName;
			}
			public void AddColumn(int column,int inserted) {
//				positionArray.Insert(inserted, positionArray[inserted]);
				canvas.SetValue(Grid.ColumnSpanProperty,column);
			}
			public void EraseColumn(int column,int erased) {
//				positionArray.Remove(erased);
				canvas.SetValue(Grid.ColumnSpanProperty, column);
			}
			public void SetRow(int index) {
				IOLabel.SetValue(Grid.RowProperty,index+1);
				canvas.SetValue(Grid.RowProperty, index + 1);
			}
			public void SetColumnSpan(int span) {
				canvas.SetValue(Grid.ColumnSpanProperty, span);
				canvas.ContextMenu = new ContextMenu();
				MenuItem item = new MenuItem();
				item.Header = "Insert Column to →";
				item.Click += MainWindow.myInstance.Callback_InsertColumn;
				canvas.ContextMenu.Items.Add(item);
				item = new MenuItem();
				item.Header = "Insert Row to ↓";
				item.Click += MainWindow.myInstance.Callback_InsertRow;
				canvas.ContextMenu.Items.Add(item);
				item = new MenuItem();
				item.Header = "Erase This Row";
				item.Click += MainWindow.myInstance.Callback_EraseRow;
				canvas.ContextMenu.Items.Add(item);
				item = new MenuItem();
				item.Header = "Erase This Column";
				item.Click += MainWindow.myInstance.Callback_EraseColumn;
				canvas.ContextMenu.Items.Add(item);

				positionArray.Clear();
				for (int i = 0; i < span; i++) {
					positionArray.Add(0);
				}
			}
		}

		// constructor
		public MainWindow() {
			InitializeComponent();
			myInstance = this;
			debugWindow = new DebugWindow();
			debugWindow.Show();

			seq = NIDaqSequence.getEmptyInstance();
			communicator = new NIDaqCommunicator(seq);
			workerThread = new Thread(communicator.Run);

			this.MouseLeftButtonDown += (sender, e) => this.DragMove();

			this.InsertColumn(0);
		}

		// callback insert column to sequence
		private void Callback_InsertColumn(object sender, RoutedEventArgs e) {

			int index = columnList.Count;
			// if event invoker is canvas grid cell , insert left of cell column
			if(e.Source is MenuItem){
				Canvas canvas = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget as Canvas;
				if (canvas != null) {
					int? column = canvas.GetValue(Grid.ColumnProperty) as int?;
					if (column.HasValue) {
						index = column.Value;
					}
				}
			}
			this.InsertColumn(index);
			debugWindow.WriteLine(String.Format("Insert Column to index={0}",index));
		}

		// callback insert row to sequence
		private void Callback_InsertRow(object sender, RoutedEventArgs e) {

			int index = rowList.Count;
			// if event invoker is canvas grid cell , insert bottom of cell row
			if (e.Source is MenuItem) {
				Canvas canvas = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget as Canvas;
				if (canvas != null) {
					int? row = canvas.GetValue(Grid.RowProperty) as int?;
					if (row.HasValue) {
						index = row.Value;
					}
				}
			}
			this.InsertRow(index);
			debugWindow.WriteLine(String.Format("Insert Row to index={0}", index));
		}

		// callback erase column to sequence
		private void Callback_EraseColumn(object sender, RoutedEventArgs e) {

			int index = -1;
	
			// if event invoker is canvas grid cell , erase this column
			if (e.Source is MenuItem) {
				Canvas canvas = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget as Canvas;
				if (canvas != null) {
					int? column = canvas.GetValue(Grid.ColumnProperty) as int?;
					if (column.HasValue) {
						index = column.Value-1;
					}
				}
			}

			if(index!=-1)this.EraseColumn(index);
			debugWindow.WriteLine(String.Format("Erase Row where index={0}", index));
		}

		// callback insert row to sequence
		private void Callback_EraseRow(object sender, RoutedEventArgs e) {
			int index = -1;
			// if event invoker is canvas grid cell , erase this row
			if (e.Source is MenuItem) {
				Canvas canvas = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget as Canvas;
				if (canvas != null) {
					int? row = canvas.GetValue(Grid.RowProperty) as int?;
					if (row.HasValue) {
						index = row.Value-1;
					}
				}
			}
			if (index != -1) this.EraseRow(index);
			debugWindow.WriteLine(String.Format("Erase Row where index={0}", index));
		}

		// insert new column to UI
		private void InsertColumn(int index) {

			// extends grid
			SequenceGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(80) });

			// add new label instance
			ColumnInfo colinfo = new ColumnInfo();
			colinfo.SetName(String.Format("Seq{0}", uniqueColumnId));
			colinfo.SetColumn(index);
			SequenceGrid.Children.Add(colinfo.sequenceLabel);
			columnList.Insert(index,colinfo);
			uniqueColumnId++;

			// expands column span of all canvases
			for (int row = 0; row < rowList.Count; row++) {
				rowList[row].AddColumn(columnList.Count,index);
			}

			// re-label right sequence label cell index
			for (int column = index+1 ; column < columnList.Count ; column++) {
				columnList[column].SetColumn(column);
			}
		}

		// insert new row to UI
		private void InsertRow(int index) {

			// extend grid
			SequenceGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(80) });

			// add new IO label instance
			RowInfo rowinfo = new RowInfo();
			rowinfo.SetName(String.Format("IO{0}", uniqueRowId));
			rowinfo.SetRow(index);
			rowinfo.SetColumnSpan(columnList.Count);
			SequenceGrid.Children.Add(rowinfo.IOLabel);
			SequenceGrid.Children.Add(rowinfo.canvas);
			rowList.Insert(index, rowinfo);
			uniqueRowId++;

			// re-label IO label and canvas
			for (int row = index + 1; row < rowList.Count; row++) {
				rowList[row].SetRow(row);
			}
		}

		// erase column
		private void EraseColumn(int index) {

			// remove label instance
			SequenceGrid.Children.Remove(columnList[index].sequenceLabel);
			columnList.RemoveAt(index);

			// shrink column span of all canvases
			for (int row = 0; row < rowList.Count; row++) {
				rowList[row].EraseColumn(columnList.Count,index);
			}

			// re-label right sequence label cell index
			for (int column = index ; column < columnList.Count; column++) {
				columnList[column].SetColumn(column);
			}

			// remove column
			SequenceGrid.ColumnDefinitions.RemoveAt(SequenceGrid.ColumnDefinitions.Count-1);
		}


		// erase row
		private void EraseRow(int index) {
			// remove label and canvasn instance
			SequenceGrid.Children.Remove(rowList[index].IOLabel);
			SequenceGrid.Children.Remove(rowList[index].canvas);
			rowList.RemoveAt(index);

			// re-label IO label and canvas
			for (int row = index ; row < rowList.Count; row++) {
				rowList[row].SetRow( row);
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
