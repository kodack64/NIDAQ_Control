using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NationalInstruments.DAQmx;

namespace NIDaqInterface {
	class TaskPack {
		double[,] waveArray;
		string[] channelNameArray;
		Task task;

		AnalogMultiChannelWriter aowriter;
		public TaskPack(string[] _name, double[,] _wave, long sampleRate, double[,] minmaxVoltage, TaskDoneEventHandler done) {
			waveArray = _wave;
			channelNameArray = _name;
			task = new Task();

			for (int i = 0; i < channelNameArray.Length; i++) {
				string name = channelNameArray[i];
				string vname = "Voltage" + i;
				task.AOChannels.CreateVoltageChannel(name, vname, minmaxVoltage[i, 0], minmaxVoltage[i, 1], AOVoltageUnits.Volts);
			}

			task.Timing.ConfigureSampleClock("", sampleRate, SampleClockActiveEdge.Rising, SampleQuantityMode.FiniteSamples, waveArray.GetLength(1));
			task.Done += done;
			task.Control(TaskAction.Verify);
			aowriter = new AnalogMultiChannelWriter(task.Stream);
		}
		public void execute() {
			aowriter.WriteMultiSample(false, waveArray);
			task.Control(TaskAction.Start);
		}
		public void stop() {
			task.Control(TaskAction.Stop);
			task.Control(TaskAction.Unreserve);
			//			task.Dispose();
		}
	}

}
