using Dastan.org.ed.ea.util;

namespace Dastan.org.ed.ea.services.modifications.scripts
{
    public class SchemaScriptStrategy : IModificationScriptStrategy
    {
        public bool CanHandle(ModificationRow row) => true;

        public string Generate(ModificationRow row)
        {
            return "modify " + ScriptEscapeUtility.Quote(row.Stereotype) + " " + ScriptEscapeUtility.Quote(row.Name) + " " + ScriptEscapeUtility.Quote(row.Property) + " " + ScriptEscapeUtility.Quote(row.NewValue) + ";";
            
        }
    }
}