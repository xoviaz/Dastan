using Dastan.org.ed.ea.constants;
using Dastan.org.ed.ea.entity;
using Dastan.org.ed.ea.util;
using EA;

namespace Dastan.org.ed.ea.services.elements.impl.naming
{
    public class ObjectScriptGenerator : ScriptGenerator<Element>
    {
        private readonly NumberScriptGenerator _numberScriptGenerator;
        private readonly NumberGeneratorConnectionScriptGenerator _numberGeneratorConnectionScriptGenerator;

        private const string Template = "add bus 'eService Object Generator' {0} {1} vault 'eService Administration' policy 'eService Object Generator' 'eService Safety Vault' {2} 'eService Retry Delay' {3} 'eService Retry Count' {4} 'eService Processing Time Limit' {5} 'eService Name Prefix' {6} 'eService Name Suffix' {7} 'eService Safety Policy' {8};";
        
        public ObjectScriptGenerator(Context context, NumberScriptGenerator numberScriptGenerator, NumberGeneratorConnectionScriptGenerator numberGeneratorConnectionScriptGenerator) : base(context)
        {
            _numberScriptGenerator = numberScriptGenerator;
            _numberGeneratorConnectionScriptGenerator = numberGeneratorConnectionScriptGenerator;
        }

        public override void Generate(Element obj)
        {
            if (obj.Stereotype != Stereotypes.ObjectNumberGenerator) return;
            
            string revision = obj.Alias;
            string name = obj.Name;
            string prefix = TagUtility.GetSafeTagValue(obj.TaggedValues, "eService Name Prefix", "");
            string suffix = TagUtility.GetSafeTagValue(obj.TaggedValues, "eService Name Suffix", "");
            string timeLimit = TagUtility.GetSafeTagValue(obj.TaggedValues, "eService Processing Time Limit", "60");
            string retryCount = TagUtility.GetSafeTagValue(obj.TaggedValues, "eService Retry Count", "5");
            string retryDelay = TagUtility.GetSafeTagValue(obj.TaggedValues, "eService Retry Delay", "1000");
            string policy = TagUtility.GetSafeTagValue(obj.TaggedValues, "eService Safety Policy", "");
            string vault = TagUtility.GetSafeTagValue(obj.TaggedValues, "eService Safety Vault", "vault_eServiceAdministration");

            Script.Add(string.Format(Template, Quote(name), Quote(revision), Quote(vault), Quote(retryDelay), Quote(retryCount), Quote(timeLimit), Quote(prefix), Quote(suffix), Quote(policy)));
            _numberScriptGenerator.Generate(obj);
            _numberGeneratorConnectionScriptGenerator.Generate(obj);

        }
    }
}