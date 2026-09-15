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
        MaximizeButton.Content = "☐";
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
                messageHandler = new(NameBox.Text.Trim(), ColourPicker.Color.ToString(), MessageBox, ConnectionText);
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
            string trimmedText = MessageInput.Text.Trim();

            messageHandler?.SendChatMessage(trimmedText);

            TextBlock messageBlock = new() {TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.DetectFromContent};
            
            if (messageHandler?.Username == messageHandler?.PreviousSender)
            {
                messageBlock?.Inlines?.Add(new Run(trimmedText));
            }
            else
            {
                var localTime = DateTime.Now;
                int hour = localTime.Hour;
                string daySection = "AM";

                if (localTime.Hour > 12)
                {
                    hour -= 12;
                    daySection = "PM";
                }

                string timeString = $"{localTime.Day}-{localTime.Month}-{localTime.Year} {hour}:{(localTime.Minute < 10 ? $"0{localTime.Minute}" : localTime.Minute)}{daySection}";

                Run nameFormatting = new($"{messageHandler?.Username} (You) ") {FontWeight = FontWeight.Bold, Foreground = SolidColorBrush.Parse(messageHandler!.UsernameColour)};
                Run timeFormatting = new($"{timeString}\n") {Foreground = SolidColorBrush.Parse("#444549")};
                messageBlock?.Inlines?.Add(nameFormatting);
                messageBlock?.Inlines?.Add(timeFormatting);
                messageBlock?.Inlines?.Add(trimmedText);
                messageHandler?.PreviousSender = messageHandler.Username;

                if (MessageBox.ItemCount > 0)
                {
                    var textBlock = (TextBlock?) MessageBox.Items[^1];
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
        if (MessageBox.ItemCount > 0)
        {
            MessageBox.Items.Add("");
            var bottomItem = MessageBox.Items[^1];

            if (bottomItem is not null)
            {
                MessageBox.ScrollIntoView(bottomItem);
                MessageBox.Items.RemoveAt(MessageBox.ItemCount - 1);
            }
        }
    }
}