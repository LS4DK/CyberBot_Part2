using System;
using System.Drawing;
using System.Media;
using System.IO;
using System.Windows.Forms;
using CyberBot_Part2.Logic;

namespace CyberBot_Part2.Forms
{
    public class MainForm : Form
    {
        private readonly ChatbotEngine _engine = new ChatbotEngine();

        private static readonly Color BgDark = Color.FromArgb(15, 15, 25);
        private static readonly Color BgPanel = Color.FromArgb(22, 22, 38);
        private static readonly Color AccentCyan = Color.FromArgb(0, 220, 220);
        private static readonly Color AccentMag = Color.FromArgb(180, 0, 220);
        private static readonly Color TextWhite = Color.FromArgb(230, 230, 240);
        private static readonly Color TextGray = Color.FromArgb(140, 140, 160);
        private static readonly Color InputBg = Color.FromArgb(28, 28, 45);

        private RichTextBox _chatBox;
        private TextBox _inputBox;
        private Button _sendBtn;
        private Label _memoryLabel;
        private ComboBox _personalityBox;
        private Label _sentimentLabel;

        private string _userName = "";
        private string _personality = "friendly";

        public MainForm()
        {
            InitializeUI();
            ShowWelcomeDialog();
            PlayGreeting();
            ShowWelcomeMessages();
        }

        private void InitializeUI()
        {
            Text = "CyberBot - Cybersecurity Awareness Assistant";
            Size = new Size(1100, 720);
            MinimumSize = new Size(900, 600);
            BackColor = BgDark;
            ForeColor = TextWhite;
            Font = new Font("Segoe UI", 10f);
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;

            // ── Help strip ─────────────────────────────────────────────────────
            Panel helpStrip = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 22,
                BackColor = Color.FromArgb(10, 10, 20)
            };
            Label helpLabel = new Label //
            {
                Text = "  Type 'exit' to quit  |  'give me a tip' for random advice  |  'what do you remember about me' to check memory",
                Dock = DockStyle.Fill,
                ForeColor = TextGray,
                Font = new Font("Segoe UI", 7.5f),
                TextAlign = ContentAlignment.MiddleLeft
            };
            helpStrip.Controls.Add(helpLabel);

            // ── Sidebar ────────────────────────────────────────────────────────
            Panel sidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 160,
                BackColor = BgPanel,
                Padding = new Padding(8)
            };

            Label asciiLabel = new Label
            {
                Text = "CYbER\nSECURITY BOT V2\n* Stay Safe *",
                Font = new Font("Consolas", 9f, FontStyle.Bold),
                ForeColor = AccentMag,
                AutoSize = false,
                Size = new Size(144, 60),
                Location = new Point(8, 8),
                TextAlign = ContentAlignment.MiddleCenter
            };
            sidebar.Controls.Add(asciiLabel);

            AddHRule(8, 75, 144, sidebar);
            AddSideLabel("PERSONALITY", 85, sidebar);

