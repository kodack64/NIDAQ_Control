using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NIDAQ_Control_Host
{
    public class Class1
    {
		public Class1(){
		}
		public void run() {
			WpfTest.MainWindow window = new WpfTest.MainWindow();
			window.ShowDialog();
		}
    }
}
