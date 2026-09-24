using Dastan.org.ed.ea.entity;
using Dastan.org.ed.ea.util;
using EA;

namespace Dastan.org.ed.ea.services.elements.impl.schema
{
    public class TriggerScriptGenerator : ScriptGenerator<Element>
    {
        private const string Template = "add bus {0} {1} {2} policy  'eService Trigger Program Policy' vault  'eService Administration' 'eService Program Argument 1' {3} 'eService Program Argument 2' {4} 'eService Program Argument 3' {5} 'eService Program Argument 4' {6} 'eService Program Argument 5' {7} 'eService Program Argument 6' {8} 'eService Program Argument 7' {9} 'eService Program Argument 8' {10} 'eService Program Argument 9' {11} 'eService Program Argument 10' {12} 'eService Program Argument 11' {13} 'eService Program Argument 12' {14} 'eService Program Argument 13' {15} 'eService Program Argument 14' {16} 'eService Program Argument 15' {17};";
        
        public TriggerScriptGenerator(Context context) : base(context)
        {
        }

        public override void Generate(Element obj)
        {
            if (obj == null || obj.Stereotype != "eService Trigger Program Parameters")
            {
                return;
            }

            var revision = obj.Alias;
            var arg01 = TagUtility.GetSafeTagValue(obj.TaggedValues, "eService Program Argument 1", string.Empty);
            var arg02 = TagUtility.GetSafeTagValue(obj.TaggedValues, "eService Program Argument 2", string.Empty);
            var arg03 = TagUtility.GetSafeTagValue(obj.TaggedValues, "eService Program Argument 3", string.Empty);
            var arg04 = TagUtility.GetSafeTagValue(obj.TaggedValues, "eService Program Argument 4", string.Empty);
            var arg05 = TagUtility.GetSafeTagValue(obj.TaggedValues, "eService Program Argument 5", string.Empty);
            var arg06 = TagUtility.GetSafeTagValue(obj.TaggedValues, "eService Program Argument 6", string.Empty);
            var arg07 = TagUtility.GetSafeTagValue(obj.TaggedValues, "eService Program Argument 7", string.Empty);
            var arg08 = TagUtility.GetSafeTagValue(obj.TaggedValues, "eService Program Argument 8", string.Empty);
            var arg09 = TagUtility.GetSafeTagValue(obj.TaggedValues, "eService Program Argument 9", string.Empty);
            var arg10 = TagUtility.GetSafeTagValue(obj.TaggedValues, "eService Program Argument 10", string.Empty);
            var arg11 = TagUtility.GetSafeTagValue(obj.TaggedValues, "eService Program Argument 11", string.Empty);
            var arg12 = TagUtility.GetSafeTagValue(obj.TaggedValues, "eService Program Argument 12", string.Empty);
            var arg13 = TagUtility.GetSafeTagValue(obj.TaggedValues, "eService Program Argument 13", string.Empty);
            var arg14 = TagUtility.GetSafeTagValue(obj.TaggedValues, "eService Program Argument 14", string.Empty);
            var arg15 = TagUtility.GetSafeTagValue(obj.TaggedValues, "eService Program Argument 15", string.Empty);

            Script.Add(string.Format(Template, Quote(obj.Stereotype), Quote(obj.Name), Quote(revision), Quote(arg01), Quote(arg02), Quote(arg03), Quote(arg04), Quote(arg05), Quote(arg06), Quote(arg07), Quote(arg08), Quote(arg09), Quote(arg10), Quote(arg11), Quote(arg12), Quote(arg13), Quote(arg14), Quote(arg15)));
        }
    }
}