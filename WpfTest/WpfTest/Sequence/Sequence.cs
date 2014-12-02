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
using System.IO;

public class TaskAssemble {
	public string deviceName;
	public string[] analogOutputChannelNames;
	public string[] digitalOutputChannelNames;
	public string[] analogInputChannelNames;
	public string[] digitalInputChannelNames;
	public double[,] outputWaves;
	public uint[,] outputDigis;
	public double[] outputMaxVoltage;
	public double[] outputMinVoltage;
	public double[] inputMaxVoltage;
	public double[] inputMinVoltage;
}

namespace NIDaqController {

	//単一のシーケンス
    public class Sequence{
		private static int uniqueId=0;
		//チャンネル
        private List<Channel> channels = new List<Channel>();
		//区間
		private List<Division> divisions = new List<Division>();
		//描画先グリッド
		private Grid bindedGrid;
		//シーケンス名
		private TextBox textSequenceName;
		public string name{
			get {
				return textSequenceName.Text;
			}
			set {
				textSequenceName.Text = value;
			}
		}
		//サンプルレート
		private TextBox textSampleRate;
		public double sampleRate {
			get {
				try {
					return double.Parse(textSampleRate.Text);
				} catch (Exception) {
					return 100;
				}
			}
			set {
				textSampleRate.Text = value.ToString();
			}
		}


		////////////////初期化
		//コンストラクタ
		public Sequence() {
			Division lastDivision = new Division(this);
			lastDivision.isLast = true;
			divisions.Add(lastDivision);
			textSequenceName = new TextBox() { Text="Sequence"+uniqueId};
			textSampleRate = new TextBox() { Text="1000"};
			uniqueId++;
		}
		public void addAllAnalogOutput(){
			foreach(string str in TaskManager.GetInstance().getAnalogOutputList()){
				insertChannel(channels.Count);
				setBindedChannelName(channels.Count - 1,str);
				channels[channels.Count - 1].isAnalog=true;
				channels[channels.Count - 1].isOutput=true;
			}
		}

		public List<TaskAssemble> taskAsm = new List<TaskAssemble>();

