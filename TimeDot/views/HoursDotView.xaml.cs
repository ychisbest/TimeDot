using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using static TimeDot.MainWindow;

namespace TimeDot.views
{
    /// <summary>
    /// HoursDotView.xaml 的交互逻辑
    /// </summary>
    public partial class HoursDotView : UserControl
    {
        DispatcherTimer timer;
        public HoursDotView()
        {
            InitializeComponent();
            UpdateTimeGrid();

            // 设置定时器每5秒更新一次
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(5);
            timer.Tick += (s, e) => UpdateTimeGrid();
            timer.Start();
        }

        private void UpdateTimeGrid()
        {
            var now = DateTime.Now;
            var currentHour = now.Hour;
            var currentMinute = now.Minute - 1;
            const int startHour = 0;
            const int endHour = 24;
            var hours = new List<HourData>();

            for (int hour = startHour; hour < endHour; hour++)
            {
                var hourData = new HourData
                {
                    HourLabel = $"{hour}:00",
                    Minutes = new List<MinuteData>()
                };

                for (int minute = 0; minute < 60; minute++)
                {
                    var minuteData = new MinuteData();

                    if (hour < currentHour || (hour == currentHour && minute < currentMinute))
                    {
                        minuteData.Color = Brushes.White;

                        minuteData.IsCurrent = false;
                    }
                    else if (hour == currentHour && minute == currentMinute)
                    {
                        minuteData.Color = Brushes.LightGreen; // 当前分钟
                        minuteData.IsCurrent = true;
                    }
                    else
                    {
                        minuteData.Color = Brushes.DarkGray;

                        minuteData.IsCurrent = false;
                    }

                    hourData.Minutes.Add(minuteData);
                }

                hours.Add(hourData);
            }

            TimeGrid.ItemsSource = hours;
        }


    }

}