            _personalityBox = new ComboBox
            {
                Location = new Point(8, 103),
                Size = new Size(144, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = InputBg,
                ForeColor = TextWhite,
                FlatStyle = FlatStyle.Flat
            };
            _personalityBox.Items.AddRange(new object[] { "Friendly", "Professional", "Futuristic AI", "Casual" });
            _personalityBox.SelectedIndex = 0;
            _personalityBox.SelectedIndexChanged += OnPersonalityChanged;
            sidebar.Controls.Add(_personalityBox);

            AddHRule(8, 138, 144, sidebar);
            AddSideLabel("MEMORY RECALL", 148, sidebar);

            _memoryLabel = new Label
            {
                Text = "Name: -\nFav Topic: -",
                Location = new Point(8, 165),
                Size = new Size(144, 45),
                ForeColor = AccentCyan,
                Font = new Font("Segoe UI", 8.5f)
            };
            sidebar.Controls.Add(_memoryLabel);

            AddHRule(8, 218, 144, sidebar);
            AddSideLabel("MOOD DETECTED", 228, sidebar);

            _sentimentLabel = new Label
            {
                Text = "Neutral",
                Location = new Point(8, 246),
                Size = new Size(144, 25),
                ForeColor = TextGray,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            sidebar.Controls.Add(_sentimentLabel);

            AddHRule(8, 278, 144, sidebar);
            AddSideLabel("QUICK TOPICS", 288, sidebar);

            string[] topics = { "Passwords", "Phishing", "Malware", "VPN", "2FA", "Firewall", "Encryption", "Privacy", "Scams" };
            for (int i = 0; i < topics.Length; i++)
            {
                string t = topics[i];
                Button btn = new Button
                {
                    Text = t,
                    Location = new Point(8, 306 + i * 24),
                    Size = new Size(144, 22),
                    BackColor = BgPanel,
                    ForeColor = AccentCyan,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 8.5f),
                    Cursor = Cursors.Hand,
                    TextAlign = ContentAlignment.MiddleLeft
                };
                btn.FlatAppearance.BorderColor = Color.FromArgb(40, 40, 70);
                btn.Click += (s, e) => { _inputBox.Text = $"Tell me about {t.ToLower()}"; ProcessInput(); };
                btn.MouseEnter += (s, e) => btn.ForeColor = Color.White;
                btn.MouseLeave += (s, e) => btn.ForeColor = AccentCyan;
                sidebar.Controls.Add(btn);
            }

            // ── Main chat area ─────────────────────────────────────────────────
            Panel mainArea = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BgDark,
                Padding = new Padding(10, 10, 10, 0)
            };

            Panel titleBar = new Panel { Dock = DockStyle.Top, Height = 42, BackColor = BgPanel };
            Label titleLabel = new Label
            {
                Text = "CYBERBOT - Cybersecurity Awareness Chat",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = AccentCyan,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };
            titleBar.Controls.Add(titleLabel);
            mainArea.Controls.Add(titleBar);

