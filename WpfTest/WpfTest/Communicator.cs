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
        public DebugWindow debugWindow = null;
        private NIDaq.Sequences seq;
        private int maxAnalogInput;
        private int maxAnalogOutput;
        private int maxDigitalInput;
        private int maxDigitalOutput;
        private double frequency;
        private volatile bool runningFlag;

        public NIDaqCommunicator(NIDaq.Sequences _seq)
        {
            seq = _seq;
            maxAnalogInput = 10;
            maxAnalogOutput = 10;
            maxDigitalInput = 10;
            maxDigitalOutput = 10;
            frequency = 1e6;
            runningFlag = false;
        }

        public void changeFrequence(int freq)
        {
            frequency = freq;
        }

        public void Run()
        {
            runningFlag = true;
            long loopCount;
            Stopwatch sw = new Stopwatch();
            double fps = 1.0 / frequency;
            double currentTime, nextTime, difTime, worstDifTime;
            double analogValue;
            bool digitalValue;

            nextTime = 0;
            difTime = 0;
            loopCount = 0;
            worstDifTime = 0;
            sw.Start();
            while (runningFlag)
            {
                currentTime = (double)sw.ElapsedTicks / Stopwatch.Frequency;
                if (currentTime < nextTime)
                {
                    continue;
                }
                else
                {
                    difTime += Math.Abs(nextTime - currentTime);
                    if (Math.Abs(nextTime - currentTime) > worstDifTime)
                    {
                        worstDifTime = Math.Abs(nextTime - currentTime);
                    }
                    nextTime += fps;
                    loopCount++;
                }

                for (int i = 0; i < maxAnalogOutput; i++)
                {
//                    analogValue = seq.getAnalogValue(i, currentTime);
                    // to daq
                }
                for (int i = 0; i < maxDigitalOutput; i++)
                {
//                    digitalValue = seq.getDigitalValue(i, currentTime);
                    // to daq
                }
                for (int i = 0; i < maxAnalogInput; i++)
                {
                    // from daq
                    analogValue = 0;
//                    seq.setAnalogValue(i, currentTime, analogValue);
                }
                for (int i = 0; i < maxDigitalInput; i++)
                {
                    // from daq
                    digitalValue = false;
//                    seq.setDigitalValue(i, currentTime, digitalValue);
                }
            }
            sw.Stop();

            if (debugWindow != null)
            {
                debugWindow.WriteLineAsyc(String.Format("Communicator thread stops"));
                debugWindow.WriteLineAsyc(String.Format("*** Running Result ***"));
                debugWindow.WriteLineAsyc(String.Format(" Highresolution timer = {0}", Stopwatch.IsHighResolution));
                debugWindow.WriteLineAsyc(String.Format(" Running Time = {0} sec", sw.ElapsedMilliseconds * 1e-3));
                debugWindow.WriteLineAsyc(String.Format(" I/O Ideal Update Count = {0}", sw.ElapsedMilliseconds * 1e-3 * frequency));
                debugWindow.WriteLineAsyc(String.Format(" I/O Update Count = {0}", loopCount));
                debugWindow.WriteLineAsyc(String.Format(" Ideal Update Frequency = {0} Hz", frequency));
                debugWindow.WriteLineAsyc(String.Format(" Update Frequency = {0} Hz", (double)1e3 * loopCount / sw.ElapsedMilliseconds));
                debugWindow.WriteLineAsyc(String.Format(" I/O Average Precision = {0} sec", difTime / loopCount));
                debugWindow.WriteLineAsyc(String.Format(" I/O Worst Precision = {0} sec", worstDifTime));
                debugWindow.WriteLineAsyc(String.Format("***"));
            }
        }
        public void Stop()
        {
            runningFlag = false;
        }
    }
}
