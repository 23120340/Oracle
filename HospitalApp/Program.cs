using HospitalApp.Forms;

namespace HospitalApp;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.EnableVisualStyles();
        Application.Run(new LoginForm());
    }
}
