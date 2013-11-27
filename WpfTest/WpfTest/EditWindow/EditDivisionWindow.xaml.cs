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
using NIDaqController;

namespace NIDaqController {
	/// <summary>
	/// EditDivisionWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class EditDivisionWindow : Window {
		public bool isOk;
		public TimeUnit resultTimeUnit;
		public double resultTimeValue;
		public EditDivisionWindow(double timeValue,TimeUnit timeUnit) {
			InitializeComponent();
			resultTimeUnit = timeUnit;
			resultTimeValue = timeValue;

			Text_TimeValue.Text = timeValue.ToString();
			foreach (string typeName in Enum.GetNames(typeof(TimeUnit))) {
				Combo_TimeUnit.Items.Add(typeName);
				if (typeName == timeUnit.ToString()) {
					Combo_TimeUnit.SelectedItem = typeName;
				}
			}
		}
		private void Click_OK(object sender, RoutedEventArgs e) {
			this.Close();
			isOk = true;
			resultTimeValue = double.Parse(Text_TimeValue.Text);
			foreach (string typeName in Enum.GetNames(typeof(TimeUnit))) {
				if (typeName == Combo_TimeUnit.Text) {
					resultTimeUnit = (TimeUnit)(Enum.Parse(typeof(TimeUnit), typeName));
				}
			}
		}

		private void Click_Cancel(object sender, RoutedEventArgs e) {
			isOk = false;
		}
		private void TextBox_TextChanged(object sender, TextChangedEventArgs e) {
			double temp;
			if (Button_OK != null && Text_TimeValue != null) {
				Button_OK.IsEnabled = double.TryParse(Text_TimeValue.Text, out temp);
			}
		}	
	}
}
