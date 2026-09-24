using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Dastan.org.ed.ea.commands;
using Dastan.org.ed.ea.entity;
using Dastan.org.ed.ea.services.modifications;
using Dastan.org.ed.ea.settings;
using Dastan.org.ed.ea.util;
using EA;

namespace Dastan.org.ed.ea.tool
{
    
    [ComVisible(true)]
    [Guid("13B7D92B-FF98-49BD-A61A-D9E425E6B66D")]
    [ProgId("ScriptGenerator.ModificationLogs")]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class ModificationLogs : UserControl
    {
        private readonly Button _btnClear;
        private readonly Button _btnGenerate;
        private readonly ComboBox _changeset;
        private readonly TextBox _search;
        private readonly ToolTip _toolTip =  new ToolTip();
        private readonly ModificationLogsManager _modificationLogsManager = new ModificationLogsManager();
        private readonly ToolStripMenuItem _showInBrowser;
        private readonly ToolStripMenuItem _copyRows;
        private readonly ToolStripMenuItem _revertRows;
        private readonly ToolStripMenuItem _removeRows;
        private Repository _repository;
        
        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        public bool IsVisibleInEa()
        {
            if (!IsHandleCreated)
                return false;

            return IsWindowVisible(Handle);
        }
        
        public ModificationLogs()
        {
            var buttonPanel = new Panel()
            {
                Dock = DockStyle.Top,
                Height = 40
            };

            
            _btnClear = new Button {  Left = 0, Top = 8, Width = 20 };
            _btnClear.Image = IconUtility.LoadImage("Dastan.icons.clear.png");
            _btnClear.ImageAlign = ContentAlignment.MiddleCenter;
            _btnClear.TextAlign = ContentAlignment.MiddleCenter;
            _toolTip.SetToolTip(_btnClear, "Clear");
            
            _btnGenerate = new Button { Left = 20, Top = 8, Width = 20 };
            _btnGenerate.Image = IconUtility.LoadImage("Dastan.icons.export.png");
            _btnGenerate.ImageAlign = ContentAlignment.MiddleCenter;
            _btnGenerate.TextAlign = ContentAlignment.MiddleCenter;
            _toolTip.SetToolTip(_btnGenerate, "Generate Script");
            
            _btnClear.Click += ExecuteClick;
            _btnGenerate.Click += ExecuteClick;

            var changesetLabel = new Label { Left = 50, Top = 12, Width = 64, Text = "Changeset:" };
            _changeset = new ComboBox
            {
                Left = 116,
                Top = 8,
                Width = 180,
                DropDownStyle = ComboBoxStyle.DropDown,
                Text = AppSettings.ActiveChangeset
            };
            _toolTip.SetToolTip(_changeset,
                "Modifications tracked from now on are filed under this name.\r\n" +
                "Pick or type an existing name to select its rows; leave it empty to select everything.");

            // The name says where new modifications are filed. Naming a set that already
            // has rows also selects them, so Generate Script acts on the whole set --
            // whether the name was picked from the list or typed and entered.
            _changeset.TextChanged += (s, e) =>
            {
                AppSettings.ActiveChangeset = _changeset.Text.Trim();

                // Emptying the box is itself the gesture for "all of them", so it takes
                // effect straight away rather than waiting for Enter.
                if (_changeset.Text.Trim().Length == 0) _modificationLogsManager.SelectAll();
            };
            _changeset.SelectedIndexChanged += (s, e) => SelectActiveChangeset();
            _changeset.KeyDown += (s, e) =>
            {
                if (e.KeyCode != Keys.Enter) return;

                SelectActiveChangeset();
                e.Handled = true;
                e.SuppressKeyPress = true;
            };

            var searchLabel = new Label { Left = 306, Top = 12, Width = 46, Text = "Search:" };
            _search = new TextBox { Left = 354, Top = 8, Width = 200 };
            _toolTip.SetToolTip(_search,
                "Shows only rows containing this text, matched against every visible column.\r\n" +
                "Hiding rows never removes them from the log.");

            _search.TextChanged += (s, e) => _modificationLogsManager.Filter(_search.Text);

            _showInBrowser = new ToolStripMenuItem("Select in Project Browser", null, (s, e) => ShowInProjectBrowser());
            _copyRows = new ToolStripMenuItem("Copy", null, (s, e) => CopySelectedRows());
            _revertRows = new ToolStripMenuItem("Revert Change", null, (s, e) => RevertSelectedRows());
            _removeRows = new ToolStripMenuItem("Remove from Log", null, (s, e) => RemoveSelectedRows());

            var rowMenu = new ContextMenuStrip();
            rowMenu.Items.Add(_showInBrowser);
            rowMenu.Items.Add(_copyRows);
            rowMenu.Items.Add(new ToolStripSeparator());
            rowMenu.Items.Add(_revertRows);
            rowMenu.Items.Add(_removeRows);
            rowMenu.Opening += (s, e) => UpdateRowMenuState();

            ListView rows = _modificationLogsManager.GetItems();
            rows.ContextMenuStrip = rowMenu;

            // The same thing the menu's first item does, for anyone who never opens it.
            rows.DoubleClick += (s, e) => ShowInProjectBrowser();

            buttonPanel.Controls.Add(_btnClear);
            buttonPanel.Controls.Add(_btnGenerate);
            buttonPanel.Controls.Add(changesetLabel);
            buttonPanel.Controls.Add(_changeset);
            buttonPanel.Controls.Add(searchLabel);
            buttonPanel.Controls.Add(_search);

            Controls.Add(_modificationLogsManager.GetItems());
            Controls.Add(buttonPanel);
        }

        // An empty box means no scope, so everything is selected and Generate Script
        // exports the whole log. A name with no rows yet changes nothing, so starting a
        // new changeset leaves any hand-picked selection alone.
        private void SelectActiveChangeset()
        {
            string name = _changeset.Text.Trim();

            if (name.Length == 0)
                _modificationLogsManager.SelectAll();
            else
                _modificationLogsManager.SelectChangeset(name);
        }

        private void UpdateRowMenuState()
        {
            ListView rows = _modificationLogsManager.GetItems();
            bool any = rows.SelectedItems.Count > 0;

            _copyRows.Enabled = any;
            _removeRows.Enabled = any;

            // Reverting writes to the model, so it needs a repository as well as rows.
            _revertRows.Enabled = any && _repository != null;

            // A row keeps its element only while that element exists; once it is deleted
            // the row holds the old id as plain text instead, and there is nothing to show.
            _showInBrowser.Enabled = any && _repository != null && rows.SelectedItems[0].Tag is Element;
        }

        // An attribute row is filed under the element that owns it, so this lands on the
        // type rather than the attribute. The row's first column says which attribute.
        private void ShowInProjectBrowser()
        {
            if (_repository == null) return;

            ListView rows = _modificationLogsManager.GetItems();
            if (rows.SelectedItems.Count == 0) return;

            Element element = rows.SelectedItems[0].Tag as Element;
            if (element == null) return;

            try
            {
                _repository.ShowInProjectView(element);
            }
            catch
            {
                // Deleted since the row was written; nothing to select.
            }
        }

        private void CopySelectedRows()
        {
            ListView rows = _modificationLogsManager.GetItems();
            if (rows.SelectedItems.Count == 0) return;

            var builder = new StringBuilder();
            foreach (ListViewItem item in rows.SelectedItems)
            {
                var cells = new List<string>();

                // Visible columns only: the hidden element id is no use in a paste.
                int last = Math.Min(item.SubItems.Count, ModificationLogsManager.ElementIdIndex);
                for (int i = 0; i < last; i++)
                    cells.Add(item.SubItems[i].Text);

                builder.AppendLine(string.Join("\t", cells.ToArray()));
            }

            try
            {
                Clipboard.SetText(builder.ToString());
            }
            catch
            {
                // Another process can hold the clipboard open; not worth interrupting for.
            }
        }

        // Writes each row's old value back onto the model, then drops the rows that were
        // undone. They have to go: a reverted row no longer describes the model, and the
        // next delta script would re-apply the change that was just taken back.
        private void RevertSelectedRows()
        {
            if (_repository == null) return;

            List<ListViewItem> rows = _modificationLogsManager.SelectedItems();
            if (rows.Count == 0) return;

            if (MessageBox.Show(
                    "Write the old value back onto the model for " + rows.Count + " change(s)?\r\n\r\n" +
                    "Every change that is undone is also removed from the log, so it will not be " +
                    "generated again. This cannot be undone.",
                    "Revert Change", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            ModificationReverter.Result result =
                new ModificationReverter(CommandsUtility.Resolve<ModifiedElementDetectionCommand>()).Revert(rows);

            if (result.Reverted.Count > 0)
            {
                _modificationLogsManager.Remove(result.Reverted);
                _modificationLogsManager.SaveItems(_repository);
                RefreshChangesets();

                // Names and stereotypes written back do not reach the Project Browser on
                // their own.
                _repository.RefreshModelView(0);
            }

            ShowRevertResult(result);
        }

        private static void ShowRevertResult(ModificationReverter.Result result)
        {
            var message = new StringBuilder();
            message.AppendLine(result.Reverted.Count + " change(s) reverted.");

            if (result.Skipped.Count > 0)
            {
                message.AppendLine();
                message.AppendLine(result.Skipped.Count + " left alone:");

                // Enough to see the pattern without a dialog taller than the screen.
                int shown = Math.Min(result.Skipped.Count, 10);
                for (int i = 0; i < shown; i++)
                    message.AppendLine("   " + result.Skipped[i]);

                if (result.Skipped.Count > shown)
                    message.AppendLine("   ... and " + (result.Skipped.Count - shown) + " more");
            }

            MessageBox.Show(message.ToString(), "Revert Change", MessageBoxButtons.OK,
                result.Skipped.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }

        // Removing is permanent -- the log is the only record of what changed, and nothing
        // regenerates it -- so it asks first.
        private void RemoveSelectedRows()
        {
            int count = _modificationLogsManager.GetItems().SelectedItems.Count;
            if (count == 0) return;

            if (MessageBox.Show("Remove " + count + " row(s) from the log? This cannot be undone.",
                    "Modification Logs", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            _modificationLogsManager.RemoveSelected();

            if (_repository != null) _modificationLogsManager.SaveItems(_repository);
            RefreshChangesets();
        }

        // Keeps the picker's list in step with what the log actually contains, without
        // disturbing whatever the user has typed as the active changeset.
        private void RefreshChangesets()
        {
            string active = _changeset.Text;

            _changeset.Items.Clear();
            foreach (string name in _modificationLogsManager.ChangesetNames())
                _changeset.Items.Add(name);

            _changeset.Text = active;
        }

        private void ExecuteClick(object sender, EventArgs e)
        {
            string name = _toolTip.GetToolTip(sender as Button);
            ICommand command = CommandsUtility.GetCommand(name);
            if (command == null) return;

            command.Execute(new Context(_repository, ObjectType.otAttribute, null, _modificationLogsManager.GetItems()));

            // Clear empties the visible rows and the stored notes, so the manager's own
            // copy of the log has to go with them or the next tracked change writes it
            // all back.
            if (name == CommandsUtility.ClearModificationLogs)
            {
                _modificationLogsManager.ClearAll();
                RefreshChangesets();
            }
        }

        public void AddRow(Context context, Element element, string time = "", string elementName = "", string type = "", string stereotype = "", string property = "", string oldValue = "", string newValue = "")
        {
            _repository = context.Repository;
            _modificationLogsManager.EnsureLoaded(context.Repository);
            _modificationLogsManager.AddItem(_modificationLogsManager.GenerateItem(element, elementName, type,
                stereotype, property, oldValue, newValue, time, _changeset.Text.Trim(),
                element.ElementID.ToString()));
            _modificationLogsManager.SaveItems(context.Repository);
            RefreshChangesets();
        }

        public void LoadItems(Repository contextRepository)
        {
            _repository = contextRepository;
            _modificationLogsManager.LoadItems(contextRepository);
            RefreshChangesets();
        }
    }
}