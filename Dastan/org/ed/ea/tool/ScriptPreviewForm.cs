using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Dastan.org.ed.ea.services.lint;
using Dastan.org.ed.ea.settings;
using Dastan.org.ed.ea.util;

namespace Dastan.org.ed.ea.tool
{
    // Shows the exact text about to be written, with any lint findings beside it, and
    // lets the export be called off. The whole script runs as one transaction, so this
    // is the last point where a bad statement can be caught for free.
    //
    // The script arrives ready to display -- line endings already normalised by the
    // caller -- and every finding's Offset indexes into it, so locating a statement is
    // exact even when two statements read identically.
    public class ScriptPreviewForm : Form
    {
        private const int DefaultWidth = 960;
        private const int DefaultHeight = 700;
        private const int DefaultFindingsHeight = 150;

        private readonly IReadOnlyList<ScriptFinding> _findings;
        private readonly string _script;
        private readonly RichTextBox _scriptBox;
        private readonly ListBox _findingsList;
        private readonly SplitContainer _split;
        private readonly Panel _findBar;
        private readonly TextBox _findBox;
        private readonly CheckBox _wrap;
        private int _findFrom;
        private int _savedSplitter;

        public static bool Confirm(string script, IReadOnlyList<ScriptFinding> findings)
        {
            using (var form = new ScriptPreviewForm(script, findings))
            {
                return form.ShowDialog() == DialogResult.OK;
            }
        }

        private ScriptPreviewForm(string script, IReadOnlyList<ScriptFinding> findings)
        {
            IconUtility.Apply(this);
            _findings = findings;
            _script = script ?? "";

            Text = findings.Count > 0
                ? "Script Preview - " + findings.Count + " problem(s) found"
                : "Script Preview";

            MinimumSize = new Size(620, 420);
            StartPosition = FormStartPosition.CenterScreen;
            MinimizeBox = false;
            ShowInTaskbar = false;
            Padding = new Padding(10, 10, 10, 6);

            bool wrap = ApplySavedLayout();

            _scriptBox = BuildScriptBox(wrap);
            _findBox = BuildFindBox();
            _findBar = BuildFindBar();
            _wrap = BuildWrapToggle(wrap);

            var scriptHost = new Panel { Dock = DockStyle.Fill };
            scriptHost.Controls.Add(_scriptBox);
            scriptHost.Controls.Add(_findBar);

            if (findings.Count > 0)
            {
                _findingsList = BuildFindingsList();
                _split = BuildSplit(scriptHost);
                Controls.Add(_split);
            }
            else
            {
                Controls.Add(scriptHost);
            }

            Controls.Add(BuildBottomBar());
        }

        private RichTextBox BuildScriptBox(bool wrap)
        {
            var box = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                WordWrap = wrap,
                ScrollBars = RichTextBoxScrollBars.Both,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                // Keeps a located statement highlighted while focus sits in the list.
                HideSelection = false,
                DetectUrls = false
            };

            box.Rtf = MqlRtfBuilder.Build(_script, MarkFlagged());
            return box;
        }

        // Character ranges covered by a flagged statement, taken straight from each
        // finding's offset, so the highlighter can give them a background.
        private bool[] MarkFlagged()
        {
            var flagged = new bool[_script.Length];

            foreach (ScriptFinding finding in _findings)
            {
                int end = Math.Min(finding.Offset + finding.Statement.Length, flagged.Length);
                for (int i = Math.Max(finding.Offset, 0); i < end; i++)
                    flagged[i] = true;
            }

            return flagged;
        }

        private ListBox BuildFindingsList()
        {
            var list = new ListBox
            {
                Dock = DockStyle.Fill,
                HorizontalScrollbar = true,
                Font = new Font("Consolas", 8.75F),
                BorderStyle = BorderStyle.FixedSingle
            };

            foreach (ScriptFinding finding in _findings)
                list.Items.Add(finding.ToString());

            list.SelectedIndexChanged += LocateSelectedFinding;
            return list;
        }

