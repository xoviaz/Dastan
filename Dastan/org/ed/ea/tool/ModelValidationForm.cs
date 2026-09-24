using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Dastan.org.ed.ea.services.validation;
using Dastan.org.ed.ea.util;
using EA;

namespace Dastan.org.ed.ea.tool
{
    // The results of a model check, listed so each one can be walked back to the object it
    // is about.
    //
    // Modeless on purpose: the point of the window is to fix what it found, and a modal
    // dialog would block the project browser it is sending you to. One instance at a time,
    // reused, so running the check again replaces the list instead of stacking windows.
    public class ModelValidationForm : Form
    {
        private static ModelValidationForm _instance;

        // Not readonly: the window outlives a run, and reopening a different project
        // hands the next run a different Repository.
        private Repository _repository;
        private readonly ListView _list;
        private readonly Label _summary;
        private readonly CheckBox _errorsOnly;
        private IReadOnlyList<ModelIssue> _issues;

        public static void ShowIssues(Repository repository, string scope, int checkedCount,
            IReadOnlyList<ModelIssue> issues)
        {
            if (_instance == null || _instance.IsDisposed)
                _instance = new ModelValidationForm(repository);

            _instance._repository = repository;
            _instance.Fill(scope, checkedCount, issues);
            _instance.Show();
            _instance.BringToFront();
        }

        private ModelValidationForm(Repository repository)
        {
            _repository = repository;

            Text = "Model Check";
            Size = new Size(1000, 560);
            MinimumSize = new Size(640, 320);
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = false;
            IconUtility.Apply(this);
            Padding = new Padding(10, 10, 10, 6);

            _list = BuildList();
            _summary = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            _errorsOnly = new CheckBox { Text = "Errors only", AutoSize = true, Top = 4, Left = 0 };
            _errorsOnly.CheckedChanged += (s, e) => Populate();

            Controls.Add(_list);
            Controls.Add(BuildTopBar());
            Controls.Add(BuildBottomBar());
        }

        private ListView BuildList()
        {
            var list = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false,
                HideSelection = false
            };

            list.Columns.Add("Severity", 70);
            list.Columns.Add("Rule", 150);
            list.Columns.Add("Kind", 110);
            list.Columns.Add("Object", 170);
            list.Columns.Add("Problem", 380);
            list.Columns.Add("Where", 220);

            list.DoubleClick += (s, e) => SelectInProjectView();
            list.KeyDown += (s, e) =>
            {
                if (e.KeyCode != Keys.Enter) return;

                SelectInProjectView();
                e.Handled = true;
            };

            return list;
        }

        private Panel BuildTopBar()
        {
            var bar = new Panel { Dock = DockStyle.Top, Height = 28 };

            var right = new Panel { Dock = DockStyle.Right, Width = 110 };
            right.Controls.Add(_errorsOnly);

            bar.Controls.Add(_summary);
            bar.Controls.Add(right);
            return bar;
        }

        private Panel BuildBottomBar()
        {
            var bar = new Panel { Dock = DockStyle.Bottom, Height = 40 };

            var close = new Button { Text = "Close", Width = 90, Height = 28, Dock = DockStyle.Right };
            close.Click += (s, e) => Hide();

            var copy = new Button { Text = "Copy", Width = 90, Height = 28, Dock = DockStyle.Right };
            copy.Click += (s, e) => CopyToClipboard();

            var spacer = new Panel { Width = 8, Dock = DockStyle.Right };

            // Docked right in reverse order, so Close ends up rightmost.
            bar.Controls.Add(copy);
            bar.Controls.Add(spacer);
            bar.Controls.Add(close);

            // Escape hides the window; Enter is left alone so the list can use it to jump
            // to the selected object.
            CancelButton = close;
            return bar;
        }

        // Not called Load: Form already has a Load event, and shadowing it reads like a
        // lifecycle hook when it is nothing of the sort.
        private void Fill(string scope, int checkedCount, IReadOnlyList<ModelIssue> issues)
        {
            _issues = issues ?? new List<ModelIssue>();

            int errors = 0;
            foreach (ModelIssue issue in _issues)
            {
                if (issue.Severity == IssueSeverity.Error) errors++;
            }

            int warnings = _issues.Count - errors;

            Text = "Model Check - " + errors + " error(s), " + warnings + " warning(s)";
            _summary.Text = "Checked " + checkedCount + " object(s) in " + scope +
                            ".  Double-click a row to select it in the project browser.";

            Populate();
        }

        private void Populate()
        {
            if (_issues == null) return;

            _list.BeginUpdate();
            try
            {
                _list.Items.Clear();

                foreach (ModelIssue issue in _issues)
                {
                    if (_errorsOnly.Checked && issue.Severity != IssueSeverity.Error) continue;

                    var item = new ListViewItem(issue.Severity.ToString());
                    item.SubItems.Add(issue.Rule);
                    item.SubItems.Add(issue.Kind);
                    item.SubItems.Add(issue.ObjectName);
                    item.SubItems.Add(issue.Message);
                    item.SubItems.Add(issue.Path);

                    item.ForeColor = issue.Severity == IssueSeverity.Error
                        ? Color.Firebrick
                        : Color.FromArgb(150, 100, 0);

                    item.Tag = issue;
                    _list.Items.Add(item);
                }
            }
            finally
            {
                _list.EndUpdate();
            }
        }

        // An attribute reports its owning element, so this always lands on something the
        // project browser can show even when the issue is about a field inside it.
        private void SelectInProjectView()
        {
            if (_list.SelectedItems.Count == 0) return;

            ModelIssue issue = _list.SelectedItems[0].Tag as ModelIssue;
            if (issue == null || string.IsNullOrEmpty(issue.ElementGuid)) return;

            try
            {
                Element element = _repository.GetElementByGuid(issue.ElementGuid);
                if (element == null) return;

                _repository.ShowInProjectView(element);
            }
            catch
            {
                // The element may have been deleted since the check ran; nothing to show.
            }
        }

        private void CopyToClipboard()
        {
            var builder = new StringBuilder();
            foreach (ListViewItem item in _list.Items)
            {
                ModelIssue issue = item.Tag as ModelIssue;
                if (issue != null) builder.AppendLine(issue.ToString());
            }

            if (builder.Length == 0) return;

            try
            {
                Clipboard.SetText(builder.ToString());
            }
            catch
            {
                // Another process can hold the clipboard open; not worth interrupting for.
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Keep the one instance alive so the next run reuses it, unless EA itself is
            // going away.
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                return;
            }

            base.OnFormClosing(e);
        }
    }
}
