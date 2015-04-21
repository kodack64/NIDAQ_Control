using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NationalInstruments.DAQmx;

namespace NIDaqInterface {
	class DeviceTaskPack {
		double[,] outputWaveArray;

		public List<String> analogInputChannelNameList = new List<String>();
		public double[,] inputWaveArray;
		uint[,] byteArray;
		double sampleRate;
		string deviceName;
		IAsyncResult readAsync;
		Task task;
		AnalogMultiChannelWriter aowriter = null;
		DigitalMultiChannelWriter dowriter = null;
		AnalogMultiChannelReader aireader = null;
		int sampleLength;
		public DeviceTaskPack(string _deviceName, double _sampleRate, int _sampleLength, TaskDoneEventHandler done) {
			task = new Task();
			task.Done += done;
			sampleRate = _sampleRate;
			deviceName = _deviceName;
			sampleLength = _sampleLength;
		}
		public void addAnalogOutputChannels(string[] channelNames, double[] minVol, double[] maxVol, double[,] wave) {
			outputWaveArray = wave;
			for (int ch = 0; ch < channelNames.Length; ch++) {
				string name = channelNames[ch];
				string vname = "Voltage" + ch;
				task.AOChannels.CreateVoltageChannel(name, vname, minVol[ch], maxVol[ch], AOVoltageUnits.Volts);
			}
			aowriter = new AnalogMultiChannelWriter(task.Stream);
		}
		public void addAnalogInputChannels(string[] channelNames, double[] minVol, double[] maxVol) {
			for (int ch = 0; ch < channelNames.Length; ch++) {
				string name = channelNames[ch];
				analogInputChannelNameList.Add(channelNames[ch]);
				string vname = "VoltageIn" + ch;
				task.AIChannels.CreateVoltageChannel(name, vname,AITerminalConfiguration.Differential, minVol[ch], maxVol[ch], AIVoltageUnits.Volts);
			}
			aireader = new AnalogMultiChannelReader(task.Stream);
			aireader.SynchronizeCallbacks = false;
		}
		public void addDigitalOutputChannels(string[] channelNames, uint[,] digis) {
			byteArray=digis;
			for (int ch = 0; ch < channelNames.Length; ch++) {
				string name = channelNames[ch];
				string vname = "DO" + ch;
//				task.DOChannels.CreateChannel(name,vname,ChannelLineGrouping.OneChannelForAllLines);
				task.DOChannels.CreateChannel(name, vname, ChannelLineGrouping.OneChannelForEachLine);
			}
			dowriter = new DigitalMultiChannelWriter(task.Stream);
		}
		public void verify() {
			task.Timing.ConfigureSampleClock("", sampleRate, SampleClockActiveEdge.Rising, SampleQuantityMode.FiniteSamples,sampleLength);
			task.Stream.Timeout = Math.Max(10000,(int)(sampleLength/sampleRate*1000)+1000);
			if (aowriter != null && outputWaveArray != null) aowriter.WriteMultiSample(false, outputWaveArray);
//			if (aireader != null) aireader.ReadMultiSample(1000);
			if (dowriter != null && byteArray != null) dowriter.WriteMultiSamplePort(false, byteArray);
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
			task.Control(TaskAction.Start);
			if (aireader != null) {
				readAsync = aireader.BeginReadMultiSample(sampleLength,null,null);
			}
		}
		public void stop() {
			if (aireader != null) {
				inputWaveArray = aireader.EndReadMultiSample(readAsync);
			}
			task.Control(TaskAction.Stop);
			task.Control(TaskAction.Unreserve);
			task.Dispose();
		}
		public void reset() {
			task.Control(TaskAction.Stop);
			if (aowriter != null && outputWaveArray != null) aowriter.WriteMultiSample(false, outputWaveArray);
//			if (aireader != null) aireader.ReadMultiSample(1000);
			if (dowriter != null && byteArray != null) dowriter.WriteMultiSamplePort(false, byteArray);
			task.Control(TaskAction.Verify);
		}
	}
}
