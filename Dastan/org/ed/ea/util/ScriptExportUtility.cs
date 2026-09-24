using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Dastan.org.ed.ea.entity;
using Dastan.org.ed.ea.services.lint;
using Dastan.org.ed.ea.services.log;
using Dastan.org.ed.ea.settings;
using Dastan.org.ed.ea.tool;

namespace Dastan.org.ed.ea.util
{
    public class ScriptExportUtility
    {
        public static string WrapTransaction(string body)
        {
            return "start transaction;\n" + body + "\ncommit transaction;";
        }

        // The configured output folder plus a single timestamp, shared by every file a
        // command writes for one export, so a Schema/Policy-style export that writes both
        // a script file and a .properties file gets matching timestamps on both.
        public readonly struct OutputPaths
        {
            private readonly string _folder;
            private readonly string _timestamp;

            public OutputPaths(string folder, string timestamp)
            {
                _folder = folder;
                _timestamp = timestamp;
            }

            public string FilePath(string fileNameSuffix, string extension = "txt")
            {
                return Path.Combine(_folder, $"{fileNameSuffix}_{_timestamp}.{extension}");
            }
        }

        // Resolves the configured output folder and a timestamp for this export.
        // Returns null if no output folder is configured (FileUtility has already
        // shown the user a message in that case).
        public static OutputPaths? ResolveOutputPaths()
        {
            string folder = FileUtility.GetFilePath();
            if (folder == null) return null;

            return new OutputPaths(folder, DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        }

        public static void WriteFile(string filePath, string content, bool includeBom = true)
        {
            Encoding encoding = includeBom ? Encoding.UTF8 : new UTF8Encoding(false);
            File.WriteAllText(filePath, content, encoding);
        }

        // Shows the finished script for review before anything is written, with the lint
        // findings beside it. Returns false when the export was called off, so the caller
        // skips the rest of its output and the completion message.
        public static bool WriteScript(Context context, string filePath, string body, bool includeBom = true)
        {
            string script = WrapTransaction(body);

            // Lint the text the preview will show, not the body: statement numbers then
            // match what is on screen, and each finding's offset indexes that same string
            // so the preview can point straight at the statement. The transaction
            // statements are well-formed and pass every rule.
            string preview = ToPreviewText(script);
            List<ScriptFinding> findings = new ScriptLinter().Lint(preview);

            LogFindings(context, findings);

            bool skip = findings.Count == 0 && AppSettings.SkipPreviewWhenClean;
            if (!skip && !ScriptPreviewForm.Confirm(preview, findings)) return false;

            // The file keeps the generators' own line endings; the conversion above is
            // only so the text renders as lines in a Windows text control.
            WriteFile(filePath, script, includeBom);
            return true;
        }

        // A multiline text control only breaks on \r\n, and the generators join on \n, so
        // without this the whole export renders as a single line.
        private static string ToPreviewText(string script)
        {
            return (script ?? "")
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Replace("\n", "\r\n");
        }

        // Keeps a record in the output tab that outlives the preview window, for when an
        // export goes ahead and the findings are worth revisiting afterwards.
        private static void LogFindings(Context context, List<ScriptFinding> findings)
        {
            if (findings.Count == 0) return;

            try
            {
                LogTabService log = TabUtility.Create(context).LogTab;
                foreach (ScriptFinding finding in findings)
                    log.Log("[Script Check] " + finding, 0);
            }
            catch
            {
                // Best-effort only; the preview already shows the findings.
            }
        }

        // Shows the standard "export completed" message for primaryFilePath, then
        // auto-opens primaryFilePath and any additional files if AutoOpenExport is enabled.
        public static void NotifyExportCompleted(string primaryFilePath, params string[] otherFilesToOpen)
        {
            MessageBox.Show(string.Format(Resources.EXPORT_COMPLETED_DESCRIPTION, primaryFilePath),
                Resources.EXPORT_COMPLETED_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Information);

            if (!AppSettings.AutoOpenExport) return;

            Process.Start(primaryFilePath);
            foreach (string path in otherFilesToOpen)
                Process.Start(path);
        }
    }
}