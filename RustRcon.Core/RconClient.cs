using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RustRcon.Core
{
    public class RconClient
    {
        private ClientWebSocket? _webSocket;
        private bool _isConnected = false;
        private int _requestId = 0;
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly Dictionary<int, TaskCompletionSource<string>> _pendingRequests = new Dictionary<int, TaskCompletionSource<string>>();

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
                await Task.Delay(100);
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
                int requestId = Interlocked.Increment(ref _requestId);

                var request = new RconRequest
                {
                    Identifier = requestId,
                    Message = command,
                    Name = "WebRCON"
                };

                var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
                _pendingRequests[requestId] = tcs;

                var json = JsonConvert.SerializeObject(request);
                var data = Encoding.UTF8.GetBytes(json);

                await _webSocket.SendAsync(
                    new ArraySegment<byte>(data),
                    WebSocketMessageType.Text,
                    true,
                    _cancellationTokenSource?.Token ?? CancellationToken.None);

                using (var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                {
                    using (timeoutCts.Token.Register(() => tcs.TrySetCanceled()))
                    {
                        try
                        {
                            return await tcs.Task.ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            throw new TimeoutException("Command timeout");
                        }
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
                                    tcs.TrySetResult(response.Message ?? string.Empty);
                                }

                                if (!string.IsNullOrEmpty(response.Message))
                                {
                                    MessageReceived?.Invoke(this, response.Message);
                                }
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
        public string Message { get; set; } = string.Empty;

        [JsonProperty("Name")]
        public string Name { get; set; } = string.Empty;
    }

    public class RconResponse
    {
        [JsonProperty("Identifier")]
        public int Identifier { get; set; }

        [JsonProperty("Message")]
        public string Message { get; set; } = string.Empty;

        [JsonProperty("Type")]
        public string Type { get; set; } = string.Empty;

        [JsonProperty("Stacktrace")]
        public string? Stacktrace { get; set; }
    }
}


