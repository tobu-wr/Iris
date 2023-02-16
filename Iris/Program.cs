public class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.Run(new Iris.UserInterface.MainWindow(args));
        return 0;
    }
}
