using Microsoft.Win32;
using System;
using System.IO;
using System.Windows.Forms;

namespace WinLocker
{
    public enum LockerState
    {
        Active,
        Inactive,
        StandBy
    }

    class Common
    {
        public static string ToTimeRange(int minutes)
        {
            var ret = "";

            if (minutes <= 0)
            {
                return "0m";
            }

            if (minutes >= 60)
            {
                var hours = minutes / 60;
                ret += $"{hours}h ";
                minutes -= hours * 60;
            }

            if (minutes > 0)
            {
                ret += $"{minutes}m";
            }

            return ret.TrimEnd();
        }


        public static bool AutoStartEnabled()
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", false);
            var startApp = rk.GetValue(nameof(WinLocker), "") as string;

            var res = string.Equals(startApp, Application.ExecutablePath);
            return res;
        }

        public static void ToggleAutoStart()
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if (AutoStartEnabled())
            {
                rk.DeleteValue(nameof(WinLocker), false);
            }
            else
            {
                rk.SetValue(nameof(WinLocker), Application.ExecutablePath);
            }
        }

        public static void SaveSettings(int idleTimeMinutes)
        {
            try
            {
                string filePath = Path.GetDirectoryName(Application.ExecutablePath) + "\\settings";
                using (var file = new StreamWriter(filePath, false))
                {
                    file.Write(idleTimeMinutes.ToString());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception while saving settings {0}", e.ToString());
            }
        }

        public static void LoadSettings(out int idleTimeMinutes)
        {
            try
            {
                string filePath = Path.GetDirectoryName(Application.ExecutablePath) + "\\settings";
                using (var file = new StreamReader(filePath, false))
                {
                    var line = file.ReadLine();
                    idleTimeMinutes = int.Parse(line);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception while loading settings {0}", e.ToString());
                idleTimeMinutes = 0;
            }
        }
    }
}
