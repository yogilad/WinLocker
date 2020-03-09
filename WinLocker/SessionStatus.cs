using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace WinLocker
{
    class SessionStatus
    {        
        public bool IsLocked { get; private set; }

        public SessionStatus()
        {
            IsLocked = false;
            SystemEvents.SessionSwitch += new SessionSwitchEventHandler(SystemEvents_SessionSwitch);
        }

        void SystemEvents_SessionSwitch(object sender, Microsoft.Win32.SessionSwitchEventArgs e)
        {
            if (e.Reason == SessionSwitchReason.SessionLock)
            {
                IsLocked = true;
            }
            else if (e.Reason == SessionSwitchReason.SessionUnlock)
            {
                IsLocked = false;
            }
        }

        [DllImport("user32.dll")]
        public static extern bool LockWorkStation();
    }
}
