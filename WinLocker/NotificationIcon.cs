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

            m_contextMenuItems[0] = new MenuItem(); // Enable / Disable is set by UpdateUI()
            m_contextMenuItems[0].Click += new System.EventHandler(ContextMenu_Click);

            m_contextMenuItems[1] = new MenuItem("S&uspend for");
            m_contextMenuItems[1].MenuItems.AddRange(suspendMenuItems.ToArray());

            m_contextMenuItems[2] = new MenuItem("L&ock after");
            m_contextMenuItems[2].MenuItems.AddRange(idleMenuItems.ToArray());

            m_contextMenuItems[3] = new MenuItem(); // "Enable Auto Start is set by UpdateUI()
            m_contextMenuItems[3].Click += new System.EventHandler(ContextMenu_Click); 
            
            m_contextMenuItems[4] = new MenuItem("About");
            m_contextMenuItems[4].MenuItems.Add(new MenuItem("Developed by Yochai Gilad (yochaig@gmail.com)"));

            m_contextMenuItems[5] = new MenuItem("E&xit");
            m_contextMenuItems[5].Click += new System.EventHandler(ContextMenu_Click);

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
            m_locker.StateChanged += LockerStateChangedHandler;
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
                case 0:
                    Console.WriteLine("Toggle Enable Disable");
                    m_locker.ToggleState();
                    break; 

                case 3:
                    Console.WriteLine("Toggle Auto Start");
                    Common.ToggleAutoStart();
                    UpdateUI();
                    break;

                case 5:
                    Console.WriteLine("Exit Application");
                    Application.Exit();
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

            switch (m_locker.State)
            {
                case LockerState.Active:
                    m_contextMenuItems[0].Text = "Disa&ble";
                    var text = $"WinLocker is Active ({Common.ToTimeRange(m_locker.LockTimeSeconds / 60)})\nDouble Click to Suspend for 1h";
                    m_notifyIcon.Text = text;
                    break;

                case LockerState.Inactive:
                    m_contextMenuItems[0].Text = "Ena&ble";
                    m_notifyIcon.Text = "WinLocker is Inactive\nDouble Click to Enable";
                    break;

                case LockerState.StandBy:
                    m_contextMenuItems[0].Text = "Ena&ble";
                    m_notifyIcon.Text = $"WinLocker is Suspended Util {m_locker.SuspendTime.ToString("HH:mm")}\nDouble Click to Enable";
                    break;
            }

            if (Common.AutoStartEnabled())
            {
                m_contextMenuItems[3].Text = "Disable Auto Start";
            }
            else
            {
                m_contextMenuItems[3].Text = "Enable Auto Start";
            }

            for (int i = 0; i < m_lockTimes.Length; i++)
            {
                m_contextMenuItems[2].MenuItems[i].Checked = m_lockTimes[i] == m_locker.LockTimeSeconds / 60;
            }
        }

        public void LockerStateChangedHandler(object Sender, EventArgs args)
        {
            UpdateUI();
        }
    }
}