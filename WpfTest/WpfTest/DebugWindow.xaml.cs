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
		public DebugWindow() {
			InitializeComponent();
		}
		private void Write(string text) {
			DebugTextBox.AppendText(text);
			DebugTextBox.ScrollToEnd();
		}
		public void WriteLine(string text) {
			this.Write(text + "\n");
		}
		public void WriteLineAsyc(string text) {
			DebugTextBox.Dispatcher.InvokeAsync(
				new Action(() => {
					this.WriteLine(text);
				})
			);
		}
		public void scroleToEnd() {
		}
	}
}
