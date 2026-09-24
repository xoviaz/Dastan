using Dastan.org.ed.ea.util;

namespace Dastan.org.ed.ea.services.modifications.scripts
{
    // An attribute joining or leaving a type is a change to the type, not to the
    // attribute: MQL says "modify type X add attribute Y", and the attribute itself is
    // untouched. So the row carries the type's name and stereotype, and the attribute's
    // name sits in the value that changed -- new when it was added, old when it was taken
    // away, the same shape the connection strategies use.
    public class AttributeMembershipScriptStrategy : IModificationScriptStrategy
    {
        public bool CanHandle(ModificationRow row)
        {
            return row.Property == ModificationProperties.AttributeAdded ||
                   row.Property == ModificationProperties.AttributeRemoved;
        }

        public string Generate(ModificationRow row)
        {
            bool added = row.Property == ModificationProperties.AttributeAdded;
            string attribute = added ? row.NewValue : row.OldValue;
            if (string.IsNullOrEmpty(attribute)) return "";

            return "modify " + ScriptEscapeUtility.Quote(row.Stereotype) + " " +
                   ScriptEscapeUtility.Quote(row.Name) +
                   (added ? " add attribute " : " remove attribute ") +
                   ScriptEscapeUtility.Quote(attribute) + ";";
        }
    }
}