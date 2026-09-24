using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Dastan.org.ed.ea.entity;
using Dastan.org.ed.ea.repository;
using Dastan.org.ed.ea.services.compare;
using Dastan.org.ed.ea.services.enovia;
using Dastan.org.ed.ea.services.log;
using Dastan.org.ed.ea.services.reverse;
using Dastan.org.ed.ea.util;
using EA;

namespace Dastan.org.ed.ea.commands
{
    // Creates model elements from an ENOVIA export: the reverse of generating a script.
    //
    // What to create is decided by comparing against the WHOLE model, not against the
    // package being imported into. Otherwise importing into a fresh package would
    // recreate every type the model already has somewhere else.
    //
    // Only ever creates. An object the model already has is left exactly as it is, even
    // when the export disagrees with it -- that is what the comparison window is for.
    public class ImportFromExportCommand : ICommand
    {
        private const string Title = "Import From ENOVIA Export";

        private readonly ElementRepository _elementRepository;

        public ImportFromExportCommand(ElementRepository elementRepository)
        {
            _elementRepository = elementRepository;
        }

        public void Execute(Context context)
        {
            Package target = SelectedPackage(context.Repository);
            if (target == null)
            {
                MessageBox.Show(
                    "Select the package to import into first.\r\n\r\n" +
                    "Elements are created inside it, in a sub-package for each kind, so that " +
                    "everything an import adds can be found in one place -- and deleted in one " +
                    "place if it is not what you wanted.", Title,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string path = AskForExport();
            if (path == null) return;

            Cursor previous = Cursor.Current;

            try
            {
                Cursor.Current = Cursors.WaitCursor;

                EnoviaExport export = new EnoviaExportReader().Read(path);
                List<SchemaItem> modelled = new ModelSchemaSnapshot(_elementRepository)
                    .Build(context.Repository, null);

                SchemaComparisonResult comparison = new SchemaComparison()
                    .Compare(new EnoviaSchemaSnapshot().Build(export), modelled);

                SchemaImportPlan plan = SchemaImportPlan.From(export, comparison);
                Cursor.Current = previous;

                string exportName = Path.GetFileName(path);

                if (plan.IsEmpty)
                {
                    MessageBox.Show(
                        "There is nothing to import from " + exportName + ".\r\n\r\n" +
                        "The model already has every type, relationship, policy and role it " +
                        "names." + Unplaced(plan), Title,
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (!Confirm(plan, target, exportName)) return;

                Cursor.Current = Cursors.WaitCursor;
                SchemaImportResult result = new SchemaImporter().Import(context.Repository, target, plan);
                Cursor.Current = previous;

                Report(context, exportName, result);
                Show(context.Repository, target);

                MessageBox.Show(
                    "Created " + result.Summary() + " in " + target.Name + "." +
                    Skipped(result), Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception e)
            {
                Cursor.Current = previous;
                MessageBox.Show("Error: " + e.Message, Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                Cursor.Current = previous;
            }
        }

        public bool HasAccess(Repository repository)
        {
            return true;
        }

        // Asked before anything is written, because EA has no undo for what an add-in
        // does through its API. Deleting the sub-packages afterwards is the only way back,
        // so the counts are stated plainly first.
        private static bool Confirm(SchemaImportPlan plan, Package target, string exportName)
        {
            var lines = new List<string>();

            Add(lines, plan.Types.Count, "type");
            Add(lines, plan.Relationships.Count, "relationship");
            Add(lines, plan.Policies.Count, "policy", "policies");
            Add(lines, plan.Roles.Count, "role");

            return MessageBox.Show(
                "Create " + string.Join(", ", lines.ToArray()) + " from " + exportName +
                " inside " + target.Name + "?\r\n\r\n" +
                "Attributes are created on the types and relationships that carry them.\r\n\r\n" +
                "Nothing existing is changed. EA cannot undo this -- to reverse it, delete the " +
                "sub-packages it creates." + Unplaced(plan), Title,
                MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK;
        }

        private static string Unplaced(SchemaImportPlan plan)
        {
            if (plan.Unplaced.Count == 0) return "";

            return "\r\n\r\n" + plan.Unplaced.Count + " attribute(s) in the export are missing from the " +
                   "model but belong to a type it already has, so they are not part of this import. " +
                   "The Output tab lists them.";
        }

        private static string Skipped(SchemaImportResult result)
        {
            if (result.Skipped.Count == 0) return "";

            return "\r\n\r\n" + result.Skipped.Count + " thing(s) could not be created. The Output tab " +
                   "says which, and why.";
        }

        private static void Add(List<string> lines, int count, string singular, string plural = null)
        {
            if (count == 0) return;

            lines.Add(count + " " + (count == 1 ? singular : plural ?? singular + "s"));
        }

        private static string AskForExport()
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Select an ENOVIA schema export to import";
                dialog.Filter = "ENOVIA export (*.xml)|*.xml|All files (*.*)|*.*";
                dialog.CheckFileExists = true;

                return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null;
            }
        }

        // A package, and only a package. An import has to put its elements somewhere, and
        // guessing at one from an element selection would scatter them somewhere the
        // modeller did not choose.
        private static Package SelectedPackage(Repository repository)
        {
            try
            {
                return repository.GetContextItem(out object item) == ObjectType.otPackage
                    ? (Package) item
                    : null;
            }
            catch
            {
                return null;
            }
        }

        private static void Show(Repository repository, Package target)
        {
            try
            {
                repository.RefreshModelView(target.PackageID);
                repository.ShowInProjectView(target);
            }
            catch
            {
                // Cosmetic only; the elements are created either way.
            }
        }

        private static void Report(Context context, string exportName, SchemaImportResult result)
        {
            try
            {
                LogTabService log = TabUtility.Create(context).LogTab;
                log.Log("[" + Title + "] " + exportName + ": created " + result.Summary(), 0);

                foreach (string skipped in result.Skipped)
                    log.Log("[" + Title + "] not created: " + skipped, 0);
            }
            catch
            {
                // Best-effort only; the message box already gave the counts.
            }
        }
    }
}
