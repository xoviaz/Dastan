using System;
using System.Drawing;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Dastan.org.ed.ea.util;

namespace Dastan.org.ed.ea.tool
{
    public class AboutForm : Form
    {
        private const string RepositoryUrl = "https://github.com/xoviaz/Dastan";
        private const string Tagline = "Enterprise Architect Add-In for ENOVIA";
        private const string Description =
            "Generates ENOVIA schema, policy, UI and trigger scripts from the model, " +
            "and tracks model modifications as they happen.";

        private const int Margin = 20;
        private const int BodyWidth = 340;

        public AboutForm()
        {
            Width = Margin * 2 + BodyWidth + 16;
            Text = "About Dastan";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            IconUtility.Apply(this);

            int bottom = BuildHeader();
            bottom = BuildSeparator(bottom);
            bottom = BuildBody(bottom);
            BuildButtonBar(bottom);
        }

        private int BuildHeader()
        {
            var logo = new PictureBox
            {
                Left = Margin, Top = Margin, Width = 48, Height = 48,
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = IconUtility.LoadImage("Dastan.icons.dastan-48.png")
            };

            var lblTitle = new Label
            {
                Left = logo.Right + 14, Top = Margin - 2,
                AutoSize = true,
                Font = new Font(Font.FontFamily, 13, FontStyle.Bold),
                Text = "Dastan"
            };

            var lblTagline = new Label
            {
                Left = lblTitle.Left, Top = lblTitle.Bottom + 1,
                AutoSize = true,
                ForeColor = SystemColors.GrayText,
                Text = Tagline
            };

            var lblVersion = new Label
            {
                Left = lblTitle.Left, Top = lblTagline.Bottom + 4,
                AutoSize = true,
                Text = "Version " + GetVersion()
            };

            Controls.Add(logo);
            Controls.Add(lblTitle);
            Controls.Add(lblTagline);
            Controls.Add(lblVersion);

            return Math.Max(logo.Bottom, lblVersion.Bottom);
        }

        private int BuildSeparator(int top)
        {
            var separator = new Panel
            {
                Left = Margin, Top = top + 14, Width = BodyWidth, Height = 1,
                BackColor = SystemColors.ControlDark
            };
            Controls.Add(separator);

            return separator.Bottom;
        }

        private int BuildBody(int top)
        {
            var lblDescription = new Label
            {
                Left = Margin, Top = top + 14,
                MaximumSize = new Size(BodyWidth, 0),
                AutoSize = true,
                Text = Description
            };

            var lblCopyright = new Label
            {
                Left = Margin, Top = lblDescription.Bottom + 12,
                AutoSize = true,
                Text = GetCopyright()
            };

            var lnkRepository = new LinkLabel
            {
                Left = Margin, Top = lblCopyright.Bottom + 4,
                AutoSize = true,
                Text = RepositoryUrl
            };
            lnkRepository.LinkClicked += (sender, e) =>
            {
                try
                {
                    System.Diagnostics.Process.Start(RepositoryUrl);
                }
                catch
                {
                    // Opening the default browser is a convenience, not a requirement --
                    // the link text is still readable and copyable if this fails.
                }
            };

            Controls.Add(lblDescription);
            Controls.Add(lblCopyright);
            Controls.Add(lnkRepository);

            return lnkRepository.Bottom;
        }

        private void BuildButtonBar(int top)
        {
            var bar = new Panel
            {
                Left = 0, Top = top + Margin, Width = ClientSize.Width, Height = 40
            };

            var close = new Button { Text = "Close", Width = 90, Height = 28, Dock = DockStyle.Right };
            close.Click += (s, e) => Close();

            var copy = new Button { Text = "Copy Diagnostic Info", Width = 150, Height = 28, Dock = DockStyle.Right };
            copy.Click += (s, e) => CopyDiagnosticInfo();

            var spacer = new Panel { Width = 8, Dock = DockStyle.Right };

            // Docked right in reverse order, so Close ends up rightmost.
            bar.Controls.Add(copy);
            bar.Controls.Add(spacer);
            bar.Controls.Add(close);

            Controls.Add(bar);

            ClientSize = new Size(ClientSize.Width, bar.Bottom + 4);
            AcceptButton = close;
            CancelButton = close;
        }

        private void CopyDiagnosticInfo()
        {
            var builder = new StringBuilder();
            builder.AppendLine("Dastan " + GetVersion());
            builder.AppendLine(GetCopyright());
            builder.AppendLine(".NET Runtime: " + Environment.Version);
            builder.AppendLine("OS: " + Environment.OSVersion.VersionString);

            try
            {
                Clipboard.SetText(builder.ToString());
            }
            catch
            {
                // Another process can hold the clipboard open; not worth interrupting for.
            }
        }

        private static string GetVersion()
        {
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            return version.ToString();
        }

        private static string GetCopyright()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var attribute = (AssemblyCopyrightAttribute) Attribute.GetCustomAttribute(
                assembly, typeof(AssemblyCopyrightAttribute));
            return attribute?.Copyright ?? "";
        }
    }
}
