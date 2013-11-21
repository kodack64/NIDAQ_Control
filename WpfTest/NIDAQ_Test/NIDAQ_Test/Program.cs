using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using NationalInstruments.Analysis.SignalGeneration;
using NationalInstruments.DAQmx;
using NationalInstruments;

namespace NIDAQ_Test {
	class Program {
		static void Main(string[] args) {
			Program prog = new Program();
//			prog.Read();
			prog.Write();
//			prog.Read2();
//			prog.Write2();
			Console.ReadKey();
		}
		List<Double> datalist = new List<Double>();
		int sampleRate = 1000;
		int repeat = 20;
		public void Read() {
			try {
				string[] channelNameList = DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.AI, PhysicalChannelAccess.External);
				if (channelNameList.Length > 0) {
					Task task = new Task();
					task.AIChannels.CreateVoltageChannel(channelNameList[0], "Voltage", AITerminalConfiguration.Differential, 0.0, 10.0, AIVoltageUnits.Volts);
					task.Timing.ConfigureSampleClock("", 100000, SampleClockActiveEdge.Rising, SampleQuantityMode.FiniteSamples);
					task.Control(TaskAction.Verify);

					AnalogSingleChannelReader airead = new AnalogSingleChannelReader(task.Stream);
					AnalogWaveform<double> waveform;
					for(int i=0;i<repeat;i++){
						waveform = airead.ReadWaveform(sampleRate);
						datalist.AddRange(waveform.GetRawData());
						Console.Out.WriteLine("Acquire " + i + "th try");
					}
					StreamWriter writer = new StreamWriter(File.Open("ai.txt", FileMode.Create));
					int c = 0;
					foreach (double d in datalist) {
						writer.WriteLine(String.Format("{0} {1}",c,d));
						c++;
					}
					writer.Close();
				}
			} catch (DaqException e) {
				Console.Out.WriteLine(e.Message);
			}
		}
		public void Write() {
			try {
				string[] channelNameList = DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.AO, PhysicalChannelAccess.External);
				if (channelNameList.Length > 0) {
					Task task1 = new Task();
					task1.AOChannels.CreateVoltageChannel(channelNameList[0], "Voltage1", 0, 10, AOVoltageUnits.Volts);
                    task1.AOChannels.CreateVoltageChannel(channelNameList[1], "Voltage2", 0, 10, AOVoltageUnits.Volts);
                    task1.AOChannels.CreateVoltageChannel(channelNameList[2], "Voltage3", 0, 10, AOVoltageUnits.Volts);
                    task1.Timing.ConfigureSampleClock("", 100, SampleClockActiveEdge.Rising, SampleQuantityMode.FiniteSamples);
                    task1.Control(TaskAction.Verify);
                    Task task2 = new Task();
                    task2.AOChannels.CreateVoltageChannel(channelNameList[0], "Voltage1", 0, 10, AOVoltageUnits.Volts);
                    task2.AOChannels.CreateVoltageChannel(channelNameList[1], "Voltage2", 0, 10, AOVoltageUnits.Volts);
                    task2.AOChannels.CreateVoltageChannel(channelNameList[2], "Voltage3", 0, 10, AOVoltageUnits.Volts);
                    task2.Timing.ConfigureSampleClock("", 100, SampleClockActiveEdge.Rising, SampleQuantityMode.FiniteSamples);
                    task2.Control(TaskAction.Verify);

                    AnalogMultiChannelWriter aowriter1 = new AnalogMultiChannelWriter(task1.Stream);
                    AnalogMultiChannelWriter aowriter2 = new AnalogMultiChannelWriter(task2.Stream);

                    double[,] wave = new double[3, 1000];
                    for (int i = 0; i < 3; i++)
                    {
                        for (int j = 0; j < 1000; j++)
                        {
                            wave[i,j] = 0.001*(1000-j);
                        }
                    }

                    Console.WriteLine("Task is ready");
                    aowriter1.WriteMultiSample(false, wave);
                    task1.Control(TaskAction.Start);
                    Console.ReadKey();
                    task1.Control(TaskAction.Stop);
                    task1.Control(TaskAction.Unreserve);
                    Console.WriteLine("Task1 is released");
                    aowriter2.WriteMultiSample(false, wave);
                    task1.Control(TaskAction.Start);
                    Console.ReadKey();
                    task1.Control(TaskAction.Stop);
                    task1.Control(TaskAction.Unreserve);
                    Console.ReadKey();
                }
			} catch (DaqException e) {
				Console.Out.WriteLine(e.Message);
			}
		}
		public void Read2() {
			try {
				string[] channelNameList = DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.DILine, PhysicalChannelAccess.External);
				if (channelNameList.Length > 0) {
					Task task = new Task("Digital Input Test");
					task.DIChannels.CreateChannel(channelNameList[0]+":7", "",ChannelLineGrouping.OneChannelForAllLines);
					task.Timing.ConfigureSampleClock("", 10000, SampleClockActiveEdge.Rising, SampleQuantityMode.FiniteSamples);
					task.Control(TaskAction.Verify);

					DigitalSingleChannelReader diread = new DigitalSingleChannelReader(task.Stream);
					DigitalWaveform waveform;
					for (int i = 0; i < repeat; i++) {
						waveform = diread.ReadWaveform(sampleRate);
						foreach(DigitalWaveformSignal signal in waveform.Signals){
							foreach (DigitalState state in signal.States) {
								if (state == DigitalState.ForceDown) {
									Console.Write(0);
								} else if (state == DigitalState.ForceUp) {
									Console.Write(1);
								} else {
									Console.Write("?");
								}
							}
							Console.WriteLine();
						}
						Console.Out.WriteLine("Acquire " + i + "th try");
					}
					StreamWriter writer = new StreamWriter(File.Open("di.txt", FileMode.Create));
					int c = 0;
					foreach (double d in datalist) {
						writer.WriteLine(String.Format("{0} {1}", c, d));
						c++;
					}
					writer.Close();
				}
			} catch (DaqException e) {
				Console.Out.WriteLine(e.Message);
			}
		}
		public void Write2() {
			try {
				string[] channelNameList = DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.DILine, PhysicalChannelAccess.External);
				string[] channelNameList2 = DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.DIPort, PhysicalChannelAccess.External);
				if (channelNameList.Length > 0) {
					Task task = new Task();
					task.DIChannels.CreateChannel(channelNameList[0], "DigitalOut", ChannelLineGrouping.OneChannelForAllLines);
					task.Timing.ConfigureSampleClock("", 10000, SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples);
					task.Control(TaskAction.Verify);
				}
			} catch (DaqException e) {
				Console.Out.WriteLine(e.Message);
			}
		}
	}
}
