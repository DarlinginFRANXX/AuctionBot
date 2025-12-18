using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AuctionBot
{
    public partial class MainWindow : Window
    {
        private CaptureMode _mode = CaptureMode.None;

        private Point SearchPoint = new Point(0, 0);
        private Point RedemptionPricesPoint = new Point(0, 0);
        private Point DownArrowPoint = new Point(0, 0);
        private Point OkPoint = new Point(0, 0);

        private Rect ScanAreaRect;
        private Point _scanStart;

        private IntPtr _hookID = IntPtr.Zero;
        private LowLevelMouseProc _proc;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            _mode = CaptureMode.Search;
            UpdateStatus("Нажмите на экран для выбора координат 'Поиск'");
        }

        private void RedemptionPrices_Click(object sender, RoutedEventArgs e)
        {
            _mode = CaptureMode.RedemptionPrices;
            UpdateStatus("Нажмите на экран для выбора координат 'Цены выкупа'");
        }

        private void DownArrow_Click(object sender, RoutedEventArgs e)
        {
            _mode = CaptureMode.DownArrow;
            UpdateStatus("Нажмите на экран для выбора координат 'Стрелка вниз'");
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            _mode = CaptureMode.Ok;
            UpdateStatus("Нажмите на экран для выбора координат 'ОК'");
        }

        private void ScanningArea_Click(object sender, RoutedEventArgs e)
        {
            _mode = CaptureMode.Scan_First;
            UpdateStatus("Нажмите ЛЕВЫЙ ВЕРХНИЙ угол зоны сканирования");
        }

        private void UpdateStatus(string message)
        {
            // Ищем TextBlock для статуса в XAML
            var statusTextBlock = FindName("StatusTextBlock") as TextBlock;
            if (statusTextBlock != null)
            {
                statusTextBlock.Text = message;
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _proc = HookCallback;
            _hookID = SetHook(_proc);
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_LBUTTONDOWN && _mode != CaptureMode.None)
            {
                var data = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                Dispatcher.Invoke(() => HandleClick(data.pt.x, data.pt.y));
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private void HandleClick(int x, int y)
        {
            switch (_mode)
            {
                case CaptureMode.Search:
                    SearchPoint = new Point(x, y);
                    SearchXY.Text = $"{x}, {y}";
                    _mode = CaptureMode.None;
                    UpdateStatus("Координаты 'Поиск' сохранены");
                    break;

                case CaptureMode.RedemptionPrices:
                    RedemptionPricesPoint = new Point(x, y);
                    RedemptionPricesXY.Text = $"{x}, {y}";
                    _mode = CaptureMode.None;
                    UpdateStatus("Координаты 'Цены выкупа' сохранены");
                    break;

                case CaptureMode.DownArrow:
                    DownArrowPoint = new Point(x, y);
                    DownArrowXY.Text = $"{x}, {y}";
                    _mode = CaptureMode.None;
                    UpdateStatus("Координаты 'Стрелка вниз' сохранены");
                    break;

                case CaptureMode.Ok:
                    OkPoint = new Point(x, y);
                    OkXY.Text = $"{x}, {y}";
                    _mode = CaptureMode.None;
                    UpdateStatus("Координаты 'ОК' сохранены");
                    break;

                case CaptureMode.Scan_First:
                    _scanStart = new Point(x, y);
                    ScanAreaXY.Text = $"{x}, {y} -> ?";
                    _mode = CaptureMode.Scan_Second;
                    UpdateStatus("Теперь нажмите ПРАВЫЙ НИЖНИЙ угол зоны сканирования");
                    break;

                case CaptureMode.Scan_Second:
                    var endPoint = new Point(x, y);
                    ScanAreaRect = new Rect(_scanStart, endPoint);
                    ScanAreaXY.Text = $"{_scanStart.X}, {_scanStart.Y} -> {x}, {y}";
                    _mode = CaptureMode.None;
                    UpdateStatus("Зона сканирования сохранена");
                    break;
            }
        }

        private const int WH_MOUSE_LL = 14;
        private const int WM_LBUTTONDOWN = 0x0201;

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr SetHook(LowLevelMouseProc proc)
        {
            IntPtr moduleHandle = GetModuleHandle(null);
            return SetWindowsHookEx(WH_MOUSE_LL, proc, moduleHandle, 0);
        }

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private struct POINT
        {
            public int x;
            public int y;
        }

        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private enum CaptureMode
        {
            None,
            Search,
            RedemptionPrices,
            DownArrow,
            Ok,
            Scan_First,
            Scan_Second
        }
    }
}