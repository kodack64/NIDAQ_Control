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

namespace WpfTest {
	/// <summary>
	/// EditValue.xaml の相互作用ロジック
	/// </summary>
	public partial class EditValueWindow : Window {
		public double resultValue=0;
		public NIDaq.NodeType resultType=NIDaq.NodeType.Hold;
		public bool isOk=false;

		public EditValueWindow(double currentValue,NIDaq.NodeType currentType) {
			InitializeComponent();
			VoltageValue.Text = currentValue.ToString();
			foreach(string typeName in Enum.GetNames(typeof(NIDaq.NodeType))){
				NodeType.Items.Add(typeName);
				if (typeName == currentType.ToString()) {
					NodeType.SelectedItem = typeName;
				}
			}
		}

		private void Click_OK(object sender, RoutedEventArgs e) {
			this.Close();
			isOk = true;
			resultValue = double.Parse(VoltageValue.Text);
			foreach (string typeName in Enum.GetNames(typeof(NIDaq.NodeType))) {
				if (typeName == NodeType.Text) {
					resultType = (NIDaq.NodeType)(Enum.Parse(typeof(NIDaq.NodeType), typeName));
				}
			}
		}

		private void Click_Cancel(object sender, RoutedEventArgs e) {
			isOk = false;
		}

		private void TextBox_TextChanged(object sender, TextChangedEventArgs e) {
			double temp;
			if (Button_OK != null && VoltageValue != null) {
				Button_OK.IsEnabled = double.TryParse(VoltageValue.Text, out temp);
			}
		}
	}
}
