using Newtonsoft.Json;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;
using XenoUI;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;

namespace TheUglyExec
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            Text = "UniversalExec";
            BuildHomePage();
            AddApplicationName();
            ClientsWindow.Initialize(false);
        }

        #region XenoStuff

        [DllImport("Xeno.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private static extern IntPtr GetClients();

        [DllImport("Xeno.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private static extern void Execute(byte[] script, int[] PIDs, int count);

        [DllImport("Xeno.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern void Attach();

        private List<int> GetReadyClientPIDs()
        {
            var pids = new List<int>();

            try
            {
                IntPtr clientsPtr = GetClients();
                if (clientsPtr == IntPtr.Zero) return pids;

                string clientsJson = Marshal.PtrToStringAnsi(clientsPtr);
                var clientsList = JsonConvert.DeserializeObject<List<List<object>>>(clientsJson);

                if (clientsList != null)
                {
                    foreach (var client in clientsList)
                    {
                        if (client.Count >= 4)
                        {
                            int pid = Convert.ToInt32(client[0]);
                            int state = Convert.ToInt32(client[3]);

                            if (state == 3) // Ready state
                            {
                                pids.Add(pid);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle or log error
            }

            return pids;
        }

        public void ExecuteScriptOnClients(string script)
        {
            if (string.IsNullOrWhiteSpace(script))
            {
                MessageBox.Show("Script is empty.", "Empty Script", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var clientPIDs = GetReadyClientPIDs();

            if (clientPIDs.Count == 0)
            {
                MessageBox.Show("No ready clients found.\n\nMake sure you've pressed Attach and waited for injection to complete.",
                    "No Ready Clients", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            try
            {
                Execute(Encoding.UTF8.GetBytes(script + "\0"), clientPIDs.ToArray(), clientPIDs.Count);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Script execution failed:\n{ex.Message}", "Execution Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        string activeTab;

        private MonacoBridge _monaco;
        private string ScriptsDirectory =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UniversalExec", "Scripts");

        private void AddApplicationName()
        {
            var appName = new Label
            {
                AutoSize = true,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.Gainsboro,
                Location = new Point(45, 13),
                Text = "UNIVERSALEXEC"
            };
            Controls.Add(appName);
            appName.BringToFront();
        }

        private void BuildHomePage()
        {
            HomeTab.Controls.Clear();
            HomeTab.BackColor = Color.FromArgb(20, 20, 20);

            var layout = new TableLayoutPanel
            {
                BackColor = Color.Transparent,
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                Padding = new Padding(34, 28, 34, 24),
                RowCount = 3
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 108));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 205));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var heading = new Label
            {
                AutoSize = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Segoe UI", 25F, FontStyle.Bold),
                ForeColor = Color.White,
                Size = new Size(660, 52),
                Text = "Welcome to UniversalExec",
                TextAlign = ContentAlignment.MiddleLeft
            };
            var subtitle = new Label
            {
                AutoSize = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(165, 165, 175),
                Size = new Size(660, 30),
                Text = "A clean workspace for your code and personal script collection.",
                TextAlign = ContentAlignment.TopLeft
            };
            var welcome = new Panel { Dock = DockStyle.Fill };
            welcome.Controls.Add(heading);
            welcome.Controls.Add(subtitle);
            heading.Location = new Point(0, 4);
            heading.Height = 52;
            subtitle.Location = new Point(2, 62);
            subtitle.Height = 30;

            var cards = new TableLayoutPanel
            {
                BackColor = Color.Transparent,
                ColumnCount = 3,
                Dock = DockStyle.Fill,
                RowCount = 1
            };
            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.334F));
            cards.Controls.Add(CreateHomeCard("01", "Write code", "Open the editor to create or edit a script.", "OPEN EDITOR", () => SwitchTab(ExeTab)), 0, 0);
            cards.Controls.Add(CreateHomeCard("02", "Your scripts", "Import your own .lua and .txt files.", "OPEN SCRIPTHUB", () => SwitchTab(ScriptTab)), 1, 0);
            cards.Controls.Add(CreateHomeCard("03", "Get started", "Open the editor and attach when you are ready.", "GO TO EDITOR", () => SwitchTab(ExeTab)), 2, 0);

            var footer = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(130, 130, 142),
                Text = "Your scripts stay on this PC. Add them any time from ScriptHub.",
                TextAlign = ContentAlignment.MiddleLeft
            };

            layout.Controls.Add(welcome, 0, 0);
            layout.Controls.Add(cards, 0, 1);
            layout.Controls.Add(footer, 0, 2);
            HomeTab.Controls.Add(layout);
        }

        private static Panel CreateHomeCard(string number, string title, string description, string action, Action onClick)
        {
            var card = new Panel
            {
                BackColor = Color.FromArgb(29, 30, 36),
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 12, 0),
                Padding = new Padding(16)
            };
            var numberLabel = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(125, 115, 255),
                Location = new Point(16, 14),
                Text = number
            };
            var titleLabel = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(16, 43),
                Text = title
            };
            var descriptionLabel = new Label
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(165, 165, 175),
                Location = new Point(16, 76),
                Size = new Size(165, 47),
                Text = description
            };
            var button = new Button
            {
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.FromArgb(47, 48, 58),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(16, 145),
                Size = new Size(165, 34),
                Text = action,
                UseVisualStyleBackColor = false
            };
            button.FlatAppearance.BorderColor = Color.FromArgb(66, 67, 80);
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(72, 68, 124);
            button.Click += (_, _) => onClick();
            card.Controls.Add(numberLabel);
            card.Controls.Add(titleLabel);
            card.Controls.Add(descriptionLabel);
            card.Controls.Add(button);
            card.Resize += (_, _) => descriptionLabel.Width = Math.Max(100, card.ClientSize.Width - 32);
            return card;
        }

        private void SwitchTab(System.Windows.Forms.Panel tab)
        {
            // Hide all panels except the one we want to show
            foreach (var pnl in new[] { HomeTab, ExeTab, ScriptTab, SettingsTab })
            {
                pnl.Visible = (pnl == tab); // Only sets true for selected tab
            }

            tab.BringToFront();
            activeTab = tab.Name;
            CheckTab(); // Update button states based on the active tab)
        }

        private void CheckTab()
        {
            try
            {
                // Unhook event handlers to avoid recursion loop
                HomeBtn.CheckedChanged -= HomeBtn_Click;
                ExeBtn.CheckedChanged -= ExeBtn_Click;
                ScriptHubBtn.CheckedChanged -= ScriptHubBtn_Click;
                SetingsBtn.CheckedChanged -= SetingsBtn_Click;

                if (activeTab == "HomeTab")
                    HomeBtn.Checked = true;
                else if (activeTab == "ExeTab")
                    ExeBtn.Checked = true;
                else if (activeTab == "ScriptTab")
                    ScriptHubBtn.Checked = true;
                else if (activeTab == "SettingsTab")
                    SetingsBtn.Checked = true;
            }
            finally
            {
                // Re-hook event handlers
                HomeBtn.CheckedChanged += HomeBtn_Click;
                ExeBtn.CheckedChanged += ExeBtn_Click;
                ScriptHubBtn.CheckedChanged += ScriptHubBtn_Click;
                SetingsBtn.CheckedChanged += SetingsBtn_Click;
            }
        }

        private void LoadScripts()
        {
            flowLayoutPanel1.Controls.Clear();

            try
            {
                Directory.CreateDirectory(ScriptsDirectory);

                var files = Directory.GetFiles(ScriptsDirectory, "*.*")
                    .Where(f => f.EndsWith(".lua", StringComparison.OrdinalIgnoreCase) ||
                                f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                foreach (var file in files)
                {
                    var scriptControl = new ScriptHub(this);
                    scriptControl.SetFile(file);
                    flowLayoutPanel1.Controls.Add(scriptControl);
                }

                ScriptLabel.Text = files.Length == 1 ? "YOUR SCRIPTS (1)" : $"YOUR SCRIPTS ({files.Length})";
                if (files.Length == 0)
                {
                    flowLayoutPanel1.Controls.Add(new Label
                    {
                        AutoSize = false,
                        Font = new Font("Segoe UI", 11F),
                        ForeColor = Color.FromArgb(165, 165, 175),
                        Height = 100,
                        Text = "No scripts yet.\r\nUse IMPORT SCRIPTS to add your .lua or .txt files.",
                        TextAlign = ContentAlignment.MiddleCenter,
                        Width = flowLayoutPanel1.ClientSize.Width - SystemInformation.VerticalScrollBarWidth
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not load your scripts: {ex.Message}", "ScriptHub Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }





        private async void Form1_Load(object sender, EventArgs e)
        {
            SwitchTab(HomeTab);
            LoadScripts(); // Load scripts into the ScriptHub panel

            // Setup folder paths:
            string rootFolder = AppContext.BaseDirectory;   // App root folder
            string htmlFile = "Editor/index.html";          // AppFolder\Editor\index.html

            _monaco = new MonacoBridge(Editor, rootFolder, htmlFile);
            await _monaco.InitializeAsync(transparent: true);

            // Editor is fully ready here:
            await _monaco.SetTheme("Dark");
            await _monaco.SetMinimap(false);
            await _monaco.SetTransparent(true);

            LoadSettings(); // Load user settings
        }

        private void HomeBtn_Click(object sender, EventArgs e)
        {
            SwitchTab(HomeTab);
        }

        private void ExeBtn_Click(object sender, EventArgs e)
        {
            SwitchTab(ExeTab);
        }

        private void ScriptHubBtn_Click(object sender, EventArgs e)
        {
            SwitchTab(ScriptTab);
        }

        private void SetingsBtn_Click(object sender, EventArgs e)
        {
            SwitchTab(SettingsTab);
        }

        private void CloseBtn_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void MiniBtn_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
        }

        private async void ClearBtn_Click(object sender, EventArgs e)
        {
            await Editor.ExecuteScriptAsync($"SetText(``);");
        }

        private async void OpnFileBtn_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "All Supported Files (*.lua;*.txt)|*.lua;*.txt|Lua Files (*.lua)|*.lua|Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
                dialog.DefaultExt = "lua";
                dialog.FilterIndex = 1;
                dialog.Title = "Open Script File";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string script = File.ReadAllText(dialog.FileName);

                        // Fully qualified name ensures no overload ambiguity
                        string jsonScript = System.Text.Json.JsonSerializer.Serialize(script);

                        await Editor.CoreWebView2.ExecuteScriptAsync($"editor.setValue({jsonScript});");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to open file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private async void SveFileBtn_Click(object sender, EventArgs e)
        {
            try
            {
                SaveFileDialog saveFileDialog1 = new SaveFileDialog
                {
                    Filter = "Lua Files (*.lua)|*.lua|Text Files (*.txt)|*.txt",
                    DefaultExt = "lua",
                    Title = "Save Lua or Text File"
                };

                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    string textToSave = await Editor.ExecuteScriptAsync("GetText();");
                    string rawText = JsonConvert.DeserializeObject<string>(textToSave);
                    await Task.Run(() => File.WriteAllText(saveFileDialog1.FileName, rawText));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshBtn_Click(object sender, EventArgs e)
        {
            LoadScripts(); // Reload scripts into the ScriptHub panel
        }

        private void ImportScriptsBtn_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "Lua and text scripts (*.lua;*.txt)|*.lua;*.txt",
                Multiselect = true,
                Title = "Import Your Scripts"
            };

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            var conflicts = new List<string>();
            var errors = new List<string>();
            try
            {
                Directory.CreateDirectory(ScriptsDirectory);
                foreach (var source in dialog.FileNames)
                {
                    var destination = Path.Combine(ScriptsDirectory, Path.GetFileName(source));
                    try
                    {
                        File.Copy(source, destination, overwrite: false);
                    }
                    catch (IOException) when (File.Exists(destination))
                    {
                        conflicts.Add(Path.GetFileName(source));
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"{Path.GetFileName(source)}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not access your scripts folder: {ex.Message}", "Import Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            LoadScripts();
            var message = $"{dialog.FileNames.Length - conflicts.Count - errors.Count} script(s) imported.";
            if (conflicts.Count > 0)
                message += $"\r\nAlready in your library (not overwritten): {string.Join(", ", conflicts)}";
            if (errors.Count > 0)
                message += $"\r\nCould not import:\r\n{string.Join("\r\n", errors)}";

            MessageBox.Show(message, errors.Count > 0 ? "Import completed with errors" : "Import complete",
                MessageBoxButtons.OK, errors.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }

        private void OpenScriptsFolderBtn_Click(object sender, EventArgs e)
        {
            try
            {
                Directory.CreateDirectory(ScriptsDirectory);
                Process.Start(new ProcessStartInfo("explorer.exe", $"\"{ScriptsDirectory}\"")
                {
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open your scripts folder: {ex.Message}", "ScriptHub Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void guna2CustomCheckBox1_Click(object sender, EventArgs e)
        {

        }

        private void SettingsTab_Paint(object sender, PaintEventArgs e)
        {

        }

        private void TopMostTog_CheckedChanged(object sender, EventArgs e)
        {
            this.TopMost = false;
            this.TopMost = TopMostTog.Checked;

            Properties.Settings.Default.TopMost = TopMostTog.Checked;
            Properties.Settings.Default.Save();
        }

        private async void guna2ToggleSwitch2_CheckedChanged(object sender, EventArgs e)
        {
            await _monaco.SetMinimap(guna2ToggleSwitch2.Checked);

            Properties.Settings.Default.MiniMap = guna2ToggleSwitch2.Checked;
            Properties.Settings.Default.Save();
        }

        private void LoadSettings()
        {
            TopMostTog.Checked = Properties.Settings.Default.TopMost;
            guna2ToggleSwitch2.Checked = Properties.Settings.Default.MiniMap;
        }

        private void AttachBtn_Click(object sender, EventArgs e)
        {
            Attach(); // Call the Attach function from Xeno.dll
        }

        private async void ExecuteBtn_Click(object sender, EventArgs e)
        {
            string scriptToExecute = await Editor.ExecuteScriptAsync("GetText();");
            string rawScript = JsonConvert.DeserializeObject<string>(scriptToExecute);
            ExecuteScriptOnClients(rawScript);
        }
    }
}
