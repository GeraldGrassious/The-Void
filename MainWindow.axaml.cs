using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace TheVoid;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        MessageBox.AddHandler(
            InputElement.KeyDownEvent,  // Detects key down event
            MessageBox_KeyDown,  // Function to call when event is fired
            RoutingStrategies.Tunnel,  // Sends event before TextBox processes it
            true);  // Allows handled events to be processed (Needed to work for some reason)
    }

    private void MessageBox_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            return;
        }

        e.Handled = true;
        // Send Message here
    }
}