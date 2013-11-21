using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WpfConnectTest;

namespace HostClass
{
    public class Class1
    {
		public void openWindow() {
			Thread thread = new Thread(new ThreadStart(myFunc));
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();
			thread.Join();
		}
		[STAThread]
		public void myFunc() {
			MainWindow window = new MainWindow();
			window.ShowDialog();
		}
    }
}
