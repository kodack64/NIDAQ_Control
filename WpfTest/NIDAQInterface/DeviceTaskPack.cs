using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NationalInstruments.DAQmx;

namespace NIDaqInterface {
	class DeviceTaskPack {
		double[,] waveArray;
		byte[,] byteArray;
		double sampleRate;
		string deviceName;
		Task task;
		AnalogMultiChannelWriter aowriter;
		DigitalMultiChannelWriter dowriter;
		public DeviceTaskPack(string _deviceName,double _sampleRate,TaskDoneEventHandler done) {
			task = new Task();
			task.Done += done;
			sampleRate = _sampleRate;
			deviceName = _deviceName;
		}
		public void addAnalogChannels(string[] channelNames, double[] minVol, double[] maxVol, double[,] wave) {
			waveArray = wave;
			for (int ch = 0; ch < channelNames.Length; ch++) {
				string name = channelNames[ch];
				string vname = "Voltage" + ch;
				task.AOChannels.CreateVoltageChannel(name, vname, minVol[ch], maxVol[ch], AOVoltageUnits.Volts);
			}
			task.Timing.ConfigureSampleClock("", sampleRate, SampleClockActiveEdge.Rising, SampleQuantityMode.FiniteSamples, waveArray.GetLength(1));
			aowriter = new AnalogMultiChannelWriter(task.Stream);
		}
		public void addDigitalChannels(string[] channelNames,byte[,] bytes) {
			byteArray=bytes;
			for (int ch = 0; ch < channelNames.Length; ch++) {
				string name = channelNames[ch];
				string vname = "DO" + ch;
				task.DOChannels.CreateChannel(name,vname,ChannelLineGrouping.OneChannelForAllLines);
			}
			dowriter = new DigitalMultiChannelWriter(task.Stream);
		}
		public void verify() {
			task.Timing.ConfigureSampleClock("", sampleRate, SampleClockActiveEdge.Rising, SampleQuantityMode.FiniteSamples, waveArray.GetLength(1));
			task.Control(TaskAction.Verify);
		}
/*
		public DeviceTaskPack(double sampleRate, string deviceName, string[] channelNames, double[] minVol, double[] maxVol, double[,] wave, TaskDoneEventHandler done) {
			waveArray = wave;
			task = new Task();
			for (int ch = 0; ch < channelNames.Length; ch++) {
				string name = channelNames[ch];
				string vname = "Voltage" + ch;
				task.AOChannels.CreateVoltageChannel(name, vname, minVol[ch], maxVol[ch], AOVoltageUnits.Volts);
			}
			task.Timing.ConfigureSampleClock("", sampleRate, SampleClockActiveEdge.Rising, SampleQuantityMode.FiniteSamples, waveArray.GetLength(1));
			task.Done += done;
			task.Control(TaskAction.Verify);
			aowriter = new AnalogMultiChannelWriter(task.Stream);
		}*/
		public void execute() {
			if(aowriter!=null && waveArray!=null)aowriter.WriteMultiSample(false, waveArray);
			if(dowriter!=null && byteArray!=null)dowriter.WriteMultiSamplePort(false,byteArray);
			task.Control(TaskAction.Start);
		}
		public void stop() {
			task.Control(TaskAction.Stop);
			task.Control(TaskAction.Unreserve);
			task.Dispose();
		}
	}
}
