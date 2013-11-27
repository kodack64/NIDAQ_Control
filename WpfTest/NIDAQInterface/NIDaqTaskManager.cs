using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NationalInstruments.DAQmx;

namespace NIDaqInterface
{
	
	public class NIDaqTaskManager {
		private volatile Queue<TaskPack> taskQueue = new Queue<TaskPack>();
		private bool isRunning;
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

		public void popTask(double sampleRate,string[] channelNameArray , double[,] waveArray,double[,] minmaxVoltage){
			try {
				if (channelNameArray.Length != waveArray.GetLength(0) || channelNameArray.Length == 0) return;
				TaskPack taskPack = new TaskPack(channelNameArray,waveArray,sampleRate,minmaxVoltage,((s,e)=>doNextTask()));
				taskQueue.Enqueue(taskPack);
			} catch (DaqException e) {
				throw new Exception(e.Message) ;
			}
		}
		public void execute() {
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
	}
}
