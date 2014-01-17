
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

		public bool isRepeatEnabled;
		public int repeatCount;
		private int currentRepeatCount;

		//シーケンス
		private Sequences seq;

		//タスク管理
		private TaskManager taskManager;

		//コンストラクタ
		public NIDaqCommunicator(Sequences _seq) {
			seq = _seq;
			taskManager = TaskManager.GetInstance();
			taskManager.addTaskEndEventHandler(TaskEnd);
			repeatCount = 0;
			currentRepeatCount = 0;
			isRepeatEnabled = false;
		}

		//現在のシーケンスからタスクを生成しキューに入れる
		public void Run() {
			currentRepeatCount = 0;
			if (seq.getCurrentSequence().getDivisionCount() <= 1){
				DebugWindow.WriteLine("オペレーションが空です。");
				MainWindow.myInstance.Dispatcher.BeginInvoke(
					new Action(() => { MainWindow.myInstance.Callback_SystemStop(); })
					);
			} else if(seq.getCurrentSequence().getChannelCount() == 0) {
				DebugWindow.WriteLine("IOが空です。");
				MainWindow.myInstance.Dispatcher.BeginInvoke(
					new Action(() => { MainWindow.myInstance.Callback_SystemStop(); })
					);
			}else{
				doTask();
			}
		}
		private void doTask() {
			Sequence current = seq.getCurrentSequence();
			current.compile();
			taskManager.clearTask();
			taskManager.setRepeatFlag(isRepeatEnabled);
			if (current.taskAsm.Count() == 0) {
				DebugWindow.WriteLine("有効なチャンネルがありません。");
				MainWindow.myInstance.Dispatcher.BeginInvoke(
					new Action(() => { MainWindow.myInstance.Callback_SystemStop(); })
					);
				return;
			}
			foreach (TaskAssemble ta in current.taskAsm) {
				if (ta.analogChannelNames.Count() > 0) {
					taskManager.initTask(ta.deviceName, current.sampleRate, current.getSequenceSampleCount());
					taskManager.popTask(ta.analogChannelNames, ta.minVoltage, ta.maxVoltage, ta.waves);
					taskManager.verify();
				}
				if (ta.digitalChannelNames.Count() > 0) {
					taskManager.initTask(ta.deviceName, current.sampleRate, current.getSequenceSampleCount());
					taskManager.popTask(ta.digitalChannelNames, ta.digis);
					taskManager.verify();
				}
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
			currentRepeatCount++;
			if (isRepeatEnabled && currentRepeatCount < repeatCount) {
				MainWindow.myInstance.Dispatcher.BeginInvoke(new Action(() => { doTask(); }));
			} else {
				MainWindow.myInstance.Dispatcher.BeginInvoke(new Action(() => { MainWindow.myInstance.Callback_SystemStop(); }));
			}
		}
	}
}
