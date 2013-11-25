using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfTest
{
    // NIDaqの通信箇所
    public class NIDaqCommunicator
    {
        private NIDaq.Sequences seq;
		private long sampleRate;

        public NIDaqCommunicator(NIDaq.Sequences _seq){
            seq = _seq;
            sampleRate = (long)2.5e6;
        }
        public void setSampleRate(long _sampleRate){
            sampleRate=_sampleRate;
        }
        public void Run(){
			NIDaq.Sequence current = seq.getCurrentSequence();
			NIDAQInterface.NIDaqInterface instance = NIDAQInterface.NIDaqInterface.GetInstance();

			current.compile(sampleRate);
			int aochan=0;
			for(int ci=0;ci<current.getChannelCount();ci++){
				if(current.getIsAnalog(ci) && current.getIsOutput(ci) && current.getIsBinded(ci)){
					aochan++;
				}
			}
			long sampleSum=0;
			for (int di = 0; di + 1 < current.getDivisionCount(); di++) {
				sampleSum += current.getDivisionSample(di, sampleRate);
			}

			string[] nameList = new string[aochan];
			double[,] waveArray = new double[aochan, sampleSum];
			double[,] minmaxVoltage = new double[aochan, 2];


			int aoCount=0;
			long sampleCount=0;
			for (int ci = 0; ci < current.getChannelCount(); ci++) {
				if (current.getIsAnalog(ci) && current.getIsOutput(ci) && current.getIsBinded(ci)) {
					sampleCount = 0;
					for (int di = 0; di + 1 < current.getDivisionCount(); di++) {
						nameList[ci] = current.getBindedName(ci);
						minmaxVoltage[ci, 0] = current.getMinVoltage(ci);
						minmaxVoltage[ci, 1] = current.getMaxVoltage(ci);
						double[] channelWave = current.getWave(ci, di);
						for (int j = 0; j < channelWave.Length; j++) {
							waveArray[aoCount, sampleCount] = channelWave[j];
							sampleCount++;
						}						
					}
					aoCount++;
				}
			}
			try {
				instance.popTask(sampleRate, nameList, waveArray,minmaxVoltage);
				instance.execute();
			} catch (Exception e) {
				DebugWindow.WriteLine("********** DAQmxエラー **********");
				DebugWindow.WriteLine(e.Message);
				DebugWindow.WriteLine("*********************************");
			}
		}
        public void Stop(){
			NIDAQInterface.NIDaqInterface instance = NIDAQInterface.NIDaqInterface.GetInstance();
			instance.stop();
		}
    }
}
