namespace Iris.UserInterface
{
    internal partial class InputSettingsDialog : Form
    {
        internal InputSettingsDialog(ref Dictionary<Keyboard.Key, Common.System.Key> keyboardMapping, ref Dictionary<XboxController.Button, Common.System.Key> xboxControllerMapping)
        {
            InitializeComponent();
        }
    }
}
