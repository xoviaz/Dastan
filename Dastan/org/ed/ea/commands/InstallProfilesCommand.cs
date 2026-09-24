using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Dastan.org.ed.ea.entity;
using Dastan.org.ed.ea.services.log;
using Dastan.org.ed.ea.services.profiles;
using Dastan.org.ed.ea.util;
using EA;

namespace Dastan.org.ed.ea.commands
{
    // Gets the add-in's MDG technologies onto disk and into EA's own Import MDG
    // Technology dialog.
    //
    // EA's Repository.ImportTechnology(string) automation call was tried first and
    // dropped: it leaves a technology listed and even toggleable in Manage Technologies,
    // but never builds the stereotype and toolbox tables the profile actually needs, and
    // no amount of enabling afterward creates them retroactively -- confirmed by hand,
    // toggling the checkbox off and back on in EA does nothing. The file-based dialog is
    // the one route that has ever produced working stereotypes, so this command's whole
    // job is to make using it a single click: clear out any broken entry from an earlier
    // attempt, write the files, and open the folder EA needs to be pointed at.
    public class InstallProfilesCommand : ICommand
    {
        private const string Title = "Install Profiles";
        private const string FolderName = "Dastan Profiles";

        public void Execute(Context context)
        {
            Repository repository = context.Repository;

            try
            {
                List<MdgTechnology> chosen = Pick(repository);
                if (chosen == null) return;

                if (chosen.Count == 0)
                {
                    MessageBox.Show("Nothing was selected.", Title, MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                if (!Confirm(chosen)) return;

                ClearBroken(context, repository, chosen);

                List<string> saved;
                try
                {
                    saved = Save(chosen);
                }
                catch (Exception e)
                {
                    MessageBox.Show("The profile files could not be saved.\r\n\r\n" + e.Message, Title,
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Log(context, "saved " + saved.Count + " profile file(s) to " + FolderPath());
                Open(FolderPath());
                ShowSteps(chosen);
            }
            catch (Exception e)
            {
                MessageBox.Show("Error: " + e.Message, Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        public bool HasAccess(Repository repository)
        {
            return true;
        }

        // Which technologies to touch at all. Not every model needs the widget profile,
        // and writing files nobody asked for is as much a surprise as writing none.
        //
        // Returns null on Cancel, distinct from an empty list, so the caller can tell
        // "stop" from "the modeller unchecked everything on purpose".
        private static List<MdgTechnology> Pick(Repository repository)
        {
            using (var form = new TechnologyPickerForm(repository))
            {
                return form.ShowDialog() == DialogResult.OK ? form.Selected : null;
            }
        }

        private static bool Confirm(List<MdgTechnology> chosen)
        {
            var message = new StringBuilder();
            message.AppendLine("Prepare the following for import?");
            message.AppendLine();

            foreach (MdgTechnology technology in chosen)
                message.AppendLine("  " + technology.Name);

            message.AppendLine();
            message.AppendLine("Any broken copy already registered from an earlier attempt is removed " +
                               "first. The files are then saved and their folder opened, ready for " +
                               "EA's own Import MDG Technology dialog.");

            return MessageBox.Show(message.ToString(), Title, MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question) == DialogResult.OK;
        }

        // Clears out a technology stuck exactly the way the automation import leaves one:
        // listed, toggleable, and doing nothing. Delete is a much simpler operation than
        // import, so it is trusted here even though import was not.
        private static void ClearBroken(Context context, Repository repository, List<MdgTechnology> chosen)
        {
            foreach (MdgTechnology technology in chosen)
            {
                if (!IsLoaded(repository, technology.Id)) continue;

                try
                {
                    repository.DeleteTechnology(technology.Id);
                    Log(context, "removed the existing '" + technology.Name + "' entry before re-import");
                }
                catch (Exception e)
                {
                    Log(context, "could not remove the existing '" + technology.Name + "' entry -- " +
                                 e.Message + ". Delete it by hand in Manage Technologies first if the " +
                                 "import below refuses to run.");
                }
            }
        }

        private static List<string> Save(List<MdgTechnology> chosen)
        {
            string folder = FolderPath();
            var saved = new List<string>();

            foreach (MdgTechnology technology in chosen) saved.Add(technology.Save(folder));

            return saved;
        }

        private static string FolderPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), FolderName);
        }

        private static void Open(string folder)
        {
            try
            {
                Process.Start(folder);
            }
            catch
            {
                // Opening the folder is a convenience; the files are written either way,
                // and ShowSteps names the path regardless.
            }
        }

        private static void ShowSteps(List<MdgTechnology> chosen)
        {
            var message = new StringBuilder();
            message.AppendLine("Saved to:");
            message.AppendLine("  " + FolderPath());
            message.AppendLine();
            message.AppendLine("In EA:");
            message.AppendLine();
            message.AppendLine("  1.  Specialize -> Publish Technologies -> Import MDG Technology");
            message.AppendLine("  2.  Point it at the file in that folder:");

            foreach (MdgTechnology technology in chosen)
                message.AppendLine("        " + technology.FileName);

            message.AppendLine("  3.  Restart EA.");

            MessageBox.Show(message.ToString(), Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static bool IsLoaded(Repository repository, string id)
        {
            try
            {
                return repository.IsTechnologyLoaded(id);
            }
            catch
            {
                return false;
            }
        }

        private static void Log(Context context, string message)
        {
            try
            {
                LogTabService log = TabUtility.Create(context).LogTab;
                log.Log("[" + Title + "] " + message, 0);
            }
            catch
            {
                // Best-effort only; the message box already said what happened.
            }
        }

        // Which of the technologies the add-in carries to touch. Every row starts
        // checked -- the common case is preparing all of them -- but nothing stops a
        // modeller who only wants the schema profile from unchecking the rest.
        private class TechnologyPickerForm : Form
        {
            private readonly CheckedListBox _list;

            public List<MdgTechnology> Selected { get; private set; }

            public TechnologyPickerForm(Repository repository)
            {
                Text = Title;
                Size = new Size(480, 320);
                MinimumSize = new Size(360, 220);
                StartPosition = FormStartPosition.CenterScreen;
                ShowInTaskbar = false;
                IconUtility.Apply(this);
                Padding = new Padding(10);

                var label = new Label
                {
                    Dock = DockStyle.Top,
                    Height = 20,
                    Text = "Choose which profiles to prepare:"
                };

                _list = new CheckedListBox
                {
                    Dock = DockStyle.Fill,
                    CheckOnClick = true,
                    IntegralHeight = false
                };

                foreach (MdgTechnology technology in MdgTechnology.All)
                {
                    bool loaded = IsLoaded(repository, technology.Id);

                    string line = technology.Name +
                                  (loaded ? "  (an entry is already registered -- it will be removed first)"
                                          : "  (new)") +
                                  "\r\n      " + technology.Notes;

                    _list.Items.Add(line, true);
                }

                Controls.Add(_list);
                Controls.Add(label);
                Controls.Add(BuildBottomBar());
            }

            private Panel BuildBottomBar()
            {
                var bar = new Panel { Dock = DockStyle.Bottom, Height = 40 };

                var cancel = new Button
                {
                    Text = "Cancel", Width = 90, Height = 28, Dock = DockStyle.Right,
                    DialogResult = DialogResult.Cancel
                };

                var ok = new Button
                {
                    Text = "Continue", Width = 90, Height = 28, Dock = DockStyle.Right,
                    DialogResult = DialogResult.OK
                };
                ok.Click += (s, e) => Collect();

                var spacer = new Panel { Width = 8, Dock = DockStyle.Right };

                bar.Controls.Add(cancel);
                bar.Controls.Add(spacer);
                bar.Controls.Add(ok);

                AcceptButton = ok;
                CancelButton = cancel;
                return bar;
            }

            private void Collect()
            {
                var picked = new List<MdgTechnology>();
                IReadOnlyList<MdgTechnology> all = MdgTechnology.All;

                for (int i = 0; i < _list.Items.Count; i++)
                {
                    if (_list.GetItemChecked(i)) picked.Add(all[i]);
                }

                Selected = picked;
            }

            private static bool IsLoaded(Repository repository, string id)
            {
                try
                {
                    return repository.IsTechnologyLoaded(id);
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}
