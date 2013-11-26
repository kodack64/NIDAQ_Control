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

		//単一のシーケンス
        public class Sequence{
			//チャンネル
            private List<Channel> channels = new List<Channel>();
			//区間
			private List<Division> divisions = new List<Division>();
			//波形
			private List<List<double[]>> waves;
			//描画先グリッド
			private Grid bindedGrid;


			////////////////初期化
			//コンストラクタ
			public Sequence() {
				Division lastDivision = new Division(this);
				lastDivision.label.Text = "Last";
				lastDivision.time = 0;
				divisions.Add(lastDivision);
				waves = new List<List<double[]>>();
			}

			////////////////波形成性
			//現在のシーケンスから波形を生成
			public void compile(long sampleRate) {
				DebugWindow.Write("シーケンスから信号を作成...");
				waves.Clear();
				for (int ci = 0; ci < channels.Count; ci++) {
					List<double[]> channelWave = new List<double[]>();

					Channel ch = channels[ci];
					for (int di = 0; di+1 < divisions.Count; di++) {
						long sampleNum = this.getDivisionSampleCount(di,sampleRate);
						double[] wave = new double[sampleNum];
						Node Node = ch.nodes[di];
						Node nextPlot = ch.nodes[di+1];

						if (Node.type == NodeType.Hold) {
							for (int i = 0; i < sampleNum; i++) {
								wave[i] = Node.value;
							}
						} else if (Node.type == NodeType.Linear) {
							for (int i = 0; i < sampleNum; i++) {
								wave[i] = Node.value + (nextPlot.value - Node.value)*i / sampleNum;
							}
						} else {
							for(int i=0;i<sampleNum;i++){
								wave[i] = Node.value;
							}
						}
						channelWave.Add(wave);
					}
					waves.Add(channelWave);
				}
				DebugWindow.WriteLine("OK");
				DebugWindow.WriteLine(" サンプルレート	:" + sampleRate);
				DebugWindow.WriteLine(" シーケンス時間	:" + getSequenceTime());
				DebugWindow.WriteLine(" サンプル数	:" + getSequenceSampleCount(sampleRate));
			}
			//前回生成した波形を取得
			public double[] getWave(int channelIndex,int divisionIndex) {
				return waves[channelIndex][divisionIndex];
			}


			////////////////情報取得
			//divisionごとのサンプル数を取得
			public long getDivisionSampleCount(int divisionIndex,long sampleRate) {
				return (long)(divisions[divisionIndex].getTime()*sampleRate);
			}
			//シーケンス全体のサンプル数を取得
			public long getSequenceSampleCount(long sampleRate) {
				long sum = 0;
				for (int i = 0; i < divisions.Count; i++) {
					sum += getDivisionSampleCount(i, sampleRate);
				}
				return sum;
			}
			//チャンネルの担当デバイスを取得
			public string getBindedName(int channelIndex) {
				return channels[channelIndex].bindedName;
			}
			//divisionの時間を取得
			public double getDivisionTime(int index) {
				return divisions[index].getTime();
			}
			//シーケンス全体の時間を取得
			public double getSequenceTime() {
				double sum = 0;
				foreach (Division div in divisions) {
					sum += div.getTime();
				}
				return sum;
			}
			//チャンネル数の取得
			public int getChannelCount(){
				return channels.Count;
			}
			//有効なチャンネル数を取得
			public int getEnabledChannelCount() {
				int count = 0;
				for(int i=0;i<channels.Count;i++){
					if (channels[i].isAnalog && channels[i].isOutput && channels[i].bindedName.Length > 0) {
						count++;
					}
				}
				return count;
			}
			//divisionの数の取得
			public int getDivisionCount() {
				return divisions.Count;
			}
			//指定インデックスのチャンネルがアナログがどうか
			public bool getIsAnalog(int index) {
				return channels[index].isAnalog;
			}
			//指定インデックスのチャンネルがデバイスに接続されているか
			public bool getIsBinded(int index) {
				return channels[index].bindedName.Length > 0;
			}
			//指定インデックスのチャンネルが出力化どうか
			public bool getIsOutput(int index) {
				return channels[index].isOutput;	
			}
			//指定インデックスの最大電圧を取得
			public double getMaxVoltage(int index) {
				return channels[index].maxVoltage;
			}
			//指定インデックスの最小電圧を取得
			public double getMinVoltage(int index) {
				return channels[index].minVoltage;
			}
			//チャンネル名を取得
			public string getChannelName(int index) {
				return channels[index].getName();
			}
			//division名を取得
			public string getDivisionName(int index) {
				return divisions[index].getName();
			}


			////////////////UI操作
			//divisionを挿入
			public void insertDivision(int index) {
				DebugWindow.WriteLine(String.Format("{0}行目に行を挿入",index));
				divisions.Insert(index, new Division(this));
				bindedGrid.ColumnDefinitions.Insert(index, new ColumnDefinition() { Width = new GridLength(Division.width) });
				foreach (Channel ch in channels) {
					ch.insertNode(index,0);
					ch.setSpan(divisions.Count);
				}
				for (int i = index; i < divisions.Count; i++) {
					divisions[i].setPosition(i);
				}
				bindedGrid.Children.Add(divisions[index].label);
				repaint();
			}
			//divisionを削除
			public void removeDivision(int index) {
				DebugWindow.WriteLine(String.Format("{0}行目を削除", index));
				bindedGrid.Children.Remove(divisions[index].label);
				divisions.RemoveAt(index);
				foreach(Channel ch in channels){
					ch.removePlot(index);
					ch.setSpan(divisions.Count);
				}
				for (int i = index; i < divisions.Count; i++) {
					divisions[i].setPosition(i);
				}
				bindedGrid.ColumnDefinitions.RemoveAt(index);
				repaint();
			}
			//チャンネルを挿入
			public void insertChannel(int index) {
				DebugWindow.WriteLine(String.Format("{0}列目に列を挿入", index));
				channels.Insert(index, new Channel(this, divisions.Count));
				bindedGrid.RowDefinitions.Insert(index, new RowDefinition() { Height = new GridLength(Channel.height) });
				for (int i = index; i < channels.Count; i++) {
					channels[i].setPosition(i);
				}
				bindedGrid.Children.Add(channels[index].channelLabel);
				bindedGrid.Children.Add(channels[index].channelCanvas);
				repaint();
			}
			//チャンネルを削除
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
			//チャンネルを上に移動
			public void moveUp(int index) {
				DebugWindow.WriteLine(String.Format("{0}列目を上に移動", index));
				Channel ch = channels[index];
				channels[index] = channels[index - 1];
				channels[index - 1] = ch;
				channels[index].setPosition(index);
				channels[index-1].setPosition(index-1);
				repaint();
			}
			//チャンネルを下に移動
			public void moveDown(int index) {
				DebugWindow.WriteLine(String.Format("{0}列目を下に移動", index));
				Channel ch = channels[index];
				channels[index] = channels[index + 1];
				channels[index + 1] = ch;
				channels[index].setPosition(index);
				channels[index + 1].setPosition(index + 1);
				repaint();
			}

			////////////////ファイル入出力
			private const string separator = "\n";
			//保存
			public string toSeq() {
				string str="";
				str += divisions.Count + separator;
				for (int i = 0; i < divisions.Count; i++) {
					str += divisions[i].toSeq() + separator;
				}
				str += channels.Count+separator;
				for (int i = 0; i < channels.Count; i++) {
					str += channels[i].toSeq() + separator;
				}
				return str;
			}
			//読み込み
			public void fromSeq(string str) {
				int tempDivisionCount;
				int tempChannelCount;
				int lineCount=0;
				string[] strs = str.Split(separator.ToCharArray());

				tempDivisionCount = int.Parse(strs[0].Trim()); lineCount++;
				divisions.Clear();
				for (int divisionCount=0; divisionCount < tempDivisionCount; divisionCount++) {
					Division label = new Division(this);
					label.fromSeq(strs[lineCount+divisionCount]);
					divisions.Add(label);
				}
				lineCount += tempDivisionCount;

				tempChannelCount = int.Parse(strs[lineCount].Trim()); lineCount++;
				channels.Clear();
				for (int channelCount = 0; channelCount < tempChannelCount; channelCount++) {
					Channel ch = new Channel(this,tempDivisionCount);
					ch.fromSeq(strs[lineCount + channelCount]);
					channels.Add(ch);
				}
				lineCount += tempChannelCount;
			}

			////////////////UI描画
			//UIを更新
			public void bindGridUI(Grid grid) {
				DebugWindow.WriteLine("シーケンスとUIを同期");
				bindedGrid = grid;
				bindedGrid.Children.Clear();
				bindedGrid.ColumnDefinitions.Clear();
				bindedGrid.RowDefinitions.Clear();
				for (int i = 0; i < divisions.Count + 1; i++) {
					bindedGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(Division.width) });
				}
				for (int i = 0; i < channels.Count + 1; i++) {
					bindedGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(Channel.height) });
				}

				Label label = new Label() { Content = "Sequence", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
				label.SetValue(Grid.RowProperty, 0);
				label.SetValue(Grid.ColumnProperty, 0);
				bindedGrid.Children.Add(label);

				for (int i = 0; i < divisions.Count; i++) {
					divisions[i].label.SetValue(Grid.RowProperty, 0);
					divisions[i].label.SetValue(Grid.ColumnProperty, i + 1);
					bindedGrid.Children.Add(divisions[i].label);
				}
				for (int i = 0; i < channels.Count; i++) {
					channels[i].setPosition(i);
					channels[i].setSpan(divisions.Count);
					bindedGrid.Children.Add(channels[i].channelCanvas);
					bindedGrid.Children.Add(channels[i].channelCanvas);
				}
				repaint();
			}
			//再描画
			public void repaint() {
				DebugWindow.WriteLine("セルを再描画");
				foreach (Channel ch in channels) {
					ch.repaint();
				}
			}
		}
    }
}
