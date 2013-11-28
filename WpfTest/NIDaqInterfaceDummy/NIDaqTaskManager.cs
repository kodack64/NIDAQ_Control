using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace NIDaqInterfaceDummy{

	public class NIDaqTaskManager{
		public delegate void TaskEvent();

		private TaskEvent taskEndEvent=null;
		private TaskEvent allTaskEndEvent=null;
		private TaskEvent taskStartEvent=null;

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
			analogOutputList = new string[3] { "dev1/ao1", "dev1/ao2", "dev2/ao3" };
			analogInputList = new string[3] { "dev1/ai1", "dev1/ai2", "dev2/ai3" };
			digitalOutputList = new string[3] { "dev1/do1", "dev1/do2", "dev2/do3" };
			digitalInputList = new string[3] { "dev1/di1", "dev1/di2", "dev2/di3" };
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

		public void popTask(double sampleRate, string[] channelNameArray, double[,] waveArray, double[,] minmaxVoltage) {			
			taskQueue.Enqueue(new Thread(this.dummyTask));
		}

		private volatile bool stopped=false;
		public void dummyTask() {
			stopped=false;
			int count=0;
			while (!stopped && count<10) {
				Thread.Sleep(100);
				count++;
			}
			if(!stopped)doNextTask();
		}

		public void registStartEvent(TaskEvent func) {
			taskStartEvent += func;
		}
		public void registEndEvent(TaskEvent func) {
			taskEndEvent += func;
		}
		public void registAllEndEvent(TaskEvent func) {
			allTaskEndEvent += func;
		}
		public void start() {
			if (!isRunning) {
				if (taskQueue.Count > 0) {
					isRunning = true;
					taskStartEvent();
					taskQueue.Peek().Start();
				}
			}
		}
		public void stop() {
			lock (this) {
				if (isRunning) {
				isRunning = false;
					if (taskQueue.Count > 0) {
						taskEndEvent();
						stopped = true;
						taskQueue.Peek().Join();
						taskQueue.Dequeue();
						if (taskQueue.Count == 0) {
							allTaskEndEvent();
						}
					}
				}
			}
		}
		public void doNextTask() {
			lock (this) {
				if (taskQueue.Count > 0) {
					taskEndEvent();
					taskQueue.Dequeue();
				}
				if (isRunning) {
					if (taskQueue.Count == 0) {
						isRunning = false;
						allTaskEndEvent();
					} else {
						taskStartEvent();
						taskQueue.Peek().Start();
					}
				}
			}
		}
	}
}
