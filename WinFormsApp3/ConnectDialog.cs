namespace RustRcon
{
    public partial class ConnectDialog : Form
    {
        public string ServerIP { get; private set; } = "127.0.0.1";
        public int Port { get; private set; } = 28016;
        public string Password { get; private set; } = "";

        private TextBox textBoxServerIP;
        private TextBox textBoxPort;
        private TextBox textBoxPassword;
        private Button buttonConnect;
        private Button buttonCancel;
        private Label labelServerIP;
        private Label labelPort;
        private Label labelPassword;

        public ConnectDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.textBoxServerIP = new TextBox();
            this.textBoxPort = new TextBox();
            this.textBoxPassword = new TextBox();
            this.buttonConnect = new Button();
            this.buttonCancel = new Button();
            this.labelServerIP = new Label();
            this.labelPort = new Label();
            this.labelPassword = new Label();
            this.SuspendLayout();
            this.labelServerIP.AutoSize = true;
            this.labelServerIP.Location = new Point(12, 15);
            this.labelServerIP.Name = "labelServerIP";
            this.labelServerIP.Size = new Size(60, 15);
            this.labelServerIP.TabIndex = 0;
            this.labelServerIP.Text = "Server IP:";
            this.textBoxServerIP.Location = new Point(90, 12);
            this.textBoxServerIP.Name = "textBoxServerIP";
            this.textBoxServerIP.Size = new Size(200, 23);
            this.textBoxServerIP.TabIndex = 1;
            this.textBoxServerIP.Text = "127.0.0.1";
            this.labelPort.AutoSize = true;
            this.labelPort.Location = new Point(12, 45);
            this.labelPort.Name = "labelPort";
            this.labelPort.Size = new Size(32, 15);
            this.labelPort.TabIndex = 2;
            this.labelPort.Text = "Port:";
            this.textBoxPort.Location = new Point(90, 42);
            this.textBoxPort.Name = "textBoxPort";
            this.textBoxPort.Size = new Size(100, 23);
            this.textBoxPort.TabIndex = 3;
            this.textBoxPort.Text = "28016";
            this.labelPassword.AutoSize = true;
            this.labelPassword.Location = new Point(12, 75);
            this.labelPassword.Name = "labelPassword";
            this.labelPassword.Size = new Size(60, 15);
            this.labelPassword.TabIndex = 4;
            this.labelPassword.Text = "Password:";
            this.textBoxPassword.Location = new Point(90, 72);
            this.textBoxPassword.Name = "textBoxPassword";
            this.textBoxPassword.PasswordChar = '*';
            this.textBoxPassword.Size = new Size(200, 23);
            this.textBoxPassword.TabIndex = 5;
            this.buttonConnect.DialogResult = DialogResult.OK;
            this.buttonConnect.Location = new Point(134, 110);
            this.buttonConnect.Name = "buttonConnect";
            this.buttonConnect.Size = new Size(75, 23);
            this.buttonConnect.TabIndex = 6;
            this.buttonConnect.Text = "Connect";
            this.buttonConnect.UseVisualStyleBackColor = true;
            this.buttonConnect.Click += new EventHandler(this.buttonConnect_Click);
            this.buttonCancel.DialogResult = DialogResult.Cancel;
            this.buttonCancel.Location = new Point(215, 110);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new Size(75, 23);
            this.buttonCancel.TabIndex = 7;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.AcceptButton = this.buttonConnect;
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new Size(302, 145);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonConnect);
            this.Controls.Add(this.textBoxPassword);
            this.Controls.Add(this.labelPassword);
            this.Controls.Add(this.textBoxPort);
            this.Controls.Add(this.labelPort);
            this.Controls.Add(this.textBoxServerIP);
            this.Controls.Add(this.labelServerIP);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ConnectDialog";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Connect to Server";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        public void SetServerSettings(string serverIP, int port, string password)
        {
            textBoxServerIP.Text = serverIP;
            textBoxPort.Text = port.ToString();
            textBoxPassword.Text = password;
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBoxServerIP.Text.Trim()))
            {
                MessageBox.Show("Please enter server IP address", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!int.TryParse(textBoxPort.Text.Trim(), out int port) || port <= 0 || port > 65535)
            {
                MessageBox.Show("Please enter a valid port number (1-65535)", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrEmpty(textBoxPassword.Text))
            {
                MessageBox.Show("Please enter RCON password", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            ServerIP = textBoxServerIP.Text.Trim();
            Port = port;
            Password = textBoxPassword.Text;

            DialogResult = DialogResult.OK;
        }
    }
}
