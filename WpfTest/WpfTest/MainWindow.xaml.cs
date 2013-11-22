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


namespace WpfTest {
	/// <summary>
	/// MainWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class MainWindow : Window {

		// 入出力
		NIDaqCommunicator communicator;

		// シーケンス
		NIDaq.Sequences seq;
		
		// コンストラクタ
		public MainWindow() {
			DebugWindow.WriteLine("初期化");
			InitializeComponent();

			//動作スレッドの初期化
			DebugWindow.WriteLine("シーケンス作成");
			seq = new NIDaq.Sequences();
			seq.currentSequence.bindGridUI(SequenceGrid);
			communicator = new NIDaqCommunicator(seq);

			//ウィンドウをどこでもつかめるように
			this.MouseLeftButtonDown += (sender, e) => this.DragMove();
		}

		// 列挿入のコールバック
		private void Callback_InsertDivision(object sender, RoutedEventArgs e) {
			seq.currentSequence.insertDivision(seq.currentSequence.getDivisionCount()-1);
		}

		// 行挿入のコールバック
		private void Callback_InsertChannel(object sender, RoutedEventArgs e) {
			seq.currentSequence.insertChannel(seq.currentSequence.getChannelCount());
		}

		// 列削除のコールバック
		private void Callback_RemoveDivision(object sender, RoutedEventArgs e) {
			seq.currentSequence.removeDivision(0);
		}

		// 行削除のコールバック
		private void Callback_RemoveChannel(object sender, RoutedEventArgs e) {
			seq.currentSequence.removeChannel(0);
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

			ToggleButton element = sender as ToggleButton;

			//起動の場合
			if (element.IsChecked.HasValue && element.IsChecked.Value) {
				//既にスレッドが起動中でなければスレッドを起動しボタンをトグル
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
					seq.currentSequence.fromText(File.ReadAllText(dialog.FileName));
					DebugWindow.WriteLine(String.Format("Open sequence file {0}",dialog.FileName));
				}
			}
		}

		// シーケンスファイルのセーブ
		private void Callback_SaveSequence(object sender, RoutedEventArgs e) {
			DebugWindow.WriteLine("シーケンス書き出し");
			SaveFileDialog dialog = new SaveFileDialog()
			{
				Filter="seqファイル|*.seq"
			};
			bool? result = dialog.ShowDialog();
			if (result.HasValue) {
				if (result.Value) {
					StreamWriter sw = File.CreateText(dialog.FileName);
					sw.Write(seq.currentSequence.toText());
					DebugWindow.WriteLine(String.Format("Save sequence file {0}", dialog.FileName));
				}
			}
		}

		// キャンバスの再描画
		public void repaint() {
			seq.currentSequence.repaint();
		}
	}
}
