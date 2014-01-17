using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NationalInstruments.DAQmx;

namespace NIDaqInterface
{
	
	public class NIDaqTaskManager {
		public delegate void TaskEvent();

		public event TaskEvent taskEndEvent = delegate { };
		public event TaskEvent allTaskEndEvent = delegate { };
		public event TaskEvent taskStartEvent = delegate { };

		private volatile Queue<TaskPack> taskQueue = new Queue<TaskPack>();
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
		private NIDaqTaskManager() {
			analogInputList = DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.AI, PhysicalChannelAccess.External);
			analogOutputList = DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.AO, PhysicalChannelAccess.External);
			digitalInputList = DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.DIPort, PhysicalChannelAccess.External);
			digitalOutputList = DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.DOPort, PhysicalChannelAccess.External);
			isRunning = false;
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

		public void popTask(double sampleRate,string[] channelNameArray , double[,] waveArray,double[,] minmaxVoltage){
			try {
				if (channelNameArray.Length != waveArray.GetLength(0) || channelNameArray.Length == 0) return;
				TaskPack taskPack = new TaskPack(channelNameArray,waveArray,sampleRate,minmaxVoltage,((s,e)=>done()));
				taskQueue.Enqueue(taskPack);
			} catch (DaqException e) {
				throw new Exception(e.Message) ;
			}
		}

		public void clearTask() {
			stop();
		}
		public void initTask(string deviceName,double sampleRate,int sampleLength) {
			DeviceTaskPack deviceTask;
			if(deviceTaskQueue.Count()==0)deviceTask = new DeviceTaskPack(deviceName, sampleRate, sampleLength, (s, e) => done());
			else deviceTask = new DeviceTaskPack(deviceName, sampleRate, sampleLength, (s, e) => { });
			deviceTaskQueue.Add(deviceTask);
		}
		public void popTask(string[] channelName, double[] minVoltage, double[] maxVoltage, double[,] wave) {
			try {
				deviceTaskQueue[deviceTaskQueue.Count()-1].addAnalogChannels(channelName, minVoltage, maxVoltage, wave);
			} catch (DaqException e) {
				throw new Exception(e.Message);
			}
		}
		public void popTask(string[] channelName, uint[,] digis) {
			try {
				deviceTaskQueue[deviceTaskQueue.Count() - 1].addDigitalChannels(channelName, digis);
			} catch (DaqException e) {
				throw new Exception(e.Message);
			}
		}
		public void verify() {
			deviceTaskQueue[deviceTaskQueue.Count() - 1].verify();
		}

		public void start() {
			taskStartEvent();
			if (!isRunning) {
				for (int i = 0; i < deviceTaskQueue.Count(); i++) {
					deviceTaskQueue[i].execute();
				}
			}
		}
		public void stop() {
			for (int i = 0; i < deviceTaskQueue.Count(); i++) {
				deviceTaskQueue[i].stop();
			}
			isRunning = false;
			deviceTaskQueue.Clear();
		}
		public void done() {
			stop();
			taskEndEvent();
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
