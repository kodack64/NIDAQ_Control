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

namespace WpfTest{
    namespace NIDaq{

        //複数のシーケンス管理
        public class Sequences{
            private List<Sequence> sequences = new List<Sequence>();
			public Sequence currentSequence;
			public Sequences() {
				sequences.Add(new Sequence());
				currentSequence = sequences[0];
			}
			public void changeActiveSequence(int index,Grid grid){
				currentSequence=sequences[index];
				currentSequence.bindGridUI(grid);
			}
        }

		//単一のシーケンス
        public class Sequence{
            private List<Channel> channels = new List<Channel>();
			private List<DivisionLabel> divisionLabels = new List<DivisionLabel>();
			private Grid bindedGrid;
			public Sequence() {
				DivisionLabel lastDivision = new DivisionLabel(this);
				lastDivision.label.Content = "Last";
				divisionLabels.Add(lastDivision);
			}
			public int getChannelCount(){
				return channels.Count;
			}
			public int getDivisionCount() {
				return divisionLabels.Count;
			}
			public bool getIsAnalog(int index) {
				return channels[index].isAnalog;
			}
			public bool getIsInput(int index) {
				return channels[index].isInput;	
			}
			public void insertDivision(int index) {
				divisionLabels.Insert(index, new DivisionLabel(this));
				bindedGrid.ColumnDefinitions.Insert(index, new ColumnDefinition() { Width = new GridLength(80) });
				foreach (Channel ch in channels) {
					ch.insertPlot(index,0);
					ch.setSpan(divisionLabels.Count);
				}
				for (int i = index; i < divisionLabels.Count; i++) {
					divisionLabels[i].setPosition(i);
				}
				bindedGrid.Children.Add(divisionLabels[index].label);
				repaint();
			}
			public void removeDivision(int index) {
				bindedGrid.Children.Remove(divisionLabels[index].label);
				divisionLabels.RemoveAt(index);
				foreach(Channel ch in channels){
					ch.removePlot(index);
					ch.setSpan(divisionLabels.Count);
				}
				for (int i = index; i < divisionLabels.Count; i++) {
					divisionLabels[i].setPosition(i);
				}
				bindedGrid.ColumnDefinitions.RemoveAt(index);
				repaint();
			}
			public void insertChannel(int index) {
				channels.Insert(index, new Channel(this,divisionLabels.Count));
				bindedGrid.RowDefinitions.Insert(index, new RowDefinition() { Height = new GridLength(80) });
				for (int i = index; i < channels.Count; i++) {
					channels[i].setPosition(i);
				}
				bindedGrid.Children.Add(channels[index].channelLabel);
				bindedGrid.Children.Add(channels[index].channelCanvas);
				repaint();
			}
			public void removeChannel(int index) {
				bindedGrid.Children.Remove(channels[index].channelLabel);
				bindedGrid.Children.Remove(channels[index].channelCanvas);
				channels.RemoveAt(index);
				for (int i = index; i < channels.Count; i++) {
					channels[i].setPosition(i);
				}
				bindedGrid.RowDefinitions.RemoveAt(index);
				repaint();
			}
			public string toText() {
				string str="";
				for (int i = 0; i < divisionLabels.Count; i++) {
					str += divisionLabels[i].toText() + ",";
				}
				str += "\n";
				for (int i = 0; i < channels.Count; i++) {
					str += channels[i].toText()+"\n";
				}
				return str;
			}
			public void fromText(string str) {
				try{
					List<DivisionLabel> tempDivisionLabels = new List<DivisionLabel>();
					List<Channel> tempChannels = new List<Channel>();

					string[] strs = str.Split('\n');

					string[] labelarray = strs[0].Split(',');
					foreach (string s in labelarray) {
						DivisionLabel label = new DivisionLabel(this);
						label.fromText(s);
						tempDivisionLabels.Add(label);
					}
		
					for (int i = 1; i < strs.Length; i++) {
						Channel ch = new Channel(this,tempDivisionLabels.Count);
						ch.fromText(strs[i]);
						tempChannels.Add(ch);
					}

					divisionLabels = tempDivisionLabels;
					channels = tempChannels;

				}catch(Exception e){
					e.ToString();
					//load fail
				}
				repaint();
			}
			public void bindGridUI(Grid grid) {
				bindedGrid = grid;
				bindedGrid.Children.Clear();
				bindedGrid.ColumnDefinitions.Clear();
				bindedGrid.RowDefinitions.Clear();
				for (int i = 0; i < divisionLabels.Count + 1; i++) {
					bindedGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(80) });
				}
				for (int i = 0; i < channels.Count + 1; i++) {
					bindedGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(80) });
				}

				Label label = new Label() { Content = "Sequence", Background = Brushes.Gray, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
				label.SetValue(Grid.RowProperty, 0);
				label.SetValue(Grid.ColumnProperty, 0);
				bindedGrid.Children.Add(label);

				for (int i = 0; i < divisionLabels.Count; i++) {
					divisionLabels[i].label.SetValue(Grid.RowProperty, 0);
					divisionLabels[i].label.SetValue(Grid.ColumnProperty, i + 1);
					bindedGrid.Children.Add(divisionLabels[i].label);
				}
				for (int i = 0; i < channels.Count; i++) {
					channels[i].setPosition(i);
					channels[i].setSpan(divisionLabels.Count);
					bindedGrid.Children.Add(channels[i].channelCanvas);
					bindedGrid.Children.Add(channels[i].channelCanvas);
				}
				repaint();
			}
			public void repaint() {
				foreach (Channel ch in channels) {
					ch.repaint();
				}
			}
        }
    }
}
