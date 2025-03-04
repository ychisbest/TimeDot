using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace TimeDot
{
    public partial class TimeGridControl : UserControl
    {
        private DispatcherTimer timer;

        public class MinuteData
        {
            public Brush Color { get; set; }
            public bool IsCurrent { get; set; }
        }

        public class HourData
        {
            public string HourLabel { get; set; }
            public List<MinuteData> Minutes { get; set; }
        }

        public TimeGridControl()
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
                        minuteData.Color = Brushes.LightGreen;
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