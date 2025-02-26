using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Forms;
using System.Drawing;
using Brushes = System.Windows.Media.Brushes;
using Brush = System.Windows.Media.Brush;
using System.Security.Policy;
using Application = System.Windows.Application;

namespace TimeGrid
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int WM_SYSCOMMAND = 0x0112;
        private const int MF_SEPARATOR = 0x0800;
        private const int MF_STRING = 0x0000;
        private const int SYSMENU_TOPMOST_ID = 1000;

        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll")]
        private static extern bool AppendMenu(IntPtr hMenu, int uFlags, int uIDNewItem, string lpNewItem);

        [DllImport("user32.dll")]
        private static extern bool CheckMenuItem(IntPtr hMenu, uint uIDCheckItem, uint uCheck);
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

        private DispatcherTimer timer;


        private NotifyIcon notifyIcon;
        public MainWindow()
        {
            InitializeComponent();
            UpdateTimeGrid();


            this.SourceInitialized += MainWindow_SourceInitialized;
            this.Loaded += MainWindow_Loaded;

            // 创建托盘图标
            notifyIcon = new NotifyIcon();
            var resourceUri = new Uri("pack://application:,,,/icon.ico");
            var resourceStream = Application.GetResourceStream(resourceUri);

            notifyIcon.Icon = new Icon(resourceStream.Stream);
            notifyIcon.Visible = true;
            notifyIcon.DoubleClick += NotifyIcon_DoubleClick;

            // 在关闭窗口时隐藏窗口而不是退出应用
            this.Closing += MainWindow_Closing;

            // 创建右键菜单
            var contextMenu = new System.Windows.Forms.ContextMenu();
            var exitMenuItem = new System.Windows.Forms.MenuItem("Exit");
            exitMenuItem.Click += ExitMenuItem_Click;

            contextMenu.MenuItems.Add(exitMenuItem);
            notifyIcon.ContextMenu = contextMenu;

            // 设置定时器每5秒更新一次
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(5);
            timer.Tick += (s, e) => UpdateTimeGrid();
            timer.Start();
        }

        // 右键菜单退出事件
        private void ExitMenuItem_Click(object sender, EventArgs e)
        {
            // 退出应用程序
            Application.Current.Shutdown();
        }
        private void NotifyIcon_DoubleClick(object sender, EventArgs e)
        {
            // 双击托盘图标时恢复窗口
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 关闭窗口时隐藏它，而不是完全退出应用
            e.Cancel = true;
            this.Hide();
        }

        private void UpdateTimeGrid()
        {
            var now = DateTime.Now;
            var currentHour = now.Hour;
            var currentMinute = now.Minute - 1;
            const int startHour = 8;
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
                        minuteData.Color = Brushes.DarkGray; // 已过去的时间
                        minuteData.IsCurrent = false;
                    }
                    else if (hour == currentHour && minute == currentMinute)
                    {
                        minuteData.Color = Brushes.LightGreen; // 当前分钟
                        minuteData.IsCurrent = true;
                    }
                    else
                    {
                        minuteData.Color = Brushes.White; // 未来的时间
                        minuteData.IsCurrent = false;
                    }

                    hourData.Minutes.Add(minuteData);
                }

                hours.Add(hourData);
            }

            TimeGrid.ItemsSource = hours;
        }


        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            WindowInteropHelper helper = new WindowInteropHelper(this);
            IntPtr hMenu = GetSystemMenu(helper.Handle, false);

            // 添加分隔线
            AppendMenu(hMenu, MF_SEPARATOR, 0, null);
            // 添加“始终置顶”菜单项
            AppendMenu(hMenu, MF_STRING, SYSMENU_TOPMOST_ID, "始终置顶");

            // 注册系统命令事件
            HwndSource source = HwndSource.FromHwnd(helper.Handle);
            source.AddHook(WndProc);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateMenuCheckState();
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_SYSCOMMAND && wParam.ToInt32() == SYSMENU_TOPMOST_ID)
            {
                this.Topmost = !this.Topmost;
                UpdateMenuCheckState();
                handled = true;
            }
            return IntPtr.Zero;
        }

        private void UpdateMenuCheckState()
        {
            WindowInteropHelper helper = new WindowInteropHelper(this);
            IntPtr hMenu = GetSystemMenu(helper.Handle, false);
            CheckMenuItem(hMenu, SYSMENU_TOPMOST_ID,
                (uint)(this.Topmost ? 0x0008 : 0x0000)); // MF_CHECKED : MF_UNCHECKED
        }


        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Topmost = !Topmost;
        }

        private void ExitClick(object sender, RoutedEventArgs e)
        {
           Application.Current.Shutdown();
        }
    }
}