        private SplitContainer BuildSplit(Control scriptHost)
        {
            var header = new Label
            {
                Dock = DockStyle.Top,
                Height = 22,
                Text = "Problems found - select one to jump to it in the script:",
                ForeColor = Color.FromArgb(150, 60, 0),
                Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold),
                Padding = new Padding(1, 4, 0, 0)
            };

            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                Panel1MinSize = 60,
                Panel2MinSize = 140
            };

            split.Panel1.Controls.Add(_findingsList);
            split.Panel1.Controls.Add(header);
            split.Panel2.Controls.Add(scriptHost);
            split.Panel2.Padding = new Padding(0, 6, 0, 0);
            return split;
        }

        private TextBox BuildFindBox()
        {
            var box = new TextBox { Width = 220, Left = 44, Top = 5 };
            box.TextChanged += (s, e) =>
            {
                _findFrom = 0;
                box.BackColor = Color.White;
            };
            box.KeyDown += (s, e) =>
            {
                if (e.KeyCode != Keys.Enter) return;

                FindNext();
                e.Handled = true;
                e.SuppressKeyPress = true;
            };
            return box;
        }

        private Panel BuildFindBar()
        {
            var label = new Label { Text = "Find:", Left = 2, Top = 8, Width = 40 };
            var next = new Button { Text = "Next", Left = 272, Top = 4, Width = 64, Height = 23 };
            var close = new Button { Text = "Close", Left = 342, Top = 4, Width = 64, Height = 23 };

            next.Click += (s, e) => FindNext();
            close.Click += (s, e) => HideFindBar();

            var bar = new Panel { Dock = DockStyle.Top, Height = 32, Visible = false };
            bar.Controls.Add(label);
            bar.Controls.Add(_findBox);
            bar.Controls.Add(next);
            bar.Controls.Add(close);
            return bar;
        }

        private CheckBox BuildWrapToggle(bool wrap)
        {
            // The trigger statement carries fifteen program arguments on one line, so
            // wrapping is the only way to read it without scrolling sideways.
            var toggle = new CheckBox
            {
                Text = "Wrap long lines",
                AutoSize = true,
                Checked = wrap,
                Margin = new Padding(0, 8, 14, 0)
            };

            toggle.CheckedChanged += (s, e) => _scriptBox.WordWrap = toggle.Checked;
            return toggle;
        }

        private Control BuildBottomBar()
        {
            var cancel = new Button { Text = "Cancel", Width = 94, Height = 27, DialogResult = DialogResult.Cancel };
            var export = new Button { Text = "Export", Width = 94, Height = 27, DialogResult = DialogResult.OK };
            var copy = new Button { Text = "Copy", Width = 94, Height = 27 };

            copy.Click += CopyScript;
            AcceptButton = export;
            CancelButton = cancel;

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                WrapContents = false,
                Padding = new Padding(0, 6, 0, 0)
            };
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(export);
            buttons.Controls.Add(copy);

            var skipWhenClean = new CheckBox
            {
                Text = "Skip when clean",
                AutoSize = true,
                Checked = AppSettings.SkipPreviewWhenClean,
                Margin = new Padding(0, 8, 14, 0)
            };
            skipWhenClean.CheckedChanged += (s, e) => AppSettings.SkipPreviewWhenClean = skipWhenClean.Checked;

            var summary = new Label
            {
                Text = Summarize(_script),
                ForeColor = SystemColors.GrayText,
                AutoSize = true,
                Margin = new Padding(0, 9, 0, 0)
            };

            var options = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true
            };
            options.Controls.Add(_wrap);
            options.Controls.Add(skipWhenClean);
            options.Controls.Add(summary);

            var bar = new Panel { Dock = DockStyle.Bottom, Height = 40 };
            bar.Controls.Add(options);
            bar.Controls.Add(buttons);
            return bar;
        }

        private static string Summarize(string script)
        {
            if (script.Length == 0) return "empty script";

            int lines = script.Split('\n').Length;
            return lines.ToString("N0") + " lines, " + script.Length.ToString("N0") + " characters";
        }

        private void CopyScript(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_script)) return;

            try
            {
                Clipboard.SetText(_script);
            }
            catch
            {
                // Another process can hold the clipboard open; not worth interrupting for.
            }
        }

        private void LocateSelectedFinding(object sender, EventArgs e)
        {
            int selected = _findingsList.SelectedIndex;
            if (selected < 0 || selected >= _findings.Count) return;

            ScriptFinding finding = _findings[selected];
            if (finding.Offset < 0 || finding.Offset >= _script.Length) return;

            _scriptBox.Select(finding.Offset, finding.Statement.Length);
            _scriptBox.ScrollToCaret();
        }

        private void ShowFindBar()
        {
            _findBar.Visible = true;
            _findBox.Focus();
            _findBox.SelectAll();
        }

        private void HideFindBar()
        {
            _findBar.Visible = false;
            _scriptBox.Focus();
        }

        private void FindNext()
        {
            string term = _findBox.Text;
            if (term.Length == 0) return;

            int at = _scriptBox.Find(term, _findFrom, RichTextBoxFinds.None);

            // Wrap once from the top before reporting nothing found.
            if (at < 0 && _findFrom > 0) at = _scriptBox.Find(term, 0, RichTextBoxFinds.None);

            if (at < 0)
            {
                _findBox.BackColor = Color.MistyRose;
                return;
            }

            _findBox.BackColor = Color.White;
            _scriptBox.Select(at, term.Length);
            _scriptBox.ScrollToCaret();
            _findFrom = at + term.Length;
        }

        // "width,height,splitter,wrap". Anything unparseable falls back to the defaults
        // rather than failing to open the window.
        private bool ApplySavedLayout()
        {
            Width = DefaultWidth;
            Height = DefaultHeight;
            _savedSplitter = DefaultFindingsHeight;

            string[] parts = AppSettings.PreviewLayout.Split(',');
            if (parts.Length < 4) return false;

            int width, height, splitter, wrap;
            if (int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out width) &&
                int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out height) &&
                width >= MinimumSize.Width && height >= MinimumSize.Height)
            {
                Width = width;
                Height = height;
            }

            if (int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out splitter) && splitter > 0)
                _savedSplitter = splitter;

            return int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out wrap) && wrap == 1;
        }

        private void SaveLayout()
        {
            // RestoreBounds carries the pre-maximise size, which is the one worth keeping.
            Size size = WindowState == FormWindowState.Normal ? Size : RestoreBounds.Size;
            int splitter = _split != null ? _split.SplitterDistance : _savedSplitter;

            AppSettings.PreviewLayout = string.Join(",", new[]
            {
                size.Width.ToString(CultureInfo.InvariantCulture),
                size.Height.ToString(CultureInfo.InvariantCulture),
                splitter.ToString(CultureInfo.InvariantCulture),
                _wrap.Checked ? "1" : "0"
            });
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.F))
            {
                ShowFindBar();
                return true;
            }

            // Escape closes the find bar first; only then does it cancel the dialog.
            if (keyData == Keys.Escape && _findBar.Visible)
            {
                HideFindBar();
                return true;
            }

            if (keyData == Keys.F3)
            {
                FindNext();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // Set once the split has a real height; assigning it in the constructor can
            // exceed the control's size and throw.
            if (_split == null) return;

            int highest = _split.Height - _split.Panel2MinSize - _split.SplitterWidth;
            if (highest < _split.Panel1MinSize) return;

            _split.SplitterDistance = Math.Min(Math.Max(_savedSplitter, _split.Panel1MinSize), highest);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Saved however the dialog was dismissed -- a resize is worth keeping even if
            // the export was called off.
            SaveLayout();
            base.OnFormClosing(e);
        }
    }
}
