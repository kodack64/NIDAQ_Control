using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using NIDAQInterface;
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
		public void popTask(long sampleRate, string[] channelNameArray, double[,] waveArray, double[,] minmaxVoltage) {
			NIDaqTaskManager.GetInstance().popTask(sampleRate, channelNameArray, waveArray, minmaxVoltage);
		}
		public void start() {
			NIDaqTaskManager.GetInstance().start();
		}
		public void stop() {
			NIDaqTaskManager.GetInstance().stop();
		}
	}
}
