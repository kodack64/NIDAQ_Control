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
using System.IO;
using Microsoft.Win32;
using NIDaqController;


namespace WpfTest {
	/// <summary>
	/// MainWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class MainWindow : Window {

		// 入出力
		private NIDaqCommunicator communicator;

		// シーケンス
		private Sequences seq;

		// インスタンス
		public static MainWindow myInstance;

		//繰り返し回数
		public int repeatCount {
			get {
				try {
					return int.Parse(Text_RepeatRun.Text);
				}catch(Exception){
					return 1;
				}
			}
		}
		//繰り返しを有効にするかどうか
		public bool IsRepeatEnable {
			get {
				return Check_RepeatRun.IsChecked.Value;
			}
		}
		
		// コンストラクタ
		public MainWindow() {
			myInstance = this;

			DebugWindow.WriteLine("初期化");
			InitializeComponent();

			//動作スレッドの初期化
			DebugWindow.WriteLine("シーケンス作成");
			seq = new Sequences();
			seq.getCurrentSequence().bindGridUI(SequenceGrid);
//			seq.getCurrentSequence().addAllAnalogOutput();
			communicator = new NIDaqCommunicator(seq);

			//ウィンドウをどこでもつかめるように
			this.MouseLeftButtonDown += (sender, e) => this.DragMove();

			TaskManager.GetInstance().addAllTaskEndEventHandler(Callback_SystemStop);
		}

		// 列挿入のコールバック
		private void Callback_InsertDivision(object sender, RoutedEventArgs e) {
			seq.getCurrentSequence().insertDivision(seq.getCurrentSequence().getDivisionCount()-1);
		}

		// 行挿入のコールバック
		private void Callback_InsertChannel(object sender, RoutedEventArgs e) {
			seq.getCurrentSequence().insertChannel(seq.getCurrentSequence().getChannelCount());
		}

		// 列削除のコールバック
		private void Callback_RemoveDivision(object sender, RoutedEventArgs e) {
			seq.getCurrentSequence().removeDivision(0);
		}

		// 行削除のコールバック
		private void Callback_RemoveChannel(object sender, RoutedEventArgs e) {
			seq.getCurrentSequence().removeChannel(0);
		}

		// 最小化
		private void Callback_WindowMinimize(object sender, RoutedEventArgs e) {
			this.WindowState = WindowState.Minimized;
		}
		// 最大とトグル
		private void Callback_WindowMaximize(object sender, RoutedEventArgs e) {
			this.WindowState = WindowState.Maximized;
			ToggleFullscreen.Content = "2";
			ToggleFullscreen.Click -= this.Callback_WindowMaximize;
			ToggleFullscreen.Click += this.Callback_WindowRestore;
		}
		// 通常へトグル
		private void Callback_WindowRestore(object sender, RoutedEventArgs e) {
			this.WindowState = WindowState.Normal;
			ToggleFullscreen.Content = "1";
			ToggleFullscreen.Click += this.Callback_WindowMaximize;
			ToggleFullscreen.Click -= this.Callback_WindowRestore;
		}
		// 閉じる
		private void Callback_WindowClose(object sender, RoutedEventArgs e) {
			DebugWindow.WriteLine("終了");
			communicator.Stop();
			DebugWindow.MyClose();
			this.Close();
		}

		// 起動ボタンの処理
		private void Callback_SystemRun(object sender, RoutedEventArgs e) {
			//起動の場合
			if (Button_Run.IsChecked.HasValue && Button_Run.IsChecked.Value) {
				//既にスレッドが起動中でなければスレッドを起動しボタンをトグル
				communicator.isRepeatEnabled = IsRepeatEnable;
				communicator.repeatCount = repeatCount;
				communicator.Run();
				Button_Run.Content = "Stop Sequence";
				DebugWindow.WriteLine("シーケンス開始");
			}
			//停止の場合
			else {
				//スレッドを停止し応答があるまで待機
				communicator.Stop();
				Button_Run.Content = "Run Sequence";
				DebugWindow.WriteLine("シーケンス中断");
			}
		}

		//終了のコールバック
		public void Callback_SystemStop() {
			Button_Run.Dispatcher.BeginInvoke(
				new Action(() => 
				{
					if (Button_Run.IsChecked.HasValue && Button_Run.IsChecked.Value) {
						Button_Run.IsChecked = false;
						Button_Run.Content = "Run Sequence";
						DebugWindow.WriteLine("シーケンス終了");
					}
				})
			);

			// 波形を保存
			for (int i = 0; i < TaskManager.GetInstance().getInputWaveDeviceCount(); i++) {
				List<String> nameList = TaskManager.GetInstance().getInputWaveNameList(i);
				double[,] dataArray =  TaskManager.GetInstance().getInputWaveArray(i);

				for (int j = 0; j < Math.Min(nameList.Count, dataArray.GetLength(0)); j++) {
					double[] data = new double[dataArray.GetLength(1)];
					for (int k = 0; k < dataArray.GetLength(1); k++) {
						data[k] = dataArray[j, k];
					}
					seq.getCurrentSequence().setWaveForm(nameList[j], data);
				}
			}
			seq.getCurrentSequence().repaint();
		}

		// シーケンスファイルのロード
		private void Callback_LoadSequence(object sender, RoutedEventArgs e) {
			DebugWindow.WriteLine("シーケンス読み込み");
			OpenFileDialog dialog = new OpenFileDialog()
			{
				Multiselect=false,
				Filter="seqファイル|*.seq"
			};
			bool? result = dialog.ShowDialog();
			if (result.HasValue) {
				if(result.Value){
					try {
						seq.loadCurrentSeq(File.ReadAllText(dialog.FileName));
						seq.getCurrentSequence().bindGridUI(SequenceGrid);
						DebugWindow.WriteLine(String.Format("{0}を読み込みました", dialog.FileName));
						repaint();
					} catch (Exception ex) {
						DebugWindow.WriteLine(String.Format("{0}を開けませんでした\n*{1}:{2}", dialog.FileName,ex.StackTrace,ex.Message));
					}
				}
			}
		}

		// シーケンスファイルのセーブ
		private void Callback_SaveSequence(object sender, RoutedEventArgs e) {
			DebugWindow.WriteLine("シーケンス保存");
			SaveFileDialog dialog = new SaveFileDialog()
			{
				Filter="seqファイル|*.seq"
			};
			bool? result = dialog.ShowDialog();
			if (result.HasValue) {
				if (result.Value) {
					var sw = File.CreateText(dialog.FileName);
					sw.Write(seq.writeCurrentSeq());
					sw.Close();
					DebugWindow.WriteLine(String.Format("{0}に保存しました。", dialog.FileName));
				}
			}
		}

		// キャンバスの再描画
		public void repaint() {
			seq.getCurrentSequence().repaint();
		}

		private void Callback_RepeatChanged(object sender, RoutedEventArgs e) {
			CheckBox cb = sender as CheckBox;
			Text_RepeatRun.IsEnabled = cb.IsChecked.Value;
		}
	}
}
