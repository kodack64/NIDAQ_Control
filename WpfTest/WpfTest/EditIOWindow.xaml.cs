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
using NIDAQInterface;

namespace WpfTest {
	/// <summary>
	/// EditIOWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class EditIOWindow : Window {
		public bool isOk=false;
		public bool resultIsAnalog;
		public bool resultIsOutput;
		public string resultBindedName;
		public string messageNotConnect = "NotConnect";

		public EditIOWindow(bool isAnalog,bool isOutput,string bindedName) {
			InitializeComponent();
			if (isAnalog) {
				Radio_Analog.IsChecked = true;
			} else {
				Radio_Digital.IsChecked = true;
			}
			if (isOutput) {
				Radio_Output.IsChecked = true;
			} else {
				Radio_Input.IsChecked = true;
			}

			Radio_Analog.Checked += (object sender, RoutedEventArgs arg) => RadioChecked();
			Radio_Digital.Checked += (object sender, RoutedEventArgs arg ) => RadioChecked();
			Radio_Input.Checked += (object sender, RoutedEventArgs arg ) => RadioChecked();
			Radio_Output.Checked += (object sender, RoutedEventArgs arg) => RadioChecked();

			RadioChecked();

			if (bindedName == "") {
				PortList.SelectedIndex = 0;
			} else {
				foreach (string str in PortList.Items) {
					if (str == bindedName) {
						PortList.SelectedItem = str;
					}
				}
			}
		}
		private void RadioChecked() {
			bool isAnalog = Radio_Analog.IsChecked.Value;
			bool isOutput = Radio_Output.IsChecked.Value;
			NIDAQInterface.NIDaqInterface intance = new NIDAQInterface.GetInstance();
			PortList.Items.Clear();
			PortList.Items.Add(messageNotConnect);
			if (isAnalog && isOutput) foreach (string str in instance.getAnalogOutputList()) PortList.Items.Add(str);
			if (isAnalog && !isOutput) foreach (string str in instance.getAnalogInputList()) PortList.Items.Add(str);
			if (!isAnalog && isOutput) foreach (string str in instance.getDigitalOutputList()) PortList.Items.Add(str);
			if (!isAnalog && !isOutput) foreach (string str in instance.getDigitalInputList()) PortList.Items.Add(str);
			PortList.SelectedIndex = 0;
		}
		private void Click_OK(object sender, RoutedEventArgs e) {
			this.Close();
			isOk = true;
			resultIsAnalog = Radio_Analog.IsChecked.Value;
			resultIsOutput = Radio_Output.IsChecked.Value;
			resultBindedName = PortList.Text==messageNotConnect?"":PortList.Text;
		}

		private void Click_Cancel(object sender, RoutedEventArgs e) {
			isOk = false;
		}
	}
}
