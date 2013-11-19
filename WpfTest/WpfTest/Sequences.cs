using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace WpfTest
{
    namespace NIDAQ{

        //複数のシーケンス管理
        public class Sequences{
            private List<Sequence> sequences = new List<Sequence>();
			public Sequence currentSequence;
			public Sequences() {
				sequences.Add(new Sequence());
			}
        }

		//単一のシーケンス
        public class Sequence{
            private List<Channel> channels = new List<Channel>();
			private List<DivisionLabel> divisionLabels = new List<DivisionLabel>();
			public Sequence() {
				divisionLabels.Add(new DivisionLabel());
			}
			public int getIOCount(){
				return channels.Count;
			}
			public bool getIsAnalog(int index) {
				return channels[index].isAnalog;
			}
			public bool getIsInput(int index) {
				return channels[index].isInput;	
			}
			public void insertDivision(int index) {
				divisionLabels.Insert(index, new DivisionLabel());
				foreach (Channel ch in channels) {
					ch.insertPlot(index,0);
				}
			}
			public void removeDivision(int index) {
				divisionLabels.RemoveAt(index);
				foreach(Channel ch in channels){
					ch.removePlot(index);
				}
			}
			public void insertChannel(int index) {
				channels.Insert(index, new Channel(divisionLabels.Count));
			}
			public void removeChannel(int index) {
				channels.RemoveAt(index);
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
						DivisionLabel label = new DivisionLabel();
						label.fromText(s);
						tempDivisionLabels.Add(label);
					}
		
					for (int i = 1; i < strs.Length; i++) {
						Channel ch = new Channel(tempDivisionLabels.Count);
						ch.fromText(strs[i]);
						tempChannels.Add(ch);
					}

					divisionLabels = tempDivisionLabels;
					channels = tempChannels;

				}catch(Exception e){
					e.ToString();
					//load fail
				}
			}
        }

		//シーケンスのうち単一の入出力ライン
        public class Channel{
			public bool isAnalog{get;private set;}
			public bool isInput{get;private set;}
			public bool isBinded { get; private set; }
			public string bindedName;

			public Canvas ChannelCanvas;
			public ChannelLabel label;

			protected List<Plot> plots = new List<Plot>();
			public Channel() {
				isAnalog = true;
				isInput = false;
				isBinded = false;
				bindedName = "";
				plots.Add(new Plot() { index=0,value=0,type=PlotType.Hold,isEnd=true});
			}
			public Channel(int divisionCount) {
				isAnalog = true;
				isInput = false;
				isBinded = false;
				bindedName = "";
				for (int i = 0; i < divisionCount-1; i++) {
					plots.Add(new Plot() { index = 0, value = 0, type = PlotType.Hold, isEnd = false });
				}
				plots.Add(new Plot() { index = 0, value = 0, type = PlotType.Hold, isEnd = true });
			}
			public void insertPlot(int index,double value) {
				plots.Insert(index,new Plot() { index = index , value=value , type=PlotType.Hold ,isEnd=false});
			}
			public void removePlot(int index) {
				plots.RemoveAt(index);
			}
			public void setPlotValue(int index, double value) {
				plots[index].value = value;
			}
			public void setPlotType(int index, PlotType type) {
				plots[index].type=type;
			}

			public string toText() {
				string str;
				str = String.Format("{0},{1},{2},{3}",isAnalog,isInput,isBinded,bindedName,plots.Count);
				for (int i = 0; i < plots.Count; i++) {
					str+=","+plots[i].toText();
				}
				return str;
			}
			public void fromText(string str) {
				try{
					plots.Clear();
					string[] strs = str.Split(',');
					isAnalog = bool.Parse(strs[0]);
					isInput = bool.Parse(strs[1]);
					isBinded = bool.Parse(strs[2]);
					bindedName = strs[3];
					int plotCount = int.Parse(strs[4]);
					for (int i = 0; i < plotCount; i++) {
						Plot plot = new Plot();
						plot.fromText(strs[5 + i]);
						plots.Add(plot);
					}
				}catch(Exception e){
					// load fail
				}
			}
		}

		public class ChannelLabel {
			public string toText() { return ""; }
			public void fromText(string s) { }
			public Label label;
		}
		public class DivisionLabel{
			public Label label;
			public int time;
			public TimeUnit units;
			public string toText() { return ""; }
			public void fromText(string s) { }
		}

        public class Plot{
			public int index;
			public double value;
			public PlotType type;
			public bool isEnd;
			public string toText() {
				string str;
				str = String.Format("{0} {1} {2} {3}",index,value,Enum.GetName(typeof(PlotType),type),isEnd);
				return str;
			}
			public void fromText(string str) {
				try {
					string[] strs = str.Split(' ');
					index = int.Parse(strs[0]);
					value = double.Parse(strs[1]);
					type = (PlotType)Enum.Parse(typeof(PlotType),strs[2]);
					isEnd = bool.Parse(strs[3]);
				} catch (Exception e) {
					// load fail
				}
			}
		}
		public enum PlotType {
			Hold,
			Linear,
			Through
		}
		public enum TimeUnit {
			s,
			ms,
			us,
			ns
		}
    }
}
