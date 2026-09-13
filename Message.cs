using System;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text;
using System.Threading;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Avalonia.Controls;

namespace TheVoid;

public class Message(string type, string sender, string data)
{
    public string Type => type;
    public string Sender => sender;
    public string Data => data;
}

public class MessageHandler(string name, TextBox receivedMessagesBox)
{
    private readonly Uri uri = new("wss://the-void.cc");
    private readonly Queue<string> messageQueue = new();

    private string username => name;
    private TextBox receivedBox => receivedMessagesBox;

    public string Username {get {return username;}}

    public async void MessageLoop()
    {
        ClientWebSocket ws = new();

        // Continue trying to connect if unable
        while (ws.State != WebSocketState.Open)
        {
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

        // Does receiving and sending without locking out one of them
        var receiveTask = ReceiveMessages(ws);
        var sendTask = SendMessages(ws);

        await Task.WhenAll(receiveTask, sendTask);
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

            await Task.Delay(10);
        }
    }

    private async Task ReceiveMessages(ClientWebSocket ws)
    {
        var receiveBuffer = new byte[1024];
        while (ws.State == WebSocketState.Open)
        {
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
                    receivedBox.Text += jsonMessage.Sender + '\n' + jsonMessage.Data + "\n\n";
                }
            }
        }
    }
    
    
    private string ChatToJson(string message)
    {
        Message newMessage = new("chat", username, message);

        string jsonString = JsonSerializer.Serialize(newMessage);

        return jsonString;
    }
}