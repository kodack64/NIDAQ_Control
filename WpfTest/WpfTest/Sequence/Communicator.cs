
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfTest;

namespace NIDaqController{
	// NIDaqの通信箇所
	public class NIDaqCommunicator {

		private delegate void taskEndEvent();

		private const long defaultSampleRate = (long)2.5e6;

		public bool isRepeatEnabled;
		public int repeatCount;
		private int currentRepeatCount;

		//シーケンス
		private Sequences seq;

		//タスク管理
		private TaskManager taskManager;

		//サンプルレート
		private double sampleRate;

		//コンストラクタ
		public NIDaqCommunicator(Sequences _seq) {
			seq = _seq;
			sampleRate = defaultSampleRate;
			taskManager = TaskManager.GetInstance();
			taskManager.addTaskEndEventHandler(TaskEnd);
			repeatCount = 0;
			currentRepeatCount = 0;
			isRepeatEnabled = false;
		}

		//現在のシーケンスからタスクを生成しキューに入れる
		public void Run() {

			Sequence current = seq.getCurrentSequence();
			sampleRate = current.samleRate;

			current.compile(sampleRate);

			convert();
		}
		public void convert(){
			int aochan = current.getEnabledChannelCount();
			long sampleSum = current.getSequenceSampleCount(sampleRate);
			string[] nameList = new string[aochan];
			double[,] waveArray = new double[aochan, sampleSum];
			double[,] minmaxVoltage = new double[aochan, 2];

			int aoCount = 0;
			long sampleCount = 0;
			for (int ci = 0; ci < current.getChannelCount(); ci++) {
				if (current.getIsAnalog(ci) && current.getIsOutput(ci) && current.getIsBinded(ci)) {
					sampleCount = 0;
					for (int di = 0; di + 1 < current.getDivisionCount(); di++) {
						nameList[ci] = current.getBindedName(ci);
						minmaxVoltage[ci, 0] = current.getMinVoltage(ci);
						minmaxVoltage[ci, 1] = current.getMaxVoltage(ci);
						double[] channelWave = current.getWave(ci, di);
						for (int j = 0; j < channelWave.Length; j++) {
							waveArray[aoCount, sampleCount] = channelWave[j];
							sampleCount++;
						}
					}
					aoCount++;
				}
			}

		}

		//停止
		public void Stop() {
			repeatCount = 0;
			taskManager.stop();
		}

		//最新タスクが終了
		public void TaskEnd() {
			if (isRepeatEnabled && currentRepeatCount<repeatCount) {
				MainWindow.myInstance.Dispatcher.BeginInvoke(
					new Action(() => { Run(); })
					);
			}
		}
	}
}
