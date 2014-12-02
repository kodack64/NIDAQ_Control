using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using NIDaqInterface;
//using NIDaqInterfaceDummy;

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
		public void clearTask() {
			NIDaqTaskManager.GetInstance().clearTask();
		}
		public void initTask(string deviceName,double sampleRate,int sampleLength) {
			NIDaqTaskManager.GetInstance().initTask(deviceName, sampleRate,sampleLength);
		}
		public void popTaskAnalogOutput(string[] channelName,double[] minVoltage,double[] maxVoltage,double[,]wave) {
			NIDaqTaskManager.GetInstance().popTaskAnalogOutput(channelName,minVoltage,maxVoltage,wave);
		}
		public void popTaskAnalogInput(string[] channelName, double[] minVoltage, double[] maxVoltage) {
			NIDaqTaskManager.GetInstance().popTaskAnalogInput(channelName, minVoltage, maxVoltage);
		}
		public void popTaskDigitalOutput(string[] channelName, uint[,] digis) {
			NIDaqTaskManager.GetInstance().popTaskDigitalOutput(channelName,digis);
		}
		public void verify() {
			NIDaqTaskManager.GetInstance().verify();
		}
//		public void popTask(double sampleRate, string[] channelNameArray, double[,] waveArray, double[,] minmaxVoltage) {
//			NIDaqTaskManager.GetInstance().popTask(sampleRate,channelNameArray,waveArray,minmaxVoltage);
//		}
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
		public void setRepeatFlag(bool flag) {
			NIDaqTaskManager.GetInstance().setRepeatFlag(flag);
		}
		public double[,] getInputWaveArray(int deviceIndex) {
			return NIDaqTaskManager.GetInstance().getInputWaveArray(deviceIndex);
		}
		public int getInputWaveDeviceCount() {
			return NIDaqTaskManager.GetInstance().getInputWaveDeviceCount();
		}
		public List<String> getInputWaveNameList(int deviceIndex) {
			return NIDaqTaskManager.GetInstance().getInputWaveNameList(deviceIndex);
		}
	}
}
