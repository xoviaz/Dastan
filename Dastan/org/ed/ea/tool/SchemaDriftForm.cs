using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Dastan.org.ed.ea.services.compare;
using Dastan.org.ed.ea.util;
using EA;

namespace Dastan.org.ed.ea.tool
{
    // Where an ENOVIA export and the model disagree, listed so each disagreement can be
    // walked back to the object it is about.
    //
    // Built like ModelValidationForm and for the same reason: the point of the window is
    // to act on what it found, so it is modeless and it stays out of the way of the
    // project browser it sends you to. One instance, reused, so comparing again replaces
    // the list instead of stacking windows.
    public class SchemaDriftForm : Form
    {
        private const string ShowEverything = "Everything";

        private static SchemaDriftForm _instance;

        // Not readonly: the window outlives a run, and reopening a different project hands
        // the next run a different Repository.
        private Repository _repository;
        private readonly ListView _list;
        private readonly Label _summary;
        private readonly ComboBox _filter;
        private IReadOnlyList<SchemaDriftFinding> _findings;

        public static void ShowFindings(Repository repository, string scope, string exportName, int compared,
            IReadOnlyList<SchemaDriftFinding> findings)
        {
            if (_instance == null || _instance.IsDisposed)
                _instance = new SchemaDriftForm(repository);

            _instance._repository = repository;
            _instance.Fill(scope, exportName, compared, findings);
            _instance.Show();
            _instance.BringToFront();
        }

        private SchemaDriftForm(Repository repository)
        {
            _repository = repository;

            Text = "Schema Comparison";
            Size = new Size(1060, 560);
            MinimumSize = new Size(700, 320);
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = false;
            IconUtility.Apply(this);
            Padding = new Padding(10, 10, 10, 6);

            _list = BuildList();
            _summary = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };

            _filter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 150,
                Top = 2,
                Left = 0
            };

            _filter.Items.Add(ShowEverything);
            _filter.Items.Add("Only in ENOVIA");
            _filter.Items.Add("Only in the model");
            _filter.Items.Add("Different");
            _filter.SelectedIndex = 0;
            _filter.SelectedIndexChanged += (s, e) => Populate();

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

            list.Columns.Add("Difference", 120);
            list.Columns.Add("Kind", 100);
            list.Columns.Add("Name", 190);
            list.Columns.Add("Field", 140);
            list.Columns.Add("In ENOVIA", 220);
            list.Columns.Add("In the model", 220);

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

            var right = new Panel { Dock = DockStyle.Right, Width = 155 };
            right.Controls.Add(_filter);

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

        private void Fill(string scope, string exportName, int compared,
            IReadOnlyList<SchemaDriftFinding> findings)
        {
            _findings = findings ?? new List<SchemaDriftFinding>();

            int missing = 0;
            int extra = 0;
            int different = 0;

            foreach (SchemaDriftFinding finding in _findings)
            {
                if (finding.Kind == SchemaDriftKind.OnlyInExport) missing++;
                else if (finding.Kind == SchemaDriftKind.OnlyInModel) extra++;
                else different++;
            }

            Text = "Schema Comparison - " + missing + " only in ENOVIA, " + extra + " only in the model, " +
                   different + " different";

            _summary.Text = "Compared " + compared + " object(s) in " + scope + " against " + exportName +
                            ".  Double-click a row to select it in the project browser.";

            Populate();
        }

        private void Populate()
        {
            if (_findings == null) return;

            string wanted = _filter.SelectedItem as string ?? ShowEverything;

            _list.BeginUpdate();
            try
            {
                _list.Items.Clear();

                foreach (SchemaDriftFinding finding in _findings)
                {
                    if (wanted != ShowEverything && finding.Label != wanted) continue;

                    var item = new ListViewItem(finding.Label);
                    item.SubItems.Add(finding.ObjectKind);
                    item.SubItems.Add(finding.Name);
                    item.SubItems.Add(finding.Field);
                    item.SubItems.Add(finding.InExport);
                    item.SubItems.Add(finding.InModel);

                    item.ForeColor = Colour(finding.Kind);
                    item.Tag = finding;
                    _list.Items.Add(item);
                }
            }
            finally
            {
                _list.EndUpdate();
            }
        }

        // Missing from the model is the one that needs work rather than a decision, so it
        // is the one that reads as a problem. The other two are differences, not faults:
        // an object only in the model may simply not be installed yet.
        private static Color Colour(SchemaDriftKind kind)
        {
            switch (kind)
            {
                case SchemaDriftKind.OnlyInExport: return Color.Firebrick;
                case SchemaDriftKind.OnlyInModel: return Color.FromArgb(0, 90, 140);
                default: return Color.FromArgb(150, 100, 0);
            }
        }

        private void SelectInProjectView()
        {
            if (_list.SelectedItems.Count == 0) return;

            SchemaDriftFinding finding = _list.SelectedItems[0].Tag as SchemaDriftFinding;

            // An object that exists only in the export has nothing to select: that is
            // exactly what the row is saying.
            if (finding == null || string.IsNullOrEmpty(finding.ElementGuid)) return;

            try
            {
                Element element = _repository.GetElementByGuid(finding.ElementGuid);
                if (element == null) return;

                _repository.ShowInProjectView(element);
            }
            catch
            {
                // The element may have been deleted since the comparison ran.
            }
        }

        private void CopyToClipboard()
        {
            var builder = new StringBuilder();
            foreach (ListViewItem item in _list.Items)
            {
                SchemaDriftFinding finding = item.Tag as SchemaDriftFinding;
                if (finding != null) builder.AppendLine(finding.ToString());
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
