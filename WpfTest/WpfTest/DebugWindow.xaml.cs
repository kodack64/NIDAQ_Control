using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.ComponentModel;

namespace WpfTest {
	/// <summary>
	/// DebugWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class DebugWindow : Window {
		private static DebugWindow debugWindow = null;
		private DebugWindow() {
			InitializeComponent();
		}
		private static void checkInstance() { if (debugWindow == null) debugWindow = new DebugWindow(); debugWindow.Show(); }
		public static void Write(string text) { checkInstance(); debugWindow._Write(text); }
		public static void WriteLine(string text) { checkInstance(); debugWindow._WriteLine(text); }
		public static void WriteLineAsync(string text) { checkInstance(); debugWindow._WriteLineAsync(text); }
		public static void MyClose() { if (debugWindow != null) debugWindow._Close(); }

		public void _Write(string text) {
			DebugTextBox.AppendText(text);
			DebugTextBox.ScrollToEnd();
		}
		public void _WriteLine(string text) {
			this._Write(text + "\n");
		}
		public void _WriteLineAsync(string text) {
			DebugTextBox.Dispatcher.InvokeAsync(
				new Action(() => {
					this._WriteLine(text);
				})
			);
		}
		public void _Close() {
			this.Close();
		}
	}
}
