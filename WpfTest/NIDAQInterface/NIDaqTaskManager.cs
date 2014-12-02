using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NationalInstruments.DAQmx;


// NIDaq 関連のインターフェイス
namespace NIDaqInterface
{
	public class NIDaqTaskManager {

		// 動作の開始時と終了時のコールバック
		public delegate void TaskEvent();

		public event TaskEvent taskEndEvent = delegate { };
		public event TaskEvent allTaskEndEvent = delegate { };
		public event TaskEvent taskStartEvent = delegate { };

		// 1シーケンスに対応するタスク
//		private volatile Queue<TaskPack> taskQueue = new Queue<TaskPack>();

		// 1シーケンス中でのタスク　各デバイスごとのリスト
		private volatile List<DeviceTaskPack> deviceTaskQueue = new List<DeviceTaskPack>();
		private bool isRunning;
		public bool isRepeat;
		private static NIDaqTaskManager myInstance;
		public static NIDaqTaskManager GetInstance() {
			if (myInstance == null) myInstance = new NIDaqTaskManager();
			return myInstance;
		}
		string[] analogInputList;
		string[] analogOutputList;
		string[] digitalInputList;
		string[] digitalOutputList;
		
		List<double[,]> inputWaveArray = new List<double[,]>();
		List<List<String>> inputWaveNameList = new List<List<String>>();

		private NIDaqTaskManager() {
			analogInputList = DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.AI, PhysicalChannelAccess.External);
			analogOutputList = DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.AO, PhysicalChannelAccess.External);
			digitalInputList = DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.DILine, PhysicalChannelAccess.External);
			digitalOutputList = DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.DOLine, PhysicalChannelAccess.External);
		}
		public string[] getAnalogInputList() {
			return analogInputList;
		}
		public string[] getDigitalInputList() {
			return digitalInputList;
		}
		public string[] getAnalogOutputList() {
			return analogOutputList;
		}
		public string[] getDigitalOutputList() {
			return digitalOutputList;
		}
		public void setRepeatFlag(bool _flag) {
			isRepeat = _flag;
		}
		/*
		public void popTask(double sampleRate,string[] channelNameArray , double[,] waveArray,double[,] minmaxVoltage){
			try {
				if (channelNameArray.Length != waveArray.GetLength(0) || channelNameArray.Length == 0) return;
				TaskPack taskPack = new TaskPack(channelNameArray,waveArray,sampleRate,minmaxVoltage,((s,e)=>done()));
				taskQueue.Enqueue(taskPack);
			} catch (DaqException e) {
				throw new Exception(e.Message) ;
			}
		}
		 */

		public void clearTask() {
			stop();
		}

		// 新規のdeviceの1シーケンスのタスクに追加
		public void initTask(string deviceName,double sampleRate,int sampleLength) {
			DeviceTaskPack deviceTask;
			if(deviceTaskQueue.Count()==0)deviceTask = new DeviceTaskPack(deviceName, sampleRate, sampleLength, (s, e) => done());
			else deviceTask = new DeviceTaskPack(deviceName, sampleRate, sampleLength, (s, e) => { });
			deviceTaskQueue.Add(deviceTask);
		}
		// 現在設定されているデバイスにAOを追加
		public void popTaskAnalogOutput(string[] channelName, double[] minVoltage, double[] maxVoltage, double[,] wave) {
			try {
				deviceTaskQueue[deviceTaskQueue.Count()-1].addAnalogOutputChannels(channelName, minVoltage, maxVoltage, wave);
			} catch (DaqException e) {
				throw new Exception(e.Message);
			}
		}
		// 現在設定されているデバイスにAIを追加
		public void popTaskAnalogInput(string[] channelName, double[] minVoltage, double[] maxVoltage) {
			try {
				deviceTaskQueue[deviceTaskQueue.Count() - 1].addAnalogInputChannels(channelName, minVoltage, maxVoltage);
			} catch (DaqException e) {
				throw new Exception(e.Message);
			}
		}
		// 現在設定されているデバイスにDOを追加
		public void popTaskDigitalOutput(string[] channelName, uint[,] digis) {
			try {
				deviceTaskQueue[deviceTaskQueue.Count() - 1].addDigitalOutputChannels(channelName, digis);
			} catch (DaqException e) {
				throw new Exception(e.Message);
			}
		}
		// 現在設定されているデバイスをverify
		public void verify() {
			deviceTaskQueue[deviceTaskQueue.Count() - 1].verify();
		}

		// 現在までに設定されたすべてのデバイスキューを実行
		public void start() {
			inputWaveArray.Clear();
			inputWaveNameList.Clear();
			taskStartEvent();
			if (!isRunning) {
				for (int i = 0; i < deviceTaskQueue.Count(); i++) {
					deviceTaskQueue[i].execute();
				}
			}
		}
		// すべてのデバイスを停止
		public void stop() {
			for (int i = 0; i < deviceTaskQueue.Count(); i++) {
				deviceTaskQueue[i].stop();
				if (deviceTaskQueue[i].inputWaveArray != null) {
					inputWaveArray.Add(deviceTaskQueue[i].inputWaveArray);
					inputWaveNameList.Add(deviceTaskQueue[i].analogInputChannelNameList);
				}
			}
			isRunning = false;
			deviceTaskQueue.Clear();
		}
		// すべてのタスクが実行された後のコールバック
		public void done() {
			stop();
			taskEndEvent();
		}

		// 入力信号を取得
		public double[,] getInputWaveArray(int deviceIndex) {
			return inputWaveArray[deviceIndex];
		}
		// 入力信号の数を取得
		public int getInputWaveDeviceCount() {
			return inputWaveArray.Count;
		}
		// 入力信号の名前を取得
		public List<String> getInputWaveNameList(int deviceIndex) {
			return inputWaveNameList[deviceIndex];
		}

		/*
		public void start() {
			if (!isRunning) {
				if (taskQueue.Count > 0) {
					isRunning = true;
					taskQueue.Peek().execute();
				}
			}
		}
		public void stop() {
			if (isRunning) {
				taskQueue.Peek().stop();
				taskQueue.Dequeue();
				isRunning = false;
			}
		}
		public void doNextTask() {
			if (taskQueue.Count > 0) {
				taskQueue.Peek().stop();
//				taskQueue.Dequeue();
			}
			if (isRunning) {
				if (taskQueue.Count == 0) {
					isRunning = false;
				} else {
					taskQueue.Peek().execute();
				}
			}
		}
		 * */
	}
}
