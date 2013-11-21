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


namespace WpfTest {
	namespace NIDaq {
		public class DivisionLabel {
			private Sequence parent;
			public TextBox label;
			public double time;
			public TimeUnit units;
			public string toText() { return ""; }
			public void fromText(string s) { }
			public DivisionLabel(Sequence _parent) {
				parent = _parent;
				label = new TextBox() { Text = "new div", Background = Brushes.LightGray , ContextMenu = new ContextMenu()};
				label.ContextMenuOpening += ((object sender, ContextMenuEventArgs arg) => CheckContextMenu());
				time = 1;
				units = TimeUnit.s;
			}
			public void CheckContextMenu() {
				label.ContextMenu.Items.Clear();
				label.ContextMenu.Items.Add(new MenuItem() { Header = String.Format("time = {0} {1}",time,units.ToString()), IsEnabled=false});
				MenuItem item;
				item = new MenuItem();
				item.Header = "Edit";
				item.Click += (object sender, RoutedEventArgs arg) => editDivision();
				label.ContextMenu.Items.Add(item);
			}
			public void editDivision() {
				EditDivisionWindow window = new EditDivisionWindow(time,units);
				window.ShowDialog();
				if (window.isOk) {
					time = window.resultTimeValue;
					units = window.resultTimeUnit;
				}
			}
			public void setPosition(int i) {
				label.SetValue(Grid.RowProperty, 0);
				label.SetValue(Grid.ColumnProperty, i + 1);
			}
			public double getTime() {
				if (units == TimeUnit.s) return time;
				else if (units == TimeUnit.ms) return time * 1e-3;
				else if (units == TimeUnit.us) return time * 1e-6;
				else if (units == TimeUnit.ns) return time * 1e-9;
				else return time;
			}
		}
	}
}
