using Iris;
public class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.Run(new MainWindow(args));
        return 0;
    }
}
