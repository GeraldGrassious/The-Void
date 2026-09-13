using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace TheVoid;

public partial class MainWindow : Window
{
    private MessageHandler? messageHandler;
    public MainWindow()
    {
        InitializeComponent();

        MessageBox.AddHandler(
            InputElement.KeyDownEvent,  // Detects key down event
            MessageBox_KeyDown,  // Function to call when event is fired
            RoutingStrategies.Tunnel,  // Sends event before TextBox processes it
            true);  // Allows handled events to be processed (Needed to work for some reason)

        messageHandler = null;

        // messageHandler.MessageLoop();
    }

    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void MinimizeButton_Clicked(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Clicked(object sender, RoutedEventArgs e)
    {
        var maximizeButton = (Button) sender;

        if (WindowState is WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
            maximizeButton.Content = "☐";
        }
        else
        {
            WindowState = WindowState.Maximized;
            maximizeButton.Content = "🗗";
        }
    }


    private void CloseButton_Clicked(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void NameBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (!string.IsNullOrEmpty(NameBox.Text) && !string.IsNullOrWhiteSpace(NameBox.Text))
            {  
                messageHandler = new(NameBox.Text, ReceivedBox);
                messageHandler.MessageLoop();

                NameBox.IsEnabled = false;
                NameBox.IsVisible = false;

                ReceivedBox.IsEnabled = true;
                ReceivedBox.IsVisible = true;

                MessageBox.IsEnabled = true;
                MessageBox.IsVisible = true;
            }
        }
    }

    private void MessageBox_KeyDown(object? sender, KeyEventArgs e)
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
        if (!string.IsNullOrEmpty(MessageBox.Text) && !string.IsNullOrWhiteSpace(MessageBox.Text))
        {
            messageHandler?.SendChatMessage(MessageBox.Text);
            ReceivedBox.Text += messageHandler?.Username + " (You)\n" + MessageBox.Text + "\n\n";
            MessageBox.Clear();

            ScrollToBottom();
        }
    }

    private void Window_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Down && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            ScrollToBottom();
        }

    }

    private void ScrollToBottom()
    {
        int lineCount = ReceivedBox.GetLineCount();
            
        if (lineCount > 0)
        {
            ReceivedBox.ScrollToLine(ReceivedBox.GetLineCount() - 1);
        }
    }
}