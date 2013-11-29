using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

//using NIDAQInterface;
using NIDaqInterfaceDummy;

namespace NIDaqController {
	class TaskManager{
		public static TaskManager myInstance;
		public static TaskManager GetInstance() {
			if (myInstance == null) myInstance = new TaskManager();
			NIDaqTaskManager.GetInstance();
			return myInstance;
		}
		public string[] getAnalogInputList() {
			return NIDaqTaskManager.GetInstance().getAnalogInputList();
		}
		public string[] getDigitalInputList() {
			return NIDaqTaskManager.GetInstance().getDigitalInputList();
		}
		public string[] getAnalogOutputList() {
			return NIDaqTaskManager.GetInstance().getAnalogOutputList();
		}
		public string[] getDigitalOutputList() {
			return NIDaqTaskManager.GetInstance().getDigitalOutputList();
		}
		public void popTask(double sampleRate, string deviceName,string[] channelName,double[] minVoltage,double[] maxVoltage,double[,]wave) {
			NIDaqTaskManager.GetInstance().popTask(sampleRate, deviceName,channelName,minVoltage,maxVoltage,wave);
		}
		public void start() {
			NIDaqTaskManager.GetInstance().start();
		}
		public void stop() {
			NIDaqTaskManager.GetInstance().stop();
		}
		public void addTaskStartEventHandler(Action func) {
			NIDaqTaskManager.GetInstance().taskStartEvent += ()=>func();
		}
		public void addTaskEndEventHandler(Action func) {
			NIDaqTaskManager.GetInstance().taskEndEvent += () => func();
		}
		public void addAllTaskEndEventHandler(Action func) {
			NIDaqTaskManager.GetInstance().allTaskEndEvent += () => func();
		}
	}
}
