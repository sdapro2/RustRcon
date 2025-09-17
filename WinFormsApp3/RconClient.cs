using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RustRcon
{
    public class RconClient
    {
        private ClientWebSocket? _webSocket;
        private bool _isConnected = false;
        private int _requestId = 0;
        private CancellationTokenSource? _cancellationTokenSource;
        private Dictionary<int, TaskCompletionSource<string>> _pendingRequests = new Dictionary<int, TaskCompletionSource<string>>();

        public bool IsConnected => _isConnected;

        public event EventHandler<string>? MessageReceived;
        public event EventHandler<string>? ErrorOccurred;

        public async Task<bool> ConnectAsync(string serverIp, int port, string password)
        {
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _webSocket = new ClientWebSocket();
                
                string uri = $"ws://{serverIp}:{port}/{password}";
                await _webSocket.ConnectAsync(new Uri(uri), _cancellationTokenSource.Token);
                
                _isConnected = true;

                _ = Task.Run(ReceiveMessagesAsync);

                await Task.Delay(100); // Небольшая задержка для установки соединения
                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Ошибка подключения: {ex.Message}");
                return false;
            }
        }

        public async Task<string> SendCommandAsync(string command)
        {
            if (!_isConnected || _webSocket == null)
            {
                throw new InvalidOperationException("Не подключен к серверу");
            }

            try
            {
                var requestId = ++_requestId;
                var request = new RconRequest
                {
                    Identifier = requestId,
                    Message = command,
                    Name = "WebRCON"
                };

                var tcs = new TaskCompletionSource<string>();
                _pendingRequests[requestId] = tcs;

                var json = JsonConvert.SerializeObject(request);
                var data = Encoding.UTF8.GetBytes(json);

                await _webSocket.SendAsync(
                    new ArraySegment<byte>(data), 
                    WebSocketMessageType.Text, 
                    true, 
                    _cancellationTokenSource?.Token ?? CancellationToken.None);

                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                {
                    cts.Token.Register(() => tcs.TrySetCanceled());
                    try
                    {
                        return await tcs.Task;
                    }
                    catch (OperationCanceledException)
                    {
                        throw new TimeoutException("Command timeout");
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Ошибка отправки команды: {ex.Message}");
                throw;
            }
        }

        private async Task ReceiveMessagesAsync()
        {
            if (_webSocket == null) return;

            var buffer = new byte[4096];
            try
            {
                while (_isConnected && _webSocket.State == WebSocketState.Open)
                {
                    var result = await _webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer), 
                        _cancellationTokenSource?.Token ?? CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        
                        try
                        {
                            var response = JsonConvert.DeserializeObject<RconResponse>(message);
                            
                            if (response != null)
                            {
                                if (_pendingRequests.TryGetValue(response.Identifier, out var tcs))
                                {
                                    _pendingRequests.Remove(response.Identifier);
                                    tcs.TrySetResult(response.Message);
                                }
                                
                                MessageReceived?.Invoke(this, response.Message);
                            }
                        }
                        catch (JsonException)
                        {
                            MessageReceived?.Invoke(this, message);
                        }
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _isConnected = false;
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Ошибка получения сообщений: {ex.Message}");
            }
        }

        public void Disconnect()
        {
            _isConnected = false;
            _cancellationTokenSource?.Cancel();
            
            foreach (var tcs in _pendingRequests.Values)
            {
                tcs.TrySetCanceled();
            }
            _pendingRequests.Clear();
            
            _webSocket?.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnect", CancellationToken.None);
            _webSocket?.Dispose();
            _webSocket = null;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    public class RconRequest
    {
        [JsonProperty("Identifier")]
        public int Identifier { get; set; }

        [JsonProperty("Message")]
        public string Message { get; set; } = "";

        [JsonProperty("Name")]
        public string Name { get; set; } = "";
    }

    public class RconResponse
    {
        [JsonProperty("Identifier")]
        public int Identifier { get; set; }

        [JsonProperty("Message")]
        public string Message { get; set; } = "";

        [JsonProperty("Type")]
        public string Type { get; set; } = "";

        [JsonProperty("Stacktrace")]
        public string? Stacktrace { get; set; }
    }
}
