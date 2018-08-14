/////////////////////////////////////////////////////////////////////////////////
// CodeLab for Paint.NET
// Copyright �2006 Rick Brewster, Tom Jackson. All Rights Reserved.
// Portions Copyright �2007-2018 BoltBait. All Rights Reserved.
// Portions Copyright �2016-2018 Jason Wendt. All Rights Reserved.
// Portions Copyright �Microsoft Corporation. All Rights Reserved.
//
// THE CODELAB DEVELOPERS MAKE NO WARRANTY OF ANY KIND REGARDING THE CODE. THEY
// SPECIFICALLY DISCLAIM ANY WARRANTY OF FITNESS FOR ANY PARTICULAR PURPOSE OR
// ANY OTHER WARRANTY.  THE CODELAB DEVELOPERS DISCLAIM ALL LIABILITY RELATING
// TO THE USE OF THIS CODE.  NO LICENSE, EXPRESS OR IMPLIED, BY ESTOPPEL OR
// OTHERWISE, TO ANY INTELLECTUAL PROPERTY RIGHTS IS GRANTED HEREIN.
//
// Latest distribution: http://www.BoltBait.com/pdn/codelab
/////////////////////////////////////////////////////////////////////////////////

using Microsoft.Win32;
using PaintDotNet.AppModel;
using ScintillaNET;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace PaintDotNet.Effects
{
    internal partial class CodeLabConfigDialog : EffectConfigDialog
    {
        private const string ThisVersion = "3.5"; // Remember to change it in CodeLab.cs too!
        private const string WebUpdateFile = "http://www.boltbait.com/versions.txt"; // The web site to check for updates
        private const string ThisApplication = "1"; // in the WebUpadteFile, CodeLab is application #1
        // format of the versions.txt file:  application number;current version;URL to download current version
        // for example: 1;2.13;http://boltbait.com/pdn/CodeLab/CodeLab213.zip
        // each application on its own line
        #region Constructor
        private const string WindowTitle = "CodeLab v" + ThisVersion;
        private string FileName = "Untitled";
        private string FullScriptPath = "";
        private bool CheckForUpdates;
        private string UpdateURL = "";
        private string UpdateVER = "";
        private bool preview = false;
        private EffectConfigToken previewToken = null;
        private Color OriginalForeColor;
        private Color OriginalBackColor;

        public CodeLabConfigDialog()
        {
            InitializeComponent();

            #region Load Settings from registry
            RegistryKey settings = Registry.CurrentUser.OpenSubKey("Software\\CodeLab", true);
            if (settings == null)
            {
                Registry.CurrentUser.CreateSubKey("Software\\CodeLab").Flush();
                settings = Registry.CurrentUser.OpenSubKey("Software\\CodeLab", true);
            }
            if (1 == (int)settings.GetValue("WordWrap", 0))
            {
                txtCode.WrapMode = WrapMode.Whitespace;
                wordWrapToolStripMenuItem.CheckState = CheckState.Checked;
            }
            if (1 == (int)settings.GetValue("WhiteSpace", 0))
            {
                txtCode.ViewWhitespace = WhitespaceMode.VisibleAlways;
                whiteSpaceToolStripMenuItem.CheckState = CheckState.Checked;
                txtCode.WrapVisualFlags = WrapVisualFlags.Start;
            }
            if (1 == (int)settings.GetValue("CodeFolding", 0))
            {
                txtCode.CodeFoldingEnabled = true;
                codeFoldingToolStripMenuItem.CheckState = CheckState.Checked;
            }
            if (1 == (int)settings.GetValue("LineNumbers", 0))
            {
                txtCode.LineNumbersEnabled = true;
                lineNumbersToolStripMenuItem.CheckState = CheckState.Checked;
            }
            if (1 == (int)settings.GetValue("Bookmarks", 0))
            {
                txtCode.BookmarksEnabled = true;
                bookmarksToolStripMenuItem.CheckState = CheckState.Checked;
            }
            if (1 == (int)settings.GetValue("ToolBar", 1))
            {
                toolBarToolStripMenuItem.CheckState = CheckState.Checked;
                toolStrip1.Visible = true;
                txtCode.Location = new Point(txtCode.Left, toolStrip1.Bottom);
            }
            else
            {
                toolBarToolStripMenuItem.CheckState = CheckState.Unchecked;
                toolStrip1.Visible = false;
                txtCode.Location = new Point(txtCode.Left, toolStrip1.Top);
            }
            if (1 == (int)settings.GetValue("ErrorBox", 1))
            {
                viewCheckBoxes(true, false);
            }
            else if (1 == (int)settings.GetValue("Output", 1))
            {
                viewCheckBoxes(false, true);
            }
            else
            {
                viewCheckBoxes(false, false);
            }
            OriginalForeColor = this.ForeColor;
            OriginalBackColor = this.BackColor;
            if ((int)Theme.Auto == (int)settings.GetValue("EditorTheme", 0))
            {
                autoToolStripMenuItem.CheckState = CheckState.Checked;
                darkToolStripMenuItem.CheckState = CheckState.Unchecked;
                lightToolStripMenuItem.CheckState = CheckState.Unchecked;
            }
            else if ((int)Theme.Dark == (int)settings.GetValue("EditorTheme", 0))
            {
                this.ForeColor = Color.White;
                this.BackColor = Color.FromArgb(40, 40, 40);
                txtCode.Theme = Theme.Dark;
                autoToolStripMenuItem.CheckState = CheckState.Unchecked;
                darkToolStripMenuItem.CheckState = CheckState.Checked;
                lightToolStripMenuItem.CheckState = CheckState.Unchecked;
            }
            else if ((int)Theme.Light == (int)settings.GetValue("EditorTheme", 0))
            {
                this.ForeColor = Color.Black;
                this.BackColor = Color.White;
                txtCode.Theme = Theme.Light;
                autoToolStripMenuItem.CheckState = CheckState.Unchecked;
                darkToolStripMenuItem.CheckState = CheckState.Unchecked;
                lightToolStripMenuItem.CheckState = CheckState.Checked;
            }
            if (1 == (int)settings.GetValue("LargeFonts", 0))
            {
                txtCode.Zoom = 2;
                largeFontToolStripMenuItem.CheckState = CheckState.Checked;
            }
            if (1 == (int)settings.GetValue("Map", 0))
            {
                txtCode.MapEnabled = true;
                indicatorMapMenuItem.CheckState = CheckState.Checked;
            }
            if (1 == (int)settings.GetValue("CheckForUpdates", 1))
            {
                CheckForUpdates = true;
                checkForUpdatesToolStripMenuItem.CheckState = CheckState.Checked;
                GoCheckForUpdates(true, false);
            }
            else
            {
                checkForUpdatesToolStripMenuItem.CheckState = CheckState.Unchecked;
            }
            string editorFont = (string)settings.GetValue("FontFamily", "Courier New");
            if (!IsFontInstalled(editorFont))
            {
                editorFont = "Courier New";
            }
            if (!IsFontInstalled(editorFont))
            {
                editorFont = "Verdana";
            }
            fontsCourierMenuItem.CheckState = ("Courier New" == editorFont) ? CheckState.Checked : CheckState.Unchecked;
            fontsConsolasMenuItem.CheckState = ("Consolas" == editorFont) ? CheckState.Checked : CheckState.Unchecked;
            fontsEnvyRMenuItem.CheckState = ("Envy Code R" == editorFont) ? CheckState.Checked : CheckState.Unchecked;
            fontsHackMenuItem.CheckState = ("Hack" == editorFont) ? CheckState.Checked : CheckState.Unchecked;
            fontsVerdanaMenuItem.CheckState = ("Verdana" == editorFont) ? CheckState.Checked : CheckState.Unchecked;
            txtCode.Styles[Style.Default].Font = editorFont;
            OutputTextBox.Font = new Font(editorFont, OutputTextBox.Font.Size);
            errorList.Font = new Font(editorFont, errorList.Font.Size);
            settings.Close();
            #endregion

            // Disable menu items if they'll have no effect
            transparencyToolStripMenuItem.Enabled = EnableOpacity;
            fontsCourierMenuItem.Enabled = IsFontInstalled("Courier New");
            fontsConsolasMenuItem.Enabled = IsFontInstalled("Consolas");
            fontsEnvyRMenuItem.Enabled = IsFontInstalled("Envy Code R");
            fontsHackMenuItem.Enabled = IsFontInstalled("Hack");

            if (fontsCourierMenuItem.Enabled) fontsCourierMenuItem.Font = new Font("Courier New", fontsCourierMenuItem.Font.Size);
            if (fontsConsolasMenuItem.Enabled) fontsConsolasMenuItem.Font = new Font("Consolas", fontsConsolasMenuItem.Font.Size);
            if (fontsEnvyRMenuItem.Enabled) fontsEnvyRMenuItem.Font = new Font("Envy Code R", fontsEnvyRMenuItem.Font.Size);
            if (fontsHackMenuItem.Enabled) fontsHackMenuItem.Font = new Font("Hack", fontsHackMenuItem.Font.Size);
            if (fontsVerdanaMenuItem.Enabled) fontsVerdanaMenuItem.Font = new Font("Verdana", fontsVerdanaMenuItem.Font.Size);

            // PDN Theme
            ApplyTheme();
            txtCode.Theme = (PdnTheme.BackColor.R < 128 && PdnTheme.BackColor.G < 128 && PdnTheme.BackColor.B < 128) ? Theme.Dark : Theme.Light;

            ResetScript();
            Build();
            txtCode.Focus();
        }
        #endregion

        #region Token functions
        protected override void InitTokenFromDialog()
        {
            CodeLabConfigToken sect = (CodeLabConfigToken)theEffectToken;
            sect.UserCode = txtCode.Text;
            sect.UserScriptObject = ScriptBuilder.UserScriptObject;
            sect.ScriptName = FileName;
            sect.ScriptPath = FullScriptPath;
            sect.Dirty = txtCode.IsDirty;
            sect.Preview = preview;
            sect.PreviewToken = previewToken;
            sect.Bookmarks = txtCode.Bookmarks;
        }

        protected override void InitDialogFromToken(EffectConfigToken effectTokenCopy)
        {
            CodeLabConfigToken sect = (CodeLabConfigToken)effectTokenCopy;

            if (sect != null)
            {
                FileName = sect.ScriptName;
                FullScriptPath = sect.ScriptPath;
                txtCode.Text = sect.UserCode;
                txtCode.ExecuteCmd(Command.ScrollToEnd); // Workaround for a scintilla bug
                txtCode.ExecuteCmd(Command.ScrollToStart);
                txtCode.EmptyUndoBuffer();
                if (!sect.Dirty)
                {
                    txtCode.SetSavePoint();
                }
                txtCode.Bookmarks = sect.Bookmarks;
            }
        }

        protected override void InitialInitToken()
        {
            theEffectToken = new CodeLabConfigToken
            {
                UserCode = ScriptWriter.DefaultCode,
                UserScriptObject = null,
                ScriptName = "Untitled",
                ScriptPath = "",
                Dirty = false,
                Preview = false,
                PreviewToken = null,
                Bookmarks = new int[0]
            };
        }
        #endregion

        #region Build Script actions
        private void Build()
        {
            ScriptBuilder.Build(txtCode.Text, OutputTextBox.Visible);

            DisplayErrors();

            txtCode.UpdateSyntaxHighlighting();

            FinishTokenUpdate();
        }

        private void RunWithDialog()
        {
            tmrCompile.Enabled = false;
            Build();
            if (errorList.Items.Count != 0)
            {
                MessageBox.Show("Before you can preview your effect, you must resolve all code errors.", "Build Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                if (!ScriptBuilder.BuildFullPreview(txtCode.Text))
                {
                    MessageBox.Show("Something went wrong, and the Preview can't be run.", "Preview Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else if (!ScriptBuilder.UserScriptObject.CheckForEffectFlags(EffectFlags.Configurable))
                {
                    MessageBox.Show("There are no UI controls, so the Preview can't be displayed.", "Preview Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    ScriptBuilder.UserScriptObject.EnvironmentParameters = this.Effect.EnvironmentParameters;
                    using (EffectConfigDialog previewDialog = ScriptBuilder.UserScriptObject.CreateConfigDialog())
                    {
                        preview = true;
                        previewToken = previewDialog.EffectToken;
                        previewDialog.EffectTokenChanged += (sender, e) => FinishTokenUpdate();

                        previewDialog.ShowDialog();
                    }

                    preview = false;
                    previewToken = null;
                    Build();
                }
            }
            tmrCompile.Enabled = true;
        }

        private void DisplayErrors()
        {
            errorList.Items.Clear();
            toolTips.SetToolTip(errorList, "");
            txtCode.errorLines.Clear();

            txtCode.IndicatorCurrent = Indicator.Error;
            txtCode.IndicatorClearRange(0, txtCode.TextLength); // Clear underlines from the previous time
            ShowErrors.Text = "Show Errors List";
            ShowErrors.ForeColor = this.ForeColor;

            if (ScriptBuilder.Errors.Count == 0)
            {
                return;
            }

            foreach (ScriptError err in ScriptBuilder.Errors)
            {
                errorList.Items.Add(err);

                if (err.Line < 1)
                {
                    continue;
                }

                txtCode.errorLines.Add(err.Line - 1);

                int errPosition = txtCode.Lines[err.Line - 1].Position + err.Column;
                int errorLength = txtCode.GetWordFromPosition(errPosition).Length;

                // if error is at the end of the line (missing semi-colon), or is a stray '.'
                if (errorLength == 0 || errPosition == txtCode.Lines[err.Line - 1].EndPosition - 2)
                {
                    errPosition--;
                    errorLength = 1;
                }

                // Underline the error
                txtCode.IndicatorFillRange(errPosition, errorLength);
            }
            ShowErrors.Text = $"Show Errors List ({errorList.Items.Count})";
            ShowErrors.ForeColor = Color.Red;
        }

        private void ResetScript()
        {
            InitialInitToken();
            InitDialogFromToken();
            FinishTokenUpdate();
        }

        private DialogResult PromptToSave()
        {
            DialogResult dr = MessageBox.Show(this, $"Do you want to save changes to '{FileName}'?", "Script Editor", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            switch (dr)
            {
                case DialogResult.Yes:
                    if (!SaveScript())
                    {
                        txtCode.Focus();
                        return DialogResult.Cancel;
                    }
                    txtCode.SetSavePoint();
                    return DialogResult.None;
                case DialogResult.No:
                    return DialogResult.None;
                case DialogResult.Cancel:
                    txtCode.Focus();
                    return DialogResult.Cancel;
            }
            return DialogResult.None;
        }

        private bool SaveScript()
        {
            if (FullScriptPath.IsNullOrEmpty() || !File.Exists(FullScriptPath))
            {
                return SaveAsScript();
            }

            bool saved = false;
            try
            {
                File.WriteAllText(FullScriptPath, txtCode.Text);
                saved = true;
            }
            catch
            {
            }

            return saved;
        }

        private bool SaveAsScript()
        {
            RegistryKey settings = Registry.CurrentUser.OpenSubKey("Software\\CodeLab", true);
            if (settings == null)
            {
                Registry.CurrentUser.CreateSubKey("Software\\CodeLab").Flush();
                settings = Registry.CurrentUser.OpenSubKey("Software\\CodeLab", true);
            }
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string initialDir = (string)settings.GetValue("LastSourceDir", desktopPath);

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Title = "Save User Script";
            sfd.DefaultExt = ".cs";
            sfd.Filter = "C# Code Files (*.CS)|*.cs";
            sfd.OverwritePrompt = true;
            sfd.AddExtension = true;
            sfd.FileName = (FileName == "Untitled") ? "MyScript.cs" : FileName + ".cs";
            sfd.InitialDirectory = initialDir;
            sfd.FileOk += (object sender, System.ComponentModel.CancelEventArgs e) =>
            {
                if (!char.IsLetter(Path.GetFileName(sfd.FileName), 0))
                {
                    e.Cancel = true;
                    MessageBox.Show("The filename must begin with a letter.", "Save User Script", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            bool saved = false;
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    File.WriteAllText(sfd.FileName, txtCode.Text);
                    FullScriptPath = sfd.FileName;
                    settings.SetValue("LastSourceDir", Path.GetDirectoryName(sfd.FileName));
                    FileName = Path.GetFileNameWithoutExtension(sfd.FileName);
                    AddToRecents(sfd.FileName);
                    saved = true;
                }
                catch
                {
                }
            }

            settings.Close();
            sfd.Dispose();
            return saved;
        }

        private bool LoadScript()
        {
            RegistryKey settings = Registry.CurrentUser.OpenSubKey("Software\\CodeLab", true);
            if (settings == null)
            {
                Registry.CurrentUser.CreateSubKey("Software\\CodeLab").Flush();
                settings = Registry.CurrentUser.OpenSubKey("Software\\CodeLab", true);
            }
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string initialDir = (string)settings.GetValue("LastSourceDir", desktopPath);

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Load User Script";
            ofd.DefaultExt = ".cs";
            ofd.Filter = "C# Code Files (*.CS)|*.cs";
            ofd.DefaultExt = ".cs";
            ofd.Multiselect = false;
            ofd.InitialDirectory = initialDir;

            bool loaded = false;
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    FullScriptPath = ofd.FileName;
                    FileName = Path.GetFileNameWithoutExtension(ofd.FileName);
                    settings.SetValue("LastSourceDir", Path.GetDirectoryName(ofd.FileName));
                    txtCode.Text = File.ReadAllText(ofd.FileName);
                    txtCode.ExecuteCmd(Command.ScrollToEnd); // Workaround for a scintilla bug
                    txtCode.ExecuteCmd(Command.ScrollToStart);
                    txtCode.EmptyUndoBuffer();
                    AddToRecents(ofd.FileName);
                    loaded = true;
                }
                catch
                {
                }
            }

            settings.Close();
            ofd.Dispose();
            return loaded;
        }

        private void txtCode_BuildNeeded(object sender, EventArgs e)
        {
            Build();
        }
        #endregion

        #region Error listbox functions
        private void errorListMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (errorList.SelectedIndex < 0)
            {
                e.Cancel = true;
                return;
            }

            ErrorCodeMenuItem.Visible = (errorList.SelectedItem is ScriptError error && error.ErrorNumber != string.Empty);
        }

        private void CopyErrorMenuItem_Click(object sender, EventArgs e)
        {
            if (errorList.SelectedIndex > -1)
            {
                string errorMsg = (errorList.SelectedItem is ScriptError error) ? error.ErrorText : errorList.SelectedItem.ToString();
                if (!errorMsg.IsNullOrEmpty())
                {
                    Clipboard.SetText(errorMsg);
                }
            }
        }

        private void FullErrorMenuItem_Click(object sender, EventArgs e)
        {
            if (errorList.SelectedIndex > -1)
            {
                using (ViewSrc VSW = new ViewSrc("Full Error Message", errorList.SelectedItem.ToString(), false))
                {
                    VSW.ShowDialog();
                }
            }
        }

        private void ErrorCodeMenuItem_Click(object sender, EventArgs e)
        {
            if (errorList.SelectedIndex > -1 && errorList.SelectedItem is ScriptError error)
            {
                Services.GetService<IShellService>().LaunchUrl(null, $"https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/{error.ErrorNumber}");
            }
        }

        private void errorList_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                errorList.SelectedIndex = errorList.IndexFromPoint(e.Location);
            }
        }

        private void listErrors_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (errorList.SelectedIndex >= 0)
            {
                ScrollToError();
                using (ViewSrc VSW = new ViewSrc("Full Error Message", errorList.SelectedItem.ToString(), false))
                {
                    VSW.ShowDialog();
                }
            }
        }

        private void listErrors_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (errorList.SelectedIndex >= 0)
            {
                ScrollToError();
            }
        }

        private void ScrollToError()
        {
            if (errorList.SelectedIndex >= 0 && errorList.SelectedItem is ScriptError errw && errw.Line > 0)
            {
                txtCode.SetEmptySelection(txtCode.Lines[errw.Line - 1].Position + errw.Column);
                txtCode.Lines[errw.Line - 1].EnsureVisible();
                txtCode.ScrollCaret();    // Make error visible by scrolling to it
                txtCode.Focus();
            }
            toolTips.SetToolTip(errorList, errorList.SelectedItem.ToString().InsertLineBreaks(100));
        }
        #endregion

        #region Timer tick Event functions
        private void tmrExceptionCheck_Tick(object sender, EventArgs e)
        {
            CodeLabConfigToken sect = (CodeLabConfigToken)theEffectToken;

            if (sect.LastExceptions.Count > 0)
            {
                string exc = sect.LastExceptions[0].ToString();
                sect.LastExceptions.Clear();

                string numString = exc.Substring(exc.IndexOf(".0.cs:line ", StringComparison.Ordinal) + 11, 4).Trim();
                if (int.TryParse(numString, out int lineNum))
                {
                    lineNum -= ScriptBuilder.LineOffset;
                }

                errorList.Items.Add($"Unhandled Exception at line {lineNum}: \r\n{exc}");
                ShowErrors.Text = $"Show Errors List ({errorList.Items.Count})";
                ShowErrors.ForeColor = Color.Red;
            }

            if (sect.Output.Count > 0)
            {
                string output = sect.Output[0];
                sect.Output.Clear();

                if (output.Trim() != string.Empty)
                {
                    OutputTextBox.AppendText(output);
                }
            }
        }

        private void tmrCompile_Tick(object sender, EventArgs e)
        {
            tmrCompile.Enabled = false;
            DisplayUpdates(true);
            UpdateToolBarButtons();
            Build();
        }
        #endregion

        #region Dialog functions - Load Icons, keyboard events
        protected override void OnLoad(EventArgs e)
        {
            this.Opacity = 1.00;
            toolStripMenuItem2.Checked = false;
            toolStripMenuItem3.Checked = false;
            toolStripMenuItem4.Checked = false;
            toolStripMenuItem5.Checked = true;
            txtCode.Focus();

            base.OnLoad(e);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            tmrCompile.Enabled = false;
            tmrExceptionCheck.Enabled = false;

            base.OnFormClosing(e);
        }

        private void txtCode_KeyUp(object sender, KeyEventArgs e)
        {
            if (
                (e.KeyCode == Keys.Delete) ||
                (e.Control && (e.KeyCode == Keys.V)) ||
                (e.Shift && (e.KeyCode == Keys.Insert))
               )
            {
                // Reset idle timer
                tmrCompile.Enabled = false;
                tmrCompile.Enabled = true;
            }
            if (e.KeyCode == Keys.F1)
            {
                helpTopicsToolStripMenuItem_Click(sender, EventArgs.Empty);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            base.OnKeyUp(e);
            UpdateToolBarButtons();
        }

        private void txtCode_KeyPress(object sender, KeyPressEventArgs e)
        {
            base.OnKeyPress(e);
            // Reset idle timer
            tmrCompile.Enabled = false;
            tmrCompile.Enabled = true;
            UpdateToolBarButtons();
        }

        private void btnBuild_Click(object sender, EventArgs e)
        {
            Build();
            txtCode.Focus();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            tmrCompile.Enabled = false;
            Build();
            tmrExceptionCheck.Enabled = false;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            tmrExceptionCheck.Enabled = false;
            tmrCompile.Enabled = false;
        }

        private static bool IsFontInstalled(string fontName)
        {
            using (Font font = new Font(fontName, 12f))
            {
                return font.Name == fontName;
            }
        }

        private void ApplyTheme()
        {
            PdnTheme.ForeColor = this.ForeColor;
            PdnTheme.BackColor = this.BackColor;
            toolStrip1.Renderer = PdnTheme.Renderer;
            menuStrip1.Renderer = PdnTheme.Renderer;
            contextMenuStrip1.Renderer = PdnTheme.Renderer;
            errorList.ForeColor = PdnTheme.ForeColor;
            errorList.BackColor = PdnTheme.BackColor;
            OutputTextBox.ForeColor = PdnTheme.ForeColor;
            OutputTextBox.BackColor = PdnTheme.BackColor;
            ShowErrors.ForeColor = (ShowErrors.Text == "Show Errors List") ? this.ForeColor : Color.Red;
        }
        #endregion

        #region Freshness Check functions
        private void DisplayUpdates(bool silentMode)
        {
            if (UpdateURL != "")
            {
                if (txtCode.Focused) // only popup if code editor has focus (otherwise, we might be doing something that we shouldn't interrupt)
                {
                    if (MessageBox.Show("An update to CodeLab is available.\n\nWould you like to download CodeLab v" + UpdateVER + "?\n\n(This will not close your current CodeLab session.)", "CodeLab Updater", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.Yes)
                    {
                        Services.GetService<IShellService>().LaunchUrl(null, UpdateURL);
                    }
                    else
                    {
                        UpdateURL = "";
                    }
                }
            }
            else if (!silentMode)
            {
                if (UpdateVER == ThisVersion)
                {
                    MessageBox.Show("You are up-to-date!", "CodeLab Updater", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("I'm not sure if you are up-to-date.\n\nI was not able to reach the update website.\n\nTry again later.", "CodeLab Updater", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void GoCheckForUpdates(bool silentMode, bool force)
        {
            UpdateVER = "";
            UpdateURL = "";

            if (WebUpdateFile == "") return;

            if (!force)
            {
                RegistryKey settings = Registry.CurrentUser.OpenSubKey("Software\\CodeLab", true);
                if (settings == null)
                {
                    Registry.CurrentUser.CreateSubKey("Software\\CodeLab").Flush();
                    settings = Registry.CurrentUser.OpenSubKey("Software\\CodeLab", true);
                }
                string PreviousCheck = (string)settings.GetValue("LatestUpdateCheck", string.Empty);
                settings.Close();

                if (PreviousCheck != string.Empty)
                {
                    DateTime LastCheck;
                    if (DateTime.TryParseExact(PreviousCheck, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out LastCheck))
                    {
                        // only check for updates every 7 days
                        if (Math.Abs((LastCheck - DateTime.Today).TotalDays) < 7)
                        {
                            return; // not time yet
                        }
                    }
                    else
                    {
                        // Date in registry didn't parse correctly so update it to today
                        using (settings = Registry.CurrentUser.OpenSubKey("Software\\CodeLab", true))
                        {
                            if (settings != null)
                            {
                                settings.SetValue("LatestUpdateCheck", DateTime.Now.ToString("yyyy-MM-dd"));
                            }
                        }
                    }
                }
            }

            Random r = new Random(); // defeat any cache by appending a random number to the URL

            WebClient web = new WebClient();
            web.OpenReadAsync(new Uri(WebUpdateFile + "?r=" + r.Next(int.MaxValue).ToString()));

            web.OpenReadCompleted += (sender, e) =>
            {
                try
                {
                    string text = "";
                    Stream stream = e.Result;
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        text = reader.ReadToEnd();
                    }
                    string[] lines = text.Split('\n');
                    for (int i = 0; i < lines.Length; i++)
                    {
                        string[] data = lines[i].Split(';');
                        if (data.Length >= 2)
                        {
                            if (data[0].Trim() == ThisApplication.Trim())
                            {
                                UpdateVER = data[1].Trim();
                                if (data[1].Trim() != ThisVersion.Trim())
                                {
                                    UpdateURL = data[2].Trim();
                                }
                            }
                        }
                    }
                }
                catch
                {
                    UpdateVER = "";
                    UpdateURL = "";
                }
                using (RegistryKey settings = Registry.CurrentUser.OpenSubKey("Software\\CodeLab", true))
                {
                    if (settings != null)
                    {
                        settings.SetValue("LatestUpdateCheck", DateTime.Now.ToString("yyyy-MM-dd"));
                    }
                }
                DisplayUpdates(silentMode);
            };
        }
        #endregion

        #region Common functions for button/menu events
        private void CreateNewFile()
        {
            if (txtCode.IsDirty && PromptToSave() == DialogResult.Cancel)
            {
                return;
            }

            FileNew fn = new FileNew();
            if (fn.ShowDialog() == DialogResult.OK)
            {
                FileName = "Untitled";
                FullScriptPath = "";
                this.Text = FileName + "* - " + WindowTitle;
                txtCode.Text = fn.CodeTemplate;
                txtCode.ExecuteCmd(Command.ScrollToEnd); // Workaround for a scintilla bug
                txtCode.ExecuteCmd(Command.ScrollToStart);
                txtCode.EmptyUndoBuffer();
                Build();
            }
            fn.Dispose();

            txtCode.Focus();
        }

        private void OpenFile()
        {
            if (txtCode.IsDirty && PromptToSave() == DialogResult.Cancel)
            {
                return;
            }

            LoadScript();
            txtCode.SetSavePoint();
            txtCode.Focus();
            Build();
        }

        private void Save()
        {
            if (SaveScript())
            {
                txtCode.SetSavePoint();
            }
            txtCode.Focus();
        }

        private void SaveAs()
        {
            if (SaveAsScript())
            {
                txtCode.SetSavePoint();
            }
            txtCode.Focus();
        }

        private void SaveAsDLL()
        {
            Build();
            if (errorList.Items.Count != 0)
            {
                MessageBox.Show("Before you can build a DLL, you must resolve all code errors.", "Build Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (FileName == "Untitled" || FileName == "")
            {
                MessageBox.Show("Before you can build a DLL, you must first save your source file using the File > Save as... menu.", "Build Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                string fullPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                fullPath = Path.Combine(fullPath, FileName);
                fullPath = Path.ChangeExtension(fullPath, ".dll");
                // Let the user pick the submenu, menu name, and icon
                BuildForm myBuildForm = new BuildForm(FileName.Trim(), txtCode.Text, FullScriptPath);
                if (myBuildForm.ShowDialog() == DialogResult.OK)
                {
                    // Everything is OK, BUILD IT!
                    if (ScriptBuilder.BuildDll(txtCode.Text, FullScriptPath, myBuildForm.SubMenuStr, myBuildForm.MenuStr, myBuildForm.IconPathStr, myBuildForm.Author, myBuildForm.MajorVer, myBuildForm.MinorVer, myBuildForm.Support, myBuildForm.WindowTitleTextStr, myBuildForm.isAdjustment, myBuildForm.Description, myBuildForm.KeyWords, myBuildForm.ForceAliasSelection, myBuildForm.ForceSingleThreaded, myBuildForm.HelpType, myBuildForm.HelpStr))
                    {
                        MessageBox.Show("Build succeeded!\r\n\r\nFile \"" + fullPath.Trim() + "\" created.\r\n\r\nYou will need to copy the file from your desktop to the Effects folder and restart Paint.NET to see it in the Effects menu.  An Install batch file has been placed on your desktop for this purpose.", "Build Finished", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        DisplayErrors();
                        MessageBox.Show("I'm sorry, I was not able to build the DLL.\r\n\r\nPerhaps the file already exists and is marked 'read only' or is in use by Paint.NET.  There may be other build errors listed in the box below.", "Build Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void UIDesigner()
        {
            txtCode.ReNumberUIVariables();
            // User Interface Designer
            UIBuilder myUIBuilderForm = new UIBuilder(txtCode.Text, ColorBgra.Black);  // This should be the current Primary color
            if (myUIBuilderForm.ShowDialog() == DialogResult.OK)
            {
                // update generated code
                txtCode.BeginUndoAction();
                if (txtCode.Text.Contains("#region UICode"))
                {
                    int uiRegPos = txtCode.Text.IndexOf("#region UICode", StringComparison.Ordinal);
                    int startLine = txtCode.LineFromPosition(uiRegPos) + 1;
                    int startPos = txtCode.Lines[startLine].Position;
                    int endPos = txtCode.Text.IndexOf("#endregion", StringComparison.Ordinal);
                    txtCode.SetTargetRange(startPos, endPos);
                    txtCode.ReplaceTarget(myUIBuilderForm.UIControlsText);
                }
                else
                {
                    for (int i = 0; i < txtCode.Lines.Count; i++)
                    {
                        string lineText = txtCode.Lines[i].Text;
                        if (lineText.Contains("int Amount1") || lineText.Contains("int Amount2") || lineText.Contains("int Amount3"))
                        {
                            int startPos = txtCode.Lines[i].Position;
                            int length = txtCode.Lines[i].EndPosition - startPos;
                            txtCode.DeleteRange(startPos, length);
                            i--;
                        }
                    }
                    txtCode.InsertText(0, "#region UICode\r\n" + myUIBuilderForm.UIControlsText + "#endregion\r\n");
                }
                txtCode.EndUndoAction();
            }
            Build();
        }

        private void CutSelection()
        {
            txtCode.Cut();
            tmrCompile.Enabled = false;
            tmrCompile.Enabled = true;
        }

        private void CopySelection()
        {
            txtCode.Copy();
        }

        private void PasteSelection()
        {
            txtCode.Paste();
            tmrCompile.Enabled = false;
            tmrCompile.Enabled = true;
        }

        private void FindCommand()
        {
            txtCode.FindAndReplace(false);
        }

        private void ReplaceCommand()
        {
            txtCode.FindAndReplace(true);
        }

        private void UndoCommand()
        {
            txtCode.Undo();
            tmrCompile.Enabled = false;
            tmrCompile.Enabled = true;
        }

        private void RedoCommand()
        {
            txtCode.Redo();
            tmrCompile.Enabled = false;
            tmrCompile.Enabled = true;
        }

        private void SelectAllCommand()
        {
            txtCode.SelectAll();
        }

        private void IndentCommand()
        {
            txtCode.Indent();
        }

        private void UndentCommand()
        {
            txtCode.UnIndent();
        }

        private void CommentCommand()
        {
            txtCode.Comment();
            tmrCompile.Enabled = false;
            tmrCompile.Enabled = true;
        }

        private void UnCommentCommand()
        {
            txtCode.UnComment();
            tmrCompile.Enabled = false;
            tmrCompile.Enabled = true;
        }

        private void RunCommand()
        {
            double SaveOpacitySetting = Opacity;
            Opacity = 0;
            RunWithDialog();
            Opacity = SaveOpacitySetting;
        }
        #endregion

        #region File Menu Event functions
        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CreateNewFile();
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFile();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Save();
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveAs();
        }

        private void saveAsDLLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveAsDLL();
        }

        private void userInterfaceDesignerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UIDesigner();
        }

        private void userInterfaceRenumberToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txtCode.ReNumberUIVariables();
        }

        private void previewEffectMenuItem_Click(object sender, EventArgs e)
        {
            RunCommand();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tmrCompile.Enabled = false;
            this.Close();
        }
        #endregion

        #region Edit menu Event functions
        private void editToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            this.cutToolStripMenuItem1.Enabled = txtCode.SelectedText.Length > 0;
            this.copyToolStripMenuItem1.Enabled = txtCode.SelectedText.Length > 0;
            this.selectAllToolStripMenuItem1.Enabled = txtCode.TextLength > 0;
            this.indentToolStripMenuItem.Enabled = true;
            this.unindentToolStripMenuItem.Enabled = true;
            this.pasteToolStripMenuItem1.Enabled = txtCode.CanPaste;
            this.commentSelectionToolStripMenuItem.Enabled = true;
            this.uncommentSelectionToolStripMenuItem.Enabled = true;
            this.undoToolStripMenuItem1.Enabled = txtCode.CanUndo;
            this.redoToolStripMenuItem1.Enabled = txtCode.CanRedo;
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UndoCommand();
        }

        private void redoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RedoCommand();
        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectAllCommand();
        }

        private void searchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FindCommand();
        }

        private void replaceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ReplaceCommand();
        }

        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CutSelection();
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CopySelection();
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PasteSelection();
        }

        private void indentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            IndentCommand();
        }

        private void unindentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UndentCommand();
        }

        private void commentSelectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CommentCommand();
        }

        private void uncommentSelectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UnCommentCommand();
        }
        #endregion

        #region View menu Event functions
        private void toolBarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!toolStrip1.Visible)
            {
                Registry.SetValue("HKEY_CURRENT_USER\\Software\\CodeLab", "ToolBar", 1);
                toolBarToolStripMenuItem.CheckState = CheckState.Checked;
                toolStrip1.Visible = true;
                txtCode.Location = new Point(txtCode.Left, toolStrip1.Bottom);
                txtCode.Height = txtCode.Height - toolStrip1.Height;
            }
            else
            {
                Registry.SetValue("HKEY_CURRENT_USER\\Software\\CodeLab", "ToolBar", 0);
                toolBarToolStripMenuItem.CheckState = CheckState.Unchecked;
                toolStrip1.Visible = false;
                txtCode.Location = new Point(txtCode.Left, toolStrip1.Top);
                txtCode.Height = txtCode.Height + toolStrip1.Height;
            }
            txtCode.Focus();
        }

        private void viewCheckBoxes(bool ErrorsVisible, bool DebugVisible)
        {
            if (ErrorsVisible)
            {
                Registry.SetValue("HKEY_CURRENT_USER\\Software\\CodeLab", "ErrorBox", 1);
                Registry.SetValue("HKEY_CURRENT_USER\\Software\\CodeLab", "Output", 0);
                viewErrorsToolStripMenuItem.CheckState = CheckState.Checked;
                viewDebugToolStripMenuItem.CheckState = CheckState.Unchecked;
                ShowErrors.Checked = true;
                ShowOutput.Checked = false;
                errorList.Visible = true;
                OutputTextBox.Visible = false;
                ClearOutput.Enabled = false;
                txtCode.Height = errorList.Top - txtCode.Top + 1;
            }
            else if (DebugVisible)
            {
                Registry.SetValue("HKEY_CURRENT_USER\\Software\\CodeLab", "ErrorBox", 0);
                Registry.SetValue("HKEY_CURRENT_USER\\Software\\CodeLab", "Output", 1);
                viewErrorsToolStripMenuItem.CheckState = CheckState.Unchecked;
                viewDebugToolStripMenuItem.CheckState = CheckState.Checked;
                ShowErrors.Checked = false;
                ShowOutput.Checked = true;
                errorList.Visible = false;
                OutputTextBox.Visible = true;
                ClearOutput.Enabled = true;
                txtCode.Height = errorList.Top - txtCode.Top + 1;
            }
            else
            {
                Registry.SetValue("HKEY_CURRENT_USER\\Software\\CodeLab", "ErrorBox", 0);
                Registry.SetValue("HKEY_CURRENT_USER\\Software\\CodeLab", "Output", 0);
                viewErrorsToolStripMenuItem.CheckState = CheckState.Unchecked;
                viewDebugToolStripMenuItem.CheckState = CheckState.Unchecked;
                ShowErrors.Checked = false;
                ShowOutput.Checked = false;
                errorList.Visible = false;
                OutputTextBox.Visible = false;
                ClearOutput.Enabled = false;
                txtCode.Height = errorList.Bottom - txtCode.Top;
            }
        }

        private void viewErrorsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ShowErrors.Checked)
            {
                viewCheckBoxes(false, false);
            }
            else
            {
                viewCheckBoxes(true, false);
            }
            txtCode.Focus();
        }

        private void viewDebugToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ShowOutput.Checked)
            {
                viewCheckBoxes(false, false);
            }
            else
            {
                viewCheckBoxes(false, true);
            }
            txtCode.Focus();
        }

        private void ShowErrors_Click(object sender, EventArgs e)
        {
            if (ShowErrors.Checked)
            {
                viewCheckBoxes(true, false);
            }
            else
            {
                viewCheckBoxes(false, false);
            }
            txtCode.Focus();
        }

        private void ShowOutput_Click(object sender, EventArgs e)
        {
            if (ShowOutput.Checked)
            {
                viewCheckBoxes(false, true);
            }
            else
            {
                viewCheckBoxes(false, false);
            }
            txtCode.Focus();
        }

        private void largeFontToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (txtCode.Zoom != 2)
            {
                txtCode.Zoom = 2;
                Registry.SetValue("HKEY_CURRENT_USER\\Software\\CodeLab", "LargeFonts", 1);
                largeFontToolStripMenuItem.CheckState = CheckState.Checked;
            }
            else
            {
                txtCode.Zoom = 0;
                Registry.SetValue("HKEY_CURRENT_USER\\Software\\CodeLab", "LargeFonts", 0);
                largeFontToolStripMenuItem.CheckState = CheckState.Unchecked;
            }
            txtCode.Focus();
        }

        private void wordWrapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (txtCode.WrapMode == WrapMode.None)
            {
                txtCode.WrapMode = WrapMode.Whitespace;
                Registry.SetValue("HKEY_CURRENT_USER\\Software\\CodeLab", "WordWrap", 1);
                wordWrapToolStripMenuItem.CheckState = CheckState.Checked;
                txtCode.WrapVisualFlags = WrapVisualFlags.Start;
            }
            else
            {
                txtCode.WrapMode = WrapMode.None;
                Registry.SetValue("HKEY_CURRENT_USER\\Software\\CodeLab", "WordWrap", 0);
                wordWrapToolStripMenuItem.CheckState = CheckState.Unchecked;
                txtCode.WrapVisualFlags = WrapVisualFlags.None;
            }
            txtCode.Focus();
        }

        private void whiteSpaceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (txtCode.ViewWhitespace == WhitespaceMode.Invisible)
            {
                txtCode.ViewWhitespace = WhitespaceMode.VisibleAlways;
                Registry.SetValue("HKEY_CURRENT_USER\\Software\\CodeLab", "WhiteSpace", 1);
                whiteSpaceToolStripMenuItem.CheckState = CheckState.Checked;
            }
            else
            {
                txtCode.ViewWhitespace = WhitespaceMode.Invisible;
                Registry.SetValue("HKEY_CURRENT_USER\\Software\\CodeLab", "WhiteSpace", 0);
                whiteSpaceToolStripMenuItem.CheckState = CheckState.Unchecked;
            }
            txtCode.Focus();
        }

        private void codeFoldingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!txtCode.CodeFoldingEnabled)
            {
                txtCode.CodeFoldingEnabled = true;
                Registry.SetValue("HKEY_CURRENT_USER\\Software\\CodeLab", "CodeFolding", 1);
                codeFoldingToolStripMenuItem.CheckState = CheckState.Checked;
            }
            else
            {
                txtCode.CodeFoldingEnabled = false;
                Registry.SetValue("HKEY_CURRENT_USER\\Software\\CodeLab", "CodeFolding", 0);
                codeFoldingToolStripMenuItem.CheckState = CheckState.Unchecked;
            }
            txtCode.Focus();
        }

        private void lineNumbersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!txtCode.LineNumbersEnabled)
            {
                txtCode.LineNumbersEnabled = true;
                Registry.SetValue("HKEY_CURRENT_USER\\Software\\CodeLab", "LineNumbers", 1);
                lineNumbersToolStripMenuItem.CheckState = CheckState.Checked;
            }
            else
            {
                txtCode.LineNumbersEnabled = false;
                Registry.SetValue("HKEY_CURRENT_USER\\Software\\CodeLab", "LineNumbers", 0);
                lineNumbersToolStripMenuItem.CheckState = CheckState.Unchecked;
            }
            txtCode.Focus();
        }

        private void bookmarksToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!txtCode.BookmarksEnabled)
            {
                txtCode.BookmarksEnabled = true;
                Registry.SetValue("HKEY_CURRENT_USER\\Software\\CodeLab", "Bookmarks", 1);
                bookmarksToolStripMenuItem.CheckState = CheckState.Checked;
            }
            else
            {
                txtCode.BookmarksEnabled = false;
                Registry.SetValue("HKEY_CURRENT_USER\\Software\\CodeLab", "Bookmarks", 0);
                bookmarksToolStripMenuItem.CheckState = CheckState.Unchecked;
            }
            txtCode.Focus();
        }

        private void indicatorMapMenuItem_Click(object sender, EventArgs e)
        {
            if (!txtCode.MapEnabled)
            {
                txtCode.MapEnabled = true;
                Registry.SetValue("HKEY_CURRENT_USER\\Software\\CodeLab", "Map", 1);
                indicatorMapMenuItem.CheckState = CheckState.Checked;
            }
            else
            {
                txtCode.MapEnabled = false;
                Registry.SetValue("HKEY_CURRENT_USER\\Software\\CodeLab", "Map", 0);
                indicatorMapMenuItem.CheckState = CheckState.Unchecked;
            }
            txtCode.Focus();
        }

        private void fontsCourierMenuItem_Click(object sender, EventArgs e)
        {
            fontsCourierMenuItem.Checked = true;
            fontsConsolasMenuItem.Checked = false;
            fontsEnvyRMenuItem.Checked = false;
            fontsHackMenuItem.Checked = false;
            fontsVerdanaMenuItem.Checked = false;
            Registry.SetValue("HKEY_CURRENT_USER\\Software\\CodeLab", "FontFamily", "Courier New");
            txtCode.Styles[Style.Default].Font = "Courier New";
            OutputTextBox.Font = new Font("Courier New", OutputTextBox.Font.Size);
            errorList.Font = new Font("Courier New", errorList.Font.Size);
            txtCode.Focus();
        }

        private void fontsConsolasMenuItem_Click(object sender, EventArgs e)
        {
            fontsCourierMenuItem.Checked = false;
            fontsConsolasMenuItem.Checked = true;
            fontsEnvyRMenuItem.Checked = false;
            fontsHackMenuItem.Checked = false;
            fontsVerdanaMenuItem.Checked = false;
            Registry.SetValue("HKEY_CURRENT_USER\\Software\\CodeLab", "FontFamily", "Consolas");
            txtCode.Styles[Style.Default].Font = "Consolas";
            OutputTextBox.Font = new Font("Consolas", OutputTextBox.Font.Size);
            errorList.Font = new Font("Consolas", errorList.Font.Size);
            txtCode.Focus();
        }

        private void fontsEnvyRMenuItem_Click(object sender, EventArgs e)
        {
            fontsCourierMenuItem.Checked = false;
            fontsConsolasMenuItem.Checked = false;
            fontsEnvyRMenuItem.Checked = true;
            fontsHackMenuItem.Checked = false;
            fontsVerdanaMenuItem.Checked = false;
            Registry.SetValue("HKEY_CURRENT_USER\\Software\\CodeLab", "FontFamily", "Envy Code R");
            txtCode.Styles[Style.Default].Font = "Envy Code R";
            OutputTextBox.Font = new Font("Envy Code R", OutputTextBox.Font.Size);
            errorList.Font = new Font("Envy Code R", errorList.Font.Size);
            txtCode.Focus();
        }

        private void fontsHackMenuItem_Click(object sender, EventArgs e)
        {
            fontsCourierMenuItem.Checked = false;
            fontsConsolasMenuItem.Checked = false;
            fontsEnvyRMenuItem.Checked = false;
            fontsHackMenuItem.Checked = true;
            fontsVerdanaMenuItem.Checked = false;
            Registry.SetValue("HKEY_CURRENT_USER\\Software\\CodeLab", "FontFamily", "Hack");
            txtCode.Styles[Style.Default].Font = "Hack";
            OutputTextBox.Font = new Font("Hack", OutputTextBox.Font.Size);
            errorList.Font = new Font("Hack", errorList.Font.Size);
            txtCode.Focus();
        }

        private void fontsVerdanaMenuItem_Click(object sender, EventArgs e)
        {
            fontsCourierMenuItem.Checked = false;
            fontsConsolasMenuItem.Checked = false;
            fontsEnvyRMenuItem.Checked = false;
            fontsHackMenuItem.Checked = false;
            fontsVerdanaMenuItem.Checked = true;
            Registry.SetValue("HKEY_CURRENT_USER\\Software\\CodeLab", "FontFamily", "Verdana");
            txtCode.Styles[Style.Default].Font = "Verdana";
            OutputTextBox.Font = new Font("Verdana", OutputTextBox.Font.Size);
            errorList.Font = new Font("Verdana", errorList.Font.Size);
            txtCode.Focus();
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            this.Opacity = 0.50;
            toolStripMenuItem2.Checked = true;
            toolStripMenuItem3.Checked = false;
            toolStripMenuItem4.Checked = false;
            toolStripMenuItem5.Checked = false;
            txtCode.Focus();
        }

        private void toolStripMenuItem5_Click(object sender, EventArgs e)
        {
            this.Opacity = 1.00;
            toolStripMenuItem2.Checked = false;
            toolStripMenuItem3.Checked = false;
            toolStripMenuItem4.Checked = false;
            toolStripMenuItem5.Checked = true;
            txtCode.Focus();
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            this.Opacity = 0.75;
            toolStripMenuItem2.Checked = false;
            toolStripMenuItem3.Checked = true;
            toolStripMenuItem4.Checked = false;
            toolStripMenuItem5.Checked = false;
            txtCode.Focus();
        }

        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            this.Opacity = 0.90;
            toolStripMenuItem2.Checked = false;
            toolStripMenuItem3.Checked = false;
            toolStripMenuItem4.Checked = true;
            toolStripMenuItem5.Checked = false;
            txtCode.Focus();
        }

        private void lightToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.ForeColor = Color.Black;
            this.BackColor = Color.White;
            ApplyTheme();
            txtCode.Theme = Theme.Light;
            Registry.SetValue("HKEY_CURRENT_USER\\Software\\CodeLab", "EditorTheme", (int)Theme.Light);
            lightToolStripMenuItem.CheckState = CheckState.Checked;
            darkToolStripMenuItem.CheckState = CheckState.Unchecked;
            autoToolStripMenuItem.CheckState = CheckState.Unchecked;
            txtCode.Focus();
        }

        private void darkToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.ForeColor = Color.White;
            this.BackColor = Color.FromArgb(40, 40, 40);
            ApplyTheme();
            txtCode.Theme = Theme.Dark;
            Registry.SetValue("HKEY_CURRENT_USER\\Software\\CodeLab", "EditorTheme", (int)Theme.Dark);
            lightToolStripMenuItem.CheckState = CheckState.Unchecked;
            darkToolStripMenuItem.CheckState = CheckState.Checked;
            autoToolStripMenuItem.CheckState = CheckState.Unchecked;
            txtCode.Focus();
        }

        private void autoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.ForeColor = OriginalForeColor;
            this.BackColor = OriginalBackColor;
            ApplyTheme();
            txtCode.Theme = (PdnTheme.BackColor.R < 128 && PdnTheme.BackColor.G < 128 && PdnTheme.BackColor.B < 128) ? Theme.Dark : Theme.Light;
            Registry.SetValue("HKEY_CURRENT_USER\\Software\\CodeLab", "EditorTheme", (int)Theme.Auto);
            lightToolStripMenuItem.CheckState = CheckState.Unchecked;
            darkToolStripMenuItem.CheckState = CheckState.Unchecked;
            autoToolStripMenuItem.CheckState = CheckState.Checked;
            txtCode.Focus();
        }

        private void ClearOutput_Click(object sender, EventArgs e)
        {
            OutputTextBox.Clear();
            txtCode.Focus();
        }
        #endregion

        #region Help menu Event functions
        private void helpTopicsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Services.GetService<IShellService>().LaunchUrl(null, "http://www.BoltBait.com/pdn/codelab/help");
        }

        private void checkForUpdatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!CheckForUpdates)
            {
                Registry.SetValue("HKEY_CURRENT_USER\\Software\\CodeLab", "CheckForUpdates", 1);
                checkForUpdatesToolStripMenuItem.CheckState = CheckState.Checked;
                CheckForUpdates = true;
                GoCheckForUpdates(false, true);
            }
            else
            {
                Registry.SetValue("HKEY_CURRENT_USER\\Software\\CodeLab", "CheckForUpdates", 0);
                checkForUpdatesToolStripMenuItem.CheckState = CheckState.Unchecked;
                CheckForUpdates = false;
            }
            txtCode.Focus();
        }

        private void changesInThisVersionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Services.GetService<IShellService>().LaunchUrl(null, "http://www.boltbait.com/pdn/codelab/history.asp#v" + ThisVersion);
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(WindowTitle + "\nCopyright � 2006-2018, All Rights Reserved.\n\nTom Jackson:\tInitial Code, Compile to DLL\n\nDavid Issel:\tEffect UI Creation, Effect Icons, Effect Help\n\t\tSystem, Editor Enhancements (including\n\t\tCode Templates, CodeLab Updater, Bug\n\t\tFixes), and Coding Tutorials\n\nJason Wendt:\tMigration to ScintillaNET editor control,\n\t\tIntelligent Assistance (including code\n\t\tcompletion with snippets and tips),\n\t\tDebug Output, Dark Theme, Bug Fixes", "About CodeLab", MessageBoxButtons.OK, MessageBoxIcon.Information);
            txtCode.Focus();
        }
        #endregion

        #region Context menu Event functions
        private void contextMenuStrip1_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.cutToolStripMenuItem.Enabled = txtCode.SelectedText.Length > 0;
            this.copyToolStripMenuItem.Enabled = txtCode.SelectedText.Length > 0;
            this.selectAllToolStripMenuItem.Enabled = txtCode.TextLength > 0;
            this.searchToolStripMenuItem.Enabled = true;
            this.indentToolStripMenuItem1.Enabled = true;
            this.unindentToolStripMenuItem1.Enabled = true;
            this.pasteToolStripMenuItem.Enabled = txtCode.CanPaste;
            this.commentSelectionToolStripMenuItem1.Enabled = txtCode.TextLength > 0;
            this.uncommentSelectionToolStripMenuItem1.Enabled = txtCode.TextLength > 0;
            this.undoToolStripMenuItem.Enabled = txtCode.CanUndo;
            this.redoToolStripMenuItem.Enabled = txtCode.CanRedo;
        }

        private void undoToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            UndoCommand();
        }

        private void redoToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            RedoCommand();
        }

        private void selectAllToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            SelectAllCommand();
            txtCode.Focus();
        }

        private void searchToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            FindCommand();
        }

        private void replaceToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ReplaceCommand();
            txtCode.Focus();
        }

        private void cutToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            CutSelection();
        }

        private void copyToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            CopySelection();
        }

        private void pasteToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            PasteSelection();
        }

        private void indentToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            IndentCommand();
        }

        private void unindentToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            UndentCommand();
        }

        private void commentSelectionToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            CommentCommand();
        }

        private void uncommentSelectionToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            UnCommentCommand();
        }
        #endregion

        #region Toolbar Event functions
        private void NewButton_Click(object sender, EventArgs e)
        {
            CreateNewFile();
        }

        private void OpenButton_Click(object sender, EventArgs e)
        {
            OpenFile();
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            Save();
        }

        private void SaveDLLButton_Click(object sender, EventArgs e)
        {
            SaveAsDLL();
        }

        private void UIDesignerButton_Click(object sender, EventArgs e)
        {
            UIDesigner();
        }

        private void CutButton_Click(object sender, EventArgs e)
        {
            CutSelection();
        }

        private void CopyButton_Click(object sender, EventArgs e)
        {
            CopySelection();
        }

        private void PasteButton_Click(object sender, EventArgs e)
        {
            PasteSelection();
        }

        private void SelectAllButton_Click(object sender, EventArgs e)
        {
            SelectAllCommand();
        }

        private void UndoButton_Click(object sender, EventArgs e)
        {
            UndoCommand();
        }

        private void RedoButton_Click(object sender, EventArgs e)
        {
            RedoCommand();
        }

        private void IndentButton_Click(object sender, EventArgs e)
        {
            IndentCommand();
        }

        private void UndentButton_Click(object sender, EventArgs e)
        {
            UndentCommand();
        }

        private void CommentButton_Click(object sender, EventArgs e)
        {
            CommentCommand();
        }

        private void UnCommentButton_Click(object sender, EventArgs e)
        {
            UnCommentCommand();
        }

        private void formatDocMenuItem_Click(object sender, EventArgs e)
        {
            txtCode.FormatDocument();
        }

        private void UpdateToolBarButtons()
        {
            CutButton.Enabled = txtCode.SelectedText.Length > 0;
            CopyButton.Enabled = txtCode.SelectedText.Length > 0;
            UndoButton.Enabled = txtCode.CanUndo;
            RedoButton.Enabled = txtCode.CanRedo;
        }

        private void txtCode_UpdateUI(object sender, UpdateUIEventArgs e)
        {
            if (e.Change.HasFlag(UpdateChange.Selection) || e.Change.HasFlag(UpdateChange.Content))
            {
                UpdateToolBarButtons();
            }
        }

        private void RunButton_Click(object sender, EventArgs e)
        {
            RunCommand();
        }
        #endregion

        #region Recent Items functions
        private void AddToRecents(string filePath)
        {
            RegistryKey settings = Registry.CurrentUser.OpenSubKey("Software\\CodeLab", true);
            if (settings == null)
            {
                Registry.CurrentUser.CreateSubKey("Software\\CodeLab").Flush();
                settings = Registry.CurrentUser.OpenSubKey("Software\\CodeLab", true);
            }
            string recents = (string)settings.GetValue("RecentDocs", string.Empty);

            if (recents == string.Empty)
            {
                recents = filePath;
            }
            else
            {
                recents = filePath + "|" + recents;

                var paths = recents.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                List<string> recentsList = new List<string>();
                foreach (string itemPath in paths)
                {
                    bool contains = false;
                    foreach (string listItem in recentsList)
                    {
                        if (listItem.Equals(itemPath, StringComparison.OrdinalIgnoreCase))
                        {
                            contains = true;
                            break;
                        }
                    }

                    if (!contains)
                    {
                        recentsList.Add(itemPath);
                    }
                }

                int length = Math.Min(8, recentsList.Count);
                recents = string.Join("|", recentsList.ToArray(), 0, length);
            }

            settings.SetValue("RecentDocs", recents);
            settings.Close();
        }

        private void openRecentToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            this.openRecentToolStripMenuItem.DropDownItems.Clear();

            RegistryKey settings = Registry.CurrentUser.OpenSubKey("Software\\CodeLab", true);
            if (settings == null)
            {
                Registry.CurrentUser.CreateSubKey("Software\\CodeLab").Flush();
                settings = Registry.CurrentUser.OpenSubKey("Software\\CodeLab", true);
            }
            string recents = (string)settings.GetValue("RecentDocs", string.Empty);
            settings.Close();

            List<ToolStripItem> recentsList = new List<ToolStripItem>();
            string[] paths = recents.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            int count = 1;
            foreach (string itemPath in paths)
            {
                if (!File.Exists(itemPath))
                {
                    continue;
                }

                ToolStripMenuItem recentItem = new ToolStripMenuItem();

                string menuText = $"&{count} {Path.GetFileName(itemPath)}";
                try
                {
                    Regex REName = new Regex(@"//[\s-[\r\n]]*Name[\s-[\r\n]]*:[\s-[\r\n]]*(?<menulabel>.*)(?=\r?\n|$)", RegexOptions.IgnoreCase);
                    Match wtn = REName.Match(File.ReadAllText(itemPath));
                    if (wtn.Success)
                    {
                        string scriptName = wtn.Groups["menulabel"].Value.Trim();
                        if (scriptName != string.Empty)
                        {
                            menuText = $"&{count} {scriptName} ({Path.GetFileName(itemPath)})";
                        }
                    }
                }
                catch
                {
                }

                recentItem.Text = menuText;
                recentItem.ToolTipText = itemPath;
                recentItem.Click += RecentItem_Click;

                string imagePath = Path.ChangeExtension(itemPath, ".png");
                if (File.Exists(imagePath))
                {
                    recentItem.Image = new Bitmap(imagePath);
                }

                recentsList.Add(recentItem);
                count++;
            }

            if (recentsList.Count > 0)
            {
                ToolStripSeparator toolStripSeparator = new ToolStripSeparator();
                recentsList.Add(toolStripSeparator);

                ToolStripMenuItem clearRecents = new ToolStripMenuItem();
                clearRecents.Text = "&Clear List";
                clearRecents.Click += ClearRecents_Click;
                recentsList.Add(clearRecents);

                this.openRecentToolStripMenuItem.DropDownItems.AddRange(recentsList.ToArray());
            }
            else
            {
                ToolStripMenuItem noRecents = new ToolStripMenuItem();
                noRecents.Text = "No Recent Items";
                noRecents.Enabled = false;

                this.openRecentToolStripMenuItem.DropDownItems.Add(noRecents);
            }
        }

        private void ClearRecents_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to clear the Open Recent list?", "CodeLab", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            using (RegistryKey settings = Registry.CurrentUser.OpenSubKey("Software\\CodeLab", true))
            {
                if (settings != null)
                {
                    settings.SetValue("RecentDocs", string.Empty);
                }
            }
        }

        private void RecentItem_Click(object sender, EventArgs e)
        {
            if (txtCode.IsDirty && PromptToSave() == DialogResult.Cancel)
            {
                return;
            }

            string filePath = (sender as ToolStripMenuItem)?.ToolTipText;
            if (!File.Exists(filePath))
            {
                MessageBox.Show("File not found.\n" + filePath, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtCode.Focus();
                return;
            }

            try
            {
                FullScriptPath = filePath;
                FileName = Path.GetFileNameWithoutExtension(filePath);
                AddToRecents(filePath);
                txtCode.Text = File.ReadAllText(filePath);
                txtCode.ExecuteCmd(Command.ScrollToEnd); // Workaround for a scintilla bug
                txtCode.ExecuteCmd(Command.ScrollToStart);
                txtCode.EmptyUndoBuffer();
                txtCode.SetSavePoint();
                txtCode.Focus();
                Build();
            }
            catch
            {
            }
        }
        #endregion

        #region Dirty Document functions
        private void txtCode_SavePointLeft(object sender, EventArgs e)
        {
            this.Text = FileName + "* - " + WindowTitle;
        }

        private void txtCode_SavePointReached(object sender, EventArgs e)
        {
            this.Text = FileName + " - " + WindowTitle;
        }
        #endregion
    }
}
