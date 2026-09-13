using System;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text;
using System.Threading;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.VisualTree;
using System.Linq;
using Avalonia.Media;

namespace TheVoid;

public class Message(string type, string sender, string data)
{
    public string Type => type;
    public string Sender => sender;
    public string Data => data;
}

public class MessageHandler(string name, ListBox receivedMessagesBox, TextBlock isConnectedText)
{
    private readonly Uri uri = new("wss://the-void.cc");
    private readonly Queue<string> messageQueue = new();

    private string username => name;
    private ListBox messageBox => receivedMessagesBox;
    private TextBlock connectionText => isConnectedText;

    private string previousSender = "";

    public string Username { get {return username;} }
    public string PreviousSender { get {return previousSender;} set {previousSender = value;} }

    public async void MessageLoop()
    {
        while (true)
        {
            ClientWebSocket ws = new();
            ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);

            // Continue trying to connect if unable
            while (ws.State != WebSocketState.Open)
            {
                SetConnectionText(false);
                try
                {
                    ws.Dispose();
                    ws = new();
                    await ws.ConnectAsync(uri, default);
                } 
                catch (WebSocketException)
                {
                    await Task.Delay(1000);
                }
            }

            SetConnectionText(true);

            // Does receiving and sending without locking out one of them
            var receiveTask = ReceiveMessages(ws);
            var sendTask = SendMessages(ws);

            await Task.WhenAll(receiveTask, sendTask);
        }
    }

    public void SendChatMessage(string chatMessage)
    {
        messageQueue.Enqueue(ChatToJson(chatMessage));
    }

    private async Task SendMessages(ClientWebSocket ws)
    {
        while (ws.State == WebSocketState.Open)
        {
            // Send message to server
            int messageQueueSize = messageQueue.Count;
            for (int i = 0; i < messageQueueSize; i++)
            {
                var buffer = Encoding.UTF8.GetBytes(messageQueue.Dequeue());
                await ws.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);     
            }

            if (ws.State != WebSocketState.Open)
            {
                return;
            }

            await Task.Delay(10);
        }
    }

    private async Task ReceiveMessages(ClientWebSocket ws)
    {
        var receiveBuffer = new byte[1024];
        while (ws.State == WebSocketState.Open)
        {
            try {
                // Listen for messages from the server
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Console.WriteLine("Server closed the connection.");
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }
                else
                {
                    // Makes sure the whole message is received
                    MemoryStream byteMessage = new();
                    byteMessage.Write(receiveBuffer, 0, result.Count);

                    while (!result.EndOfMessage)
                    {
                        result = await ws.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);
                        byteMessage.Write(receiveBuffer, 0, result.Count);
                    }

                    string receivedMessage = Encoding.UTF8.GetString(byteMessage.ToArray(), 0, (int) byteMessage.Length);
                    Message? jsonMessage = JsonSerializer.Deserialize<Message>(receivedMessage);

                    if (jsonMessage is not null)
                    {
                        if (jsonMessage.Type == "chat")
                        {
                            // Scroll if you're at bottom
                            bool shouldScrollDown = false;
                            ScrollViewer? messageBoxScroll = messageBox.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
                            if (messageBoxScroll is not null)
                            {
                                if (messageBoxScroll.Offset.Y == messageBoxScroll.ScrollBarMaximum.Y)
                                {
                                    shouldScrollDown = true;
                                }
                            }

                            string messageText; //username == previousSender ? jsonMessage.Data : jsonMessage.Sender + " (You)\n" + jsonMessage.Data;

                            if (jsonMessage.Sender == previousSender)
                            {
                                messageText = jsonMessage.Data;
                            }
                            else
                            {
                                messageText = jsonMessage.Sender + "\n" + jsonMessage.Data;
                                previousSender = jsonMessage.Sender;

                                int itemCount = messageBox.ItemCount;

                                if (itemCount > 0)
                                {
                                    messageBox.Items[itemCount - 1] += "\n";
                                }
                            }

                            messageBox.Items.Add(messageText);

                            if (shouldScrollDown)
                            {
                                messageBoxScroll?.ScrollToEnd();
                            }
                        }
                    }
                }
            }
            catch (WebSocketException)
            {
                return;
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }
    
    
    private string ChatToJson(string message)
    {
        Message newMessage = new("chat", username, message);

        string jsonString = JsonSerializer.Serialize(newMessage);

        return jsonString;
    }

    private void SetConnectionText(bool connected)
    {
        if (connected)
        {
            connectionText.Text = "Connected";
            connectionText.Foreground = SolidColorBrush.Parse("#33CC33");
            messageBox.Foreground = SolidColorBrush.Parse("#EEEEEE");
        }
        else
        {
            connectionText.Text = "Attempting to Connect...";
            connectionText.Foreground = SolidColorBrush.Parse("#CC3333");

            int itemCount = messageBox.ItemCount;
        }
    }
}