using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace WinLocker
{
    public class NotificationIcon : IDisposable
    {
        private System.ComponentModel.IContainer m_components;
        private NotifyIcon m_notifyIcon;
        private ContextMenu m_contextMenu;
        private Locker m_locker;
        private MenuItem[] m_contextMenuItems;
        private readonly int[] m_suspendTimes = { 15, 30, 45, 60, 90, 120, 150, 180 };
        private readonly int[] m_lockTimes = { 3, 4, 5, 10, 15, 20, 30 };


        const int stopStartMenuIndex = 0;
        const int toggleAutoStartMenuIndex = 1;
        const int IdleTimeMenuItem = 2;
        const int suspendMenuItem = 3;
        const int aboutMenuItemIndex = 4;
        const int exitMenuItem = 5;

        public NotificationIcon()
        {
            m_components = new System.ComponentModel.Container();

            // Initialize the idle time menu
            var idleMenuItems = new List<MenuItem>();
            for (int i = 0; i < m_lockTimes.Length; i++)
            {
                var item = new MenuItem(Common.ToTimeRange(m_lockTimes[i]));

                item.Click += new System.EventHandler(IdleMenu_Click);
                idleMenuItems.Add(item);
            }

            // Initialize the suspension menu
            var suspendMenuItems = new List<MenuItem>();
            for (int i = 0; i < m_suspendTimes.Length; i++)
            {
                var item = new MenuItem(Common.ToTimeRange(m_suspendTimes[i]));

                item.Click += new System.EventHandler(SuspendMenu_Click);
                suspendMenuItems.Add(item);
            }

            // Initialize the context manu
            m_contextMenuItems = new MenuItem[6];

            m_contextMenuItems[stopStartMenuIndex] = new MenuItem(); // Enable / Disable is set by UpdateUI()
            m_contextMenuItems[stopStartMenuIndex].Click += new System.EventHandler(ContextMenu_Click);

            m_contextMenuItems[suspendMenuItem] = new MenuItem("Suspend for");
            m_contextMenuItems[suspendMenuItem].MenuItems.AddRange(suspendMenuItems.ToArray());

            m_contextMenuItems[IdleTimeMenuItem] = new MenuItem("Lock after");
            m_contextMenuItems[IdleTimeMenuItem].MenuItems.AddRange(idleMenuItems.ToArray());

            m_contextMenuItems[toggleAutoStartMenuIndex] = new MenuItem(); // "Enable Auto Start is set by UpdateUI()
            m_contextMenuItems[toggleAutoStartMenuIndex].Text = "Auto start";
            m_contextMenuItems[toggleAutoStartMenuIndex].Click += new System.EventHandler(ContextMenu_Click);

            m_contextMenuItems[aboutMenuItemIndex] = new MenuItem("About");
            //m_contextMenuItems[aboutMenuItemIndex].MenuItems.Add(new MenuItem("Developed by Yochai Gilad (yochaig@gmail.com)"));
            m_contextMenuItems[aboutMenuItemIndex].Click += new System.EventHandler(ContextMenu_Click);

            m_contextMenuItems[exitMenuItem] = new MenuItem("Exit");
            m_contextMenuItems[exitMenuItem].Click += new System.EventHandler(ContextMenu_Click);

            // Set the context menu
            m_contextMenu = new ContextMenu();
            m_contextMenu.MenuItems.AddRange(m_contextMenuItems);

            // Create the NotifyIcon.
            this.m_notifyIcon = new System.Windows.Forms.NotifyIcon(this.m_components);

            // The ContextMenu property sets the menu that will
            // appear when the systray icon is right clicked.
            m_notifyIcon.ContextMenu = this.m_contextMenu;

            // Handle the DoubleClick event to activate the form.
            m_notifyIcon.DoubleClick += new System.EventHandler(this.NotifyIcon_DoubleClick);

            // Run the locker
            int idleMinutes;

            Common.LoadSettings(out idleMinutes);
            m_locker = new Locker();
            m_locker.LockTimeSeconds = idleMinutes * 60;
            m_locker.StateChangedEvent += LockerStateChangedEventHandler;
            m_locker.AboutToLockEvent += AboutToLockEventHandler;
            m_locker.RunAsync();

            // Dispaly the notification icon
            UpdateUI();
            m_notifyIcon.Visible = true;
        }

        public void Dispose() => Dispose(true);

        public void Dispose(bool disposing)
        {
            // Clean up any components being used.
            if (disposing)
                if (m_components != null)
                    m_components.Dispose();
        }

        private void NotifyIcon_DoubleClick(object Sender, EventArgs e)
        {
            m_locker.ToggleState(60);
        }

        private void ContextMenu_Click(object Sender, EventArgs e)
        {
            var menu = Sender as MenuItem;

            switch (menu.Index)
            {
                case stopStartMenuIndex:
                    Console.WriteLine("Toggle Stop Start");
                    m_locker.ToggleState();
                    break;

                case toggleAutoStartMenuIndex:
                    Console.WriteLine("Toggle Auto Start");
                    Common.ToggleAutoStart();
                    UpdateUI();
                    break;

                case exitMenuItem:
                    Console.WriteLine("Exit Application");
                    Application.Exit();
                    break;

                case aboutMenuItemIndex:
                    var form = new AboutForm();
                    form.Show();
                    break;
            }
        }
        private void SuspendMenu_Click(object Sender, EventArgs e)
        {
            var menu = Sender as MenuItem;
            m_locker.ToggleState(m_suspendTimes[menu.Index]);
        }

        private void IdleMenu_Click(object Sender, EventArgs e)
        {
            var menu = Sender as MenuItem;
            m_locker.LockTimeSeconds = m_lockTimes[menu.Index] * 60;
            Common.SaveSettings(m_locker.LockTimeSeconds / 60);
            UpdateUI();
        }

        private void UpdateUI()
        {
            m_notifyIcon.Icon = ResourceLoader.GetIcon(m_locker.State);
            m_contextMenuItems[toggleAutoStartMenuIndex].Checked = Common.AutoStartEnabled();

            switch (m_locker.State)
            {
                case LockerState.Active:
                    m_contextMenuItems[stopStartMenuIndex].Text = "Disable";
                    var text = $"WinLocker is Active ({Common.ToTimeRange(m_locker.LockTimeSeconds / 60)})\nDouble Click to Suspend for 1h";
                    m_notifyIcon.Text = text;
                    break;

                case LockerState.Inactive:
                    m_contextMenuItems[stopStartMenuIndex].Text = "Enable";
                    m_notifyIcon.Text = "WinLocker is Inactive\nDouble Click to Enable";
                    break;

                case LockerState.StandBy:
                    m_contextMenuItems[stopStartMenuIndex].Text = "Enable";
                    m_notifyIcon.Text = $"WinLocker is Suspended Util {m_locker.SuspendTime.ToString("HH:mm")}\nDouble Click to Enable";
                    break;
            }

            for (int i = 0; i < m_lockTimes.Length; i++)
            {
                m_contextMenuItems[IdleTimeMenuItem].MenuItems[i].Checked = m_lockTimes[i] == m_locker.LockTimeSeconds / 60;
            }
        }

        public void LockerStateChangedEventHandler(object Sender, EventArgs args)
        {
            UpdateUI();
        }

        public void AboutToLockEventHandler(object Sender, EventArgs args)
        {
            var evArgs = args as Locker.AboutToLockEventArgs;

            if (evArgs.ShowMessage)
            {
                m_notifyIcon.BalloonTipTitle = "WinLocker";
                m_notifyIcon.BalloonTipText = $"Desktop will lock in {evArgs.Seconds} seconds!";
                m_notifyIcon.BalloonTipIcon = ToolTipIcon.None;
                m_notifyIcon.ShowBalloonTip(evArgs.Seconds * 1000);
            }
            else
            {
                // This is a hack, but there does not seem to be another way to progrematically dimiss the Balloon Tip
                m_notifyIcon.Visible = false;
                m_notifyIcon.Visible = true;
            }
        }
    }
}