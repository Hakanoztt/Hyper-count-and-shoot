using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge
{
    public class SimpleStopwatch
    {
        private System.TimeSpan timeSpan;
        private System.Diagnostics.Stopwatch timer;

        public SimpleStopwatch()
        {
            timer = new System.Diagnostics.Stopwatch();
            timer.Start();
        }

        private void Stop()
        {
            timer.Stop();
            timeSpan = timer.Elapsed;
        }

        public void Reset()
        {
            timer.Reset();
            timeSpan = timer.Elapsed;
        }

        public string ReportMeasurement()
        {
            Stop();
            string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds,
                timeSpan.Milliseconds / 10);
            return elapsedTime;
        }
    }
}