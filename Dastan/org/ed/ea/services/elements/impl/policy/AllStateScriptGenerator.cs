using System;
using System.Collections.Generic;
using System.Text;
using Dastan.org.ed.ea.entity;
using Dastan.org.ed.ea.repository;
using Dastan.org.ed.ea.util;
using EA;

namespace Dastan.org.ed.ea.services.elements.impl.policy
{
    public class AllStateScriptGenerator : ScriptGenerator<Element>
    {
        private readonly ElementRepository _elementRepository;
        
        public AllStateScriptGenerator(Context context, ElementRepository elementRepository) : base(context)
        {
            _elementRepository = elementRepository;
        }

        public override void Generate(Element obj)
        {
            StringBuilder allstateStatementBuilder = new StringBuilder();
            allstateStatementBuilder.Append(" allstate \n");
            
            Diagram diagram = obj.Diagrams.GetAt(0) as Diagram;
            
            List<Element> users = _elementRepository.GetStandaloneElementByDiagramIdAndTypeAndStereotype(Context,
                diagram.DiagramID, "Actor", "User");

            foreach (Element user in users)
            {
                // if (!_elementRepository.IsElementStandaloneInCurrentDiagram(Context, diagram)) continue;
                string accesses = TagUtility.GetSafeTagValue(user.TaggedValues, "Accesses", "");
                string filterVal = TagUtility.GetSafeTagValue(user.TaggedValues, "Filter", "");
                string filter = !string.IsNullOrEmpty(filterVal) ? $"Filter {Quote(filterVal)}" : "";
            
                if (string.Equals(user.Name, "owner", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(user.Name, "public", StringComparison.OrdinalIgnoreCase))
                    allstateStatementBuilder.Append($"{user.Name} {accesses} {filter}".Trim() + "\n");
                else 
                    allstateStatementBuilder.Append($"{user.Stereotype} {user.Name} {accesses} {filter}".Trim() + "\n");
            }
            if (allstateStatementBuilder[allstateStatementBuilder.Length - 1] == '\n') allstateStatementBuilder.Length--;
            Script.Add(allstateStatementBuilder.ToString());
        }
    }
}