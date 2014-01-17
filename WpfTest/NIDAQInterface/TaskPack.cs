using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NationalInstruments.DAQmx;

namespace NIDaqInterface {
	class TaskPack {
		double[,] waveArray;
		string[] channelNameArray;
		List<string> deviceName = new List<string>();
		Task[] task;

		AnalogMultiChannelWriter[] aowriter;
		public TaskPack(string[] _name, double[,] _wave, double sampleRate, double[,] minmaxVoltage, TaskDoneEventHandler done) {
			waveArray = _wave;
			channelNameArray = _name;

			deviceName.Clear();
			for (int i = 0; i < channelNameArray.Count(); i++) {
				string[] spstr = channelNameArray[i].Split('/');
				if (deviceName.Count((n) => (n == spstr[0]))>0) {
					deviceName.Add(spstr[0]);
				}
			}

			task = new Task[deviceName.Count];
			aowriter = new AnalogMultiChannelWriter[deviceName.Count];

			for (int i = 0; i < task.Length; i++) {
				task[i] = new Task();

				for (int ch = 0; ch < channelNameArray.Length; ch++) {
					string name = channelNameArray[ch];
					if (name.Split('/')[0] == deviceName[i]) {
						string vname = "Voltage" + i;
						task[i].AOChannels.CreateVoltageChannel(name, vname, minmaxVoltage[ch, 0], minmaxVoltage[ch, 1], AOVoltageUnits.Volts);
					}
				}

				task[i].Timing.ConfigureSampleClock("", sampleRate, SampleClockActiveEdge.Rising, SampleQuantityMode.FiniteSamples, waveArray.GetLength(1));
				task[i].Done += done;
				task[i].Control(TaskAction.Verify);
				aowriter[i] = new AnalogMultiChannelWriter(task[i].Stream);
			}
		}
		public void execute() {
			for (int i = 0; i < task.Count(); i++) {
				aowriter[i].WriteMultiSample(false, waveArray);
			}
			for (int i = 0; i < task.Count(); i++) {
				task[i].Control(TaskAction.Start);
			}
		}
		public void stop() {
			for (int i = 0; i < task.Count(); i++) {
				task[i].Control(TaskAction.Stop);
				task[i].Control(TaskAction.Unreserve);
				task[i].Dispose();
			}
		}
	}

}
