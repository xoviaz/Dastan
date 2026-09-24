using Dastan.org.ed.ea.services.mql;
using Dastan.org.ed.ea.util;

namespace Dastan.org.ed.ea.services.modifications.scripts
{
    public class TaggedValueScriptStrategy : IModificationScriptStrategy
    {
        public bool CanHandle(ModificationRow row)
        {
            return MqlTagTerms.Find(row.Stereotype, row.Property) != null;
        }

        public string Generate(ModificationRow row)
        {
            IMqlTagTerm term = MqlTagTerms.Find(row.Stereotype, row.Property);
            if (term == null) return "";

            string clause = term.ForModify(row.NewValue);
            if (clause.Length == 0) return "";
            
            return "modify " + ScriptEscapeUtility.Quote(row.Stereotype) + " " + ScriptEscapeUtility.Quote(row.Name) + " " + clause + ";";
        }
    }
}