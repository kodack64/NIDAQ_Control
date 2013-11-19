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

	
		// ログ表示用ウィンドウ
		DebugWindow debugWindow;

		// 動作スレッド
		Thread workerThread;

		// 動作オブジェクト
		NIDaqCommunicator communicator;

		// シーケンス
		NIDaqSequence seq;
		
		// 行リスト
		List<ColumnInfo> columnList = new List<ColumnInfo>();
		//行のユニークID
		private int uniqueColumnId = 0;

		// 列リスト
		List<RowInfo> rowList = new List<RowInfo>();
		//列のユニークID
		private int uniqueRowId = 0;

		// ウィンドウの自身のインスタンス
		static MainWindow myInstance;

		// 列（シーケンス）情報
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

			// 自身のグリッド上の列番号を修正
			public void SetColumn(int index) {
				sequenceLabel.SetValue(Grid.ColumnProperty,index+1);
			}
		}

		// 行（IOポート）情報
		class RowInfo {
			private int myRow;
			public Label IOLabel{get;private set;}
			public Canvas canvas { get; private set; }
			public string myName { get; set; }
			public int myBindingId { get; set; }
			public bool isAnalog { get; set; }
			public List<double> positionArray { get; set; }
			public int currentTargetColumn;
			public RowInfo() {
				IOLabel = new Label() { Background=Brushes.Gray};
				canvas = new Canvas() { Background=Brushes.White};
				canvas.ContextMenuOpening += new ContextMenuEventHandler(CheckContextMenu);
				positionArray = new List<double>();
				IOLabel.SetValue(Grid.ColumnProperty,0);
				canvas.SetValue(Grid.ColumnProperty, 1);
			}

			// キャンバス上でメニューがクリックされたらキャンバス上の情報に応じてコンテキストメニューを作成
			public void CheckContextMenu(object sender, ContextMenuEventArgs e) {

				Point p = Mouse.GetPosition(canvas);
				currentTargetColumn = (int)(p.X / 80);

				int maxColumn = (int)canvas.GetValue(Grid.ColumnSpanProperty);
				bool isLast = (maxColumn-1 == currentTargetColumn);
				
				canvas.ContextMenu = new ContextMenu();
				MenuItem item;
				item = new MenuItem();
				item.Header = "IO:" + myRow + "	Sequence" + currentTargetColumn;
				item.IsEnabled = false;
				canvas.ContextMenu.Items.Add(item);

				canvas.ContextMenu.Items.Add(new Separator());

				MenuItem itemHead;
				itemHead = new MenuItem();
				itemHead.Header = "Edit Sequence";
				{
					item = new MenuItem();
					item.Header = "Insert Column to ←";
					item.Click += MainWindow.myInstance.Callback_InsertColumn;
					itemHead.Items.Add(item);
					item = new MenuItem();
					item.Header = "Insert Row to ↓";
					item.Click += MainWindow.myInstance.Callback_InsertRow;
					itemHead.Items.Add(item);
					item = new MenuItem();
					item.Header = "Erase This Row";
					item.Click += MainWindow.myInstance.Callback_EraseRow;
					itemHead.Items.Add(item);
					item = new MenuItem();
					item.Header = "Erase This Column";
					item.Click += MainWindow.myInstance.Callback_EraseColumn;
					if (isLast) item.IsEnabled = false;
					itemHead.Items.Add(item);
				}
				canvas.ContextMenu.Items.Add(itemHead);
			}

			public void SetName(string name) {
				myName = name;
				IOLabel.Content = myName;
			}

			//列が挿入されたり削除されたりしたら列方向に伸び縮みする
			public void AddColumn(int column,int inserted) {
				if (inserted == 0) {
					positionArray.Insert(inserted, 0);
				} else {
					positionArray.Insert(inserted, positionArray[inserted - 1]);
				}
				canvas.SetValue(Grid.ColumnSpanProperty,column);
			}
			public void EraseColumn(int column,int erased) {
				positionArray.RemoveAt(erased);
				canvas.SetValue(Grid.ColumnSpanProperty, column);
			}

			// 自身のグリッド上の行番号を修正
			public void SetRow(int index) {
				IOLabel.SetValue(Grid.RowProperty,index+1);
				canvas.SetValue(Grid.RowProperty, index + 1);
				myRow = index;
			}
			//現在の列の数からキャンバス表示域の長さを指定
			public void SetColumnSpan(int span) {
				canvas.SetValue(Grid.ColumnSpanProperty, span);
				canvas.ContextMenu = new ContextMenu();

				positionArray.Clear();
				for (int i = 0; i < span; i++) {
					positionArray.Add(0);
				}
			}

			//再描画
			public void repaint() {
				canvas.Children.Clear();
				for (int i = 0; i < positionArray.Count; i++) {
					Ellipse plot = new Ellipse();
					plot.Fill = Brushes.Black;
					plot.StrokeThickness = 2;
					plot.SetValue(Canvas.LeftProperty,80.0*i-4.0);
					plot.SetValue(Canvas.TopProperty,positionArray[i]+40.0-4.0);
					plot.Width = 8;
					plot.Height = 8;
					canvas.Children.Add(plot);
					if (i != 0) {
						Line line = new Line();
						line.Stroke = Brushes.Gray;
						line.StrokeThickness = 2;
						line.X1 = 80 * (i - 1);
						line.X2 = 80 * i;
						line.Y1 = positionArray[i - 1]+40;
						line.Y2 = positionArray[i]+40;
						canvas.Children.Add(line);
					}
				}
			}
		}

		// コンストラクタ
		public MainWindow() {
			InitializeComponent();
			myInstance = this;

			debugWindow = new DebugWindow();
			debugWindow.Show();

			//動作スレッドの初期化
			seq = NIDaqSequence.getEmptyInstance();
			communicator = new NIDaqCommunicator(seq);
			workerThread = new Thread(communicator.Run);

			//ウィンドウをどこでもつかめるように
			this.MouseLeftButtonDown += (sender, e) => this.DragMove();

			//とりあえずシーケンスとIOをひとつ入れる。
			this.InsertColumn(0);
			this.InsertRow(0);
			repaint();
		}

		// 列挿入のコールバック
		private void Callback_InsertColumn(object sender, RoutedEventArgs e) {

			int index = columnList.Count;
			// コンテキストメニューからの呼び出しの場合、親のキャンバスのクリックされた列を取得
			if(e.Source is MenuItem){
				Canvas canvas = (((e.Source as MenuItem).Parent as MenuItem).Parent as ContextMenu).PlacementTarget as Canvas;
				for (int i = 0; i < rowList.Count; i++) {
					if (canvas == rowList[i].canvas) {
						index = rowList[i].currentTargetColumn;
					}
				}
			}
			// 一番右には挿入させない
			index--;
			this.InsertColumn(index);
			this.repaint();
			debugWindow.WriteLine(String.Format("Insert Column to index={0}",index));
		}

		// 行挿入のコールバック
		private void Callback_InsertRow(object sender, RoutedEventArgs e) {

			int index = rowList.Count;
			if (e.Source is MenuItem) {
				Canvas canvas = (((e.Source as MenuItem).Parent as MenuItem).Parent as ContextMenu).PlacementTarget as Canvas;
				if (canvas != null) {
					int? row = canvas.GetValue(Grid.RowProperty) as int?;
					if (row.HasValue) {
						index = row.Value;
					}
				}
			}
			this.InsertRow(index);
			this.repaint();
			debugWindow.WriteLine(String.Format("Insert Row to index={0}", index));
		}

		// 列削除のコールバック
		private void Callback_EraseColumn(object sender, RoutedEventArgs e) {

			int index = -1;
	
			if (e.Source is MenuItem) {
				Canvas canvas = (((e.Source as MenuItem).Parent as MenuItem).Parent as ContextMenu).PlacementTarget as Canvas;
				for (int i = 0; i < rowList.Count; i++) {
					if (canvas == rowList[i].canvas) {
						index = rowList[i].currentTargetColumn;
					}
				}
			}

			if(index!=-1)this.EraseColumn(index);
			this.repaint();
			debugWindow.WriteLine(String.Format("Erase Row where index={0}", index));
		}

		// 行削除のコールバック
		private void Callback_EraseRow(object sender, RoutedEventArgs e) {
			int index = -1;
			if (e.Source is MenuItem) {
				Canvas canvas = (((e.Source as MenuItem).Parent as MenuItem).Parent as ContextMenu).PlacementTarget as Canvas;
				if (canvas != null) {
					int? row = canvas.GetValue(Grid.RowProperty) as int?;
					if (row.HasValue) {
						index = row.Value-1;
					}
				}
			}
			if (index != -1) this.EraseRow(index);
			this.repaint();
			debugWindow.WriteLine(String.Format("Erase Row where index={0}", index));
		}

		// 列の挿入
		private void InsertColumn(int index) {

			// グリッドの延長
			SequenceGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(80) });

			// 新しい列情報の作成
			ColumnInfo colinfo = new ColumnInfo();
			colinfo.SetName(String.Format("Seq{0}", uniqueColumnId));
			colinfo.SetColumn(index);
			SequenceGrid.Children.Add(colinfo.sequenceLabel);
			columnList.Insert(index,colinfo);
			uniqueColumnId++;

			// 全ての行のキャンバスを延長
			for (int row = 0; row < rowList.Count; row++) {
				rowList[row].AddColumn(columnList.Count,index);
			}

			// 挿入に寄って影響を受けるセルを全て処理
			for (int column = index+1 ; column < columnList.Count ; column++) {
				columnList[column].SetColumn(column);
			}
		}

		//行の挿入
		private void InsertRow(int index) {

			SequenceGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(80) });

			RowInfo rowinfo = new RowInfo();
			rowinfo.SetName(String.Format("IO{0}", uniqueRowId));
			rowinfo.SetRow(index);
			rowinfo.SetColumnSpan(columnList.Count);
			SequenceGrid.Children.Add(rowinfo.IOLabel);
			SequenceGrid.Children.Add(rowinfo.canvas);
			rowList.Insert(index, rowinfo);
			uniqueRowId++;

			for (int row = index + 1; row < rowList.Count; row++) {
				rowList[row].SetRow(row);
			}
		}

		// 列の削除
		private void EraseColumn(int index) {

			SequenceGrid.Children.Remove(columnList[index].sequenceLabel);
			columnList.RemoveAt(index);

			for (int row = 0; row < rowList.Count; row++) {
				rowList[row].EraseColumn(columnList.Count,index);
			}

			for (int column = index ; column < columnList.Count; column++) {
				columnList[column].SetColumn(column);
			}

			SequenceGrid.ColumnDefinitions.RemoveAt(SequenceGrid.ColumnDefinitions.Count-1);
		}


		// 行の削除
		private void EraseRow(int index) {
			SequenceGrid.Children.Remove(rowList[index].IOLabel);
			SequenceGrid.Children.Remove(rowList[index].canvas);
			rowList.RemoveAt(index);

			for (int row = index ; row < rowList.Count; row++) {
				rowList[row].SetRow( row);
			}

			SequenceGrid.RowDefinitions.RemoveAt(SequenceGrid.RowDefinitions.Count - 1);
		}


		// 最小化
		private void WindowMinimize(object sender, RoutedEventArgs e) {
			this.WindowState = WindowState.Minimized;
		}
		// 最大とトグル
		private void WindowMaximize(object sender, RoutedEventArgs e) {
			this.WindowState = WindowState.Maximized;
			ToggleFullscreen.Content = "2";
			ToggleFullscreen.Click -= this.WindowMaximize;
			ToggleFullscreen.Click += this.WindowRestore;
		}
		// 通常へトグル
		private void WindowRestore(object sender, RoutedEventArgs e) {
			this.WindowState = WindowState.Normal;
			ToggleFullscreen.Content = "1";
			ToggleFullscreen.Click += this.WindowMaximize;
			ToggleFullscreen.Click -= this.WindowRestore;
		}
		// 閉じる
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

		// 起動ボタンの処理
		private void SystemRun(object sender, RoutedEventArgs e) {

			ToggleButton element = sender as ToggleButton;

			//起動の場合
			if (element.IsChecked.HasValue && element.IsChecked.Value) {
				//既にスレッドが起動中でなければスレッドを起動しボタンをトグル
				if (!workerThread.IsAlive) {
					workerThread = new Thread(communicator.Run);
					workerThread.Start();
					communicator.debugWindow = debugWindow;
					Button_Run.Content = "Stop Sequence";
					debugWindow.WriteLine("Run communicator");
				} else {
					debugWindow.WriteLine("Error : Try to running communicator but communicator is running now");
				}
			}
			//停止の場合
			else {
				//スレッドを停止し応答があるまで待機
				communicator.Stop();
				debugWindow.WriteLine("Request communicator to stop...");
				workerThread.Join();
				debugWindow.WriteLine("Communicator is terminated");
				Button_Run.Content = "Run Sequence";
			}
			debugWindow.scroleToEnd();
		}

		// シーケンスファイルのロード
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

		// シーケンスファイルのセーブ
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

		// キャンバスの再描画
		public void repaint() {
			for (int i = 0; i < rowList.Count; i++) {
				rowList[i].repaint();
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

	//シーケンス
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

	// NIDaqの通信箇所
	public class NIDaqCommunicator {
		public DebugWindow debugWindow=null;
		private NIDaqSequence seq;
		private int maxAnalogInput;
		private int maxAnalogOutput;
		private int maxDigitalInput;
		private int maxDigitalOutput;
		private double frequency;
		private volatile bool runningFlag;
		NationalInstruments.DAQmx.Task task = new NationalInstruments.DAQmx.Task("communicator");

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

		public void BufferDone(object sender, NationalInstruments.DAQmx.TaskDoneEventArgs arg) {
		}

		public void Run() {
			runningFlag = true;
			long loopCount;
			Stopwatch sw = new Stopwatch();
			double fps = 1.0 / frequency;
			double currentTime,nextTime,difTime,worstDifTime;
			double analogValue;
			bool digitalValue;

			task.Done += new NationalInstruments.DAQmx.TaskDoneEventHandler(this.BufferDone);

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
