﻿using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace WinLocker
{
    public static class InputTimer
    {
        public static TimeSpan GetInputIdleTime()
        {
            var plii = new NativeMethods.LastInputInfo();
            plii.cbSize = (UInt32) Marshal.SizeOf(plii);

            if (NativeMethods.GetLastInputInfo(ref plii))
            {
                return TimeSpan.FromMilliseconds(NativeMethods.GetTickCount() - plii.dwTime);
            }
            else
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        public static DateTimeOffset GetLastInputTime()
        {
            return DateTimeOffset.Now.Subtract(GetInputIdleTime());
        }

        private static class NativeMethods
        {
            public struct LastInputInfo
            {
                public UInt32 cbSize;
                public UInt32 dwTime;
            }

            [DllImport("user32.dll")]
            public static extern bool GetLastInputInfo(ref LastInputInfo plii);

            [DllImport("kernel32.dll")]
            public static extern uint GetTickCount();
        }
    }
}
