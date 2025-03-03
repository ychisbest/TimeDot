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
using Color = System.Windows.Media.Color;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;

namespace TimeDot
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
        private Task _backTask;
        private CancellationTokenSource _cts; // 用于取消任务的Token源

        private NotifyIcon notifyIcon;
        public MainWindow()
        {
            InitializeComponent();
            UpdateTimeGrid();

            JsonConvert.SerializeObject(new { });


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
            this.IsVisibleChanged += MainWindow_IsVisibleChanged;

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

        private void MainWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.IsVisible)
            {
                if (_cts != null)
                {
                    _cts.Cancel();
                    _cts.Dispose();
                }
            }
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

        private void ExitClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void AboutClick(object sender, RoutedEventArgs e)
        {
            var result = System.Windows.MessageBox.Show("This is version V0.0.1 beta,more function is under development. Would you like to visit the official website?", "About", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (result == MessageBoxResult.Yes)
            {
                Process.Start(new ProcessStartInfo("https://timedot.net") { UseShellExecute = true });
            }
        }

        private void TopmostClick(object sender, RoutedEventArgs e)
        {
            Topmost = !Topmost;
        }


        private async void doubleClick(object sender, MouseButtonEventArgs e)
        {

            await hiden();
        }

        private async void hidenClick(object sender, RoutedEventArgs e)
        {
            await hiden();
        }

        private async Task hiden()
        {
            // 取消之前的任务并释放资源
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
            }

            // 创建新的CancellationTokenSource
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            this.ShowActivated = false;
            this.Hide();

            // 启动后台任务并传入取消Token
            _backTask = Task.Run(async () =>
            {
                try
                {
                    // 等待5分钟，期间响应取消请求
                    await Task.Delay(60 * 1000, token);

                    // 再次检查是否已取消（可能在Delay完成后被取消）
                    if (token.IsCancellationRequested)
                        return;

                    // 在UI线程更新窗口状态
                    await this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        // 最终确认未被取消后执行显示操作
                        if (!token.IsCancellationRequested)
                        {
                            this.Show();
                            this.WindowState = WindowState.Normal;
                        }
                    }));
                }
                catch (OperationCanceledException)
                {
                    // 任务被取消时的预期异常，无需处理
                }
            }, token);
        }


    }

    public class BorderColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((bool)value)
                return new SolidColorBrush(Colors.LightBlue);

            else
                return new SolidColorBrush( Colors.Transparent);
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }
    }
}