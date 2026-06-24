using HospitalApp.Forms;
using HospitalApp.Theme;

namespace HospitalApp;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Áp Montserrat làm font mặc định cho toàn ứng dụng
        Application.SetDefaultFont(UiTheme.Body());

        Application.Run(new LoginForm());
    }
}
