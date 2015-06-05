using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Research.DynamicDataDisplay.DataSources;
using System.Windows;
using System.Windows.Threading;

namespace JmeterDashboard
{
    public class TPS
    {
        public ObservableDataSource<Point> TPSDataSource { get; set; }

        public TPS(){}

        public TPS(int x, double y)
        {
           // Point p1 = new Point(x, y);
            //TPSDataSource.AppendAsync(Dispatcher, p1);
        }
        
    }
}
