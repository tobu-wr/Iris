namespace Iris.UserInterface
{
    internal partial class KeyMappingDialog : Form
    {
        internal KeyMappingDialog(ref Dictionary<Keyboard.Key, Common.System.Key> keyboardMapping, ref Dictionary<XboxController.Button, Common.System.Key> xboxControllerMapping)
        {
            InitializeComponent();
        }
    }
}