            Panel inputPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 55,
                BackColor = BgPanel,
                Padding = new Padding(8)
            };

            _sendBtn = new Button
            {
                Text = "Send",
                Dock = DockStyle.Right,
                Width = 100,
                BackColor = AccentMag,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _sendBtn.FlatAppearance.BorderSize = 0;
            _sendBtn.Click += OnSendClick;
            _sendBtn.MouseEnter += (s, e) => _sendBtn.BackColor = Color.FromArgb(0, 180, 180);
            _sendBtn.MouseLeave += (s, e) => _sendBtn.BackColor = AccentMag;
            inputPanel.Controls.Add(_sendBtn);

            _inputBox = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = InputBg,
                ForeColor = TextWhite,
                Font = new Font("Segoe UI", 11f),
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "Type your message here and press Enter or Send..."
            };
            _inputBox.KeyDown += OnInputKeyDown;
            inputPanel.Controls.Add(_inputBox);
            mainArea.Controls.Add(inputPanel);

            _chatBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = BgDark,
                ForeColor = TextWhite,
                Font = new Font("Consolas", 10f), // monospace for ASCII art
                BorderStyle = BorderStyle.None,
                ScrollBars = RichTextBoxScrollBars.Both,
                WordWrap = false, // wrapping of ASCII art
                ZoomFactor = 1.1f
            };
            mainArea.Controls.Add(_chatBox);

            // ── ADD CONTROLS IN CORRECT ORDER ──────────────────────────────────
            Controls.Add(mainArea);   // fill first
            Controls.Add(sidebar);    // then left dock
            Controls.Add(helpStrip);  // then bottom
        }

        private void ShowWelcomeDialog()
        {
            using (Form dialog = new Form())
            {
                dialog.Text = "Welcome to CyberBot";
                dialog.Size = new Size(400, 260);
                dialog.StartPosition = FormStartPosition.CenterScreen;
                dialog.BackColor = BgPanel;
                dialog.ForeColor = TextWhite;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;

                Label title = new Label
                {
                    Text = "Welcome to CyberBot",
                    Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                    ForeColor = AccentCyan,
                    Location = new Point(20, 15),
                    Size = new Size(360, 30)
                };
                dialog.Controls.Add(title);

                Label nameLabel = new Label { Text = "Your Name:", Location = new Point(20, 60), AutoSize = true };
                dialog.Controls.Add(nameLabel);

                TextBox nameBox = new TextBox
                {
                    Location = new Point(20, 82),
                    Size = new Size(350, 26),
                    BackColor = InputBg,
                    ForeColor = TextWhite,
                    BorderStyle = BorderStyle.FixedSingle,
                    Font = new Font("Segoe UI", 11f)
                };
                dialog.Controls.Add(nameBox);

                Label pLabel = new Label { Text = "Choose Personality:", Location = new Point(20, 120), AutoSize = true };
                dialog.Controls.Add(pLabel);

                ComboBox pBox = new ComboBox
                {
                    Location = new Point(20, 142),
                    Size = new Size(350, 26),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    BackColor = InputBg,
                    ForeColor = TextWhite
                };
                pBox.Items.AddRange(new object[] { "Friendly", "Professional", "Futuristic AI", "Casual" });
                pBox.SelectedIndex = 0;
                dialog.Controls.Add(pBox);

                Button ok = new Button
                {
                    Text = "Start Chatting!",
                    Location = new Point(130, 185),
                    Size = new Size(140, 35),
                    BackColor = AccentMag,
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 10f, FontStyle.Bold)
                };
                ok.FlatAppearance.BorderSize = 0;
                ok.Click += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(nameBox.Text))
                    {
                        MessageBox.Show("Please enter your name.", "CyberBot", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    dialog.DialogResult = DialogResult.OK;
                    dialog.Close();
                };
                dialog.Controls.Add(ok);
                dialog.AcceptButton = ok;
                dialog.ShowDialog();

                _userName = string.IsNullOrWhiteSpace(nameBox.Text) ? "User" : nameBox.Text.Trim();
                _engine.Memory.Name = _userName;
                _personality = ParsePersonality(pBox.SelectedItem?.ToString() ?? "");
                _personalityBox.SelectedIndex = pBox.SelectedIndex;
                UpdateMemoryDisplay();
            }
        }

        private void ShowWelcomeMessages()
        {
            string logo = @"

 ██████╗   ██╗   ██╗  ██████╗   ███████╗  ██████╗
██╔════╝   ╚██╗ ██╔╝  ██╔══██╗  ██╔════╝  ██╔══██╗
██║         ╚████╔╝   ██████╔╝  █████╗    ██████╔╝
██║          ╚██╔╝    ██╔══██╗  ██╔══╝    ██╔══██╗
╚██████╗      ██║     ██████╔╝  ███████╗  ██║  ██║
 ╚═════╝      ╚═╝     ╚═════╝   ╚══════╝  ╚═╝  ╚═╝

        CYBER SECURITY BOT v2.0
====================================================";

            AppendSystem(logo);

            AppendBot($"Hello {_userName}! I'm CyberBot, your cybersecurity awareness assistant.");

            AppendBot("I can help with: Passwords | Phishing | Malware | VPN | 2FA | Firewalls | Encryption | Scams | Privacy");

            AppendBot("Tell me your favourite topic (e.g. 'I'm interested in privacy') and I'll remember it!");
        }

        private void OnSendClick(object sender, EventArgs e) => ProcessInput();

        private void OnInputKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; ProcessInput(); }
        }

        private void ProcessInput()
        {
            string input = _inputBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(input)) return;
            _inputBox.Clear();

            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                AppendUser(input);
                AppendBot($"Goodbye, {_userName}! Stay safe online.");
                System.Threading.Thread.Sleep(600);
                Application.Exit();
                return;
            }

            AppendUser(input);
            Sentiment s = SentimentDetector.Detect(input);
            UpdateSentimentLabel(s);
            string response = _engine.GetResponse(input, _personality);
            AppendBot(response);
            UpdateMemoryDisplay();
        }

        private void AppendUser(string text)
        {
            _chatBox.SelectionColor = AccentCyan;
            _chatBox.SelectionFont = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            _chatBox.AppendText($"\nYou ({_userName}):\n");
            _chatBox.SelectionColor = TextWhite;
            _chatBox.SelectionFont = new Font("Segoe UI", 10.5f);
            _chatBox.AppendText($"  {text}\n");
            _chatBox.ScrollToCaret();
        }

        private void AppendBot(string text)
        {
            _chatBox.SelectionColor = AccentMag;
            _chatBox.SelectionFont = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            _chatBox.AppendText("\nCyberBot:\n");
            _chatBox.SelectionColor = TextWhite;
            _chatBox.SelectionFont = new Font("Segoe UI", 10.5f);
            _chatBox.AppendText($"  {text}\n");
            _chatBox.ScrollToCaret();
        }

        private void AppendSystem(string text)
        {
            _chatBox.SelectionColor = Color.FromArgb(255, 120, 220);
            _chatBox.SelectionFont = new Font("Consolas",17f, FontStyle.Regular);
            _chatBox.AppendText(text + "\n");
            _chatBox.ScrollToCaret();
        }

        private void UpdateMemoryDisplay()
        {
            string name = string.IsNullOrEmpty(_engine.Memory.Name) ? "-" : _engine.Memory.Name;
            string topic = string.IsNullOrEmpty(_engine.Memory.FavouriteTopic) ? "-" : _engine.Memory.FavouriteTopic;
            _memoryLabel.Text = $"Name: {name}\nFav Topic: {topic}";
        }

        private void UpdateSentimentLabel(Sentiment s)
        {
            switch (s)
            {
                case Sentiment.Worried: _sentimentLabel.Text = "Worried"; _sentimentLabel.ForeColor = Color.Orange; break;
                case Sentiment.Frustrated: _sentimentLabel.Text = "Frustrated"; _sentimentLabel.ForeColor = Color.Tomato; break;
                case Sentiment.Curious: _sentimentLabel.Text = "Curious"; _sentimentLabel.ForeColor = AccentCyan; break;
                case Sentiment.Happy: _sentimentLabel.Text = "Happy"; _sentimentLabel.ForeColor = Color.LightGreen; break;
                default: _sentimentLabel.Text = "Neutral"; _sentimentLabel.ForeColor = TextGray; break;
            }
        }

        private void OnPersonalityChanged(object sender, EventArgs e)
        {
            _personality = ParsePersonality(_personalityBox.SelectedItem?.ToString() ?? "");
            AppendSystem($"-- Personality changed to: {_personalityBox.SelectedItem} --");
        }

        private static string ParsePersonality(string label)
        {
            if (label.Contains("Professional")) return "professional";
            if (label.Contains("AI") || label.Contains("Futuristic")) return "ai";
            if (label.Contains("Casual")) return "casual";
            return "friendly";
        }

        private static void PlayGreeting()
        {
            try
            {
                string[] paths =
                {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "greeting.wav"),
                    Path.Combine(Environment.CurrentDirectory, "greeting.wav")
                };
                foreach (string p in paths)
                    if (File.Exists(p)) { new SoundPlayer(p).Play(); return; }
            }
            catch { }
        }

        private static void AddHRule(int x, int y, int w, Panel parent)
        {
            parent.Controls.Add(new Panel
            {
                Location = new Point(x, y),
                Size = new Size(w, 1),
                BackColor = Color.FromArgb(40, 40, 70)
            });
        }

        private static void AddSideLabel(string text, int y, Panel parent)
        {
            parent.Controls.Add(new Label
            {
                Text = text,
                Location = new Point(8, y),
                AutoSize = true,
                ForeColor = Color.FromArgb(100, 100, 140),
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold)
            });
        }
    }
}