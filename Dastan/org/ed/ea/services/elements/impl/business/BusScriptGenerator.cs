using System;
using System.Collections.Generic;
using System.Text;
using Dastan.org.ed.ea.constants;
using Dastan.org.ed.ea.entity;
using Dastan.org.ed.ea.util;
using EA;

namespace Dastan.org.ed.ea.services.elements.impl.business
{
    public class BusScriptGenerator : ScriptGenerator<Element>
    {
        private readonly ConnectionScriptGenerator _connectionScriptGenerator;
        private readonly LabelScriptGenerator _labelScriptGenerator;
        private readonly TooltipScriptGenerator _tooltipScriptGenerator;
        private const string Template = "add bus {0} {1} - policy CW_Element vault \"eService Production\" {2};";
        private readonly List<string> _excludedLabelTooltipTypes = new List<string>() { "CW_Table", "CW_Form" };
        
        public BusScriptGenerator(Context context, ConnectionScriptGenerator connectionScriptGenerator, LabelScriptGenerator labelScriptGenerator, TooltipScriptGenerator tooltipScriptGenerator) : base(context)
        {
            _connectionScriptGenerator = connectionScriptGenerator;
            _labelScriptGenerator = labelScriptGenerator;
            _tooltipScriptGenerator = tooltipScriptGenerator;
        }

        public override void Generate(Element obj)
        {
            if (obj == null) return;
            string fq = obj?.FQStereotype ?? "";
            int sep = fq.IndexOf("::", StringComparison.Ordinal);
            if (sep <= 0) return;

            string profile = fq.Substring(0, sep);
            if (profile != Profiles.Widget) return;
            
            if (PassedElements.Contains(obj.ElementID)) return;
            PassedElements.Add(obj.ElementID);
                
            StringBuilder attributeStatementBuilder  = new StringBuilder();
            foreach (TaggedValue tag in obj.TaggedValues)
            {
                if (!string.IsNullOrEmpty(tag.Name) && !string.IsNullOrEmpty(tag.Value) && tag.Name != "Label" && tag.Name != "Tooltip")
                {
                    attributeStatementBuilder.Append($"{Quote(tag.Name)} {Quote(tag.Value)} ");
                }
            }
            string attributeStatement = attributeStatementBuilder.ToString();

            Script.Add(string.Format(Template, Quote(obj.Stereotype), Quote(obj.Name), attributeStatement));

            if (TagUtility.HasTaggedValue(obj, "Label") && !_excludedLabelTooltipTypes.Contains(obj.Stereotype))
            {
                _labelScriptGenerator.Generate(obj);
                _tooltipScriptGenerator.Generate(obj);
            }

            foreach (Connector conn in obj.Connectors)
            {
                if (!(conn.ClientID == obj.ElementID &&
                    (conn.Type == ConnectorTypes.Aggregation || conn.Type == ConnectorTypes.Composition))) continue;
                Element child = Context.Repository.GetElementByID(conn.SupplierID);
                if (child != null) Generate(child);
                _connectionScriptGenerator.Generate(conn);
            }
        }
    }
}