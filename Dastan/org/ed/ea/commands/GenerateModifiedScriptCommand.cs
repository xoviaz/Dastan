using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Dastan.org.ed.ea.constants;
using System.IO;
using Dastan.org.ed.ea.entity;
using Dastan.org.ed.ea.services.modifications;
using Dastan.org.ed.ea.services.modifications.scripts;
using Dastan.org.ed.ea.services.mql;
using Dastan.org.ed.ea.settings;
using Dastan.org.ed.ea.util;
using EA;
using File = System.IO.File;

namespace Dastan.org.ed.ea.commands
{
    public class GenerateModifiedScriptCommand : ICommand
    {
        private readonly List<string> _busProfiles = new List<string>() { Profiles.Widget };
        private readonly List<string> _busStereotypes = new List<string>() { "eService Trigger Program Parameters" };

        // Order matters: SchemaScriptStrategy handles anything, so it stays last.
        private readonly List<IModificationScriptStrategy> _strategies = new List<IModificationScriptStrategy>()
        {
            new ConnectionScriptStrategy(),
            new DisconnectionScriptStrategy(),
            new AttributeMembershipScriptStrategy(),
            // Before the bus and catch-all strategies: a tagged value MQL has a word for
            // must not be written out as its own name and value.
            new TaggedValueScriptStrategy(),
            new TooltipScriptStrategy(),
            new LabelScriptStrategy(),
            new BusScriptStrategy(),
            new SchemaScriptStrategy()
        };

        // Tracked so the log tells the whole story, but there is nothing in the schema MQL
        // to generate from them.
        private static readonly HashSet<string> UnscriptableProperties = new HashSet<string>
        {
            ModificationProperties.OperationAdded,
            ModificationProperties.OperationRemoved
        };

        public void Execute(Context context)
        {
            try
            {
                List<ModificationRow> rows = BuildRows(context.ListView.SelectedItems);

                // Definitions first, and in their own pass, for two reasons.
                //
                // "modify type X add attribute Y" cannot attach a Y that does not exist
                // yet, so every definition has to be written before any statement that
                // might reach one -- whatever order the rows happened to be selected in.
                //
                // And a definition is read from the attribute as it stands now, so it
                // already carries every later change. Knowing which attributes this script
                // creates is what lets those changes be left out below.
                StringBuilder definitions = new StringBuilder();
                var defined = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (ModificationRow row in rows)
                    AppendAttributeDefinition(definitions, defined, row);

                StringBuilder scripts = new StringBuilder();
                foreach (ModificationRow row in rows)
                {
                    if (IsCoveredByDefinition(row, defined)) continue;

                    string script = _strategies.FirstOrDefault(s => s.CanHandle(row))?.Generate(row);
                    if (string.IsNullOrEmpty(script)) continue;
                    scripts.Append(script);
                    scripts.Append("\n");
                }

                ScriptExportUtility.OutputPaths? paths = ScriptExportUtility.ResolveOutputPaths();
                if (paths == null) return;

                string body = (definitions.ToString() + scripts.ToString()).TrimEnd('\n');

                string filePath = paths.Value.FilePath(OutputName(context.ListView.SelectedItems));
                if (!ScriptExportUtility.WriteScript(context, filePath, body)) return;

                ScriptExportUtility.NotifyExportCompleted(filePath);
            }
            catch (Exception e)
            {
                MessageBox.Show("Error: " + e.Message, "Generate Script");
            }
        }

        public bool HasAccess(Repository repository)
        {
            return true;
        }

        // Names the file after the changeset when every selected row belongs to the same
        // one, so an export says which set it came from. A mixed or unfiled selection has
        // no single answer, so it keeps the plain name.
        private static string OutputName(ListView.SelectedListViewItemCollection items)
        {
            string shared = null;

            foreach (ListViewItem item in items)
            {
                string changeset = ModificationLogsManager.ChangesetIndex < item.SubItems.Count
                    ? item.SubItems[ModificationLogsManager.ChangesetIndex].Text.Trim()
                    : "";

                if (changeset.Length == 0) return "Modified_Output";
                if (shared == null) shared = changeset;
                else if (!string.Equals(shared, changeset, StringComparison.OrdinalIgnoreCase)) return "Modified_Output";
            }

            return shared == null ? "Modified_Output" : "Modified_Output_" + ForFileName(shared);
        }

