using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Dastan.org.ed.ea.settings;
using Dastan.org.ed.ea.util;

namespace Dastan.org.ed.ea.tool
{
    [ComVisible(true)]
    [Guid("6EB9FF5C-79B6-4526-BA29-E0278347973A")]
    [ProgId("ScriptGenerator.Settings")]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class Settings : Form
    {
        public Settings()
        {
            InitializeComponent();
            IconUtility.Apply(this);
        }
        
        private void LoadValues(object sender, EventArgs e)
        {
            outputFolderTBX.Text = AppSettings.DefaultOutputFolder;
            prefixTBX.Text = AppSettings.StringResourcePrefix;
            exportCHBX.Checked = AppSettings.AutoOpenExport;
            trackCHBX.Checked = AppSettings.TrackModifications;
            jiraDomainTXB.Text = AppSettings.JiraDomain;
            jiraProjectTXB.Text = AppSettings.JiraProject;
            jiraTokenTBX.Text = AppSettings.JiraToken;
            jiraPageSizeTBX.Text = AppSettings.JiraPageSize;
            jiraScanIncremental.Checked = AppSettings.JiraScanIncremental;
            SetCombo(jiraDraftEquivalent, AppSettings.JiraDraftEquivalent, "Proposed");
            SetCombo(jiraToDoEquivalent, AppSettings.JiraToDoEquivalent, "Proposed");
            SetCombo(jiraInProgressEquivalent, AppSettings.JiraInProgressEquivalent, "Validated");
            SetCombo(jiraInReviewEquivalent, AppSettings.JiraInReviewEquivalent, "Approved");
            SetCombo(jiraDoneEquivalent, AppSettings.JiraDoneEquivalent, "Implemented");
            SetCombo(jiraTaskType, AppSettings.JiraTaskType, "Implemented");
            jiraCustomJQL.Text = AppSettings.JiraCustomJql;
        }

        // Writes the saved settings, not what is currently typed into this dialog. Both
        // buttons work on what is stored, so exporting straight after editing without
        // saving writes the old values -- save first.
        private void ExportSettings(object sender, EventArgs e)
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Title = "Export Dastan Settings";
                dialog.Filter = "Dastan settings (*.json)|*.json|All files (*.*)|*.*";
                dialog.FileName = "dastan-settings.json";

                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    SettingsTransfer.Export(dialog.FileName);
                    MessageBox.Show(
                        "Settings written to:\r\n" + dialog.FileName + "\r\n\r\n" +
                        "The Jira token is not included. It is a personal credential, and it is " +
                        "encrypted against this Windows account, so whoever imports this enters " +
                        "their own.",
                        "Export Settings", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception error)
                {
                    MessageBox.Show("Could not export: " + error.Message, "Export Settings",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // Applies straight to the stored settings and then refreshes this dialog to match,
        // so Cancel will not put them back. Said plainly in the confirmation.
        private void ImportSettings(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Import Dastan Settings";
                dialog.Filter = "Dastan settings (*.json)|*.json|All files (*.*)|*.*";
                dialog.CheckFileExists = true;

                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                if (MessageBox.Show(
                        "Replace your current settings with the ones in this file?\r\n\r\n" +
                        "This applies immediately and Cancel will not undo it. Your Jira token is " +
                        "left alone.",
                        "Import Settings", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    return;

                try
                {
                    SettingsTransfer.ImportResult result = SettingsTransfer.Import(dialog.FileName);
                    LoadValues(this, EventArgs.Empty);
                    ShowImportResult(result);
                }
                catch (Exception error)
                {
                    MessageBox.Show("Could not import: " + error.Message, "Import Settings",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private static void ShowImportResult(SettingsTransfer.ImportResult result)
        {
            var message = new StringBuilder();
            message.AppendLine(result.Applied + " setting(s) applied.");

            // A file that leaves something out tops up rather than wipes, so say which
            // ones kept their current value.
            if (result.Missing.Count > 0)
            {
                message.AppendLine();
                message.AppendLine("Not in the file, so left as they were:");
                message.AppendLine("   " + string.Join(", ", result.Missing.ToArray()));
            }

            if (result.Unknown.Count > 0)
            {
                message.AppendLine();
                message.AppendLine("In the file but not recognised, so ignored:");
                message.AppendLine("   " + string.Join(", ", result.Unknown.ToArray()));
            }

            MessageBox.Show(message.ToString(), "Import Settings", MessageBoxButtons.OK,
                result.Unknown.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }

        private void SaveAndClose(object sender, EventArgs e)
        {
            AppSettings.DefaultOutputFolder = outputFolderTBX.Text.Trim();
            AppSettings.StringResourcePrefix = prefixTBX.Text.Trim();
            AppSettings.AutoOpenExport = exportCHBX.Checked;
            AppSettings.TrackModifications = trackCHBX.Checked;
            AppSettings.JiraDomain = jiraDomainTXB.Text.Trim();
            AppSettings.JiraProject = jiraProjectTXB.Text.Trim();
            AppSettings.JiraToken = jiraTokenTBX.Text.Trim();
            AppSettings.JiraPageSize = jiraPageSizeTBX.Text.Trim();
            AppSettings.JiraScanIncremental = jiraScanIncremental.Checked;
            AppSettings.JiraDraftEquivalent = GetCombo(jiraDraftEquivalent,"Proposed");
            AppSettings.JiraToDoEquivalent = GetCombo(jiraToDoEquivalent,"Proposed");
            AppSettings.JiraInProgressEquivalent = GetCombo(jiraInProgressEquivalent,"Validated");
            AppSettings.JiraInReviewEquivalent = GetCombo(jiraInReviewEquivalent,"Approved");
            AppSettings.JiraDoneEquivalent = GetCombo(jiraDoneEquivalent,"Implemented");
            AppSettings.JiraTaskType = GetCombo(jiraTaskType,"All");
            AppSettings.JiraCustomJql = jiraCustomJQL.Text.Trim();
            Close();
        }
        
        private static void SetCombo(ComboBox combo, string value, string fallback)
        {
            string v = string.IsNullOrWhiteSpace(value) ? fallback : value;
            int index = combo.Items.IndexOf(v);
            combo.SelectedIndex = index >= 0 ? index : 0;
        }

        private static string GetCombo(ComboBox combo, string fallback)
        {
            return combo.SelectedItem?.ToString() ?? combo.Text?.Trim() ?? fallback;
        }

        private void BrowseFolder(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Select default output folder";
                if (!string.IsNullOrEmpty(outputFolderTBX.Text))
                    dlg.SelectedPath = outputFolderTBX.Text;

                if (dlg.ShowDialog() == DialogResult.OK)
                    outputFolderTBX.Text = dlg.SelectedPath;
            }
        }

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.scriptSettingGBX = new GroupBox();
            this.trackCHBX = new CheckBox();
            this.exportCHBX = new CheckBox();
            this.prefixTBX = new TextBox();
            this.prefixLbl = new Label();
            this.outputFolderBtn = new Button();
            this.outputFolderTBX = new TextBox();
            this.outputFolderLbl = new Label();
            this.groupBox1 = new GroupBox();
            this.jiraTaskType = new ComboBox();
            this.label7 = new Label();
            this.jiraCustomJQL = new TextBox();
            this.label6 = new Label();
            this.statesMappingGBX = new GroupBox();
            this.jiraDoneEquivalent = new ComboBox();
            this.jiraInReviewEquivalent = new ComboBox();
            this.jiraInProgressEquivalent = new ComboBox();
            this.jiraToDoEquivalent = new ComboBox();
            this.jiraDraftEquivalent = new ComboBox();
            this.label5 = new Label();
            this.label4 = new Label();
            this.label3 = new Label();
            this.label2 = new Label();
            this.label1 = new Label();
            this.jiraScanIncremental = new CheckBox();
            this.jiraPageSizeLbl = new Label();
            this.jiraPageSizeTBX = new TextBox();
            this.jiraTokenTBX = new TextBox();
            this.jiraTokenLbl = new Label();
            this.jiraProjectTXB = new TextBox();
            this.jiraProjectLbl = new Label();
            this.jiraDomainTXB = new TextBox();
            this.jiraDomainLbl = new Label();
            this.saveBtn = new Button();
            this.cancelBtn = new Button();
            this.importBtn = new Button();
            this.exportBtn = new Button();
            this.scriptSettingGBX.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.statesMappingGBX.SuspendLayout();
            this.SuspendLayout();
            // 
            // scriptSettingGBX
            // 
            this.scriptSettingGBX.Controls.Add(this.trackCHBX);
            this.scriptSettingGBX.Controls.Add(this.exportCHBX);
            this.scriptSettingGBX.Controls.Add(this.prefixTBX);
            this.scriptSettingGBX.Controls.Add(this.prefixLbl);
            this.scriptSettingGBX.Controls.Add(this.outputFolderBtn);
            this.scriptSettingGBX.Controls.Add(this.outputFolderTBX);
            this.scriptSettingGBX.Controls.Add(this.outputFolderLbl);
            this.scriptSettingGBX.Location = new Point(11, 10);
            this.scriptSettingGBX.Name = "scriptSettingGBX";
            this.scriptSettingGBX.Size = new Size(366, 136);
            this.scriptSettingGBX.TabIndex = 0;
            this.scriptSettingGBX.TabStop = false;
            this.scriptSettingGBX.Text = "Script Settings";
            // 
            // trackCHBX
            // 
            this.trackCHBX.Location = new Point(8, 108);
            this.trackCHBX.Name = "trackCHBX";
            this.trackCHBX.Size = new Size(352, 22);
            this.trackCHBX.TabIndex = 6;
            this.trackCHBX.Text = "Track Element Modifications";
            this.trackCHBX.UseVisualStyleBackColor = true;
            // 
            // exportCHBX
            // 
            this.exportCHBX.Location = new Point(8, 82);
            this.exportCHBX.Name = "exportCHBX";
            this.exportCHBX.Size = new Size(352, 20);
            this.exportCHBX.TabIndex = 5;
            this.exportCHBX.Text = "Open Exported File After Generate";
            this.exportCHBX.UseVisualStyleBackColor = true;
            // 
            // prefixTBX
            // 
            this.prefixTBX.Location = new Point(154, 48);
            this.prefixTBX.Name = "prefixTBX";
            this.prefixTBX.Size = new Size(206, 20);
            this.prefixTBX.TabIndex = 4;
            // 
            // prefixLbl
            // 
            this.prefixLbl.Location = new Point(8, 51);
            this.prefixLbl.Name = "prefixLbl";
            this.prefixLbl.Size = new Size(140, 22);
            this.prefixLbl.TabIndex = 3;
            this.prefixLbl.Text = "String Resource Prefix:";
            // 
            // outputFolderBtn
            // 
            this.outputFolderBtn.Location = new Point(334, 18);
            this.outputFolderBtn.Name = "outputFolderBtn";
            this.outputFolderBtn.Size = new Size(26, 20);
            this.outputFolderBtn.TabIndex = 2;
            this.outputFolderBtn.Text = "...";
            this.outputFolderBtn.UseVisualStyleBackColor = true;
            this.outputFolderBtn.Click += new EventHandler(this.BrowseFolder);
            // 
            // outputFolderTBX
            // 
            this.outputFolderTBX.Location = new Point(154, 18);
            this.outputFolderTBX.Name = "outputFolderTBX";
            this.outputFolderTBX.Size = new Size(174, 20);
            this.outputFolderTBX.TabIndex = 1;
            // 
            // outputFolderLbl
            // 
            this.outputFolderLbl.Location = new Point(8, 22);
            this.outputFolderLbl.Name = "outputFolderLbl";
            this.outputFolderLbl.Size = new Size(140, 16);
            this.outputFolderLbl.TabIndex = 0;
            this.outputFolderLbl.Text = "Default Output Folder:";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.jiraTaskType);
            this.groupBox1.Controls.Add(this.label7);
            this.groupBox1.Controls.Add(this.jiraCustomJQL);
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.statesMappingGBX);
            this.groupBox1.Controls.Add(this.jiraScanIncremental);
            this.groupBox1.Controls.Add(this.jiraPageSizeLbl);
            this.groupBox1.Controls.Add(this.jiraPageSizeTBX);
            this.groupBox1.Controls.Add(this.jiraTokenTBX);
            this.groupBox1.Controls.Add(this.jiraTokenLbl);
            this.groupBox1.Controls.Add(this.jiraProjectTXB);
            this.groupBox1.Controls.Add(this.jiraProjectLbl);
            this.groupBox1.Controls.Add(this.jiraDomainTXB);
            this.groupBox1.Controls.Add(this.jiraDomainLbl);
            this.groupBox1.Location = new Point(9, 157);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new Size(367, 403);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Jira Settings";
            // 
            // jiraTaskType
            // 
            this.jiraTaskType.FormattingEnabled = true;
            this.jiraTaskType.Items.AddRange(new object[] { "All", "Story", "Task", "Bug", "Epic" });
            this.jiraTaskType.Location = new Point(156, 218);
            this.jiraTaskType.Name = "jiraTaskType";
            this.jiraTaskType.Size = new Size(205, 21);
            this.jiraTaskType.TabIndex = 13;
            // 
            // label7
            // 
            this.label7.Location = new Point(9, 216);
            this.label7.Name = "label7";
            this.label7.Size = new Size(138, 23);
            this.label7.TabIndex = 12;
            this.label7.Text = "Type:";
            // 
            // jiraCustomJQL
            // 
            this.jiraCustomJQL.Location = new Point(156, 185);
            this.jiraCustomJQL.Name = "jiraCustomJQL";
            this.jiraCustomJQL.Size = new Size(205, 20);
            this.jiraCustomJQL.TabIndex = 11;
            // 
            // label6
            // 
            this.label6.Location = new Point(10, 185);
            this.label6.Name = "label6";
            this.label6.Size = new Size(140, 21);
            this.label6.TabIndex = 10;
            this.label6.Text = "Custom JQL:";
            // 
            // statesMappingGBX
            // 
            this.statesMappingGBX.Controls.Add(this.jiraDoneEquivalent);
            this.statesMappingGBX.Controls.Add(this.jiraInReviewEquivalent);
            this.statesMappingGBX.Controls.Add(this.jiraInProgressEquivalent);
            this.statesMappingGBX.Controls.Add(this.jiraToDoEquivalent);
            this.statesMappingGBX.Controls.Add(this.jiraDraftEquivalent);
            this.statesMappingGBX.Controls.Add(this.label5);
            this.statesMappingGBX.Controls.Add(this.label4);
            this.statesMappingGBX.Controls.Add(this.label3);
            this.statesMappingGBX.Controls.Add(this.label2);
            this.statesMappingGBX.Controls.Add(this.label1);
            this.statesMappingGBX.Location = new Point(6, 242);
            this.statesMappingGBX.Name = "statesMappingGBX";
            this.statesMappingGBX.Size = new Size(352, 154);
            this.statesMappingGBX.TabIndex = 9;
            this.statesMappingGBX.TabStop = false;
            this.statesMappingGBX.Text = "States Mapping";
            // 
            // jiraDoneEquivalent
            // 
            this.jiraDoneEquivalent.FormattingEnabled = true;
            this.jiraDoneEquivalent.Items.AddRange(new object[] { "Proposed", "Approved", "Validated", "Implemented", "Rejected" });
            this.jiraDoneEquivalent.Location = new Point(155, 120);
            this.jiraDoneEquivalent.Name = "jiraDoneEquivalent";
            this.jiraDoneEquivalent.Size = new Size(191, 21);
            this.jiraDoneEquivalent.TabIndex = 9;
            // 
            // jiraInReviewEquivalent
            // 
            this.jiraInReviewEquivalent.FormattingEnabled = true;
            this.jiraInReviewEquivalent.Items.AddRange(new object[] { "Proposed", "Approved", "Validated", "Implemented", "Rejected" });
            this.jiraInReviewEquivalent.Location = new Point(155, 93);
            this.jiraInReviewEquivalent.Name = "jiraInReviewEquivalent";
            this.jiraInReviewEquivalent.Size = new Size(191, 21);
            this.jiraInReviewEquivalent.TabIndex = 8;
            // 
            // jiraInProgressEquivalent
            // 
            this.jiraInProgressEquivalent.FormattingEnabled = true;
            this.jiraInProgressEquivalent.Items.AddRange(new object[] { "Proposed", "Approved", "Validated", "Implemented", "Rejected" });
            this.jiraInProgressEquivalent.Location = new Point(155, 69);
            this.jiraInProgressEquivalent.Name = "jiraInProgressEquivalent";
            this.jiraInProgressEquivalent.Size = new Size(191, 21);
            this.jiraInProgressEquivalent.TabIndex = 7;
            // 
            // jiraToDoEquivalent
            // 
            this.jiraToDoEquivalent.FormattingEnabled = true;
            this.jiraToDoEquivalent.Items.AddRange(new object[] { "Proposed", "Approved", "Validated", "Implemented", "Rejected" });
            this.jiraToDoEquivalent.Location = new Point(155, 43);
            this.jiraToDoEquivalent.Name = "jiraToDoEquivalent";
            this.jiraToDoEquivalent.Size = new Size(191, 21);
            this.jiraToDoEquivalent.TabIndex = 6;
            // 
            // jiraDraftEquivalent
            // 
            this.jiraDraftEquivalent.FormattingEnabled = true;
            this.jiraDraftEquivalent.Items.AddRange(new object[] { "Proposed", "Approved", "Validated", "Implemented", "Rejected" });
            this.jiraDraftEquivalent.Location = new Point(155, 16);
            this.jiraDraftEquivalent.Name = "jiraDraftEquivalent";
            this.jiraDraftEquivalent.Size = new Size(191, 21);
            this.jiraDraftEquivalent.TabIndex = 5;
            // 
            // label5
            // 
            this.label5.Location = new Point(6, 124);
            this.label5.Name = "label5";
            this.label5.Size = new Size(131, 17);
            this.label5.TabIndex = 4;
            this.label5.Text = "Done";
            // 
            // label4
            // 
            this.label4.Location = new Point(9, 98);
            this.label4.Name = "label4";
            this.label4.Size = new Size(131, 17);
            this.label4.TabIndex = 3;
            this.label4.Text = "In Review";
            // 
            // label3
            // 
            this.label3.Location = new Point(9, 69);
            this.label3.Name = "label3";
            this.label3.Size = new Size(131, 17);
            this.label3.TabIndex = 2;
            this.label3.Text = "In Progress";
            // 
            // label2
            // 
            this.label2.Location = new Point(9, 43);
            this.label2.Name = "label2";
            this.label2.Size = new Size(131, 17);
            this.label2.TabIndex = 1;
            this.label2.Text = "To Do";
            // 
            // label1
            // 
            this.label1.Location = new Point(9, 19);
            this.label1.Name = "label1";
            this.label1.Size = new Size(131, 17);
            this.label1.TabIndex = 0;
            this.label1.Text = "Draft";
            // 
            // jiraScanIncremental
            // 
            this.jiraScanIncremental.Location = new Point(10, 151);
            this.jiraScanIncremental.Name = "jiraScanIncremental";
            this.jiraScanIncremental.Size = new Size(351, 22);
            this.jiraScanIncremental.TabIndex = 8;
            this.jiraScanIncremental.Text = "Scan Incremental";
            this.jiraScanIncremental.UseVisualStyleBackColor = true;
            // 
            // jiraPageSizeLbl
            // 
            this.jiraPageSizeLbl.Location = new Point(10, 122);
            this.jiraPageSizeLbl.Name = "jiraPageSizeLbl";
            this.jiraPageSizeLbl.Size = new Size(118, 16);
            this.jiraPageSizeLbl.TabIndex = 7;
            this.jiraPageSizeLbl.Text = "Page Size:";
            // 
            // jiraPageSizeTBX
            // 
            this.jiraPageSizeTBX.Location = new Point(156, 118);
            this.jiraPageSizeTBX.Name = "jiraPageSizeTBX";
            this.jiraPageSizeTBX.Size = new Size(205, 20);
            this.jiraPageSizeTBX.TabIndex = 6;
            // 
            // jiraTokenTBX
            // 
            this.jiraTokenTBX.Location = new Point(156, 88);
            this.jiraTokenTBX.Name = "jiraTokenTBX";
            this.jiraTokenTBX.PasswordChar = '*';
            this.jiraTokenTBX.Size = new Size(205, 20);
            this.jiraTokenTBX.TabIndex = 5;
            this.jiraTokenTBX.UseSystemPasswordChar = true;
            // 
            // jiraTokenLbl
            // 
            this.jiraTokenLbl.Location = new Point(8, 91);
            this.jiraTokenLbl.Name = "jiraTokenLbl";
            this.jiraTokenLbl.Size = new Size(118, 16);
            this.jiraTokenLbl.TabIndex = 4;
            this.jiraTokenLbl.Text = "Token:";
            // 
            // jiraProjectTXB
            // 
            this.jiraProjectTXB.Location = new Point(156, 58);
            this.jiraProjectTXB.Name = "jiraProjectTXB";
            this.jiraProjectTXB.Size = new Size(205, 20);
            this.jiraProjectTXB.TabIndex = 3;
            // 
            // jiraProjectLbl
            // 
            this.jiraProjectLbl.Location = new Point(8, 61);
            this.jiraProjectLbl.Name = "jiraProjectLbl";
            this.jiraProjectLbl.Size = new Size(117, 16);
            this.jiraProjectLbl.TabIndex = 2;
            this.jiraProjectLbl.Text = "Project:";
            // 
            // jiraDomainTXB
            // 
            this.jiraDomainTXB.Location = new Point(156, 25);
            this.jiraDomainTXB.Name = "jiraDomainTXB";
            this.jiraDomainTXB.Size = new Size(206, 20);
            this.jiraDomainTXB.TabIndex = 1;
            // 
            // jiraDomainLbl
            // 
            this.jiraDomainLbl.Location = new Point(9, 28);
            this.jiraDomainLbl.Name = "jiraDomainLbl";
            this.jiraDomainLbl.Size = new Size(119, 16);
            this.jiraDomainLbl.TabIndex = 0;
            this.jiraDomainLbl.Text = "Domain:";
            // 
            // saveBtn
            // 
            this.saveBtn.DialogResult = DialogResult.OK;
            this.saveBtn.Location = new Point(9, 604);
            this.saveBtn.Name = "saveBtn";
            this.saveBtn.Size = new Size(170, 30);
            this.saveBtn.TabIndex = 2;
            this.saveBtn.Text = "Save";
            this.saveBtn.UseVisualStyleBackColor = true;
            this.saveBtn.Click += new EventHandler(this.SaveAndClose);
            // 
            // cancelBtn
            // 
            this.cancelBtn.DialogResult = DialogResult.Cancel;
            this.cancelBtn.Location = new Point(207, 604);
            this.cancelBtn.Name = "cancelBtn";
            this.cancelBtn.Size = new Size(170, 30);
            this.cancelBtn.TabIndex = 3;
            this.cancelBtn.Text = "Cancel";
            this.cancelBtn.UseVisualStyleBackColor = true;
            // 
            // importBtn
            // 
            this.importBtn.Location = new Point(9, 566);
            this.importBtn.Name = "importBtn";
            this.importBtn.Size = new Size(170, 30);
            this.importBtn.TabIndex = 4;
            this.importBtn.Text = "Import...";
            this.importBtn.UseVisualStyleBackColor = true;
            this.importBtn.Click += new EventHandler(this.ImportSettings);
            // 
            // exportBtn
            // 
            this.exportBtn.Location = new Point(207, 566);
            this.exportBtn.Name = "exportBtn";
            this.exportBtn.Size = new Size(170, 30);
            this.exportBtn.TabIndex = 5;
            this.exportBtn.Text = "Export...";
            this.exportBtn.UseVisualStyleBackColor = true;
            this.exportBtn.Click += new EventHandler(this.ExportSettings);
            // 
            // Settings
            // 
            this.AcceptButton = this.saveBtn;
            this.CancelButton = this.cancelBtn;
            this.ClientSize = new Size(389, 640);
            this.Controls.Add(this.importBtn);
            this.Controls.Add(this.exportBtn);
            this.Controls.Add(this.cancelBtn);
            this.Controls.Add(this.saveBtn);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.scriptSettingGBX);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.Name = "Settings";
            this.Text = "Dastan Settings";
            this.Load += new EventHandler(this.LoadValues);
            this.scriptSettingGBX.ResumeLayout(false);
            this.scriptSettingGBX.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.statesMappingGBX.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private Label label7;
        private ComboBox jiraTaskType;

        private Label label6;
        private TextBox jiraCustomJQL;

        private ComboBox jiraDraftEquivalent;
        private ComboBox jiraToDoEquivalent;
        private ComboBox jiraInProgressEquivalent;
        private ComboBox jiraInReviewEquivalent;
        private ComboBox jiraDoneEquivalent;

        private GroupBox statesMappingGBX;
        private Label label1;
        private Label label2;
        private Label label3;
        private Label label4;
        private Label label5;

        private CheckBox jiraScanIncremental;

        private TextBox jiraPageSizeTBX;
        private Label jiraPageSizeLbl;

        private Button outputFolderBtn;
        private Label prefixLbl;
        private Label outputFolderLbl;
        private TextBox prefixTBX;
        private TextBox outputFolderTBX;
        private CheckBox exportCHBX;
        private CheckBox trackCHBX;
        private Label jiraDomainLbl;
        private TextBox jiraDomainTXB;
        private Label jiraProjectLbl;
        private TextBox jiraProjectTXB;
        private Button cancelBtn;
        private Button importBtn;
        private Button exportBtn;
        private GroupBox scriptSettingGBX;

        private Button saveBtn;

        private Label jiraTokenLbl;
        private TextBox jiraTokenTBX;

        private GroupBox groupBox1;

        private void IsNumeric(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }
    }
}