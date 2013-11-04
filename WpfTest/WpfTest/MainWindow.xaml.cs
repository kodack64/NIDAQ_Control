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
		DebugWindow debugWindow;
		Thread workerThread;
		NIDaqCommunicator communicator;
		NIDaqSequence seq;
		public MainWindow() {
			InitializeComponent();
			debugWindow = new DebugWindow();
			debugWindow.Show();

			seq = NIDaqSequence.getEmptyInstance();
			communicator = new NIDaqCommunicator(seq);
			workerThread = new Thread(communicator.Run);

			this.MouseLeftButtonDown += (sender, e) => this.DragMove();
		}
		private void WindowMinimize(object sender, RoutedEventArgs e) {
			this.WindowState = WindowState.Minimized;
		}
		private void WindowMaximize(object sender, RoutedEventArgs e) {
			this.WindowState = WindowState.Maximized;
			ToggleFullscreen.Content = "2";
			ToggleFullscreen.Click -= this.WindowMaximize;
			ToggleFullscreen.Click += this.WindowRestore;
		}
		private void WindowRestore(object sender, RoutedEventArgs e) {
			this.WindowState = WindowState.Normal;
			ToggleFullscreen.Content = "1";
			ToggleFullscreen.Click += this.WindowMaximize;
			ToggleFullscreen.Click -= this.WindowRestore;
		}
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

	public class NIDaqSequence {
		private NIDaqSequence(){}

		// only accessed from UI thread
		static public NIDaqSequence getEmptyInstance() {
			return new NIDaqSequence();
		}
		public string toString() {
			return "";
		}
		static public NIDaqSequence fromString() {
			return new NIDaqSequence();
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
				debugWindow.WriteLineAsyc(String.Format(" Update Frequency = {0} Hz", frequency));
				debugWindow.WriteLineAsyc(String.Format(" I/O Update Count = {0}", loopCount));
				debugWindow.WriteLineAsyc(String.Format(" I/O Ideal Update Count = {0}", sw.ElapsedMilliseconds*1e-3*frequency));
				debugWindow.WriteLineAsyc(String.Format(" I/O Resolution = {0} loops/ms", (double)loopCount / sw.ElapsedMilliseconds));
				debugWindow.WriteLineAsyc(String.Format(" I/O Average Precision = {0} sec", difTime/loopCount));
				debugWindow.WriteLineAsyc(String.Format(" I/O Worst Precision = {0} sec", worstDifTime));
				debugWindow.WriteLineAsyc(String.Format("***"));
			}
		}
		public void Stop() {
			runningFlag = false;
		}
	}
}
