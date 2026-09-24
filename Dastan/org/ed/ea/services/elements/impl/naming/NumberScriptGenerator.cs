using Dastan.org.ed.ea.constants;
using Dastan.org.ed.ea.entity;
using Dastan.org.ed.ea.util;
using EA;

namespace Dastan.org.ed.ea.services.elements.impl.naming
{
    public class NumberScriptGenerator : ScriptGenerator<Element>
    {
        
        private const string Template = "add bus 'eService Number Generator' {0} {1} vault 'eService Administration' policy 'eService Object Generator' 'eService Next Number' {2};";
        
        public NumberScriptGenerator(Context context) : base(context)
        {
            
        }

        public override void Generate(Element obj)
        {
            
            if (obj.Stereotype != Stereotypes.ObjectNumberGenerator) return;
            
            string nextNumber = TagUtility.GetSafeTagValue(obj.TaggedValues, "eService Next Number", "");
            Script.Add(string.Format(Template, Quote(obj.Name), Quote(obj.Alias), Quote(nextNumber)));
        }
    }
}