using System;
using System.Collections.Generic;
using System.Text;
using Dastan.org.ed.ea.constants;
using Dastan.org.ed.ea.entity;
using Dastan.org.ed.ea.settings;
using Dastan.org.ed.ea.util;
using EA;
using Attribute = EA.Attribute;

namespace Dastan.org.ed.ea.services.elements.impl.schema
{
    public class TypeScriptGenerator : ScriptGenerator<Element>
    {
        private readonly RelationshipScriptGenerator _relationshipScriptGenerator;
        private readonly AttributeScriptGenerator _attributeScriptGenerator;
        private const string RegisterTemplate = "add property installer on type \"{0}\" value ENOVIAEngineering;\nadd property application on type \"{0}\" value Framework;\nadd property \"installed date\" on type \"{0}\" value \"{1}\";\nadd property \"original name\" on type \"{0}\" value \"{0}\";\nadd property version on type \"{0}\" value R419;\nadd property type_{0} on program eServiceSchemaVariableMapping.tcl to Type \"{0}\";";
        private const string Template = "add {0} {1} {2} description {3} {4} {5} {6};";

        private readonly HashSet<string> _excludeElementNames = new HashSet<string>
        {
            "Document",
            "*"
        };

        public TypeScriptGenerator(Context context, RelationshipScriptGenerator relationshipScriptGenerator,
            AttributeScriptGenerator attributeScriptGenerator) : base(context)
        {
            _relationshipScriptGenerator = relationshipScriptGenerator;
            _attributeScriptGenerator = attributeScriptGenerator;
        }

        private Element GetGeneralizationParent(Element element, Repository repository)
        {
            foreach (Connector conn in element.Connectors)
            {
                if (conn.Type == ConnectorTypes.Generalization && conn.ClientID == element.ElementID)
                {
                    return repository.GetElementByID(conn.SupplierID);
                }
            }

            return null;
        }


        public override void Generate(Element obj)
        {   
            if (obj.Stereotype != Stereotypes.Type) return;
            
            if (_excludeElementNames.Contains(obj.Name))
                return;

            if (PassedElements.Contains(obj.ElementID)) return;
            PassedElements.Add(obj.ElementID);

            string abstractStatement = (obj.Abstract == "1") ? "abstract true" : "abstract false";

            string stringResource = string.Format(StringResourceTemplate, AppSettings.StringResourcePrefix,
                (obj.Stereotype ?? ""), obj.Name, obj.Alias);

            Element parent = GetGeneralizationParent(obj, Context.Repository);
            string parentStatement = parent != null ? "derived" + Quote(parent.Name) + " " : "";

            StringBuilder attributeStatementBuilder = new StringBuilder();
            foreach (Attribute attr in obj.Attributes)
            {
                _attributeScriptGenerator.Generate(attr);
                attributeStatementBuilder.Append("attribute " + Quote(attr.Name));
            }

            string attributeStatement = attributeStatementBuilder.ToString();

            StringBuilder triggerStatementBuilder = new StringBuilder();
            foreach (Connector connGen in obj.Connectors)
            {
                if (connGen.Type != ConnectorTypes.Usage) continue;

                int targetIdGen = connGen.SupplierID;

                if (PassedElements.Contains(targetIdGen)) continue;
                PassedElements.Add(targetIdGen);

                Element trigger = Context.Repository.GetElementByID(targetIdGen);

                string eventVal = TagUtility.GetSafeTagValue(connGen.TaggedValues, "Event", "");
                string typeVal = TagUtility.GetSafeTagValue(connGen.TaggedValues, "Type", "");

                triggerStatementBuilder.Append(
                    $"{connGen.Stereotype} {eventVal} {typeVal} emxTriggerManager input {trigger.Name} ");
            }

            string triggerStatement = triggerStatementBuilder.ToString();

            Script.Add(string.Format(Template, Quote(obj.Stereotype), Quote(obj.Name), abstractStatement, Quote(obj.Notes), parentStatement, attributeStatement, triggerStatement));
            StringResource.Add(stringResource);
            Register.Add(string.Format(RegisterTemplate, obj.Name, DateTime.Now.ToString("M/d/yyyy h:mm:ss tt")));

            foreach (Connector connGen in obj.Connectors)
            {
                if (connGen.Type != ConnectorTypes.Generalization) continue;
                int targetIdGen = (connGen.ClientID == obj.ElementID)
                    ? connGen.SupplierID
                    : connGen.ClientID;

                Element childGen = Context.Repository.GetElementByID(targetIdGen);
                if (childGen == null) continue;

                Generate(childGen);
            }

            foreach (Connector connAssoc in obj.Connectors)
            {
                if (connAssoc.Type == ConnectorTypes.Association)
                {
                    _relationshipScriptGenerator.Generate(connAssoc);
                }
            }
        }
    }
}