﻿using System;
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

			private List<List<double[]>> waves;

			private Grid bindedGrid;
			public Sequence() {
				DivisionLabel lastDivision = new DivisionLabel(this);
				lastDivision.label.Text = "Last";
				lastDivision.time = 0;
				divisionLabels.Add(lastDivision);
				waves = new List<List<double[]>>();
			}

			public void compile(long sampleRate) {
				DebugWindow.Write("シーケンスから信号を作成...");
				waves.Clear();
				for (int ci = 0; ci < channels.Count; ci++) {
					List<double[]> channelWave = new List<double[]>();

					Channel ch = channels[ci];
					for (int di = 0; di+1 < divisionLabels.Count; di++) {
						long sampleNum = this.getDivisionSample(di,sampleRate);
						double[] wave = new double[sampleNum];
						Plot plot = ch.plots[di];
						Plot nextPlot = ch.plots[di+1];

						if (plot.type == PlotType.Hold) {
							for (int i = 0; i < sampleNum; i++) {
								wave[i] = plot.value;
							}
						} else if (plot.type == PlotType.Linear) {
							for (int i = 0; i < sampleNum; i++) {
								wave[i] = plot.value + (nextPlot.value - plot.value)*i / sampleNum;
							}
						} else {
							for(int i=0;i<sampleNum;i++){
								wave[i] = plot.value;
							}
						}
						channelWave.Add(wave);
					}
					waves.Add(channelWave);
				}
				DebugWindow.WriteLine("OK");
				DebugWindow.WriteLine(" サンプルレート	:" + sampleRate);
				DebugWindow.WriteLine(" シーケンス時間	:" + getTotalTime());
				DebugWindow.WriteLine(" サンプル数	:" + getTotalDivisionSample(sampleRate));
			}
			public double[] getWave(int channelIndex,int divisionIndex) {
				return waves[channelIndex][divisionIndex];
			}
			public long getDivisionSample(int divisionIndex,long sampleRate) {
				return (long)(divisionLabels[divisionIndex].getTime()*sampleRate);
			}
			public long getTotalDivisionSample(long sampleRate) {
				long sum = 0;
				for (int i = 0; i < divisionLabels.Count; i++) {
					sum += getDivisionSample(i, sampleRate);
				}
				return sum;
			}
			public string getBindedName(int channelIndex) {
				return channels[channelIndex].bindedName;
			}
			public double getTotalTime() {
				double sum = 0;
				foreach (DivisionLabel div in divisionLabels) {
					sum += div.getTime();
				}
				return sum;
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
			public bool getIsBinded(int index) {
				return channels[index].bindedName.Length > 0;
			}
			public bool getIsOutput(int index) {
				return channels[index].isOutput;	
			}
			public double getMaxVoltage(int index) {
				return channels[index].maxVoltage;
			}
			public double getMinVoltage(int index) {
				return channels[index].minVoltage;
			}
			public string getChannelName(int index) {
				return channels[index].getName();
			}
			public string getDivisionName(int index) {
				return divisionLabels[index].getName();
			}
			public void insertDivision(int index) {
				DebugWindow.WriteLine(String.Format("{0}行目に行を挿入",index));
				divisionLabels.Insert(index, new DivisionLabel(this));
				bindedGrid.ColumnDefinitions.Insert(index, new ColumnDefinition() { Width = new GridLength(DivisionLabel.width) });
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
				DebugWindow.WriteLine(String.Format("{0}行目を削除", index));
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
				DebugWindow.WriteLine(String.Format("{0}列目に列を挿入", index));
				channels.Insert(index, new Channel(this, divisionLabels.Count));
				bindedGrid.RowDefinitions.Insert(index, new RowDefinition() { Height = new GridLength(Channel.height) });
				for (int i = index; i < channels.Count; i++) {
					channels[i].setPosition(i);
				}
				bindedGrid.Children.Add(channels[index].channelLabel);
				bindedGrid.Children.Add(channels[index].channelCanvas);
				repaint();
			}
			public void removeChannel(int index) {
				DebugWindow.WriteLine(String.Format("{0}列目を削除", index));
				bindedGrid.Children.Remove(channels[index].channelLabel);
				bindedGrid.Children.Remove(channels[index].channelCanvas);
				channels.RemoveAt(index);
				for (int i = index; i < channels.Count; i++) {
					channels[i].setPosition(i);
				}
				bindedGrid.RowDefinitions.RemoveAt(index);
				repaint();
			}
			public void moveUp(int index) {
				DebugWindow.WriteLine(String.Format("{0}列目を上に移動", index));
				Channel ch = channels[index];
				channels[index] = channels[index - 1];
				channels[index - 1] = ch;
				channels[index].setPosition(index);
				channels[index-1].setPosition(index-1);
				repaint();
			}
			public void moveDown(int index) {
				DebugWindow.WriteLine(String.Format("{0}列目を下に移動", index));
				Channel ch = channels[index];
				channels[index] = channels[index + 1];
				channels[index + 1] = ch;
				channels[index].setPosition(index);
				channels[index + 1].setPosition(index + 1);
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
				DebugWindow.WriteLine("シーケンスとUIを同期");
				bindedGrid = grid;
				bindedGrid.Children.Clear();
				bindedGrid.ColumnDefinitions.Clear();
				bindedGrid.RowDefinitions.Clear();
				for (int i = 0; i < divisionLabels.Count + 1; i++) {
					bindedGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(DivisionLabel.width) });
				}
				for (int i = 0; i < channels.Count + 1; i++) {
					bindedGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(Channel.height) });
				}

				Label label = new Label() { Content = "Sequence", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
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
				DebugWindow.WriteLine("セルを再描画");
				foreach (Channel ch in channels) {
					ch.repaint();
				}
			}
        }
    }
}
