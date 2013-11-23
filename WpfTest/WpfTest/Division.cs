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
			private const string separator = ",";
			public static readonly int width = 80;
			private static int uniqueId = 0;
			private Sequence parent;
			public TextBox label;
			public double time;
			public TimeUnit units;
			private int myColumn;
			public string toSeq() {
				string str = "";
				str += label.Text + separator;
				str += time.ToString() + separator;
				str += units.ToString() + separator;
				return ""; 
			}
			public void fromSeq(string s) {
				string[] strs = s.Trim().Split(separator.ToCharArray());
				label.Text = strs[0];
				time = double.Parse(strs[1]);
				units = (TimeUnit)Enum.Parse(typeof(TimeUnit), strs[2]);
			}
			public DivisionLabel(Sequence _parent) {
				parent = _parent;
				label = new TextBox() { Text = "Div "+uniqueId, Background = Brushes.LightGray , ContextMenu = new ContextMenu()};
				label.ContextMenuOpening += ((object sender, ContextMenuEventArgs arg) => CheckContextMenu());
				time = 1;
				units = TimeUnit.s;
				uniqueId++;
			}
			public void CheckContextMenu() {
				label.ContextMenu.Items.Clear();
				label.ContextMenu.Items.Add(new MenuItem() { Header = String.Format("time = {0} {1}",time,units.ToString()), IsEnabled=false});
				MenuItem item;
				item = new MenuItem() { Header = "Edit Division"};
				item.Click += (object sender, RoutedEventArgs arg) => editDivision();
				label.ContextMenu.Items.Add(item);
				item = new MenuItem() { Header = "Remove This Division" };
				item.Click += (object sender, RoutedEventArgs arg) => parent.removeDivision(myColumn);
				label.ContextMenu.Items.Add(item);
			}
			public void editDivision() {
				EditDivisionWindow window = new EditDivisionWindow(time, units);
				window.ShowDialog();
				if (window.isOk) {
					time = window.resultTimeValue;
					units = window.resultTimeUnit;
				}
				DebugWindow.WriteLine("Divisionの情報を更新");
			}
			public void setPosition(int i) {
				myColumn = i;
				label.SetValue(Grid.RowProperty, 0);
				label.SetValue(Grid.ColumnProperty, i + 1);
			}
			public double getTime() {
				if (units == TimeUnit.s) return time;
				else if (units == TimeUnit.ms) return time * 1e-3;
				else if (units == TimeUnit.us) return time * 1e-6;
				else return time;
			}
			public string getName() {
				return label.Text;
			}
		}
	}
}
