using System;
using System.Text;
using Dastan.org.ed.ea.constants;
using Dastan.org.ed.ea.entity;
using Dastan.org.ed.ea.settings;
using Dastan.org.ed.ea.util;
using EA;
using Attribute = EA.Attribute;

namespace Dastan.org.ed.ea.services.elements.impl.schema
{
    public class RelationshipScriptGenerator : ScriptGenerator<Connector>
    {
        private readonly AttributeScriptGenerator _attributeScriptGenerator;
        private const string Template = "add {0} {1} {2} {3} description {4} from type {5} cardinality {6} clone {7} revision {8} {9} {10} to type {11} cardinality {12} clone {13} revision {14} {15} {16} {17} {18};";
        private const string RegisterTemplate = "add property installer on relationship \"{0}\" value ENOVIAEngineering;\nadd property application on relationship \"{0}\" value Framework;\nadd property \"installed date\" on relationship \"{0}\" value \"{1}\";\nadd property \"original name\" on relationship \"{0}\" value \"{0}\";\nadd property version on relationship \"{0}\" value R419;\nadd property relationship_{0} on program eServiceSchemaVariableMapping.tcl to Relationship \"{0}\";";

        public RelationshipScriptGenerator(Context context, AttributeScriptGenerator attributeScriptGenerator) :
            base(context)
        {
            _attributeScriptGenerator = attributeScriptGenerator;
        }

        private Connector GetGeneralizationParentConnector(Element element, Repository repository)
        {
            
            foreach (Connector conn in element.Connectors)
            {
                if (conn.Type == ConnectorTypes.Generalization && conn.ClientID == element.ElementID)
                {
                    Element conElem = repository.GetElementByID(conn.SupplierID);
                    return repository.GetConnectorByID(conElem.AssociationClassConnectorID);
                }
            }

            return null;
        }

        public override void Generate(Connector obj)
        {
            if (PassedRelationships.Contains(obj.ConnectorID)) return;
            PassedRelationships.Add(obj.ConnectorID);

            Element assocClass = obj.AssociationClass;
            if (assocClass == null) return;
            if (assocClass.Stereotype != Stereotypes.Relationship) return;

            Connector parent = GetGeneralizationParentConnector(assocClass, Context.Repository);
            if (parent != null)
            {
                Generate(parent);
            }
            
            string derivedStatement = "";
            if (parent != null)
            {
                derivedStatement = "derived " + Quote(parent.AssociationClass.Name);
            }
            
            string abstractStatement =
                (assocClass.Abstract == "1") ? "abstract true" : "abstract false";

            string fromClone = TagUtility.GetSafeTagValue(assocClass.TaggedValues, "From Clone", "");
            string fromRevision = TagUtility.GetSafeTagValue(assocClass.TaggedValues, "From Revision", "");
            string fromPropagateConnection = TagUtility.GetSafeTagValue(assocClass.TaggedValues, "From Propagate connection", "False") == "True" ? "propagateconnection" : "notpropagateconnection";
            string fromPropagateModify = TagUtility.GetSafeTagValue(assocClass.TaggedValues, "From Propagate modify", "False") == "True" ? "propagatemodify" : "notpropagatemodify";

            string fromCardinalityEa = obj.ClientEnd.Cardinality;
            if (string.IsNullOrEmpty(fromCardinalityEa))
                throw new Exception("No 'from cardinality' found for " + assocClass.Name);
            string fromCardinality = (fromCardinalityEa == "1") ? "one" : "many";

            string toClone = TagUtility.GetSafeTagValue(assocClass.TaggedValues, "To Clone", "");
            string toRevision = TagUtility.GetSafeTagValue(assocClass.TaggedValues, "To Revision", "");
            string toPropagateConnection = TagUtility.GetSafeTagValue(assocClass.TaggedValues, "To Propagate connection", "False") == "True" ? "propagateconnection" : "notpropagateconnection";
            string toPropagateModify = TagUtility.GetSafeTagValue(assocClass.TaggedValues, "To Propagate modify", "False") == "True" ? "propagatemodify" : "notpropagatemodify";

            string toCardinalityEa = obj.SupplierEnd.Cardinality;
            if (string.IsNullOrEmpty(toCardinalityEa))
                throw new Exception("No 'to cardinality' found for " + assocClass.Name);
            string toCardinality = (toCardinalityEa == "1") ? "one" : "many";

            string preventDuplicate =
                TagUtility.GetSafeTagValue(assocClass.TaggedValues, "Prevent Duplicate", "True") == "True" ? "preventduplicates" : "notpreventduplicates";

            string fromType = Context.Repository.GetElementByID(obj.ClientID).Name;
            string toType = Context.Repository.GetElementByID(obj.SupplierID).Name;

            string stringResource = $"{AppSettings.StringResourcePrefix}.{assocClass.Stereotype}.{assocClass.Name} = {assocClass.Alias}";

            StringBuilder attributeStatementBuilder = new StringBuilder();
            foreach (Attribute attrAssoc in assocClass.Attributes)
            {
                _attributeScriptGenerator.Generate(attrAssoc);
                attributeStatementBuilder.Append("attribute " + Quote(attrAssoc.Name) + "");
            }
            
            string attributeStatement = attributeStatementBuilder.ToString();
            
            Script.Add(string.Format(Template, Quote(assocClass.Stereotype), Quote(assocClass.Name), derivedStatement, abstractStatement, Quote(assocClass.Notes), Quote(fromType), fromCardinality, Quote(fromClone), Quote(fromRevision), fromPropagateConnection, 
                fromPropagateModify, Quote(toType), toCardinality, Quote(toClone), Quote(toRevision), toPropagateConnection, toPropagateModify, preventDuplicate, attributeStatement));
            StringResource.Add(stringResource);
            Register.Add(string.Format(RegisterTemplate, assocClass.Name, DateTime.Now.ToString("M/d/yyyy h:mm:ss tt")));
        }
    }
}