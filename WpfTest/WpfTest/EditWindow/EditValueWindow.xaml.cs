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
	/// EditValue.xaml の相互作用ロジック
	/// </summary>
	public partial class EditValueWindow : Window {
		public double resultValue=0;
		public NodeType resultType = NodeType.Hold;
		public bool isOk=false;

		public EditValueWindow(double currentValue,NodeType currentType) {
			InitializeComponent();
			VoltageValue.Text = currentValue.ToString();
			foreach(string typeName in Enum.GetNames(typeof(NodeType))){
				Combo_NodeType.Items.Add(typeName);
				if (typeName == currentType.ToString()) {
					Combo_NodeType.SelectedItem = typeName;
				}
			}
		}

		private void Click_OK(object sender, RoutedEventArgs e) {
			this.Close();
			isOk = true;
			resultValue = double.Parse(VoltageValue.Text);
			foreach (string typeName in Enum.GetNames(typeof(NodeType))) {
				if (typeName == Combo_NodeType.Text) {
					resultType = (NodeType)(Enum.Parse(typeof(NodeType), typeName));
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
