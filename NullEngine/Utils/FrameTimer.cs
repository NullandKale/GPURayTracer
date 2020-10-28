using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace NullEngine.Utils
{
    class FrameTimer
    {
        public double lastFrameTimeMS;
        public double lastFrameUpdateRate;
        public double averageUpdateRate;

        private Stopwatch timer;
        private double updateTimeCounterS;
        private double updateCount;

        public FrameTimer()
        {
            timer = new Stopwatch();
        }

        public void startUpdate()
        {
            timer.Restart();
        }

        public void endUpdate()
        {
            timer.Stop();

            lastFrameTimeMS = timer.Elapsed.TotalMilliseconds;
            lastFrameUpdateRate = 1.0 / timer.Elapsed.TotalSeconds;
            updateTimeCounterS += timer.Elapsed.TotalSeconds;
            updateCount++;
            averageUpdateRate = updateCount / updateTimeCounterS;
        }

        public double endUpdateForTargetUpdateTime(double targetUpdateTimeMS, bool sleep)
        {
            endUpdate();

            double sleepTime = targetUpdateTimeMS - lastFrameTimeMS;

            if (sleep && sleepTime >= 0)
            {
                Thread.Sleep((int)sleepTime);
                updateTimeCounterS += (sleepTime / 1000.0);
                averageUpdateRate = updateCount / updateTimeCounterS;

            }

            return sleepTime;
        }
    }
}
