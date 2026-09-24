using Dastan.org.ed.ea.util;

namespace Dastan.org.ed.ea.services.modifications.scripts
{
    public class BusScriptStrategy : IModificationScriptStrategy
    {
        public bool CanHandle(ModificationRow row) => row.IsBusScript;

        public string Generate(ModificationRow row)
        {
            return "modify bus " + ScriptEscapeUtility.Quote(row.Stereotype) + " " + ScriptEscapeUtility.Quote(row.Name) + " '-' " + ScriptEscapeUtility.Quote(row.Property) + " " + ScriptEscapeUtility.Quote(row.NewValue) + ";";        }
    }
}