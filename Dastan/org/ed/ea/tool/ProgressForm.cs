using System.Windows.Forms;
using Dastan.org.ed.ea.util;

namespace Dastan.org.ed.ea.services.jira
{
    public class ProgressForm : Form
    {
        private readonly ProgressBar _progressBar;
        private readonly Label _lblStatus;

        public ProgressForm()
        {
            Width = 420;
            Height = 140;
            Text = "Jira Import Progress";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            IconUtility.Apply(this);
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;
            TopMost = true;

            _lblStatus = new Label { Left = 20, Top = 15, Width = 360, Text = "Starting sync..." };
            _progressBar = new ProgressBar { Left = 20, Top = 40, Width = 360, Height = 25 };

            Controls.Add(_lblStatus);
            Controls.Add(_progressBar);
        }

        public void UpdateProgress(int current, int total, string statusMessage)
        {
            _progressBar.Maximum = total;
            _progressBar.Value = current;
            _lblStatus.Text = $"[{current}/{total}] {statusMessage}";

            // Yield control back to Windows UI pump so the form repaints during single-threaded processing
            Application.DoEvents();
        }
    }
}