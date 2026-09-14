using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;

namespace TheVoid;

public partial class MainWindow : Window
{
    private MessageHandler? messageHandler;
    public MainWindow()
    {
        InitializeComponent();

        MessageInput.AddHandler(
            KeyDownEvent,  // Detects key down event
            MessageInput_KeyDown,  // Function to call when event is fired
            RoutingStrategies.Tunnel,  // Sends event before TextBox processes it
            true  // Allows handled events to be processed (Needed to work for some reason)
        );

        MessageBox.AddHandler(
            KeyDownEvent,
            MessageBox_KeyDown,
            RoutingStrategies.Tunnel,
            true
        );

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
                messageHandler = new(NameBox.Text, ColourPicker.Color.ToString(), MessageBox, ConnectionText);
                messageHandler.MessageLoop();

                NameBox.IsEnabled = false;
                NameBox.IsVisible = false;

                ColourPicker.IsEnabled = false;
                ColourPicker.IsVisible = false;

                MessageBox.IsEnabled = true;
                MessageBox.IsVisible = true;

                MessageInput.IsEnabled = true;
                MessageInput.IsVisible = true;

                ConnectionText.IsVisible = true;
            }
        }
    }

    private void ColourPicker_ColourChanged(object? sender, ColorChangedEventArgs e)
    {
        NameBox.Foreground = SolidColorBrush.Parse(e.NewColor.ToString());
    }

    private void MessageInput_KeyDown(object? sender, KeyEventArgs e)
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
        if (!string.IsNullOrEmpty(MessageInput.Text) && !string.IsNullOrWhiteSpace(MessageInput.Text))
        {
            messageHandler?.SendChatMessage(MessageInput.Text);

            TextBlock messageBlock = new() {TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.DetectFromContent};
            
            if (messageHandler?.Username == messageHandler?.PreviousSender)
            {
                messageBlock?.Inlines?.Add(new Run(MessageInput.Text));
            }
            else
            {
                var localTime = DateTime.Now;
                string timeString = $"{localTime.Day}-{localTime.Month}-{localTime.Year} {localTime.Hour}:{(localTime.Minute < 10 ? $"0{localTime.Minute}" : localTime.Minute)}";

                Run nameFormatting = new($"{messageHandler?.Username} (You) {timeString}\n") {FontWeight = FontWeight.Bold, Foreground = SolidColorBrush.Parse(messageHandler!.UsernameColour)};
                messageBlock?.Inlines?.Add(nameFormatting);
                messageBlock?.Inlines?.Add(MessageInput.Text);
                messageHandler?.PreviousSender = messageHandler.Username;

                int itemCount = MessageBox.ItemCount;

                if (itemCount > 0)
                {
                    var textBlock = (TextBlock?) MessageBox.Items[itemCount - 1];
                    textBlock?.Inlines?.Add(new Run("\n"));
                }
            }
                

            MessageBox.Items.Add(messageBlock);
            MessageInput.Clear();

            ScrollToBottom();
        }
    }

    private void MessageBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Down)
        {
            return;
        }

        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            return;
        }

        e.Handled = true;
        ScrollToBottom();
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
        int lineCount = MessageBox.ItemCount;
            
        if (lineCount > 0)
        {
            var bottomItem = MessageBox.Items[lineCount - 1];

            if (bottomItem is not null)
            {
                MessageBox.ScrollIntoView(bottomItem);
            }
        }
    }
}