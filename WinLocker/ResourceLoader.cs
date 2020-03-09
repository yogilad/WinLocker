using System.Drawing;
using System.IO;
using System.Reflection;

namespace WinLocker
{
    class ResourceLoader
    {
        
        
        static readonly Icon m_clockIcon;
        static readonly Icon m_unlockedIcon;
        static readonly Icon m_lockedIcon;

        static ResourceLoader()
        {
            string style = "Pad3";

            m_clockIcon = new Icon(LoadResourceAsStream(style + "Clock.ico", "Icons"));
            m_unlockedIcon = new Icon(LoadResourceAsStream(style + "Unlocked.ico", "Icons")); 
            m_lockedIcon = new Icon(LoadResourceAsStream(style + "Locked.ico", "Icons"));
        }

        private static Stream LoadResourceAsStream(string resourceName, string folder = null, string name_space = nameof(WinLocker))
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string resourcePath = $"{name_space}.{folder}.{resourceName}";
            Stream stream = assembly.GetManifestResourceStream(resourcePath);

            return stream;
        }

        public static Icon GetIcon(LockerState state)
        {
            switch (state)
            {
                case LockerState.Active:
                    return m_lockedIcon;

                case LockerState.Inactive:
                    return m_unlockedIcon;

                case LockerState.StandBy:
                    return m_clockIcon;

                default:
                    return null;
            }
        }
    }
}
