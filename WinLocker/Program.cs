using System.Windows.Forms;

namespace WinLocker
{
    static class Program
    {
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (var notificationIcon = new NotificationIcon())
            {
                Application.Run();
            }
        }
    }
}
