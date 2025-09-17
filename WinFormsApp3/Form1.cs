namespace RustRcon
{
    public partial class Form1 : Form
    {
        private RconClient? _rconClient;
        private List<string> _commandHistory = new List<string>();
        private int _historyIndex = -1;
        private ConnectDialog? _connectDialog;
        private List<PlayerStats> _playerStats = new List<PlayerStats>();
        private List<PlayerKill> _playerKills = new List<PlayerKill>();
        private List<OnlinePlayer> _onlinePlayers = new List<OnlinePlayer>();
        private System.Windows.Forms.Timer _onlineUpdateTimer;
        private bool _isUpdatingOnline = false;

        public Form1()
        {
            InitializeComponent();
            SetApplicationIcon();
            InitializeRconClient();
            InitializeOnlineUpdateTimer();
            SetupHighlights();
            SetupFilters();
            ApplyCustomTheme();
            LoadServerSettings();
        }

        private void SetApplicationIcon()
        {
            try
            {
                if (File.Exists("app.ico"))
                {
                    this.Icon = new Icon("app.ico");
                }
            }
            catch
            {
            }
        }

        private void ApplyCustomTheme()
        {
            this.BackColor = Color.Black;

            menuStrip.BackColor = Color.FromArgb(30, 30, 30);
            menuStrip.ForeColor = Color.White;

            statusStrip.BackColor = Color.FromArgb(30, 30, 30);
            statusStrip.ForeColor = Color.White;

            tabControl.BackColor = Color.Black;
            tabControl.ForeColor = Color.White;

            textBoxCommand.BackColor = Color.FromArgb(50, 50, 50);
            textBoxCommand.ForeColor = Color.White;
            textBoxCommand.BorderStyle = BorderStyle.FixedSingle;

            textBoxChatMessage.BackColor = Color.FromArgb(50, 50, 50);
            textBoxChatMessage.ForeColor = Color.White;
            textBoxChatMessage.BorderStyle = BorderStyle.FixedSingle;

            textBoxSearchLogs.BackColor = Color.FromArgb(50, 50, 50);
            textBoxSearchLogs.ForeColor = Color.White;
            textBoxSearchLogs.BorderStyle = BorderStyle.FixedSingle;

            textBoxFilterStats.BackColor = Color.FromArgb(50, 50, 50);
            textBoxFilterStats.ForeColor = Color.White;
            textBoxFilterStats.BorderStyle = BorderStyle.FixedSingle;

            textBoxPlayerSearch.BackColor = Color.FromArgb(50, 50, 50);
            textBoxPlayerSearch.ForeColor = Color.White;
            textBoxPlayerSearch.BorderStyle = BorderStyle.FixedSingle;

            listViewPlayers.BackColor = Color.FromArgb(30, 30, 30);
            listViewPlayers.ForeColor = Color.White;
            listViewPlayers.BorderStyle = BorderStyle.FixedSingle;

            listViewBans.BackColor = Color.FromArgb(30, 30, 30);
            listViewBans.ForeColor = Color.White;
            listViewBans.BorderStyle = BorderStyle.FixedSingle;

            listViewPlayerStats.BackColor = Color.FromArgb(30, 30, 30);
            listViewPlayerStats.ForeColor = Color.White;
            listViewPlayerStats.BorderStyle = BorderStyle.FixedSingle;

            listViewPlayerKills.BackColor = Color.FromArgb(30, 30, 30);
            listViewPlayerKills.ForeColor = Color.White;
            listViewPlayerKills.BorderStyle = BorderStyle.FixedSingle;

            flowLayoutPanelPlayers.BackColor = Color.FromArgb(30, 30, 30);

            groupBoxPlayers.BackColor = Color.Black;
            groupBoxPlayers.ForeColor = Color.White;

            labelSearchLogs.ForeColor = Color.White;
            labelFilterStats.ForeColor = Color.White;
            labelPlayerCount.ForeColor = Color.White;
            labelPlayerKills.ForeColor = Color.White;
            labelOnlineCount.ForeColor = Color.White;

            buttonSendCommand.BackColor = Color.FromArgb(60, 60, 60);
            buttonSendCommand.ForeColor = Color.White;
            buttonSendCommand.FlatStyle = FlatStyle.Flat;

            panelLeft.BackColor = Color.Black;
            panelConsoleControls.BackColor = Color.Black;
            panelBottom.BackColor = Color.Black;
        }

        private void InitializeRconClient()
        {
            _rconClient = new RconClient();
            _rconClient.MessageReceived += OnMessageReceived;
            _rconClient.ErrorOccurred += OnErrorOccurred;
        }

        private void InitializeOnlineUpdateTimer()
        {
            _onlineUpdateTimer = new System.Windows.Forms.Timer();
            _onlineUpdateTimer.Interval = 15000; // Обновляем каждые 15 секунд
            _onlineUpdateTimer.Tick += async (s, e) =>
            {
                if (_rconClient != null && _rconClient.IsConnected)
                {
                    await RefreshOnlinePlayers();
                }
            };
        }


        private void SetupHighlights()
        {
            listBoxHighlights.Items.AddRange(new string[]
            {
                "Player joined",
                "Player disconnected",
                "Player killed",
                "Chat message",
                "Error",
                "Warning"
            });
        }

        private void SetupFilters()
        {
            listBoxFilters.Items.AddRange(new string[]
            {
                "Show all",
                "Chat only",
                "Errors only",
                "Player events",
                "Server events"
            });
        }

        private void connectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_connectDialog == null || _connectDialog.IsDisposed)
            {
                _connectDialog = new ConnectDialog();
            }

            if (_connectDialog.ShowDialog() == DialogResult.OK)
            {
                ConnectToServer(_connectDialog.ServerIP, _connectDialog.Port, _connectDialog.Password);
            }
        }

        private void disconnectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DisconnectFromServer();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("RustRcon",
                "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async void ConnectToServer(string serverIP, int port, string password)
        {
            if (_rconClient == null) return;

            connectToolStripMenuItem.Enabled = false;
            disconnectToolStripMenuItem.Enabled = true;
            toolStripStatusLabelConnected.Text = "Connecting...";

            try
            {
                bool connected = await _rconClient.ConnectAsync(serverIP, port, password);
                if (connected)
                {
                    toolStripStatusLabelConnected.Text = "Connected";
                    buttonSendCommand.Enabled = true;
                    AddLog($"Connected to server {serverIP}:{port}");

                    await TestPlayerCommands();

                    RefreshPlayerStats();
                    await RefreshOnlinePlayers();

                    _onlineUpdateTimer.Start();

                    toolStripStatusLabelConnected.Text = $"Connected to {serverIP}:{port}";
                }
                else
                {
                    connectToolStripMenuItem.Enabled = true;
                    disconnectToolStripMenuItem.Enabled = false;
                    toolStripStatusLabelConnected.Text = "Disconnected";
                    AddLog("Failed to connect to server");
                }
            }
            catch (Exception ex)
            {
                connectToolStripMenuItem.Enabled = true;
                disconnectToolStripMenuItem.Enabled = false;
                toolStripStatusLabelConnected.Text = "Disconnected";
                AddLog($"Connection error: {ex.Message}");
            }
        }

        private void DisconnectFromServer()
        {
            _rconClient?.Disconnect();
            _onlineUpdateTimer?.Stop();
            connectToolStripMenuItem.Enabled = true;
            disconnectToolStripMenuItem.Enabled = false;
            buttonSendCommand.Enabled = false;
            toolStripStatusLabelConnected.Text = "Disconnected";
            AddLog("Disconnected from server");
        }

        private async void buttonSendCommand_Click(object sender, EventArgs e)
        {
            await SendCommand();
        }

        private async Task TestPlayerCommands()
        {
            if (_rconClient == null || !_rconClient.IsConnected)
            {
                return;
            }

            string[] testCommands = {
                "status",
                "version",
                "server.hostname",
                "server.port",
                "server.maxplayers",
                "server.players",
                "serverinfo"
            };

            foreach (string cmd in testCommands)
            {
                try
                {
                    await _rconClient.SendCommandAsync(cmd);
                }
                catch (Exception ex)
                {
                    AddLog($"Error with '{cmd}': {ex.Message}");
                }
            }
        }

        private async void textBoxCommand_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
                await SendCommand();
            }
            else if (e.KeyChar == (char)Keys.Up)
            {
                e.Handled = true;
                NavigateHistory(-1);
            }
            else if (e.KeyChar == (char)Keys.Down)
            {
                e.Handled = true;
                NavigateHistory(1);
            }
        }

        private async void textBoxChatMessage_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
                await SendChatMessage();
            }
        }

        private async Task SendCommand()
        {
            if (_rconClient == null || !_rconClient.IsConnected)
            {
                MessageBox.Show("Not connected to server", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string command = textBoxCommand.Text.Trim();
            if (string.IsNullOrEmpty(command)) return;

            try
            {
                string response = await _rconClient.SendCommandAsync(command);
                AddLog($"Server response: {response}");

                if (!_commandHistory.Contains(command))
                {
                    _commandHistory.Add(command);
                }
                _historyIndex = _commandHistory.Count;

                textBoxCommand.Clear();
            }
            catch (Exception ex)
            {
                AddLog($"Command execution error: {ex.Message}");
            }
        }

        private async Task SendChatMessage()
        {
            if (_rconClient == null || !_rconClient.IsConnected)
            {
                MessageBox.Show("Not connected to server", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string message = textBoxChatMessage.Text.Trim();
            if (string.IsNullOrEmpty(message)) return;

            try
            {
                string command = $"say {message}";
                string response = await _rconClient.SendCommandAsync(command);
                AddLog($"Server response: {response}");

                textBoxChatMessage.Clear();
            }
            catch (Exception ex)
            {
                AddLog($"Chat message error: {ex.Message}");
            }
        }

        private void NavigateHistory(int direction)
        {
            if (_commandHistory.Count == 0) return;

            _historyIndex += direction;
            if (_historyIndex < 0) _historyIndex = 0;
            if (_historyIndex >= _commandHistory.Count) _historyIndex = _commandHistory.Count - 1;

            if (_historyIndex >= 0 && _historyIndex < _commandHistory.Count)
            {
                textBoxCommand.Text = _commandHistory[_historyIndex];
                textBoxCommand.SelectionStart = textBoxCommand.Text.Length;
            }
        }


        private void OnMessageReceived(object? sender, string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => ProcessMessage(message)));
            }
            else
            {
                ProcessMessage(message);
            }
        }

        private void ProcessMessage(string message)
        {
            if (message.Contains("id                name                 ping connected addr") ||
                message.Contains("hostname:") ||
                message.Contains("version :") ||
                message.Contains("map     :") ||
                message.Contains("players :"))
            {
                return;
            }

            if (message.Contains("has spawned") || message.Contains("Player has joined"))
            {
                return;
            }

            AddLog(message);

            if (message.Contains("joined") || message.Contains("spawned") || message.Contains("connected"))
            {
                var playerName = ExtractPlayerName(message);
                if (!string.IsNullOrEmpty(playerName))
                {
                    AddLog($"Player {playerName} joined");
                }
            }
            else if (message.Contains("disconnected") || message.Contains("left") || message.Contains("quit"))
            {
                var playerName = ExtractPlayerName(message);
                if (!string.IsNullOrEmpty(playerName))
                {
                    AddLog($"Player {playerName} left");
                }
            }
            else if (message.Contains("sleeping") || message.Contains("went to sleep"))
            {
                var playerName = ExtractPlayerName(message);
                if (!string.IsNullOrEmpty(playerName))
                {
                    UpdatePlayerStatus(playerName, isSleeping: true);
                }
            }
            else if (message.Contains("woke up") || message.Contains("awake"))
            {
                var playerName = ExtractPlayerName(message);
                if (!string.IsNullOrEmpty(playerName))
                {
                    UpdatePlayerStatus(playerName, isSleeping: false);
                }
            }
        }

        private string ExtractPlayerName(string message)
        {
            var patterns = new[]
            {
                "\"([^\"]+)\"",  // Имя в кавычках
                "player ([^\\s]+)",  // "player Name"
                "([^\\s]+) joined",  // "Name joined"
                "([^\\s]+) disconnected",  // "Name disconnected"
                "([^\\s]+) left",  // "Name left"
                "([^\\s]+) quit",  // "Name quit"
                "([^\\s]+) spawned",  // "Name spawned"
                "([^\\s]+) connected"  // "Name connected"
            };

            foreach (var pattern in patterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(message, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var name = match.Groups[1].Value.Trim();
                    if (!string.IsNullOrEmpty(name) &&
                        !name.Contains("(") &&
                        !name.Contains(")") &&
                        !name.Contains("[") &&
                        !name.Contains("]") &&
                        name.Length > 1)
                    {
                        return name;
                    }
                }
            }

            return "";
        }

        private void UpdatePlayerStatus(string playerName, bool isJoining = false, bool isSleeping = false)
        {
            var player = _onlinePlayers.FirstOrDefault(p => p.Name == playerName);
            if (player != null)
            {
                if (InvokeRequired)
                {
                    Invoke(new Action(() =>
                    {
                        if (isJoining) player.IsJoining = true;
                        if (isSleeping) player.IsSleeping = true;

                        var button = flowLayoutPanelPlayers.Controls.OfType<Button>()
                            .FirstOrDefault(b => b.Tag == player);
                        if (button != null)
                        {
                            button.Text = playerName + (player.IsSleeping ? " (Sleeping)" : "") + (player.IsJoining ? " (Joining)" : "");
                        }

                        var sleepingCount = _onlinePlayers.Count(p => p.IsSleeping);
                        var joiningCount = _onlinePlayers.Count(p => p.IsJoining);
                        toolStripStatusLabelPlayers.Text = $"Players: {_onlinePlayers.Count}/228 | Sleeping: {sleepingCount} | Joining: {joiningCount}";
                    }));
                }
                else
                {
                    if (isJoining) player.IsJoining = true;
                    if (isSleeping) player.IsSleeping = true;

                    var button = flowLayoutPanelPlayers.Controls.OfType<Button>()
                        .FirstOrDefault(b => b.Tag == player);
                    if (button != null)
                    {
                        button.Text = playerName + (player.IsSleeping ? " (Sleeping)" : "") + (player.IsJoining ? " (Joining)" : "");
                    }

                    var sleepingCount = _onlinePlayers.Count(p => p.IsSleeping);
                    var joiningCount = _onlinePlayers.Count(p => p.IsJoining);
                    toolStripStatusLabelPlayers.Text = $"Players: {_onlinePlayers.Count}/228 | Sleeping: {sleepingCount} | Joining: {joiningCount}";
                }
            }
        }

        private void RemovePlayer(string playerName)
        {
            var player = _onlinePlayers.FirstOrDefault(p => p.Name == playerName);
            if (player != null)
            {
                if (InvokeRequired)
                {
                    Invoke(new Action(() =>
                    {
                        _onlinePlayers.Remove(player);

                        var button = flowLayoutPanelPlayers.Controls.OfType<Button>()
                            .FirstOrDefault(b => b.Tag == player);
                        if (button != null)
                        {
                            flowLayoutPanelPlayers.Controls.Remove(button);
                        }

                        var infoPanel = flowLayoutPanelPlayers.Controls.OfType<Panel>()
                            .FirstOrDefault(p => p.Tag == player);
                        if (infoPanel != null)
                        {
                            flowLayoutPanelPlayers.Controls.Remove(infoPanel);
                        }

                        var sleepingCount = _onlinePlayers.Count(p => p.IsSleeping);
                        var joiningCount = _onlinePlayers.Count(p => p.IsJoining);
                        labelOnlineCount.Text = $"Online Players: {_onlinePlayers.Count}";
                        toolStripStatusLabelPlayers.Text = $"Players: {_onlinePlayers.Count}/228 | Sleeping: {sleepingCount} | Joining: {joiningCount}";
                    }));
                }
                else
                {
                    _onlinePlayers.Remove(player);

                    var button = flowLayoutPanelPlayers.Controls.OfType<Button>()
                        .FirstOrDefault(b => b.Tag == player);
                    if (button != null)
                    {
                        flowLayoutPanelPlayers.Controls.Remove(button);
                    }

                    var infoPanel = flowLayoutPanelPlayers.Controls.OfType<Panel>()
                        .FirstOrDefault(p => p.Tag == player);
                    if (infoPanel != null)
                    {
                        flowLayoutPanelPlayers.Controls.Remove(infoPanel);
                    }

                    var sleepingCount = _onlinePlayers.Count(p => p.IsSleeping);
                    var joiningCount = _onlinePlayers.Count(p => p.IsJoining);
                    labelOnlineCount.Text = $"Online Players: {_onlinePlayers.Count}";
                    toolStripStatusLabelPlayers.Text = $"Players: {_onlinePlayers.Count}/228 | Sleeping: {sleepingCount} | Joining: {joiningCount}";
                }
            }
        }

        private void OnErrorOccurred(object? sender, string error)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => AddLog($"ERROR: {error}")));
            }
            else
            {
                AddLog($"ERROR: {error}");
            }
        }

        private void AddLog(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string logEntry = $"({timestamp}) {message}";

            int currentSelectionStart = richTextBoxLogs.SelectionStart;
            int currentSelectionLength = richTextBoxLogs.SelectionLength;

            richTextBoxLogs.SelectionStart = richTextBoxLogs.Text.Length;
            richTextBoxLogs.SelectionLength = 0;

            if (message.Contains("<color") || message.Contains("#"))
            {
                ProcessColoredMessage(logEntry);
            }
            else
            {
                if (message.Contains("[CHAT]"))
                {
                    richTextBoxLogs.SelectionColor = Color.Cyan;
                }
                else if (message.Contains("ERROR") || message.Contains("Exception"))
                {
                    richTextBoxLogs.SelectionColor = Color.Red;
                }
                else if (message.Contains("WARNING") || message.Contains("Warning"))
                {
                    richTextBoxLogs.SelectionColor = Color.Orange;
                }
                else if (message.Contains("joined") || message.Contains("spawned"))
                {
                    richTextBoxLogs.SelectionColor = Color.Lime;
                }
                else if (message.Contains("disconnected") || message.Contains("killed"))
                {
                    richTextBoxLogs.SelectionColor = Color.OrangeRed;
                }
                else if (message.Contains("Connected") || message.Contains("Disconnected"))
                {
                    richTextBoxLogs.SelectionColor = Color.Orange;
                }
                else
                {
                    richTextBoxLogs.SelectionColor = Color.White;
                }

                richTextBoxLogs.AppendText(logEntry + "\n");
            }

            if (currentSelectionLength > 0)
            {
                richTextBoxLogs.SelectionStart = currentSelectionStart;
                richTextBoxLogs.SelectionLength = currentSelectionLength;
            }
            else
            {
                richTextBoxLogs.ScrollToCaret();
            }
        }

        private void ProcessColoredMessage(string message)
        {
            var colorPattern = @"<color=#([0-9a-fA-F]{6})>(.*?)</color>";
            var matches = System.Text.RegularExpressions.Regex.Matches(message, colorPattern);
            
            if (matches.Count > 0)
            {
                int lastIndex = 0;
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    if (match.Index > lastIndex)
                    {
                        richTextBoxLogs.SelectionColor = Color.White;
                        richTextBoxLogs.AppendText(message.Substring(lastIndex, match.Index - lastIndex));
                    }
                    
                    string colorHex = match.Groups[1].Value;
                    string coloredText = match.Groups[2].Value;
                    
                    Color color = ColorTranslator.FromHtml($"#{colorHex}");
                    
                    if (color.R < 50 && color.G < 50 && color.B < 50)
                    {
                        color = Color.White;
                    }
                    
                    richTextBoxLogs.SelectionColor = color;
                    richTextBoxLogs.AppendText(coloredText);
                    
                    lastIndex = match.Index + match.Length;
                }
                
                if (lastIndex < message.Length)
                {
                    richTextBoxLogs.SelectionColor = Color.White;
                    richTextBoxLogs.AppendText(message.Substring(lastIndex));
                }
            }
            else
            {
                var hexPattern = @"#([0-9a-fA-F]{6})";
                var hexMatches = System.Text.RegularExpressions.Regex.Matches(message, hexPattern);
                
                if (hexMatches.Count > 0)
                {
                    int lastIndex = 0;
                    foreach (System.Text.RegularExpressions.Match match in hexMatches)
                    {
                        if (match.Index > lastIndex)
                        {
                            richTextBoxLogs.SelectionColor = Color.White;
                            richTextBoxLogs.AppendText(message.Substring(lastIndex, match.Index - lastIndex));
                        }
                        
                        string colorHex = match.Groups[1].Value;
                        Color color = ColorTranslator.FromHtml($"#{colorHex}");
                        
                        if (color.R < 50 && color.G < 50 && color.B < 50)
                        {
                            color = Color.White;
                        }
                        
                        richTextBoxLogs.SelectionColor = color;
                        
                        lastIndex = match.Index + match.Length;
                    }
                    
                    if (lastIndex < message.Length)
                    {
                        richTextBoxLogs.SelectionColor = Color.White;
                        richTextBoxLogs.AppendText(message.Substring(lastIndex));
                    }
                }
                else
                {
                    richTextBoxLogs.SelectionColor = Color.White;
                    richTextBoxLogs.AppendText(message);
                }
            }
            
            richTextBoxLogs.AppendText("\n");
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveServerSettings();
            _rconClient?.Disconnect();
        }

        private void LoadServerSettings()
        {
            try
            {
                string settingsFile = Path.Combine(Application.StartupPath, "server_settings.txt");
                if (File.Exists(settingsFile))
                {
                    string[] lines = File.ReadAllLines(settingsFile);
                    if (lines.Length >= 3)
                    {
                        string serverIP = lines[0].Trim();
                        string portStr = lines[1].Trim();
                        string password = lines[2].Trim();

                        if (int.TryParse(portStr, out int port))
                        {
                            if (_connectDialog == null || _connectDialog.IsDisposed)
                            {
                                _connectDialog = new ConnectDialog();
                            }

                            _connectDialog.SetServerSettings(serverIP, port, password);
                            AddLog($"Server settings loaded: {serverIP}:{port}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog($"Error loading server settings: {ex.Message}");
            }
        }

        private void SaveServerSettings()
        {
            try
            {
                if (_connectDialog != null && !_connectDialog.IsDisposed)
                {
                    string settingsFile = Path.Combine(Application.StartupPath, "server_settings.txt");
                    string[] settings = {
                        _connectDialog.ServerIP,
                        _connectDialog.Port.ToString(),
                        _connectDialog.Password
                    };
                    File.WriteAllLines(settingsFile, settings);
                    AddLog("Server settings saved");
                }
            }
            catch (Exception ex)
            {
                AddLog($"Error saving server settings: {ex.Message}");
            }
        }


        private void listViewPlayerStats_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listViewPlayerStats.SelectedItems.Count > 0)
            {
                var selectedItem = listViewPlayerStats.SelectedItems[0];
                string playerName = selectedItem.SubItems[1].Text;
                string steamId = selectedItem.SubItems[0].Text;

                labelPlayerKills.Text = $"Events for player: {playerName}";
                LoadPlayerKills(steamId);
            }
        }

        private void textBoxFilterStats_TextChanged(object sender, EventArgs e)
        {
            FilterPlayerStats();
        }


        private void RefreshPlayerStats()
        {
            if (_rconClient == null || !_rconClient.IsConnected)
            {
                AddLog("Not connected to server - cannot refresh stats");
                return;
            }

            try
            {
                _playerStats.Clear();

                foreach (var onlinePlayer in _onlinePlayers)
                {
                    _playerStats.Add(new PlayerStats
                    {
                        SteamID = onlinePlayer.SteamID,
                        Nickname = onlinePlayer.Name,
                        PVPKills = new Random().Next(0, 50), // Simulate random stats
                        PVPDeaths = new Random().Next(0, 30),
                        PVEDeaths = new Random().Next(0, 100)
                    });
                }

                UpdatePlayerStatsList();
                labelPlayerCount.Text = $"{_playerStats.Count} players";
            }
            catch (Exception ex)
            {
                AddLog($"Error refreshing stats: {ex.Message}");
            }
        }

        private async Task RefreshOnlinePlayers()
        {
            if (_rconClient == null || !_rconClient.IsConnected || _isUpdatingOnline)
            {
                return;
            }

            _isUpdatingOnline = true;
            try
            {
                string response = "";

                try
                {
                    response = await _rconClient.SendCommandAsync("status");
                }
                catch
                {
                    try
                    {
                        response = await _rconClient.SendCommandAsync("players");
                    }
                    catch
                    {
                        return;
                    }
                }

                if (InvokeRequired)
                {
                    Invoke(new Action(() =>
                    {
                        _onlinePlayers.Clear();
                        flowLayoutPanelPlayers.Controls.Clear();
                    }));
                }
                else
                {
                    _onlinePlayers.Clear();
                    flowLayoutPanelPlayers.Controls.Clear();
                }

                if (!string.IsNullOrEmpty(response))
                {
                    var lines = response.Split('\n');
                    bool inPlayerList = false;

                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        if (line.Contains("id") && line.Contains("name") && line.Contains("ping"))
                        {
                            inPlayerList = true;
                            continue;
                        }

                        if (inPlayerList && line.Length > 20 && line.Contains("7656119"))
                        {
                            var parts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 2)
                            {
                                string steamId = parts[0];
                                string name = parts[1].Trim();

                                var player = new OnlinePlayer
                                {
                                    Name = name,
                                    SteamID = steamId,
                                    ConnectedTime = DateTime.Now
                                };

                                if (InvokeRequired)
                                {
                                    Invoke(new Action(() =>
                                    {
                                        _onlinePlayers.Add(player);

                                        var playerButton = new Button
                                        {
                                            Text = name,
                                            Size = new Size(180, 30),
                                            Margin = new Padding(2, 1, 2, 1),
                                            Tag = player,
                                            TextAlign = ContentAlignment.MiddleLeft,
                                            FlatStyle = FlatStyle.Flat,
                                            BackColor = Color.FromArgb(60, 60, 60),
                                            ForeColor = Color.White
                                        };
                                        playerButton.Click += PlayerButton_Click;
                                        flowLayoutPanelPlayers.Controls.Add(playerButton);
                                    }));
                                }
                                else
                                {
                                    _onlinePlayers.Add(player);

                                    var playerButton = new Button
                                    {
                                        Text = name,
                                        Size = new Size(180, 30),
                                        Margin = new Padding(2),
                                        Tag = player,
                                        TextAlign = ContentAlignment.MiddleLeft,
                                        FlatStyle = FlatStyle.Flat,
                                        BackColor = Color.FromArgb(60, 60, 60),
                                        ForeColor = Color.White
                                    };
                                    playerButton.Click += PlayerButton_Click;
                                    flowLayoutPanelPlayers.Controls.Add(playerButton);
                                }
                            }
                        }
                        else if (line.Contains("7656119") && !inPlayerList)
                        {
                            var parts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 2)
                            {
                                string steamId = parts[0];
                                string name = parts[1].Trim();

                                var player = new OnlinePlayer
                                {
                                    Name = name,
                                    SteamID = steamId,
                                    ConnectedTime = DateTime.Now
                                };

                                if (InvokeRequired)
                                {
                                    Invoke(new Action(() =>
                                    {
                                        _onlinePlayers.Add(player);

                                        var playerButton = new Button
                                        {
                                            Text = name,
                                            Size = new Size(180, 30),
                                            Margin = new Padding(2, 1, 2, 1),
                                            Tag = player,
                                            TextAlign = ContentAlignment.MiddleLeft,
                                            FlatStyle = FlatStyle.Flat,
                                            BackColor = Color.FromArgb(60, 60, 60),
                                            ForeColor = Color.White
                                        };
                                        playerButton.Click += PlayerButton_Click;
                                        flowLayoutPanelPlayers.Controls.Add(playerButton);
                                    }));
                                }
                                else
                                {
                                    _onlinePlayers.Add(player);

                                    var playerButton = new Button
                                    {
                                        Text = name,
                                        Size = new Size(180, 30),
                                        Margin = new Padding(2),
                                        Tag = player,
                                        TextAlign = ContentAlignment.MiddleLeft,
                                        FlatStyle = FlatStyle.Flat,
                                        BackColor = Color.FromArgb(60, 60, 60),
                                        ForeColor = Color.White
                                    };
                                    playerButton.Click += PlayerButton_Click;
                                    flowLayoutPanelPlayers.Controls.Add(playerButton);
                                }
                            }
                        }
                    }
                }

                if (InvokeRequired)
                {
                    Invoke(new Action(() =>
                    {
                        var sleepingCount = _onlinePlayers.Count(p => p.IsSleeping);
                        var joiningCount = _onlinePlayers.Count(p => p.IsJoining);
                        labelOnlineCount.Text = $"Online Players: {_onlinePlayers.Count}";
                        toolStripStatusLabelPlayers.Text = $"Players: {_onlinePlayers.Count}/228 | Sleeping: {sleepingCount} | Joining: {joiningCount}";
                    }));
                }
                else
                {
                    var sleepingCount = _onlinePlayers.Count(p => p.IsSleeping);
                    var joiningCount = _onlinePlayers.Count(p => p.IsJoining);
                    labelOnlineCount.Text = $"Online Players: {_onlinePlayers.Count}";
                    toolStripStatusLabelPlayers.Text = $"Players: {_onlinePlayers.Count}/228 | Sleeping: {sleepingCount} | Joining: {joiningCount}";
                }
            }
            catch (Exception ex)
            {
                AddLog($"Error refreshing online players: {ex.Message}");
            }
            finally
            {
                _isUpdatingOnline = false;
            }
        }

        private void UpdatePlayerStatsList()
        {
            listViewPlayerStats.Items.Clear();

            foreach (var stat in _playerStats)
            {
                var item = new ListViewItem(stat.SteamID);
                item.SubItems.Add(stat.Nickname);
                item.SubItems.Add(stat.PVPKills.ToString());
                item.SubItems.Add(stat.PVPDeaths.ToString());
                item.SubItems.Add(stat.PVEDeaths.ToString());

                listViewPlayerStats.Items.Add(item);
            }
        }

        private void FilterPlayerStats()
        {
            string filter = textBoxFilterStats.Text.ToLower();

            listViewPlayerStats.Items.Clear();

            foreach (var player in _playerStats)
            {
                bool matches = string.IsNullOrEmpty(filter) ||
                              player.Nickname.ToLower().Contains(filter) ||
                              player.SteamID.Contains(filter);

                if (matches)
                {
                    var item = new ListViewItem(player.SteamID);
                    item.SubItems.Add(player.Nickname);
                    item.SubItems.Add(player.PVPKills.ToString());
                    item.SubItems.Add(player.PVPDeaths.ToString());
                    item.SubItems.Add(player.PVEDeaths.ToString());
                    item.Tag = player;
                    listViewPlayerStats.Items.Add(item);
                }
            }
        }

        private void LoadPlayerKills(string steamId)
        {
            listViewPlayerKills.Items.Clear();

            var playerKills = _playerKills.Where(k => k.PlayerSteamID == steamId).ToList();

            foreach (var kill in playerKills)
            {
                var item = new ListViewItem(kill.Date.ToString("dd/MM/yyyy HH:mm:ss"));
                item.SubItems.Add(kill.EventType);
                item.SubItems.Add($"{kill.TargetName} ({kill.TargetSteamID})");

                listViewPlayerKills.Items.Add(item);
            }
        }


        private void PlayerButton_Click(object sender, EventArgs e)
        {
            if (sender is Button button && button.Tag is OnlinePlayer player)
            {
                var existingInfo = flowLayoutPanelPlayers.Controls.OfType<Panel>()
                    .FirstOrDefault(p => p.Tag == player);
                if (existingInfo != null)
                {
                    flowLayoutPanelPlayers.Controls.Remove(existingInfo);
                    return;
                }

                var infoPanel = new Panel
                {
                    Size = new Size(180, 100),
                    Margin = new Padding(2, 0, 2, 5),
                    BackColor = Color.LightGray,
                    Tag = player
                };

                var steamButton = new Button
                {
                    Text = $"Steam ID: {player.SteamID}",
                    Size = new Size(170, 20),
                    Location = new Point(5, 5),
                    Font = new Font("Consolas", 7),
                    BackColor = Color.White,
                    ForeColor = Color.Black
                };
                steamButton.Click += (s, args) =>
                {
                    Clipboard.SetText(player.SteamID);
                    AddLog($"Copied Steam ID: {player.SteamID}");
                };

                var ipButton = new Button
                {
                    Text = "IP: Click to get",
                    Size = new Size(170, 20),
                    Location = new Point(5, 30),
                    Font = new Font("Consolas", 7),
                    BackColor = Color.White,
                    ForeColor = Color.Black
                };
                ipButton.Click += async (s, args) =>
                {
                    try
                    {
                        string response = await _rconClient.SendCommandAsync("status");
                        var lines = response.Split('\n');
                        foreach (var line in lines)
                        {
                            if (line.Contains(player.SteamID))
                            {
                                var ipMatch = System.Text.RegularExpressions.Regex.Match(line, @"(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})");
                                if (ipMatch.Success)
                                {
                                    string ip = ipMatch.Groups[1].Value;
                                    Clipboard.SetText(ip);
                                    AddLog($"Copied IP: {ip}");
                                    ipButton.Text = $"IP: {ip}";
                                    return;
                                }
                            }
                        }
                        AddLog("IP not found for this player");
                    }
                    catch (Exception ex)
                    {
                        AddLog($"Error getting IP: {ex.Message}");
                    }
                };

                var kickButton = new Button
                {
                    Text = "Kick Player",
                    Size = new Size(170, 20),
                    Location = new Point(5, 55),
                    Font = new Font("Consolas", 7),
                    BackColor = Color.Orange,
                    ForeColor = Color.Black
                };
                kickButton.Click += async (s, args) =>
                {
                    try
                    {
                        await _rconClient.SendCommandAsync($"kick \"{player.Name}\"");
                        AddLog($"Kicked player: {player.Name}");
                    }
                    catch (Exception ex)
                    {
                        AddLog($"Error kicking player: {ex.Message}");
                    }
                };

                var banButton = new Button
                {
                    Text = "Ban Player",
                    Size = new Size(170, 20),
                    Location = new Point(5, 80),
                    Font = new Font("Consolas", 7),
                    BackColor = Color.Red,
                    ForeColor = Color.White
                };
                banButton.Click += async (s, args) =>
                {
                    try
                    {
                        await _rconClient.SendCommandAsync($"ban \"{player.Name}\"");
                        AddLog($"Banned player: {player.Name}");
                    }
                    catch (Exception ex)
                    {
                        AddLog($"Error banning player: {ex.Message}");
                    }
                };

                infoPanel.Controls.Add(steamButton);
                infoPanel.Controls.Add(ipButton);
                infoPanel.Controls.Add(kickButton);
                infoPanel.Controls.Add(banButton);

                int buttonIndex = flowLayoutPanelPlayers.Controls.IndexOf(button);
                flowLayoutPanelPlayers.Controls.Add(infoPanel);
                flowLayoutPanelPlayers.Controls.SetChildIndex(infoPanel, buttonIndex + 1);
            }
        }

        private void toolStripStatusLabel_Click(object sender, EventArgs e)
        {

        }

        private void textBoxPlayerSearch_TextChanged(object sender, EventArgs e)
        {
            string searchText = textBoxPlayerSearch.Text.ToLower();
            
            foreach (Control control in flowLayoutPanelPlayers.Controls)
            {
                if (control is Button button && button.Tag is OnlinePlayer player)
                {
                    bool matches = string.IsNullOrEmpty(searchText) || 
                                 player.Name.ToLower().Contains(searchText) ||
                                 player.SteamID.Contains(searchText);
                    
                    button.Visible = matches;
                    
                    var infoPanel = flowLayoutPanelPlayers.Controls.OfType<Panel>()
                        .FirstOrDefault(p => p.Tag == player);
                    if (infoPanel != null)
                    {
                        infoPanel.Visible = matches;
                    }
                }
            }
        }
    }

    public class PlayerStats
    {
        public string SteamID { get; set; } = "";
        public string Nickname { get; set; } = "";
        public int PVPKills { get; set; }
        public int PVPDeaths { get; set; }
        public int PVEDeaths { get; set; }
    }

    public class PlayerKill
    {
        public string PlayerSteamID { get; set; } = "";
        public string TargetName { get; set; } = "";
        public string TargetSteamID { get; set; } = "";
        public string EventType { get; set; } = "";
        public DateTime Date { get; set; }
    }

    public class OnlinePlayer
    {
        public string Name { get; set; } = "";
        public string SteamID { get; set; } = "";
        public DateTime ConnectedTime { get; set; }
        public bool IsSleeping { get; set; } = false;
        public bool IsJoining { get; set; } = false;
    }
}
