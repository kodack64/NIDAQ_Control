using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NationalInstruments.DAQmx;

namespace NIDAQInterface
{
	class TaskPack {
		double[,] waveArray;
		string[] channelNameArray;
		Task task;

		AnalogMultiChannelWriter aowriter;
		public TaskPack(string[] _name,double[,] _wave,long sampleRate,double[,] minmaxVoltage,TaskDoneEventHandler done) {
			waveArray = _wave;
			channelNameArray = _name;
			task = new Task();

			for (int i = 0; i < channelNameArray.Length; i++) {
				string name = channelNameArray[i];
				string vname = "Voltage" + i;
				task.AOChannels.CreateVoltageChannel(name, vname, minmaxVoltage[i,0], minmaxVoltage[i,1], AOVoltageUnits.Volts);
			}

			task.Timing.ConfigureSampleClock("", sampleRate, SampleClockActiveEdge.Rising, SampleQuantityMode.FiniteSamples, waveArray.GetLength(1));
			task.Done += done;
			task.Control(TaskAction.Verify);
			aowriter = new AnalogMultiChannelWriter(task.Stream);
		}
		public void execute(){
			aowriter.WriteMultiSample(false, waveArray);
			task.Control(TaskAction.Start);
		}
		public void stop() {
			task.Control(TaskAction.Stop);
			task.Control(TaskAction.Unreserve);
			task.Dispose();
		}
	}
	
	public class NIDaqInterface {
		private volatile Queue<TaskPack> taskQueue = new Queue<TaskPack>();
		private bool isRunning;
		private static NIDaqInterface myInstance;

		public static NIDaqInterface GetInstance() {
			if (myInstance == null) myInstance = new NIDaqInterface();
			return myInstance;
		}
		string[] analogInputList;
		string[] analogOutputList;
		string[] digitalInputList;
		string[] digitalOutputList;
		private NIDaqInterface() {
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

		public void popTask(long sampleRate,string[] channelNameArray , double[,] waveArray,double[,] minmaxVoltage){
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
				taskQueue.Dequeue();
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