		////////////////波形成性
		//現在のシーケンスから波形を生成
		public void compile() {
			DebugWindow.Write("シーケンスから信号を作成...");

			taskAsm.Clear();

			//デバイスごとにTaskAssembleを作成
			List<string> deviceList = getEnabledDeviceList();
			int divisionCount = divisions.Count;
			for (int i = 0; i < deviceList.Count(); i++) {
				TaskAssemble ta = new TaskAssemble();
				long sampleCount = this.getSequenceSampleCount();

				ta.deviceName = deviceList[i];

				// すべてのチャンネルから特定デバイスに属するAO/DO/AI/DIをフィルター
				List<Channel> analogOutputChannels = channels.FindAll((ch) => ch.deviceName == ta.deviceName && ch.isAnalog && ch.isOutput);
				List<Channel> analogInputChannels = channels.FindAll((ch) => ch.deviceName == ta.deviceName && ch.isAnalog && !ch.isOutput);
				List<Channel> digitalOutputChannels = channels.FindAll((ch) => ch.deviceName == ta.deviceName && !ch.isAnalog && ch.isOutput);
				int analogOutputChannelCount = analogOutputChannels.Count();
				int analogInputChannelCount = analogInputChannels.Count();
				int digitalOutputChannelCount = digitalOutputChannels.Count();

				ta.analogOutputChannelNames = new string[analogOutputChannelCount];
				ta.outputMaxVoltage = new double[analogOutputChannelCount];
				ta.outputMinVoltage = new double[analogOutputChannelCount];
				ta.outputWaves = new double[analogOutputChannelCount, sampleCount];
	
				ta.digitalOutputChannelNames = new string[digitalOutputChannelCount];
				ta.outputDigis = new uint[digitalOutputChannelCount, sampleCount];

				ta.analogInputChannelNames = new string[analogInputChannelCount];
				ta.inputMaxVoltage = new double[analogInputChannelCount];
				ta.inputMinVoltage = new double[analogInputChannelCount];

				// AOを整理
				for (int ci = 0; ci < analogOutputChannelCount; ci++) {
					ta.analogOutputChannelNames[ci] = analogOutputChannels[ci].channelName;
					ta.outputMinVoltage[ci] = analogOutputChannels[ci].minVoltage;
					ta.outputMaxVoltage[ci] = analogOutputChannels[ci].maxVoltage;
					long offset = 0;
					// division ごとに波形をサンプルに変換
					for (int di = 0; di+1 < divisionCount; di++) {
						long divisionSample = getDivisionSampleCount(di);
						NodeType type = analogOutputChannels[ci].nodes[di].type;
						double current = analogOutputChannels[ci].nodes[di].value;
						double next = analogOutputChannels[ci].nodes[di + 1].value;
						if (type == NodeType.Hold) {
							for (int si = 0; si < divisionSample; si++) {
								ta.outputWaves[ci, offset + si] = current;
							}
						} else if (type == NodeType.Linear) {
							double val = current;
							double step = (next - current) / divisionSample;
							for (int si = 0; si < divisionSample; si++) {
								ta.outputWaves[ci, offset + si] = val;
								val += step;
							}
						}
						offset += divisionSample;
					}
				}

				// DOの情報を整理
				for (int ci = 0; ci < digitalOutputChannelCount; ci++) {
					ta.digitalOutputChannelNames[ci] = digitalOutputChannels[ci].channelName;
					long offset=0;
					for(int di=0;di+1<divisionCount;di++){
						long divisionSample = getDivisionSampleCount(di);
						double value = digitalOutputChannels[ci].nodes[di].value;
						string[] namespl = digitalOutputChannels[ci].channelName.Split("line".ToCharArray());
						int linenum = int.Parse(namespl[namespl.Count()-1]);
						for (int si = 0; si < divisionSample; si++) {
							ta.outputDigis[ci, offset + si] = (uint)(value>0?1<<linenum:0);
						}
						offset += divisionSample;
					}
				}

				// AIの情報を整理
				for (int ci = 0; ci < analogInputChannelCount; ci++) {
					ta.analogInputChannelNames[ci] = analogInputChannels[ci].channelName;
					ta.inputMinVoltage[ci] = analogInputChannels[ci].minVoltage;
					ta.inputMaxVoltage[ci] = analogInputChannels[ci].maxVoltage;
				}

				taskAsm.Add(ta);
			}
			DebugWindow.WriteLine("OK");
			DebugWindow.WriteLine(" サンプルレート	:" + sampleRate);
			DebugWindow.WriteLine(" 使用デバイス数 :" + deviceList.Count());
			DebugWindow.WriteLine(" シーケンス時間	:" + getSequenceTime());
			DebugWindow.WriteLine(" サンプル数	:" + getSequenceSampleCount());
			DebugWindow.WriteLine(" 通信量	:" + getSequenceSampleCount()*sizeof(double)*getEnabledChannelCount()*1e-6+"MByte");
		}
		////////////////波形取得
		public void setWaveForm(String channelName , double[] data) {
			List<Channel> matchedChannel = channels.FindAll((ch) => ch.channelName == channelName);
			if (matchedChannel.Count > 0) matchedChannel[0].inputWaveArray=data;

			String fileName = "_in_" + channelName.Replace('/', '_') + ".txt";
			StreamWriter sw = new StreamWriter(fileName);
			for(int i=0;i<data.Length;i++){
				sw.WriteLine("{0} ", data[i]);
			}
			sw.Close();
		}

