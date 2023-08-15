using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Gma.System.MouseKeyHook;

namespace GameOverlayApp
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new OverlayForm());
        }
    }

    class OverlayForm : Form
    {
        private const int UPDATE_INTERVAL = 1000;
        private Label systemInfoLabel;
        private NotifyIcon trayIcon;
        private IKeyboardMouseEvents m_GlobalHook;

        const int GWL_EXSTYLE = -20;
        const int WS_EX_TRANSPARENT = 0x20;
        const int WS_EX_LAYERED = 0x80000;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        public OverlayForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.Black;
            this.Opacity = 0.6;
            this.TopMost = true;
            this.ShowInTaskbar = false;

            // System tray icon setup
            trayIcon = new NotifyIcon();
            trayIcon.Text = "Game Overlay App";
            trayIcon.Icon = new Icon(SystemIcons.Application, 40, 40);
            trayIcon.Visible = true;

            // Add a context menu to the tray icon for exit
            trayIcon.ContextMenuStrip = new ContextMenuStrip();
            trayIcon.ContextMenuStrip.Items.Add("Exit", null, OnExitClicked);

            systemInfoLabel = new Label();
            systemInfoLabel.ForeColor = Color.White;
            systemInfoLabel.AutoSize = true;
            systemInfoLabel.Font = new Font(systemInfoLabel.Font.FontFamily, systemInfoLabel.Font.Size * 2); // Double the font size
            this.Controls.Add(systemInfoLabel);

            // Make the form click-through
            int initialStyle = GetWindowLong(this.Handle, GWL_EXSTYLE);
            SetWindowLong(this.Handle, GWL_EXSTYLE, initialStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED);

            Timer updateTimer = new Timer();
            updateTimer.Interval = UPDATE_INTERVAL;
            updateTimer.Tick += UpdateTimer_Tick;
            updateTimer.Start();

            UpdateOverlaySizeAndPosition();

            Subscribe();
        }

        private void Subscribe()
        {
            m_GlobalHook = Hook.GlobalEvents();
            m_GlobalHook.KeyDown += GlobalHookKeyPress;
        }

        private void GlobalHookKeyPress(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.L && e.Modifiers == Keys.LWin) // or e.Modifiers == Keys.RWin for right Windows key
            {
                if (this.Visible)
                    this.Hide();
                else
                    this.Show();

                e.Handled = true; // This will prevent the workstation from getting locked.
            }
        }

        private void OnExitClicked(object sender, EventArgs e)
        {
            Unsubscribe();
            Application.Exit();
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            string systemInfo = GetSystemInfo();
            systemInfoLabel.Text = systemInfo;

            UpdateOverlaySizeAndPosition();
            this.Invalidate();
        }

        private string GetSystemInfo()
        {
            // Get current time in 12-hour format without seconds and with AM/PM
            string time = DateTime.Now.ToString("hh:mm tt");

            string batteryStatus = SystemInformation.PowerStatus.BatteryLifePercent.ToString("P0");
            
            // Check if the device is charging
            if (SystemInformation.PowerStatus.BatteryChargeStatus.HasFlag(BatteryChargeStatus.Charging))
            {
                return String.Format("{0} | Battery: {1} (Charging)", time, batteryStatus);
            }
            else
            {
                int batteryLifeInSeconds = SystemInformation.PowerStatus.BatteryLifeRemaining;
                int batteryHours = batteryLifeInSeconds / 3600; // Convert seconds to hours
                int batteryMinutes = (batteryLifeInSeconds % 3600) / 60; // Get the remaining minutes

                return String.Format("{0} | Battery: {1} ({2} hrs {3} mins left)", time, batteryStatus, batteryHours, batteryMinutes);
            }
        }

        private void UpdateOverlaySizeAndPosition()
        {
            Size textSize = TextRenderer.MeasureText(systemInfoLabel.Text, systemInfoLabel.Font);
            this.Size = textSize + new Size(10, 5);  // Add a small margin
            this.Location = new Point(10, 10);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.FillRectangle(new SolidBrush(Color.Black), this.ClientRectangle);
        }

        // Ensure that the system tray icon and global hook are disposed when the application is closed.
        protected override void OnClosed(EventArgs e)
        {
            trayIcon.Dispose();
            Unsubscribe();
            base.OnClosed(e);
        }

        public void Unsubscribe()
        {
            m_GlobalHook.KeyDown -= GlobalHookKeyPress;
            m_GlobalHook.Dispose();
        }
    }

    class PerformanceInfo
    {
        [DllImport("psapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetPerformanceInfo([Out] out PERFORMANCE_INFORMATION PerformanceInformation, [In] int Size);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PERFORMANCE_INFORMATION
    {
        public int Size;
        public IntPtr CommitTotal;
        public IntPtr CommitLimit;
        public IntPtr CommitPeak;
        public IntPtr PhysicalTotal;
        public IntPtr PhysicalAvailable;
        public IntPtr SystemCache;
        public IntPtr KernelTotal;
        public IntPtr KernelPaged;
        public IntPtr KernelNonPaged;
        public IntPtr PageSize;
        public int HandlesCount;
        public int ProcessCount;
        public int ThreadCount;
    }
}
