using Dastan.org.ed.ea.util;

namespace Dastan.org.ed.ea.services.modifications.scripts
{
    public class TextHelperScriptStrategy : IModificationScriptStrategy
    {
        private readonly string _busType;
        private readonly string _property;

        protected TextHelperScriptStrategy(string busType, string property)
        {
            _busType = busType;
            _property = property;
        }
        
        public bool CanHandle(ModificationRow row) => row.IsBusScript && row.Property == _property;

        public string Generate(ModificationRow row)
        {
            string helperName = ScriptEscapeUtility.Quote(row.Name + "-" + row.Stereotype);
            string value = ScriptEscapeUtility.Quote(row.NewValue);

            return "modify bus '" + _busType + "' " + helperName + " '-' 'CW_EN' " + value + " 'CW_FA' " + value + ";";
        }
    }
}