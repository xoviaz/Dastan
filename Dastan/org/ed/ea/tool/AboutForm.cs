using System;
using System.Reflection;
using System.Windows.Forms;
using Dastan.org.ed.ea.util;

namespace Dastan.org.ed.ea.tool
{
    public class AboutForm : Form
    {
        private const string RepositoryUrl = "https://github.com/xoviaz/Dastan";

        public AboutForm()
        {
            Width = 380;
            Height = 260;
            Text = "About Dastan";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            IconUtility.Apply(this);

            var logo = new PictureBox
            {
                Left = 20, Top = 20, Width = 48, Height = 48,
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = IconUtility.LoadImage("Dastan.icons.dastan-48.png")
            };

            var lblTitle = new Label
            {
                Left = 84, Top = 20, Width = 260, Height = 24,
                Font = new System.Drawing.Font(Font.FontFamily, 12, System.Drawing.FontStyle.Bold),
                Text = "Dastan"
            };

            var lblVersion = new Label
            {
                Left = 84, Top = 46, Width = 260, Height = 18,
                Text = "Version " + GetVersion()
            };

            var lblDescription = new Label
            {
                Left = 20, Top = 84, Width = 320, Height = 60,
                Text = "An Enterprise Architect add-in for generating ENOVIA schema, " +
                       "policy, UI and trigger scripts, and for tracking model modifications."
            };

            var lblCopyright = new Label
            {
                Left = 20, Top = 150, Width = 320, Height = 18,
                Text = GetCopyright()
            };

            var lnkRepository = new LinkLabel
            {
                Left = 20, Top = 172, Width = 320, Height = 18,
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

            var btnClose = new Button
            {
                Left = 264, Top = 200, Width = 80, Height = 26,
                Text = "Close",
                DialogResult = DialogResult.OK
            };

            Controls.Add(logo);
            Controls.Add(lblTitle);
            Controls.Add(lblVersion);
            Controls.Add(lblDescription);
            Controls.Add(lblCopyright);
            Controls.Add(lnkRepository);
            Controls.Add(btnClose);

            AcceptButton = btnClose;
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
