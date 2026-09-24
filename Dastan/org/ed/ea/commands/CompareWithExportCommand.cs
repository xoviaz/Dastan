using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Dastan.org.ed.ea.entity;
using Dastan.org.ed.ea.repository;
using Dastan.org.ed.ea.services.compare;
using Dastan.org.ed.ea.services.enovia;
using Dastan.org.ed.ea.services.log;
using Dastan.org.ed.ea.tool;
using Dastan.org.ed.ea.util;
using EA;

namespace Dastan.org.ed.ea.commands
{
    // Compares the model against a schema export taken from ENOVIA, and says where the
    // two have drifted apart.
    //
    // The export is produced in MQL with "export type * xml into file ...", because there
    // is no supported way to read the target system's schema from here. That also means
    // the comparison is against a moment in time: the file, not the live system.
    public class CompareWithExportCommand : ICommand
    {
        private const string Title = "Schema Comparison";

        private readonly ElementRepository _elementRepository;

        public CompareWithExportCommand(ElementRepository elementRepository)
        {
            _elementRepository = elementRepository;
        }

        public void Execute(Context context)
        {
            string path = AskForExport();
            if (path == null) return;

            Cursor previous = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;

            try
            {
                EnoviaExport export = new EnoviaExportReader().Read(path);

                Package scope = ScopeOf(context.Repository, out string scopeName);
                List<SchemaItem> modelled = new ModelSchemaSnapshot(_elementRepository)
                    .Build(context.Repository, scope);
                List<SchemaItem> exported = new EnoviaSchemaSnapshot().Build(export);

                SchemaComparisonResult result = new SchemaComparison().Compare(exported, modelled);
                List<SchemaDriftFinding> findings = SchemaDriftFinding.From(result);

                Cursor.Current = previous;

                string exportName = Path.GetFileName(path);
                Report(context, exportName, export, findings);

                if (result.InStep)
                {
                    MessageBox.Show(
                        "Compared " + modelled.Count + " object(s) in " + scopeName + " against " +
                        exportName + ".\r\n\r\nThe model and the export agree.", Title,
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                SchemaDriftForm.ShowFindings(context.Repository, scopeName, exportName, modelled.Count, findings);
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

        private static string AskForExport()
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Select an ENOVIA schema export";
                dialog.Filter = "ENOVIA export (*.xml)|*.xml|All files (*.*)|*.*";
                dialog.CheckFileExists = true;

                return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null;
            }
        }

        // The package that is selected, or the one holding the selected element. Nothing
        // useful selected means the whole model, which is what the menu item says it
        // compares.
        private static Package ScopeOf(Repository repository, out string scopeName)
        {
            try
            {
                ObjectType type = repository.GetContextItem(out object item);

                if (type == ObjectType.otPackage)
                {
                    var package = (Package) item;
                    scopeName = "package " + package.Name;
                    return package;
                }

                if (type == ObjectType.otElement)
                {
                    var element = (Element) item;
                    Package package = repository.GetPackageByID(element.PackageID);
                    if (package != null)
                    {
                        scopeName = "package " + package.Name;
                        return package;
                    }
                }
            }
            catch
            {
                // Nothing selected, or a selection this cannot make sense of. The whole
                // model is the honest fallback.
            }

            scopeName = "the whole model";
            return null;
        }

        // A record in the output tab that outlives the window, the same way the model
        // check keeps one.
        private static void Report(Context context, string exportName, EnoviaExport export,
            List<SchemaDriftFinding> findings)
        {
            try
            {
                LogTabService log = TabUtility.Create(context).LogTab;

                // Worth saying out loud: an element the reader does not know about is a
                // part of the export that took no part in the comparison, so a clean
                // result is only as complete as this list is empty.
                foreach (KeyValuePair<string, int> skipped in export.Skipped)
                {
                    log.Log("[" + Title + "] " + exportName + " contains " + skipped.Value + " '" +
                            skipped.Key + "' element(s) that were not read and so not compared.", 0);
                }

                foreach (SchemaDriftFinding finding in findings)
                    log.Log("[" + Title + "] " + finding, 0);
            }
            catch
            {
                // Best-effort only; the window already shows the findings.
            }
        }
    }
}
