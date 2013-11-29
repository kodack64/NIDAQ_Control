
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
			current.compile();
			foreach(TaskAssemble ta in current.taskAsm){
				taskManager.popTask(current.sampleRate,ta.deviceName,ta.channelNames,ta.minVoltage,ta.maxVoltage,ta.waves);
			}
			taskManager.start();
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