        private static string ForFileName(string value)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(value.Length);

            foreach (char c in value)
                sb.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);

            return sb.ToString();
        }

        private List<ModificationRow> BuildRows(ListView.SelectedListViewItemCollection items)
        {
            var rows = new List<ModificationRow>();

            foreach (ListViewItem item in items)
            {
                // A row whose element no longer exists carries only its first subitem, so
                // the tag has to be checked before anything past it is read.
                Element el = item.Tag as Element;
                if (el == null) continue;

                ModificationRow row = BuildRow(el, item.SubItems[0].Text, item.SubItems[2].Text,
                    item.SubItems[3].Text, item.SubItems[4].Text, item.SubItems[5].Text);

                if (row != null) rows.Add(row);
            }

            return rows;
        }

        // An attribute attached to a type is an attribute that was just created, so the
        // script has to create it -- with the same definition a full schema export would
        // have written, read off the attribute itself rather than rebuilt here.
        //
        // Once per name: attaching one attribute to three types in the same changeset is
        // three attach statements and one definition, which is also what keeps the
        // linter's duplicate-add rule quiet.
        private static void AppendAttributeDefinition(StringBuilder definitions, HashSet<string> defined,
            ModificationRow row)
        {
            if (row.Property != ModificationProperties.AttributeAdded) return;
            if (string.IsNullOrEmpty(row.NewValue)) return;

            EA.Attribute attribute = FindAttribute(row.Element, row.NewValue);

            // Gone from the model since the change was logged. The attach statement still
            // goes out, and the preview shows there is nothing defining it -- which is
            // also why the name is only recorded as defined once one really was written.
            if (attribute == null) return;
            if (!defined.Add(row.NewValue)) return;

            definitions.Append(AttributeStatements.Add(attribute));
            definitions.Append("\n");
        }

        // A change to the attribute itself -- its type, its description, one of its tags --
        // when the attribute is one this script defines. The definition is built from the
        // attribute as it stands now, so it already carries the final value; restating it
        // would repeat the definition at best, and fail at worst, since MQL will not change
        // an attribute's type once it exists.
        private static bool IsCoveredByDefinition(ModificationRow row, HashSet<string> defined)
        {
            return row.Stereotype == Stereotypes.Attribute && defined.Contains(row.Name);
        }

        private static EA.Attribute FindAttribute(Element element, string name)
        {
            if (element == null) return null;

            // The element was fetched when the row was logged or the panel loaded, so its
            // attributes can predate the one being looked for.
            element.Attributes.Refresh();

            foreach (EA.Attribute attribute in element.Attributes)
            {
                if (string.Equals(attribute.Name, name, StringComparison.Ordinal)) return attribute;
            }

            return null;
        }

        private ModificationRow BuildRow(Element el, string name, string stereotype, string property, string oldValue,
            string newValue)
        {
            bool isBusContext = false;

            if (UnscriptableProperties.Contains(property)) return null;

            // Nothing can be modified in MQL without saying what kind of object it is, and
            // the stereotype is what carries that. An operation, or anything else that was
            // never stereotyped, falls out here.
            if (string.IsNullOrWhiteSpace(stereotype)) return null;

            if (property != ModificationProperties.Connection &&
                property != ModificationProperties.Disconnection)
            {
                string fq = el.FQStereotype ?? "";
                int sep = fq.IndexOf("::", StringComparison.Ordinal);
                if (sep <= 0) return null;

                string profile = fq.Substring(0, sep);

                // An attribute is a schema object wherever it hangs: "modify attribute X
                // description ..." is the same statement whether or not the type carrying
                // it belongs to the widget profile. Only the element itself can be a bus.
                isBusContext = stereotype != Stereotypes.Attribute &&
                               (_busProfiles.Contains(profile) || _busStereotypes.Contains(stereotype));
            }
            
            return new ModificationRow(el, name, property,stereotype, oldValue, newValue, isBusContext);
        }
    }
}