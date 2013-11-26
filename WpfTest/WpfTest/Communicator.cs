
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NIDaqInterface;

namespace WpfTest
{
	namespace NIDaq{
		// NIDaqの通信箇所
		public class NIDaqCommunicator {

			private const long defaultSampleRate = (long)2.5e6;

			//シーケンス
			private Sequences seq;

			//タスク管理
			private NIDaqTaskManager taskManager;

			//サンプルレート
			private long sampleRate;

			//コンストラクタ
			public NIDaqCommunicator(NIDaq.Sequences _seq) {
				seq = _seq;
				sampleRate = defaultSampleRate;
				taskManager = NIDaqTaskManager.GetInstance();
			}
			//サンプルレートの設定
			public void changeSampleRate(long _sampleRate) {
				sampleRate = _sampleRate;
			}

			//現在のシーケンスからタスクを生成しキューに入れる
			public void Run() {
				Sequence current = seq.getCurrentSequence();

				current.compile(sampleRate);
				int aochan = current.getEnabledChannelCount();
				long sampleSum = current.getSequenceSampleCount(sampleRate);
				string[] nameList = new string[aochan];
				double[,] waveArray = new double[aochan, sampleSum];
				double[,] minmaxVoltage = new double[aochan, 2];

				int aoCount = 0;
				long sampleCount = 0;
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

				//タスクキューに積んで開始
				try {
					taskManager.popTask(sampleRate, nameList, waveArray, minmaxVoltage);
					taskManager.execute();
				} catch (Exception e) {
					DebugWindow.WriteLine("********** DAQmxエラー **********");
					DebugWindow.WriteLine(e.Message);
					DebugWindow.WriteLine("*********************************");
				}
			}

			//停止
			public void Stop() {
				taskManager.stop();
			}
		}
    }
}
