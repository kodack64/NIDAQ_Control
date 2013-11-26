using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace NIDaqInterfaceDummy {

	public class NIDaqTaskManager{
		private Queue <Thread> taskQueue = new Queue<Thread>();

		private static NIDaqTaskManager myInstance;
		bool isRunning;
		public static NIDaqTaskManager GetInstance() {
			if (myInstance == null) myInstance = new NIDaqTaskManager();
			return myInstance;
		}
		string[] analogInputList;
		string[] analogOutputList;
		string[] digitalInputList;
		string[] digitalOutputList;
		private NIDaqTaskManager() {
			analogOutputList = new string[3] { "ao1", "ao2", "ao3" };
			analogInputList = new string[3] { "ai1", "ai2", "ai3" };
			digitalOutputList = new string[3] { "do1", "do2", "do3" };
			digitalInputList = new string[3] { "di1", "di2", "di3" };
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

		public void popTask(long sampleRate, string[] channelNameArray, double[,] waveArray, double[,] minmaxVoltage) {			
			taskQueue.Enqueue(new Thread(this.dummyTask));
		}

		private volatile bool stoped=false;
		public void dummyTask() {
			stoped=false;
			int count=0;
			while (!stoped && count<10) {
				Thread.Sleep(100);
				count++;
			}
			doNextTask();
		}
		public void execute() {
			if (!isRunning) {
				if (taskQueue.Count > 0) {
					isRunning = true;
					taskQueue.Peek().Start();
				}
			}
		}
		public void stop() {
			if (isRunning) {
				isRunning = false;
				taskQueue.Peek().Join();
				taskQueue.Dequeue();
			}
		}
		public void doNextTask() {
			if (taskQueue.Count > 0) {
				taskQueue.Dequeue();
			}
			if (isRunning) {
				if (taskQueue.Count == 0) {
					isRunning = false;
				} else {
					taskQueue.Peek().Start();
				}
			}
		}
	}
}
