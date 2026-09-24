using Dastan.org.ed.ea.entity;
using Dastan.org.ed.ea.util;
using EA;

namespace Dastan.org.ed.ea.services.elements.impl.business
{
    public class TextHelperScriptGenerator : ScriptGenerator<Element>
    {
        private readonly string _busType;
        private readonly string _tagName;
        
        public TextHelperScriptGenerator(Context context, string busType, string tagName) : base(context)
        {
            _busType = busType;
            _tagName = tagName;
        }

        public override void Generate(Element obj)
        {
            string text = Quote(TagUtility.GetSafeTagValue(obj.TaggedValues, _tagName, ""));

            // The connect statement has to name the exact object the add statement
            // created, so both build the helper name through the same escaping.
            string helperName = Quote(obj.Name + "-" + obj.Stereotype);

            Script.Add($"add bus '{_busType}' {helperName} '-' policy CW_Element vault 'eService Production' CW_EN {text} CW_FA {text};");
            Script.Add($"connect bus {Quote(obj.Stereotype)} {Quote(obj.Name)} '-' relationship 'CW_UIComponentRelatedTextHelper' to '{_busType}' {helperName} '-';");
        }
    }
}