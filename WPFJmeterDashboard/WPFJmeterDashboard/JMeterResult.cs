using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JmeterDashboard
{
    public class JMeterResult
    {
        public long timeStamp { get; set; }
        public long elapsed { get; set; }
        public String label { get; set; }
        public String responseCode { get; set; }
        public String threadName { get; set; }
        public long bytes { get; set; }
        public int grpThreads { get; set; }
        public int allThreads { get; set; }
        public int latency { get; set; }
        public int sampleCount { get; set; }
        public int errorCount { get; set; }
        public String hostname { get; set; }
        public String testThreadName { get; set; }
        public int testThread { get; set; }
        public int testIteration { get; set; }

        public JMeterResult() { }

        public JMeterResult(string InputString)
        {
            String[] myArray = InputString.Split(';');
            timeStamp = long.Parse(myArray[0].Trim());
            elapsed = long.Parse(myArray[1].Trim());
            label = myArray[2];
            responseCode = myArray[3];
            threadName = myArray[4]; // please name your threadGroup without "space", example :"S01_RecentTransmission" instead of "S01 RecentTransmission"
            bytes = long.Parse(myArray[5].Trim());
            grpThreads = int.Parse(myArray[6]);
            allThreads = int.Parse(myArray[7]);
            latency = int.Parse(myArray[8].Trim());
            sampleCount = int.Parse(myArray[9].Trim());
            errorCount = int.Parse(myArray[10].Trim());
            hostname = myArray[11];
            String[] myThreadNameArray = myArray[4].Split(' ');
            testThreadName = myThreadNameArray[0];
            String[] myThreadDetailsArray = myThreadNameArray[1].Split('-');
            testThread = int.Parse(myThreadDetailsArray[0].Trim());
            testIteration = int.Parse(myThreadDetailsArray[1].Trim());

        }
    }
}
