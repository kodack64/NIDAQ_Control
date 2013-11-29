using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace NIDaqInterfaceDummy{

	public class NIDaqTaskManager{
		public delegate void TaskEvent();

		public event TaskEvent taskEndEvent = delegate { };
		public event TaskEvent allTaskEndEvent = delegate { };
		public event TaskEvent taskStartEvent = delegate { };

		Thread taskThread;
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

		public void popTask(double sampleRate, string deviceName,string[] channelName,double[] minVoltage,double[] maxVoltage,double[,]wave) {
			waitTime = wave.GetLength(1)/sampleRate;
			taskQueue.Enqueue(new Thread(this.dummyTask));
		}

		private double waitTime = 0;
		private volatile bool stopped=false;
		public void dummyTask() {
			stopped = false;
			int count=0;
			while (!stopped && count<10) {
				Thread.Sleep((int)((waitTime/10)*1000));
				count++;
			}
		}
		public void executeAsync() {
			taskStartEvent();
			if (!isRunning) {
				isRunning = true;
				foreach (Thread task in taskQueue) {
					task.Start();
				}
				foreach (Thread task in taskQueue) {
					task.Join();
					taskEndEvent();
				}
				taskQueue.Clear();
				isRunning = false;
			}
			allTaskEndEvent();
		}
		public void start() {
			taskThread = new Thread(new ThreadStart(executeAsync));
			taskThread.Start();
		}

		public void stop() {
			stopped = true;
			taskThread.Join();
		}
	}
}
