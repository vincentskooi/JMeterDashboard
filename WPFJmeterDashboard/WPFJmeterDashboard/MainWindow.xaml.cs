using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.DataSources; // EnumerableDataSource
using Microsoft.Research.DynamicDataDisplay.PointMarkers;
using System.IO;
using System.Collections.Concurrent;
using System.Threading; // CirclePointMarker
using tTimer = System.Timers.Timer;
using System.Timers;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Controls.DataVisualization.Charting;
using System.Windows.Threading;
using System.Collections;
using System.Configuration;

namespace JmeterDashboard
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const String TPS = "TPSSeries";
        const String VU = "VUSeries";
        const String RST = "RSTSeries";
        const String ER = "ERSeries";
        const String FS = "FSSeries";
        const String OVERVIEW_VU = "OVERVIEW_VU";
        const String ALL_TPS = "ALLTPSSeries";
        const String ALL_VU = "ALLVUSeries";
        const String ALL_RST = "ALLRSTSeries";
        const String ALL_ER = "ALLERSeries";
        const String ALL_FS = "ALLFSSeries";

        public ConcurrentQueue<JMeterResult> JmeterList = new ConcurrentQueue<JMeterResult>();
        public ConcurrentQueue<JMeterResult> JmeterResultList = new ConcurrentQueue<JMeterResult>();
        
        private double myOverallStartTime = 0; // this is the start time, it is the timestamp when you started the test.
        public int timerInterval = int.Parse(ConfigurationManager.AppSettings["timerInterval"]); //Timer Interval for collecting data.
        public int GraphTimeRange = int.Parse(ConfigurationManager.AppSettings["ChartTimeRange"]); // X Width - so far 180seconds (3 mins) works fine
        public double currentChartHeight = int.Parse(ConfigurationManager.AppSettings["ChartHeight"]); // Y Height
        public int iWidthCount = 0;
        public DateTime pstStartDatetime = new DateTime();
        TimeZoneInfo pstZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");

        public EnumerableDataSource<int> timeXDataSource;
        public EnumerableDataSource<int> TPSYDataSource;
        public EnumerableDataSource<int> VUYDataSource;
        public ObservableDataSource<Point> TPSyObservableDS = null;
        public ObservableDataSource<Point> VUyObservableDS = null;

        ObservableCollection<KeyValuePair<int, int>> TPSKeyValueOC = new ObservableCollection<KeyValuePair<int, int>>();
        ObservableCollection<KeyValuePair<int, int>> VUKeyValueOC = new ObservableCollection<KeyValuePair<int, int>>();
        ObservableCollection<KeyValuePair<int, int>> ResponseTimeKeyValueOC = new ObservableCollection<KeyValuePair<int, int>>();
        ObservableCollection<KeyValuePair<int, int>> ErrorKeyValueOC = new ObservableCollection<KeyValuePair<int, int>>();
        ObservableCollection<KeyValuePair<int, int>> FilesizeKeyValueOC = new ObservableCollection<KeyValuePair<int, int>>();

        ObservableCollection<KeyValuePair<int, int>> groupTPSKeyValueOC = new ObservableCollection<KeyValuePair<int, int>>();
        ObservableCollection<KeyValuePair<int, int>> groupVUKeyValueOC = new ObservableCollection<KeyValuePair<int, int>>();
        ObservableCollection<KeyValuePair<int, int>> groupResponseTimeKeyValueOC = new ObservableCollection<KeyValuePair<int, int>>();
        ObservableCollection<KeyValuePair<int, int>> groupErrorKeyValueOC = new ObservableCollection<KeyValuePair<int, int>>();
        ObservableCollection<KeyValuePair<int, int>> groupFileSizeKeyValueOC = new ObservableCollection<KeyValuePair<int, int>>();

        ConcurrentDictionary<String, ObservableCollection<KeyValuePair<int, int>>> ConcurrentVUCD = new ConcurrentDictionary<string, ObservableCollection<KeyValuePair<int, int>>>();

        // these variables keep the Min Timestamp that is displayed in the graph, so it filters out all unwanted records for calculation.
        public int TPSCurrentCount = 0;
        public int VUCurrentCount = 0;
        public int RSTCurrentCount = 0;
        public int ErrorCurrentCount = 0;
        public int ConcurrentVUCount = 0;
        public int FileSizeCurrentCount = 0;

        // Get txtFileToMonitor 
        delegate string GetText();
        string GetTxtFileToMonitor()
        {
            return txtFileToMonitor.Text;
        }

        public MainWindow()
        {
            InitializeComponent();
           // Loaded += new RoutedEventHandler(Window1_Loaded);
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            Environment.Exit(Environment.ExitCode);
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // ... Get TabControl reference.
            var item = sender as TabControl;
            // ... Set Title to selected tab header.
            var selected = item.SelectedItem as TabItem;
            this.Title = "Mac-Monitor : " + selected.Header.ToString();
        }

        private void toolBarBtn_ClearCharts_Click(object sender, RoutedEventArgs e)
        {

            clearChart_AllRealtime_button();
            clearChart_AllResult_button();

            //removeAllCharts();
        }

        public void removeAllCharts()
        {
            int count = lineChart.Series.Count();
            for (int i = 0; i < count; i++)
            {   
                lineChart.Series.RemoveAt(0);
            }
        }

        public void clearChart_AllRealtime_button()
        {
            // Reset all color
            btnResponseTimeLeft.Background = Brushes.WhiteSmoke;
            btnResponseTimeLeft.Foreground = Brushes.Black;
            btnResponseTimeRight.Background = Brushes.WhiteSmoke;
            btnResponseTimeRight.Foreground = Brushes.Black;
            btnVirtualUserLeft.Background = Brushes.WhiteSmoke;
            btnVirtualUserLeft.Foreground = Brushes.Black;
            btnVirtualUserRight.Background = Brushes.WhiteSmoke;
            btnVirtualUserRight.Foreground = Brushes.Black;
            btnTxnPSLeft.Background = Brushes.WhiteSmoke;
            btnTxnPSLeft.Foreground = Brushes.Black;
            btnTxnPSRight.Background = Brushes.WhiteSmoke;
            btnTxnPSRight.Foreground = Brushes.Black;
            btnErrorCountLeft.Background = Brushes.WhiteSmoke;
            btnErrorCountLeft.Foreground = Brushes.Black;
            btnSizeCountLeft.Background = Brushes.WhiteSmoke;
            btnSizeCountLeft.Foreground = Brushes.Black;

            RemoveSeries(TPS);
            RemoveSeries(VU);
            RemoveSeries(RST);
            RemoveSeries(ER);
            RemoveSeries(FS);
            RemoveSeries(OVERVIEW_VU);

        }

        public void clearChart_AllResult_button()
        {
            //All
            btnAllTPSLeft.Background = Brushes.WhiteSmoke;
            btnAllTPSLeft.Foreground = Brushes.Black;
            btnAllVULeft.Background = Brushes.WhiteSmoke;
            btnAllVULeft.Foreground = Brushes.Black;
            btnAllResponseTimeLeft.Background = Brushes.WhiteSmoke;
            btnAllResponseTimeLeft.Foreground = Brushes.Black;
            btnAllErrorLeft.Background = Brushes.WhiteSmoke;
            btnAllErrorLeft.Foreground = Brushes.Black;
            btnAllFileSizeLeft.Background = Brushes.WhiteSmoke;
            btnAllFileSizeLeft.Foreground = Brushes.Black;

            // reset default values;
            myCustomCurrentName = "";
            myCustomCurrentChartHeight = 50;

            RemoveSeries(ALL_TPS);
            RemoveSeries(ALL_VU);
            RemoveSeries(ALL_RST);
            RemoveSeries(ALL_ER);
            RemoveSeries(ALL_FS);

            //int count = lineChart.Series.Count();
            //for (int i = 0; i < count; i++)
            //{
            //    lineChart.Series.RemoveAt(0);
            //}
        }

       
        // ******************************
        // Description: Starting all the listeners threads
        // 
        // ******************************
        private void btnStartListener_Click(object sender, RoutedEventArgs e)
        {
            if ((txtFileToMonitor.Text.Count() < 0) || (!File.Exists(txtFileToMonitor.Text)))
            {
                MessageBox.Show("File to monitor is not found", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Thread threadInitialization = new Thread(new ThreadStart(InitializationStart));
            threadInitialization.Start();

            DispatcherTimer TPStimer = new DispatcherTimer();
            TPStimer.Interval = new TimeSpan(0, 0, timerInterval);
            TPStimer.Tick += new EventHandler(myTxnPS_Elapsed);
            TPStimer.IsEnabled = true;

            DispatcherTimer VUTimer = new DispatcherTimer();
            VUTimer.Interval = new TimeSpan(0, 0, timerInterval);
            VUTimer.Tick += new EventHandler(myVU_Elapsed);
            VUTimer.IsEnabled = true;

            DispatcherTimer RSTTimer = new DispatcherTimer();
            RSTTimer.Interval = new TimeSpan(0, 0, timerInterval);
            RSTTimer.Tick += new EventHandler(myRST_Elapsed);
            RSTTimer.IsEnabled = true;

            //DispatcherTimer ConcurrentVUTimer = new DispatcherTimer();
            //ConcurrentVUTimer.Interval = new TimeSpan(0, 0, timerInterval);  
            //ConcurrentVUTimer.Tick += new EventHandler(myConcurrentVU_Elapsed);
            //ConcurrentVUTimer.IsEnabled = true;

            DispatcherTimer ErrorTimer = new DispatcherTimer();
            ErrorTimer.Interval = new TimeSpan(0, 0, timerInterval);
            ErrorTimer.Tick += new EventHandler(myError_Elapsed);
            ErrorTimer.IsEnabled = true;

            DispatcherTimer FilesizeTimer = new DispatcherTimer();
            FilesizeTimer.Interval = new TimeSpan(0, 0, timerInterval);
            FilesizeTimer.Tick += new EventHandler(myFilesize_Elapsed);
            FilesizeTimer.IsEnabled = true;


            DispatcherTimer GraphCleanupTimer = new DispatcherTimer();
            GraphCleanupTimer.Interval = new TimeSpan(0, 0, timerInterval);
            GraphCleanupTimer.Tick += new EventHandler(myCleanup_Elapsed);
            GraphCleanupTimer.IsEnabled = true;

            //DispatcherTimer ChartAutoWidthTimer = new DispatcherTimer();
            //ChartAutoWidthTimer.Interval = new TimeSpan(0, 0, 10);  // per 10 seconds, you could change it
            //ChartAutoWidthTimer.Tick += new EventHandler(myChartAutoWidth_Elapsed);
            //ChartAutoWidthTimer.IsEnabled = true;                       

            DispatcherTimer SummaryTimer = new DispatcherTimer();
            SummaryTimer.Interval = new TimeSpan(0, 0, 10);
            SummaryTimer.Tick += new EventHandler(mySummary_Elapsed);
            SummaryTimer.IsEnabled = true;


            DispatcherTimer AllResultTimer = new DispatcherTimer();
            AllResultTimer.Interval = new TimeSpan(0, 0, 60);
            AllResultTimer.Tick += new EventHandler(myAllResult_Elapse);
            AllResultTimer.IsEnabled = true;

            TextBlockStatus.Text = "Reading to monitor";
            btnStartListener.IsEnabled = false;
        }

        // ******************************
        // Description: main thread Initialization
        // 
        // ******************************
        public void InitializationStart()
        {   
            var producerWorker = Task.Factory.StartNew(() => RunProducer());
            Task.WaitAll(producerWorker);
        }

        // ******************************
        // Description: this is the main thread that reads the jtl file and put everything in the collection.
        // Log file format with column: timeStamp;elapsed;label;responseCode;threadName;bytes;grpThreads;allThreads;Latency;SampleCount;ErrorCount;Hostname
        // ******************************
        private void RunProducer()
        {
            String myFileToMonitor = (String) txtFileToMonitor.Dispatcher.Invoke(new GetText(GetTxtFileToMonitor));
            Console.WriteLine(myFileToMonitor);

            using (var fs = new FileStream(myFileToMonitor, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)) // this allows you to share the file wil JMeter continues to write to it
            using (var reader = new StreamReader(fs))
            {
                reader.ReadLine(); // skip the first line
                while (true)
                {
                    var line = reader.ReadLine();

                    if (!String.IsNullOrWhiteSpace(line))
                    {  
                        JMeterResult JmeterItem = new JMeterResult(line);
                        if (JmeterList.Count < 1) // marked the startTime.
                        {
                            myOverallStartTime = Math.Round(Convert.ToDouble(JmeterItem.timeStamp / 1000));
                            
                            DateTime myDateTime = FromUnixTime(long.Parse(myOverallStartTime.ToString()));
                            pstStartDatetime = TimeZoneInfo.ConvertTimeFromUtc(myDateTime, pstZone);
                            Console.WriteLine("StartDate = " + pstStartDatetime.ToString());
                        }
                        // Add to JmeterList
                        JmeterList.Enqueue(JmeterItem);
                    }
                }
            }
        }
        
        #region all timer processes
      
        // ******************************
        // Description: Not used for now, to populate the UIControl.
        // 
        // ******************************
        private void myExample_Elapsed(object source, ElapsedEventArgs e)
        {
            TimeZoneInfo pstZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
            DateTime myDateTime = FromUnixTime(long.Parse(myOverallStartTime.ToString()));
            DateTime pstDateTime = TimeZoneInfo.ConvertTimeFromUtc(myDateTime, pstZone);
            Console.WriteLine("StartDate = " + pstDateTime.ToString());

            listBoxSummaryResult.Dispatcher.Invoke((Action)(() => listBoxSummaryResult.Items.Add("Test Start Time = " + pstDateTime.ToString())));


            //long myLatestCount = JmeterList.Count();

            //if (myPreviousCount != myLatestCount)
            //{
            //    myPreviousCount = myLatestCount;
            //    Console.WriteLine("New Reading + " + myLatestCount);
            //    listBoxSummaryResult.Dispatcher.Invoke((Action)(() => listBoxSummaryResult.Items.Add("New Reading + " + myLatestCount)));

            //    var myAvgList = from p in JmeterList
            //                    group p by new { p.label, p.testThreadName } into g
            //                    select new { label = g.Key.label, testThreadName = g.Key.testThreadName, elapseAvg = g.Average(p => p.elapsed) };

            //    foreach (var myElapse in myAvgList)
            //    {
            //        listBoxSummaryResult.Dispatcher.Invoke((Action)(() => listBoxSummaryResult.Items.Add("Elapsed Everage = " + myElapse.label + ":" + myElapse.testThreadName + ";" + myElapse.elapseAvg)));
            //    }
            //}
            //else
            //{
            //    listBoxSummaryResult.Dispatcher.Invoke((Action)(() => listBoxSummaryResult.Items.Add("No new readings")));
            //}
        }
        
        // ******************************
        // Description: Not used for now, was originally used for automatically set the "X axis".
        // 
        // ******************************
        void myChartAutoWidth_Elapsed(object sender, EventArgs e)
        {
            if (iWidthCount == 0)
            {
                iWidthCount = TPSKeyValueOC.Count();
            }
            if (TPSKeyValueOC.Count() > 10)
            {
                if (iWidthCount != TPSKeyValueOC.Count())
                {
                    lineChart.Width = 80 * TPSKeyValueOC.Count();
                    ChartScrollViewer.ScrollToRightEnd();
                    iWidthCount = TPSKeyValueOC.Count();
                }
            }

            //else
            //    lineChart.Width = this.Width - 80;
        }

        // ******************************
        // Description: Time Process to calculate Summary
        // 
        // ******************************

        void myAllResult_Elapse(object sender, EventArgs e)
        {
            var myNewJmeterList = from u in JmeterList
                                  orderby u.timeStamp
                                  select new { newTimeStamp = Convert.ToInt32((Math.Round(Convert.ToDouble(u.timeStamp) / 1000)) - myOverallStartTime), u.testThreadName, u.allThreads, u.elapsed, u.errorCount, u.responseCode, u.bytes };

            #region Calculate All Metrics

            var myMaxTimeStamp = myNewJmeterList.Max(p => p.newTimeStamp);
            int myInterval = myMaxTimeStamp / 50;

            var ranges = Enumerable.Range(0, myMaxTimeStamp).Where(n => (n % myInterval) == 0).ToList();


            //// All Response time & TPS
            var AllResponseTimeGrouped = myNewJmeterList.GroupBy(x => ranges.FirstOrDefault(r => r > x.newTimeStamp))
                                        .Select(g => new { Size = g.Key, Count = g.Count(), Sum = g.Sum(p => p.elapsed), Average = Math.Round(g.Average(p => p.elapsed), 2) })
                                        .ToList();

            groupResponseTimeKeyValueOC.Clear();
            groupTPSKeyValueOC.Clear();
            foreach (var group in AllResponseTimeGrouped)
            {
                groupResponseTimeKeyValueOC.Add(new KeyValuePair<int, int>(group.Size, (int)Math.Round(group.Average)));
                groupTPSKeyValueOC.Add(new KeyValuePair<int, int>(group.Size, group.Count));
            }

            //All Virtual Users
            var AllVirtualUserGrouped = myNewJmeterList.GroupBy(x => ranges.FirstOrDefault(r => r > x.newTimeStamp))
                                        .Select(g => new { Size = g.Key, Average = Math.Round(g.Average(p => p.allThreads), 2) })
                                        .ToList();
            groupVUKeyValueOC.Clear();
            foreach (var group in AllVirtualUserGrouped)
            {
                groupVUKeyValueOC.Add(new KeyValuePair<int, int>(group.Size, (int)Math.Round(group.Average)));
            }

            //All Errors
            var myErrorJmeterList = from u in JmeterList
                                    where u.errorCount > 0
                                    orderby u.timeStamp
                                    select new { newTimeStamp = Convert.ToInt32((Math.Round(Convert.ToDouble(u.timeStamp) / 1000)) - myOverallStartTime), u.errorCount, u.responseCode };

            var AllErrorGrouped = myErrorJmeterList.GroupBy(x => ranges.FirstOrDefault(r => r > x.newTimeStamp))
                                  .Select(g => new { Size = g.Key, Count = g.Count() })
                                  .ToList();
            groupErrorKeyValueOC.Clear();
            foreach (var group in AllErrorGrouped)
            {
                groupErrorKeyValueOC.Add(new KeyValuePair<int, int>(group.Size, group.Count));
            }


            // All Size downloaded
            var AllFileSizeGrouped = myNewJmeterList.GroupBy(x => ranges.FirstOrDefault(r => r > x.newTimeStamp))
                                    .Select(g => new { Size = g.Key, Sum = int.Parse((g.Sum(p => p.bytes) / 1000000).ToString()) })
                                    .ToList();
            groupFileSizeKeyValueOC.Clear();
            foreach (var group in AllFileSizeGrouped)
            {
                groupFileSizeKeyValueOC.Add(new KeyValuePair<int, int>(group.Size, group.Sum));
            }

            #endregion
        }

        void mySummary_Elapsed(object sender, EventArgs e)
        {

            listBoxSummaryResult.Dispatcher.Invoke((Action)(() => listBoxSummaryResult.Items.Clear()));

            var myNewJmeterList = from u in JmeterList
                                  orderby u.timeStamp
                                  select new { newTimeStamp = Convert.ToInt32((Math.Round(Convert.ToDouble(u.timeStamp) / 1000)) - myOverallStartTime), u.testThreadName, u.allThreads, u.elapsed, u.errorCount, u.responseCode, u.bytes };

          
            #region Test Summary

            // Test Start Time.
            listBoxSummaryResult.Dispatcher.Invoke((Action)(() => listBoxSummaryResult.Items.Add("Test Start Time = " + pstStartDatetime.ToString())));

            // Test Duration
            var myMaxTime = JmeterList.Max(p => p.timeStamp) / 1000;
            DateTime currentMaxDateTime = new DateTime();
            currentMaxDateTime = TimeZoneInfo.ConvertTimeFromUtc(FromUnixTime(long.Parse(myMaxTime.ToString())), pstZone);
            TimeSpan myTimeSpan = currentMaxDateTime - pstStartDatetime;
            listBoxSummaryResult.Dispatcher.Invoke((Action)(() => listBoxSummaryResult.Items.Add("Test Duration= " + myTimeSpan.Days + " Days, " + myTimeSpan.Hours + " Hours, " + myTimeSpan.Minutes + " Minutes, " + myTimeSpan.Seconds + " Seconds")));

            // Transaction Count
            int myCount = JmeterList.Count;
            listBoxSummaryResult.Dispatcher.Invoke((Action)(() => listBoxSummaryResult.Items.Add("Transaction Count= " + myCount)));

            // Average TPS
            //var myNewJmeterList = from u in JmeterList
            //                      orderby u.timeStamp
            //                      select new { newTimeStamp = Convert.ToInt32((Math.Round(Convert.ToDouble(u.timeStamp) / 1000)) - myOverallStartTime), u.testThreadName, u.allThreads u.elapsed };

            var myTPSGroup = from u in myNewJmeterList
                             group u by new { u.newTimeStamp } into g
                             select new { Sec = g.Key.newTimeStamp, Count = g.Count() };

            double myTPS = myTPSGroup.Average(p => p.Count);
            listBoxSummaryResult.Dispatcher.Invoke((Action)(() => listBoxSummaryResult.Items.Add("Average TPS= " + Math.Round(myTPS, 2))));

            // Average Response Time
            var myAverageResponseTime = JmeterList.Where(p => p.responseCode == "200").Average(p => p.elapsed);
            listBoxSummaryResult.Dispatcher.Invoke((Action)(() => listBoxSummaryResult.Items.Add("Average Response Time= " + Math.Round(myAverageResponseTime, 2) + " ms")));

            // Median Response Time
            var sortedElapse = from u in JmeterList
                               where u.responseCode == "200"
                               orderby u.elapsed
                               select u.elapsed;
            int halfIndex = myCount / 2;

            double myMedianResponseTime;
            if ((myCount % 2) == 0)
            {
                myMedianResponseTime = ((sortedElapse.ElementAt(halfIndex) + sortedElapse.ElementAt((halfIndex - 1))) / 2);
            }
            else
            {
                myMedianResponseTime = sortedElapse.ElementAt(halfIndex);
            }

            listBoxSummaryResult.Dispatcher.Invoke((Action)(() => listBoxSummaryResult.Items.Add("Median Response Time= " + myMedianResponseTime + " ms")));

            // 90th Percentile
            double my90thpercentile = Percentile(sortedElapse.ToList(), 0.9);
            listBoxSummaryResult.Dispatcher.Invoke((Action)(() => listBoxSummaryResult.Items.Add("90% Response Time= " + Math.Round(my90thpercentile, 2) + " ms")));

            // Min Response Time
            var myMinResponseTime = JmeterList.Where(p => p.responseCode == "200").Min(p => p.elapsed);
            listBoxSummaryResult.Dispatcher.Invoke((Action)(() => listBoxSummaryResult.Items.Add("Min Response Time= " + myMinResponseTime + " ms")));

            // Max Response Time
            var myMaxResponseTime = JmeterList.Where(p => p.responseCode == "200").Max(p => p.elapsed);
            listBoxSummaryResult.Dispatcher.Invoke((Action)(() => listBoxSummaryResult.Items.Add("Max Response Time= " + myMaxResponseTime + " ms")));

            // Average / Max Virtual Users
            var myVUDistinctGroup = from u in myNewJmeterList
                                    group u by new { u.newTimeStamp, u.testThreadName } into g
                                    select new { Sec = g.Key.newTimeStamp, Max = g.Max(x => x.allThreads) }; // get the max from all the timestamp + testthread

            var myVUMaxGroup = from u in myVUDistinctGroup
                               group u by new { u.Sec } into g
                               select new { Sec = g.Key.Sec, Max = g.Max(x => x.Max) }; // get the max from each seconds

            listBoxSummaryResult.Dispatcher.Invoke((Action)(() => listBoxSummaryResult.Items.Add("Average Virtual Users= " + Math.Round(myVUMaxGroup.Average(p => p.Max), 2))));
            listBoxSummaryResult.Dispatcher.Invoke((Action)(() => listBoxSummaryResult.Items.Add("Max Virtual Users= " + myVUMaxGroup.Max(p => p.Max))));

            // HTTP Codes Presence
            //var myHTTPCodes = JmeterList.GroupBy(p => p.responseCode).Select(g => g.First()).ToList();
            var myHTTPCodes = (from u in JmeterList
                               select u.responseCode).Distinct();


            String httpCode = "";
            foreach (var myhttpcode in myHTTPCodes)
                httpCode = httpCode + ", " + myhttpcode;
            listBoxSummaryResult.Dispatcher.Invoke((Action)(() => listBoxSummaryResult.Items.Add("Http Code Presence= " + httpCode)));
            #endregion

            #region http responsecode distribution
            var myResponseCodeList = from u in JmeterList
                                     group u by new { u.responseCode } into g
                                     select new { responseCode = g.Key.responseCode, Count = g.Count(), Share = Math.Round((((double)g.Count() / JmeterList.Count)*100),2) };

            //var myData = from u in myResponseCodeList
            //             select new { responseCode = u.responseCode, Count = u.Count, Share = (double)u.Count / 99 };
        
            listViewHTTPResponseCodeResult.Dispatcher.Invoke((Action)(() => listViewHTTPResponseCodeResult.ItemsSource = myResponseCodeList));

            #endregion

            #region threadgroup distribution
            var myThreadlist = from u in JmeterList
                               where u.responseCode == "200"
                               select new { u.label, u.testThreadName, u.elapsed };
            var myThreadGroupDistribution = from u in myThreadlist
                                            group u by new { u.testThreadName, u.label } into g
                                            select new { threadName = g.Key.testThreadName, threadTxnController = g.Key.label, SampleCount = g.Count(), AvgRST = Math.Round(g.Average(p => p.elapsed), 2), MinRST = g.Min(p => p.elapsed), MaxRST = g.Max(p => p.elapsed) };

            var SortedThreadGroupDistribution = myThreadGroupDistribution.OrderBy(p => p.threadName);

            listViewThreadGroupResult.Dispatcher.Invoke((Action)(() => listViewThreadGroupResult.ItemsSource = SortedThreadGroupDistribution));
            #endregion

           

        }
      
        // ******************************
        // Description: This is to cleanup all the ObservableCollection to reduce the size of the collection to the specified GraphTimeRange (X axis)
        // 
        // ******************************
        void myCleanup_Elapsed(object sender, EventArgs e)
        {
            double myMax = (JmeterList.Last().timeStamp / 1000) - myOverallStartTime;
            int myResult = Convert.ToInt32(Math.Round(myMax)) - GraphTimeRange;

            for (int i = 0; i < TPSKeyValueOC.Count; i++)
            {
                if (TPSKeyValueOC[i].Key < myResult)
                {
                    TPSKeyValueOC.RemoveAt(i);
                }
            }

            for (int i = 0; i < VUKeyValueOC.Count; i++)
            {
                if (VUKeyValueOC[i].Key < myResult)
                {
                    VUKeyValueOC.RemoveAt(i);
                }
            }

            for (int i = 0; i < ResponseTimeKeyValueOC.Count; i++)
            {
                if (ResponseTimeKeyValueOC[i].Key < myResult)
                {
                    ResponseTimeKeyValueOC.RemoveAt(i);
                }
            }
                      
            for (int i = 0; i < ErrorKeyValueOC.Count; i++)
            {
                if (ErrorKeyValueOC[i].Key < myResult)
                {
                    ErrorKeyValueOC.RemoveAt(i);
                }
            }

            for (int i = 0; i < FilesizeKeyValueOC.Count; i++)
            {
                if (FilesizeKeyValueOC[i].Key < myResult)
                {
                    FilesizeKeyValueOC.RemoveAt(i);
                }
            }
          
        }

        // ******************************
        // Description: Timer Process to calculate Transaction per Seconds
        // 
        // ******************************
        void myTxnPS_Elapsed(object sender, EventArgs e)
        {
          
            var myStartTime = myOverallStartTime;
          
            var myNewJmeterList = from u in JmeterList
                                  orderby u.timeStamp
                                  select new { newTimeStamp = Convert.ToInt32((Math.Round(Convert.ToDouble(u.timeStamp) / 1000)) - myStartTime), u.testThreadName };

            TPSCurrentCount = myNewJmeterList.Max(p => p.newTimeStamp) - GraphTimeRange;

            var myTPSGroup = from u in myNewJmeterList
                             where u.newTimeStamp > TPSCurrentCount
                             group u by new { u.newTimeStamp } into g
                             select new { Sec = g.Key.newTimeStamp, Count = g.Count() };

            foreach (var TPS in myTPSGroup)
            {
                var s = from u in TPSKeyValueOC
                        where u.Key == TPS.Sec
                        select u;

                if (s.Count() > 0)
                {
                    foreach (var item in s.ToList())
                    {
                        if (item.Value != TPS.Count)
                        {
                            TPSKeyValueOC.Remove(item);
                            TPSKeyValueOC.Add(new KeyValuePair<int, int>(TPS.Sec, TPS.Count));
                        }
                    }
                }
                else
                {
                    TPSKeyValueOC.Add(new KeyValuePair<int, int>(TPS.Sec, TPS.Count));
                }

                //foreach (var s in TPSKeyValueOC.Where(p => p.Key == TPS.Sec).ToList())
                //{
                //    TPSKeyValueOC.Remove(s);
                //}
                //TPSKeyValueOC.Add(new KeyValuePair<int, int>(TPS.Sec, TPS.Count));
            }
        }

        // ******************************
        // Description: Timer Process to calculate Virtual User per Seconds
        // 
        // ******************************
        void myVU_Elapsed(object sender, EventArgs e)
        {
            var myStartTime = myOverallStartTime;
            
            var myNewJmeterList = from u in JmeterList
                                  orderby u.timeStamp
                                  select new { newTimeStamp = Convert.ToInt32((Math.Round(Convert.ToDouble(u.timeStamp) / 1000)) - myStartTime), u.testThreadName, u.allThreads };

            VUCurrentCount = myNewJmeterList.Max(p => p.newTimeStamp) - GraphTimeRange; 

            var myVUDistinctGroup = from u in myNewJmeterList
                                    where u.newTimeStamp > VUCurrentCount
                                    group u by new { u.newTimeStamp, u.testThreadName } into g
                                    select new { Sec = g.Key.newTimeStamp, Max = g.Max(x => x.allThreads) };
          
            var myVUSumGroup = from u in myVUDistinctGroup
                               group u by new { u.Sec } into g
                               select new { Sec = g.Key.Sec, Max = g.Max(x => x.Max) };

            
            foreach (var VU in myVUSumGroup)
            {
                var s = from u in VUKeyValueOC
                        where u.Key == VU.Sec
                        select u;

                if (s.Count() > 0)
                {
                    foreach (var item in s.ToList())
                    {
                        if (item.Value != VU.Max)
                        {
                            VUKeyValueOC.Remove(item);
                            VUKeyValueOC.Add(new KeyValuePair<int, int>(VU.Sec, VU.Max));
                        }
                    }
                }
                else
                {
                    VUKeyValueOC.Add(new KeyValuePair<int, int>(VU.Sec, VU.Max));
                }


                //foreach (var s in VUKeyValueOC.Where(p => p.Key == VU.Sec).ToList())
                //{
                //    VUKeyValueOC.Remove(s);
                //}
                //VUKeyValueOC.Add(new KeyValuePair<int, int>(VU.Sec, VU.Max));
            }
        }

        // ******************************
        // Description: Timer Process to calculate ResponseTime per Seconds
        // 
        // ******************************
        void myRST_Elapsed(object sender, EventArgs e)
        {
            var myStartTime = myOverallStartTime;

            var myNewJmeterList = from u in JmeterList
                                  orderby u.timeStamp
                                  select new { newTimeStamp = Convert.ToInt32((Math.Round(Convert.ToDouble(u.timeStamp) / 1000)) - myStartTime), u.elapsed };

            RSTCurrentCount = myNewJmeterList.Max(p => p.newTimeStamp) - GraphTimeRange; // always refresh the whole graphs values... has an impact on performance.

            var myRSTGroup = from u in myNewJmeterList
                             where u.newTimeStamp > RSTCurrentCount
                             group u by new { u.newTimeStamp } into g
                             select new { Sec = g.Key.newTimeStamp, Average = g.Average(p => p.elapsed) / 1000 };

            foreach (var RST in myRSTGroup)
            {
                var s = from u in ResponseTimeKeyValueOC
                        where u.Key == RST.Sec
                        select u;

                if (s.Count() > 0)
                {
                    foreach (var item in s.ToList())
                    {
                        if (item.Value != RST.Average)
                        {
                            ResponseTimeKeyValueOC.Remove(item);
                            ResponseTimeKeyValueOC.Add(new KeyValuePair<int, int>(RST.Sec, (int)Math.Round(RST.Average)));
                        }
                    }
                }
                else
                {
                    ResponseTimeKeyValueOC.Add(new KeyValuePair<int, int>(RST.Sec, (int)Math.Round(RST.Average)));
                }

            }
        }

        //void myRST_Elapsed(object sender, EventArgs e)
        //{
        //    var myStartTime = myOverallStartTime;

        //    var myNewJmeterList = from u in JmeterList
        //                          orderby u.timeStamp
        //                          select new { newTimeStamp = Convert.ToInt32((Math.Round(Convert.ToDouble(u.timeStamp) / 1000)) - myStartTime), u.elapsed };

        //    #region Calculate All
        //    var myMaxTimeStamp = myNewJmeterList.Max(p => p.newTimeStamp);
        //    int myInterval = myMaxTimeStamp / 50;

        //    var ranges = Enumerable.Range(0, myMaxTimeStamp).Where(n => (n % myInterval) == 0).ToList();
            
        //    var grouped = myNewJmeterList.GroupBy(x => ranges.FirstOrDefault(r => r > x.newTimeStamp))
        //                  .Select(g => new { Size = g.Key, Count = g.Count(), Sum = g.Sum(p => p.elapsed), Average = Math.Round(g.Average(p=>p.elapsed),2) })
        //                  .ToList();

        //    groupResponseTimeKeyValueOC.Clear();
        //    foreach (var group in grouped)
        //    {
        //        groupResponseTimeKeyValueOC.Add(new KeyValuePair<int, int>(group.Size, (int)Math.Round(group.Average)));
        //    }
        //    #endregion

        //    # region Calculate RealTime
        //    RSTCurrentCount = myNewJmeterList.Max(p => p.newTimeStamp) - GraphTimeRange; // always refresh the whole graphs values... has an impact on performance.
        //    var myRSTGroup = from u in myNewJmeterList
        //                     where u.newTimeStamp > RSTCurrentCount
        //                     group u by new { u.newTimeStamp } into g
        //                     select new { Sec = g.Key.newTimeStamp, Average = g.Average(p => p.elapsed) / 1000 };

        //    foreach (var RST in myRSTGroup)
        //    {
        //        var s = from u in ResponseTimeKeyValueOC
        //                where u.Key == RST.Sec
        //                select u;

        //        if (s.Count() > 0)
        //        {
        //            foreach (var item in s.ToList())
        //            {
        //                if (item.Value != RST.Average)
        //                {
        //                    ResponseTimeKeyValueOC.Remove(item);
        //                    ResponseTimeKeyValueOC.Add(new KeyValuePair<int, int>(RST.Sec, (int)Math.Round(RST.Average)));
        //                }
        //            }
        //        }
        //        else
        //        {
        //            ResponseTimeKeyValueOC.Add(new KeyValuePair<int, int>(RST.Sec, (int)Math.Round(RST.Average)));
        //        }

        //    }
        //    #endregion
        //}

        // ******************************
        // Description: Timer Process to calculate Error Count per Seconds
        // 
        // ******************************
        void myError_Elapsed(object sender, EventArgs e)
        {
            var myStartTime = myOverallStartTime;

            var myNewJmeterList = from u in JmeterList
                                  orderby u.timeStamp
                                  select new { newTimeStamp = Convert.ToInt32((Math.Round(Convert.ToDouble(u.timeStamp) / 1000)) - myStartTime), u.testThreadName, u.label, u.errorCount, u.responseCode };

            ErrorCurrentCount = myNewJmeterList.Max(p => p.newTimeStamp) - GraphTimeRange;

            //var myErrorDistinctGroup = from u in myNewJmeterList
            //                           where u.newTimeStamp > ErrorCurrentCount & u.errorCount > 0
            //                           group u by new { u.newTimeStamp } into g
            //                           select new { Sec = g.Key.newTimeStamp, ErrorCount = g.Sum(x => x.errorCount) };
            var myErrorDistinctGroup = from u in myNewJmeterList
                                       where u.newTimeStamp > ErrorCurrentCount & u.errorCount > 0
                                       group u by new { u.newTimeStamp } into g
                                       select new { Sec = g.Key.newTimeStamp, ErrorCount = g.Count() };

            foreach (var ER in myErrorDistinctGroup)
            {
                var s = from u in ErrorKeyValueOC
                        where u.Key == ER.Sec
                        select u;

                if (s.Count() > 0)
                {
                    foreach (var item in s.ToList())
                    {
                        if (item.Value != ER.ErrorCount)
                        {
                            ErrorKeyValueOC.Remove(item);
                            ErrorKeyValueOC.Add(new KeyValuePair<int, int>(ER.Sec, ER.ErrorCount));
                        }
                    }
                }
                else
                {
                    ErrorKeyValueOC.Add(new KeyValuePair<int, int>(ER.Sec, ER.ErrorCount));
                }

                //foreach (var s in ErrorKeyValueOC.Where(p => p.Key == ER.Sec).ToList())
                //{
                //    if (s.Value != ER.ErrorCount)
                //    {
                //        ErrorKeyValueOC.Remove(s);
                //        ErrorKeyValueOC.Add(new KeyValuePair<int, int>(ER.Sec, ER.ErrorCount));
                //    }
                //}
                
            }

        }

        // ******************************
        // Description: Timer Process to calculate Filesize downloaded per second
        // 
        // ******************************
        void myFilesize_Elapsed(object sender, EventArgs e)
        {
            var myStartTime = myOverallStartTime;

            var myNewJmeterList = from u in JmeterList
                                  orderby u.timeStamp
                                  select new { newTimeStamp = Convert.ToInt32((Math.Round(Convert.ToDouble(u.timeStamp) / 1000)) - myStartTime), u.bytes };

            FileSizeCurrentCount = myNewJmeterList.Max(p => p.newTimeStamp) - GraphTimeRange;

            var myFilesizeDistinctGroup = from u in myNewJmeterList
                                           where u.newTimeStamp > FileSizeCurrentCount
                                           group u by new { u.newTimeStamp } into g
                                           select new { Sec = g.Key.newTimeStamp, Sum = int.Parse(((g.Sum(x => x.bytes)/1000).ToString())) };

            foreach (var FS in myFilesizeDistinctGroup)
            {
                var s = from u in FilesizeKeyValueOC
                        where u.Key == FS.Sec
                        select u;

                if (s.Count() > 0)
                {
                    foreach (var item in s.ToList())
                    {
                        if (item.Value != FS.Sum)
                        {
                            FilesizeKeyValueOC.Remove(item);
                            FilesizeKeyValueOC.Add(new KeyValuePair<int, int>(FS.Sec, FS.Sum));
                        }
                    }
                }
                else
                {
                    FilesizeKeyValueOC.Add(new KeyValuePair<int, int>(FS.Sec, FS.Sum));
                }
            }

        }

        // ******************************
        // Description: Timer Process to calculate Virtual User Count by Hostname per seconds
        // 
        // ******************************
        void myConcurrentVU_Elapsed(object sender, EventArgs e)
        {
            var myStartTime = myOverallStartTime;
           
            var myNewJmeterList = from u in JmeterList
                                  orderby u.timeStamp
                                  select new { newTimeStamp = Convert.ToInt32((Math.Round(Convert.ToDouble(u.timeStamp) / 1000)) - myStartTime), u.testThreadName, u.hostname, u.allThreads };

            ConcurrentVUCount = myNewJmeterList.Max(p => p.newTimeStamp) - GraphTimeRange;

            var myVUDistinctGroup = from u in myNewJmeterList
                                    where u.newTimeStamp > ConcurrentVUCount
                                    group u by new { u.newTimeStamp, u.hostname, u.testThreadName } into g
                                    select new { Sec = g.Key.newTimeStamp, Hostname = g.Key.hostname, Max = g.Max(x => x.allThreads) };

            var myVUSumGroup = from u in myVUDistinctGroup
                               group u by new { u.Sec, u.Hostname } into g
                               select new { Sec = g.Key.Sec, Hostname = g.Key.Hostname.Replace("-", ""), Sum = g.Max(x => x.Max) };

            foreach (var VU in myVUSumGroup)
            {
                ObservableCollection<KeyValuePair<int, int>> myTempOC = new ObservableCollection<KeyValuePair<int, int>>();
                if (ConcurrentVUCD.TryGetValue(VU.Hostname, out myTempOC))
                    myTempOC = (ObservableCollection<KeyValuePair<int, int>>)ConcurrentVUCD[VU.Hostname];
                else
                    myTempOC = new ObservableCollection<KeyValuePair<int, int>>();

                myTempOC.Add(new KeyValuePair<int, int>(VU.Sec, VU.Sum));
                ConcurrentVUCD.AddOrUpdate(VU.Hostname, myTempOC, (key, oldValue) => myTempOC);
            }
          
        }

        #endregion

        # region All the buttons
        private void btnTxnPSLeft_Click(object sender, RoutedEventArgs e)
        {
            clearChart_AllResult_button();

            btnTxnPSRight.Background = Brushes.WhiteSmoke;
            btnTxnPSRight.Foreground = Brushes.Black;
            btnTxnPSLeft.Background = Brushes.DodgerBlue;
            btnTxnPSLeft.Foreground = Brushes.White;
            RemoveSeries(TPS);
            createLineSeries(TPS, "TPS", "Value", "Key", "LEFT", TPSKeyValueOC, Brushes.DodgerBlue);
        }
        private void btnTxnPSRight_Click(object sender, RoutedEventArgs e)
        {
            clearChart_AllResult_button();
            btnTxnPSLeft.Background = Brushes.WhiteSmoke;
            btnTxnPSLeft.Foreground = Brushes.Black;
            btnTxnPSRight.Background = Brushes.DodgerBlue;
            btnTxnPSRight.Foreground = Brushes.White;
            RemoveSeries(TPS);
            createAreaSeries(TPS, "TPS", "Value", "Key", "RIGHT", TPSKeyValueOC, Brushes.DodgerBlue);
        }

        private void btnVirtualUserLeft_Click(object sender, RoutedEventArgs e)
        {
            clearChart_AllResult_button();
            btnVirtualUserRight.Background = Brushes.WhiteSmoke;
            btnVirtualUserRight.Foreground = Brushes.Black;
            btnVirtualUserLeft.Background = Brushes.Orange;
            btnVirtualUserLeft.Foreground = Brushes.White;
            RemoveSeries(VU);
            createLineSeries(VU, "VU", "Value", "Key", "LEFT", VUKeyValueOC, Brushes.Orange);
        }     
        private void btnVirtualUserRight_Click(object sender, RoutedEventArgs e)
        {
            clearChart_AllResult_button();
            btnVirtualUserLeft.Background = Brushes.WhiteSmoke;
            btnVirtualUserLeft.Foreground = Brushes.Black;
            btnVirtualUserRight.Background = Brushes.Orange;
            btnVirtualUserRight.Foreground = Brushes.White;
            RemoveSeries(VU);
            createAreaSeries(VU, "VU", "Value", "Key", "RIGHT", VUKeyValueOC, Brushes.Orange);
        }

        private void btnResponseTimeLeft_Click(object sender, RoutedEventArgs e)
        {
            clearChart_AllResult_button();
            btnResponseTimeRight.Background = Brushes.WhiteSmoke;
            btnResponseTimeRight.Foreground = Brushes.Black;
            btnResponseTimeLeft.Background = Brushes.Green;
            btnResponseTimeLeft.Foreground = Brushes.White;
            RemoveSeries(RST);
            createLineSeries(RST, "Response Time(ms)", "Value", "Key", "LEFT", ResponseTimeKeyValueOC, Brushes.Green);
        }
        private void btnResponseTimeRight_Click(object sender, RoutedEventArgs e)
        {
            clearChart_AllResult_button();
            btnResponseTimeLeft.Background = Brushes.WhiteSmoke;
            btnResponseTimeLeft.Foreground = Brushes.Black;
            btnResponseTimeRight.Background = Brushes.Green;
            btnResponseTimeRight.Foreground = Brushes.White;
            RemoveSeries(RST);
            createAreaSeries(RST, "Response Time(ms)", "Value", "Key", "RIGHT", ResponseTimeKeyValueOC, Brushes.Green);
        }

        private void btnVUOverViewLeft_Click(object sender, RoutedEventArgs e)
        {
            RemoveSeries(VU);
          //  createAreaSeries(VU, "VU", "Value", "Key", "NA", VUKeyValueOC, Brushes.Orange);
            foreach (KeyValuePair<String, ObservableCollection<KeyValuePair<int, int>>> myValue in ConcurrentVUCD)
            {
                RemoveSeries(myValue.Key.ToString());
                createLineSeries(myValue.Key.ToString(), myValue.Key.ToString(), "Value", "Key", "NA", myValue.Value, null);
            }
        }
        private void btnVUOverViewRight_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("not used");
            return;
            //RemoveSeries("VUSeries");
            //createAreaSeries("VUSeries", "VU", "Value", "Key", "NA", VUKeyValueOC, Brushes.Orange);
            //foreach (KeyValuePair<String, ObservableCollection<KeyValuePair<int, int>>> myValue in ConcurrentVUCD)
            //{
            //    RemoveSeries(myValue.Key.ToString());
            //    createLineSeries(myValue.Key.ToString(), myValue.Key.ToString(), "Value", "Key", "NA", myValue.Value, null);
            //}
        }

        private void btnErrorCountLeft_Click(object sender, RoutedEventArgs e)
        {
            clearChart_AllResult_button();
            btnErrorCountRight.Background = Brushes.WhiteSmoke;
            btnErrorCountRight.Foreground = Brushes.Black;
            btnErrorCountLeft.Background = Brushes.Red;
            btnErrorCountLeft.Foreground = Brushes.White;
            RemoveSeries(ER);
            createAutoScaleLineSeries(ER, "ERROR", "Value", "Key", "LEFT", ErrorKeyValueOC, Brushes.Red);

        }
        private void btnErrorCountRight_Click(object sender, RoutedEventArgs e)
        {
            clearChart_AllResult_button();
            btnErrorCountLeft.Background = Brushes.WhiteSmoke;
            btnErrorCountLeft.Foreground = Brushes.Black;
            btnErrorCountRight.Background = Brushes.Red;
            btnErrorCountRight.Foreground = Brushes.White;
            RemoveSeries(ER);
            createAutoScaleAreaSeries(ER, "ERROR", "Value", "Key", "RIGHT", ErrorKeyValueOC, Brushes.Red);
        }

        private void btnSizeLeft_Click(object sender, RoutedEventArgs e)
        {
            clearChart_AllResult_button();
            btnSizeCountRight.Background = Brushes.WhiteSmoke;
            btnSizeCountRight.Foreground = Brushes.Black;
            btnSizeCountLeft.Background = Brushes.DarkGoldenrod;
            btnSizeCountLeft.Foreground = Brushes.White;
            RemoveSeries(FS);
            createAutoScaleLineSeries(FS, "FileSize", "Value", "Key", "LEFT", FilesizeKeyValueOC, Brushes.DarkGoldenrod);
        }
        private void btnSizeRight_Click(object sender, RoutedEventArgs e)
        {
            clearChart_AllResult_button();
            btnSizeCountLeft.Background = Brushes.WhiteSmoke;
            btnSizeCountLeft.Foreground = Brushes.Black;
            btnSizeCountRight.Background = Brushes.DarkGoldenrod;
            btnSizeCountRight.Foreground = Brushes.White;
            RemoveSeries(FS);
            createAutoScaleAreaSeries(FS, "FileSize", "Value", "Key", "RIGHT", FilesizeKeyValueOC, Brushes.DarkGoldenrod);
        }

        private void btnAllTPSLeft_Click(object sender, RoutedEventArgs e)
        {
            if (groupTPSKeyValueOC.Count < 1)
            {
                MessageBox.Show("Still processing data");
                return;
            }
            clearChart_AllRealtime_button();
            btnAllTPSRight.Background = Brushes.WhiteSmoke;
            btnAllTPSRight.Foreground = Brushes.Black;
            btnAllTPSLeft.Background = Brushes.DodgerBlue;
            btnAllTPSLeft.Foreground = Brushes.White;
            RemoveSeries(ALL_TPS);
            createAutoScaleLineSeries(ALL_TPS, "Txn Count", "Value", "Key", "LEFT", groupTPSKeyValueOC, Brushes.DodgerBlue);
        }
        private void btnAllTPSRight_Click(object sender, RoutedEventArgs e)
        {
            if (groupTPSKeyValueOC.Count < 1)
            {
                MessageBox.Show("Still processing data");
                return;
            }
            clearChart_AllRealtime_button();
            btnAllTPSLeft.Background = Brushes.WhiteSmoke;
            btnAllTPSLeft.Foreground = Brushes.Black;
            btnAllTPSRight.Background = Brushes.DodgerBlue;
            btnAllTPSRight.Foreground = Brushes.White;
            RemoveSeries(ALL_TPS);
            createAutoScaleAreaSeries(ALL_TPS, "Txn Count", "Value", "Key", "RIGHT", groupTPSKeyValueOC, Brushes.DodgerBlue);
        }

        private void btnAllResponseTimeLeft_Click(object sender, RoutedEventArgs e)
        {
            if (groupResponseTimeKeyValueOC.Count < 1)
            {
                MessageBox.Show("Still processing data");
                return;
            }
            clearChart_AllRealtime_button();
            btnAllResponseTimeRight.Background = Brushes.WhiteSmoke;
            btnAllResponseTimeRight.Foreground = Brushes.Black;
            btnAllResponseTimeLeft.Background = Brushes.Green;
            btnAllResponseTimeLeft.Foreground = Brushes.White;
            RemoveSeries(ALL_RST);
            createAutoScaleLineSeries(ALL_RST, "All Response Time (sec)", "Value", "Key", "LEFT", groupResponseTimeKeyValueOC, Brushes.Green);
        }
        private void btnAllResponseTimeRight_Click(object sender, RoutedEventArgs e)
        {
            if (groupResponseTimeKeyValueOC.Count < 1)
            {
                MessageBox.Show("Still processing data");
                return;
            }
            clearChart_AllRealtime_button();
            btnAllResponseTimeLeft.Background = Brushes.WhiteSmoke;
            btnAllResponseTimeLeft.Foreground = Brushes.Black;
            btnAllResponseTimeRight.Background = Brushes.Green;
            btnAllResponseTimeRight.Foreground = Brushes.White;
            RemoveSeries(ALL_RST);
            createAutoScaleAreaSeries(ALL_RST, "All Response Time (sec)", "Value", "Key", "RIGHT", groupResponseTimeKeyValueOC, Brushes.Green);
        }

        private void btnAllVULeft_Click(object sender, RoutedEventArgs e)
        {
            if (groupVUKeyValueOC.Count < 1)
            {
                MessageBox.Show("Still processing data");
                return;
            }
            clearChart_AllRealtime_button();
            btnAllVURight.Background = Brushes.WhiteSmoke;
            btnAllVURight.Foreground = Brushes.Black;
            btnAllVULeft.Background = Brushes.Orange;
            btnAllVULeft.Foreground = Brushes.White;
            RemoveSeries(ALL_VU);
            createAutoScaleLineSeries(ALL_VU, "VU", "Value", "Key", "LEFT", groupVUKeyValueOC, Brushes.Orange);
        }
        private void btnAllVURight_Click(object sender, RoutedEventArgs e)
        {
            if (groupVUKeyValueOC.Count < 1)
            {
                MessageBox.Show("Still processing data");
                return;
            }
            clearChart_AllRealtime_button();
            btnAllVULeft.Background = Brushes.WhiteSmoke;
            btnAllVULeft.Foreground = Brushes.Black;
            btnAllVURight.Background = Brushes.Orange;
            btnAllVURight.Foreground = Brushes.White;
            RemoveSeries(ALL_VU);
            createAutoScaleAreaSeries(ALL_VU, "VU", "Value", "Key", "RIGHT", groupVUKeyValueOC, Brushes.Orange);
        }

        private void btnAllErrorLeft_Click(object sender, RoutedEventArgs e)
        {
            if (groupErrorKeyValueOC.Count < 1)
            {
                MessageBox.Show("Still processing data");
                return;
            }
            clearChart_AllRealtime_button();
            btnAllErrorRight.Background = Brushes.WhiteSmoke;
            btnAllErrorRight.Foreground = Brushes.Black;
            btnAllErrorLeft.Background = Brushes.Red;
            btnAllErrorLeft.Foreground = Brushes.White;
            RemoveSeries(ALL_ER);
            createAutoScaleLineSeries(ALL_ER, "ERROR", "Value", "Key", "LEFT", groupErrorKeyValueOC, Brushes.Red);
        }
        private void btnAllErrorRight_Click(object sender, RoutedEventArgs e)
        {
            if (groupErrorKeyValueOC.Count < 1)
            {
                MessageBox.Show("Still processing data");
                return;
            }
            clearChart_AllRealtime_button();
            btnAllErrorLeft.Background = Brushes.WhiteSmoke;
            btnAllErrorLeft.Foreground = Brushes.Black;
            btnAllErrorRight.Background = Brushes.Red;
            btnAllErrorRight.Foreground = Brushes.White;
            RemoveSeries(ALL_ER);
            createAutoScaleAreaSeries(ALL_ER, "ERROR", "Value", "Key", "RIGHT", groupErrorKeyValueOC, Brushes.Red);
        }

        private void btnAllFilesizeLeft_Click(object sender, RoutedEventArgs e)
        {
            if (groupFileSizeKeyValueOC.Count < 1)
            {
                MessageBox.Show("Still processing data");
                return;
            }
            clearChart_AllRealtime_button();
            btnAllFileSizeRight.Background = Brushes.WhiteSmoke;
            btnAllFileSizeRight.Foreground = Brushes.Black;
            btnAllFileSizeLeft.Background = Brushes.DarkGoldenrod;
            btnAllFileSizeLeft.Foreground = Brushes.White;
            RemoveSeries(ALL_FS);
            createAutoScaleLineSeries(ALL_FS, "FileSize(MB)", "Value", "Key", "LEFT", groupFileSizeKeyValueOC, Brushes.DarkGoldenrod);
        }
        private void btnAllFilesizeRight_Click(object sender, RoutedEventArgs e)
        {
            if (groupFileSizeKeyValueOC.Count < 1)
            {
                MessageBox.Show("Still processing data");
                return;
            }
            clearChart_AllRealtime_button();
            btnAllFileSizeLeft.Background = Brushes.WhiteSmoke;
            btnAllFileSizeLeft.Foreground = Brushes.Black;
            btnAllFileSizeRight.Background = Brushes.DarkGoldenrod;
            btnAllFileSizeRight.Foreground = Brushes.White;
            RemoveSeries(ALL_FS);
            createAutoScaleAreaSeries(ALL_FS, "FileSize(MB)", "Value", "Key", "RIGHT", groupFileSizeKeyValueOC, Brushes.DarkGoldenrod);
        }
        
        private void menuItem_close_click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        #endregion

        private void menuItem_aboutBox_click(object sender, RoutedEventArgs e)
        {
            AboutBox myAboutBox = new AboutBox();
         
            myAboutBox.Show();
        }

        #region Commonly used method
        double myCustomCurrentChartHeight = 300;
        String myCustomCurrentName = "";
        // autoscale Y axis
        private void createAutoScaleLineSeries(String name, String title, String value, String key, String orientation, ObservableCollection<KeyValuePair<int, int>> OCollection, SolidColorBrush brushColor)
        {
            if (OCollection.Count() < 1)
            {
                return;
            }
            LineSeries myLineSeries = new LineSeries();
            myLineSeries.Name = name;
            myLineSeries.Title = title;
            myLineSeries.DependentValuePath = value;
            myLineSeries.IndependentValuePath = key;
            myLineSeries.ItemsSource = OCollection;
            
            bool drawGrid = true;

            int MaxValue = OCollection.Max(p => p.Value);
           

            if (MaxValue >= myCustomCurrentChartHeight) // check if need to redraw the Chart Height
            {
                myCustomCurrentChartHeight = MaxValue + 10;
                myCustomCurrentName = name;
            }
            else
                drawGrid = false;

            double myYInteval = myCustomCurrentChartHeight / 5; // only draw 5 grid lines

            if (brushColor != null)
            {
                Style s = new Style(typeof(LineDataPoint));
                s.Setters.Add(new Setter(LineDataPoint.BackgroundProperty, brushColor));
                myLineSeries.DataPointStyle = s;
            }

            // Drawing the line.
            LinearAxis myLinearAxis = new LinearAxis();
            myLinearAxis.Orientation = AxisOrientation.Y;
            myLinearAxis.Title = title;
            myLinearAxis.Minimum = 1;
            myLinearAxis.Maximum = myCustomCurrentChartHeight;
            myLinearAxis.Interval = myYInteval;
            //myLinearAxis.HorizontalAlignment = orientation == "LEFT" ? HorizontalAlignment.Left : HorizontalAlignment.Right;
            myLinearAxis.ShowGridLines = true;
       

            myLinearAxis.Visibility = System.Windows.Visibility.Visible; // by default is visible.
            myLinearAxis.Location = orientation == "LEFT" ? AxisLocation.Left : AxisLocation.Right;

            if ((!drawGrid) && (myCustomCurrentName != name))
            {
                myLinearAxis.Visibility = System.Windows.Visibility.Hidden;                
                myLinearAxis.ShowGridLines = false;
            }
            
            myLineSeries.DependentRangeAxis = myLinearAxis;
            
            // To draw the chart
            if (lineChart.Series.Count == 0)
                lineChart.Series.Insert(0, myLineSeries);
            else
            {
                if (drawGrid)
                {
                    int count = lineChart.Series.Count();
                    for (int i = 0; i < count; i++)
                    {
                        lineChart.Series.RemoveAt(0);
                    }
                    lineChart.Series.Insert(0, myLineSeries);
                }
                else
                {
                    lineChart.Series.Insert(1, myLineSeries);
                }
            }

        }

        private void createAutoScaleAreaSeries(String name, String title, String value, String key, String orientation, ObservableCollection<KeyValuePair<int, int>> OCollection, SolidColorBrush brushColor)
        {
            if (OCollection.Count() < 1)
            {
                return;
            }
            //LineSeries myLineSeries = new LineSeries();
            AreaSeries myAreaSeries = new AreaSeries();
            myAreaSeries.Name = name;
            myAreaSeries.Title = title;
            myAreaSeries.DependentValuePath = value;
            myAreaSeries.IndependentValuePath = key;
            myAreaSeries.ItemsSource = OCollection;

            bool drawGrid = true;

            int MaxValue = OCollection.Max(p => p.Value);


            if (MaxValue >= myCustomCurrentChartHeight) // check if need to redraw the Chart Height
            {
                myCustomCurrentChartHeight = MaxValue + 10;
                myCustomCurrentName = name;
            }
            else
                drawGrid = false;

            double myYInteval = myCustomCurrentChartHeight / 5; // only draw 5 grid lines
                       
            Style s = new Style(typeof(AreaDataPoint));
            s.Setters.Add(new Setter(AreaDataPoint.BackgroundProperty, brushColor));
            myAreaSeries.DataPointStyle = s;

            // Drawing the line.
            LinearAxis myLinearAxis = new LinearAxis();
            myLinearAxis.Orientation = AxisOrientation.Y;
            myLinearAxis.Title = title;
            myLinearAxis.Minimum = 1;
            myLinearAxis.Maximum = myCustomCurrentChartHeight;
            myLinearAxis.Interval = myYInteval;
            //myLinearAxis.HorizontalAlignment = orientation == "LEFT" ? HorizontalAlignment.Left : HorizontalAlignment.Right;
            myLinearAxis.ShowGridLines = true;
            myLinearAxis.Visibility = System.Windows.Visibility.Visible; // by default is visible.
            myLinearAxis.Location = orientation == "LEFT" ? AxisLocation.Left : AxisLocation.Right;

            if ((!drawGrid) && (myCustomCurrentName != name))
            {
                // myLinearAxis.Visibility = System.Windows.Visibility.Hidden;                
                myLinearAxis.ShowGridLines = false;
            }

            myAreaSeries.DependentRangeAxis = myLinearAxis;

            // To draw the chart
            if (lineChart.Series.Count == 0)
                lineChart.Series.Insert(0, myAreaSeries);
            else
            {
                if (drawGrid)
                {
                    int count = lineChart.Series.Count();
                    for (int i = 0; i < count; i++)
                    {
                        lineChart.Series.RemoveAt(0);
                    }
                    lineChart.Series.Insert(0, myAreaSeries);
                }
                else
                {
                    lineChart.Series.Insert(1, myAreaSeries);
                }
            }

        }


        // fixed Y axis
        private void createLineSeries(String name, String title, String value, String key, String orientation, ObservableCollection<KeyValuePair<int, int>> OCollection, SolidColorBrush brushColor)
        {
            //double myInteval = 0;

            //if (OCollection.Count() > 0)
            //{
            //    int MaxValue = OCollection.Max(i => i.Value);
            //   // myInteval = MaxValue / 5;
            //    if (MaxValue > currentChartHeight)
            //        currentChartHeight = MaxValue + 10;
            //    //if (name == "VUSeries")
            //    //    myInteval = MaxValue;
            //}

            //if (name == "VUSeries")
            //    currentChartHeight = 60;
            //else
            //    currentChartHeight = 20;

            LineSeries LineSeries1 = new LineSeries();
            LineSeries1.Name = name;
            LineSeries1.Title = title;
            LineSeries1.DependentValuePath = value;
            LineSeries1.IndependentValuePath = key;
            if (orientation == "LEFT")
               // LineSeries1.DependentRangeAxis = new LinearAxis() { Orientation = AxisOrientation.Y, HorizontalAlignment = HorizontalAlignment.Left, Title = title, ShowGridLines = true, Interval = myInteval };
                LineSeries1.DependentRangeAxis = new LinearAxis() { Orientation = AxisOrientation.Y, HorizontalAlignment = HorizontalAlignment.Left, Title = title, ShowGridLines = true, Minimum = 1, Maximum = currentChartHeight, Interval = 5 };
            else if (orientation == "RIGHT")
                //LineSeries1.DependentRangeAxis = new LinearAxis() { Orientation = AxisOrientation.Y, HorizontalAlignment = HorizontalAlignment.Right, Title = title, ShowGridLines = true, Interval = myInteval };
                LineSeries1.DependentRangeAxis = new LinearAxis() { Orientation = AxisOrientation.Y, HorizontalAlignment = HorizontalAlignment.Right, Title = title, ShowGridLines = true, Minimum = 1, Maximum = currentChartHeight, Interval = 5 };

            LineSeries1.ItemsSource = OCollection;

            if (brushColor != null)
            {
                Style s = new Style(typeof(LineDataPoint));
                s.Setters.Add(new Setter(LineDataPoint.BackgroundProperty, brushColor));
                LineSeries1.DataPointStyle = s;
            }
            if (lineChart.Series.Count == 0)
                lineChart.Series.Insert(0, LineSeries1);
            else
                lineChart.Series.Insert(1, LineSeries1);
        }

        // fixed Y axis
        private void createAreaSeries(String name, String title, String value, String key, String orientation, ObservableCollection<KeyValuePair<int, int>> OCollection, SolidColorBrush brushColor)
        {
            AreaSeries AreaSeries1 = new AreaSeries();
            AreaSeries1.Name = name;
            AreaSeries1.Title = title;
            AreaSeries1.DependentValuePath = value;
            AreaSeries1.IndependentValuePath = key;
            if (orientation == "LEFT")
               // AreaSeries1.DependentRangeAxis = new LinearAxis() { Orientation = AxisOrientation.Y, HorizontalAlignment = HorizontalAlignment.Left, Title = title, ShowGridLines = true, Interval = myInteval };
                AreaSeries1.DependentRangeAxis = new LinearAxis() { Orientation = AxisOrientation.Y, HorizontalAlignment = HorizontalAlignment.Left, Title = title, ShowGridLines = true, Minimum = 1, Maximum = currentChartHeight, Interval = 5 };
            else if (orientation == "RIGHT")
                //AreaSeries1.DependentRangeAxis = new LinearAxis() { Orientation = AxisOrientation.Y, HorizontalAlignment = HorizontalAlignment.Right, Title = title, ShowGridLines = true, Interval = myInteval };
                AreaSeries1.DependentRangeAxis = new LinearAxis() { Orientation = AxisOrientation.Y, HorizontalAlignment = HorizontalAlignment.Right, Title = title, ShowGridLines = true, Minimum = 1, Maximum = currentChartHeight, Interval = 5 };


            AreaSeries1.ItemsSource = OCollection;

            Style s = new Style(typeof(AreaDataPoint));
            s.Setters.Add(new Setter(AreaDataPoint.BackgroundProperty, brushColor));
            AreaSeries1.DataPointStyle = s;

            if (lineChart.Series.Count == 0)
                lineChart.Series.Insert(0, AreaSeries1);
            else
                lineChart.Series.Insert(1, AreaSeries1);
        }

        private void RemoveSeries(String name)
        {
            var myLineSeries = lineChart.Series.OfType<LineSeries>().Where(x => x.Name == name);
            if (myLineSeries.Count() > 0)
                lineChart.Series.Remove(myLineSeries.Single());

            var myAreaSeries = lineChart.Series.OfType<AreaSeries>().Where(x => x.Name == name);
            if (myAreaSeries.Count() > 0)
                lineChart.Series.Remove(myAreaSeries.Single());
        }

        public DateTime FromUnixTime(long unixTime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(unixTime);
        }

        public double Percentile(IEnumerable<long> seq, double percentile)
        {
            var elements = seq.ToArray();
            Array.Sort(elements);
            double realIndex = percentile * (elements.Length - 1);
            int index = (int)realIndex;
            double frac = realIndex - index;
            if (index + 1 < elements.Length)
                return elements[index] * (1 - frac) + elements[index + 1] * frac;
            else
                return elements[index];
        }
        #endregion

       
      
      
       


        
    }

}