		////////////////情報取得
		//divisionごとのサンプル数を取得
		public int getDivisionSampleCount(int divisionIndex) {
			return (int)(divisions[divisionIndex].time*sampleRate);
		}
		//シーケンス全体のサンプル数を取得
		public int getSequenceSampleCount() {
			int sum = 0;
			for (int i = 0; i < divisions.Count; i++) {
				sum += getDivisionSampleCount(i);
			}
			return sum;
		}
		//divisionの時間を取得
		public double getDivisionTime(int divisionIndex) {
			return divisions[divisionIndex].time;
		}
		//シーケンス全体の時間を取得
		public double getSequenceTime() {
			double sum = 0;
			divisions.ForEach((div) => { sum += div.time; });
			return sum;
		}
		//チャンネル数の取得
		public int getChannelCount(){
			return channels.Count;
		}
		//有効なチャンネル数を取得
		public int getEnabledChannelCount() {
			return channels.Count((ch) => ch.isAnalog && ch.isOutput && ch.channelName.Length > 0);
		}
		//有効なデバイス数を取得
		public int getEnabledDeviceCount() {
			List<string> devs = new List<string>();
			channels.ForEach(ch => { if (devs.Count(dev => dev == ch.deviceName) > 0)devs.Add(ch.deviceName); });
			return devs.Count();
		}
		//有効なデバイスのリストを取得
		public List<string> getEnabledDeviceList() {
			List<string> devs = new List<string>();
			channels.ForEach(ch => { if (devs.Count(dev => (dev == ch.deviceName)) == 0 && ch.deviceName.Length>0)devs.Add(ch.deviceName); });
			return devs;
		}
		//チャンネルの担当チャンネル名を取得
		public string getBindedChannelName(int channelIndex) {
			return channels[channelIndex].channelName;
		}
		//チャンネルの所属デバイス名を取得
		public string getBindedDeviceName(int channelIndex) {
			return channels[channelIndex].deviceName;
		}
		//チャンネルの担当チャンネルを指定
		private void setBindedChannelName(int channelIndex,string channelName) {
			channels[channelIndex].channelName=channelName;
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
			return channels[index].channelName.Length > 0;
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
			return channels[index].virtualName;
		}
		//division名を取得
		public string getDivisionName(int index) {
			return divisions[index].name;
		}


		////////////////UI操作
		//divisionを挿入
		public void insertDivision(int index) {
			DebugWindow.WriteLine(String.Format("{0}行目に行を挿入",index));
			divisions.Insert(index, new Division(this));
			bindedGrid.ColumnDefinitions.Insert(index, new ColumnDefinition() { Width = new GridLength(Division.width) });
			foreach (Channel ch in channels) {
				ch.insertNode(index,0);
			}
			for (int i = index; i < divisions.Count; i++) {
				divisions[i].columnIndex=i;
			}
			bindedGrid.Children.Add(divisions[index].panel);
			repaint();
		}
		//divisionを削除
		public void removeDivision(int index) {
			DebugWindow.WriteLine(String.Format("{0}行目を削除", index));
			bindedGrid.Children.Remove(divisions[index].panel);
			divisions.RemoveAt(index);
			foreach(Channel ch in channels){
				ch.removePlot(index);
			}
			for (int i = index; i < divisions.Count; i++) {
				divisions[i].columnIndex=i;
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
				channels[i].rowIndex=i;
			}
			bindedGrid.Children.Add(channels[index].panel);
			bindedGrid.Children.Add(channels[index].canvas);
			repaint();
		}
		//チャンネルを削除
		public void removeChannel(int index) {
			DebugWindow.WriteLine(String.Format("{0}列目を削除", index));
			bindedGrid.Children.Remove(channels[index].panel);
			bindedGrid.Children.Remove(channels[index].canvas);
			channels.RemoveAt(index);
			for (int i = index; i < channels.Count; i++) {
				channels[i].rowIndex=i;
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
			channels[index].rowIndex=index;
			channels[index-1].rowIndex=index-1;
			repaint();
		}
		//チャンネルを下に移動
		public void moveDown(int index) {
			DebugWindow.WriteLine(String.Format("{0}列目を下に移動", index));
			Channel ch = channels[index];
			channels[index] = channels[index + 1];
			channels[index + 1] = ch;
			channels[index].rowIndex=index;
			channels[index + 1].rowIndex=index + 1;
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

			{
				StackPanel miniStack = new StackPanel();
				Grid.SetRow(miniStack, 0);
				Grid.SetColumn(miniStack, 0);
				miniStack.Children.Add(new Label() { Content="Name"});
				miniStack.Children.Add(textSequenceName);
				miniStack.Children.Add(new Label() { Content = "SampleRate" });
				miniStack.Children.Add(textSampleRate);
				bindedGrid.Children.Add(miniStack);
			}

			for (int i = 0; i < divisions.Count; i++) {
				divisions[i].columnIndex = i;
				bindedGrid.Children.Add(divisions[i].panel);
			}
			for (int i = 0; i < channels.Count; i++) {
				channels[i].rowIndex=i;
				channels[i].span=divisions.Count;
				bindedGrid.Children.Add(channels[i].canvas);
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
