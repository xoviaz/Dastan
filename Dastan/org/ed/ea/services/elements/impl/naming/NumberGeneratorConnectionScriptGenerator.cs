using Dastan.org.ed.ea.entity;
using EA;

namespace Dastan.org.ed.ea.services.elements.impl.naming
{
    public class NumberGeneratorConnectionScriptGenerator : ScriptGenerator<Element>
    {
        private const string Template = "add connection 'eService Number Generator' from 'eService Object Generator' {0} {1} to 'eService Number Generator' {0} {1};";
        
        public NumberGeneratorConnectionScriptGenerator(Context context) : base(context)
        {
        }

        public override void Generate(Element obj)
        {
            Script.Add(string.Format(Template, Quote(obj.Name), Quote(obj.Alias)));
        }
    }
}