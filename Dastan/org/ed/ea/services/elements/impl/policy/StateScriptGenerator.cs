using System;
using System.Text;
using System.Xml;
using Dastan.org.ed.ea.constants;
using Dastan.org.ed.ea.entity;
using Dastan.org.ed.ea.settings;
using Dastan.org.ed.ea.util;
using EA;

namespace Dastan.org.ed.ea.services.elements.impl.policy
{
    public class StateScriptGenerator : ScriptGenerator<Element>
    {

        private const string RegisterTemplate = "add property state_{0} on policy {1} value {0};";
        
        public StateScriptGenerator(Context context) : base(context)
        {
        }

        public override void Generate(Element obj)
        {
            if (PassedElements.Contains(obj.ElementID)) return;
            PassedElements.Add(obj.ElementID);
            Element policy = Context.Repository.GetContextObject() as Element;

            string stereotype = (obj.Stereotype ?? "").Replace("Start", "").Replace("Final", "").Trim();
            StringBuilder stateBuilder = new StringBuilder();
            stateBuilder.Append($"{stereotype} {obj.Name}".Trim() + "\n");
            stateBuilder.Append($"minorrevision {TagUtility.GetSafeTagValue(obj.TaggedValues, "minorrevisionable", "false")}\n");
            stateBuilder.Append($"majorrevision {TagUtility.GetSafeTagValue(obj.TaggedValues, "majorrevisionable", "false")}\n");
            stateBuilder.Append($"version {TagUtility.GetSafeTagValue(obj.TaggedValues, "versionable", "false")}\n");
            stateBuilder.Append($"promote {TagUtility.GetSafeTagValue(obj.TaggedValues, "promote", "true")}\n");
            string displayName = TagUtility.GetSafeTagValue(obj.TaggedValues, "Display Name", "");
            StringResource.Add(string.Format(StringResourceTemplate, AppSettings.StringResourcePrefix, "State",
                policy.Name + "." + obj.Name, displayName));

            if (obj.Type == "FinalState")
            {
                return;
            }

            foreach (Connector conn in obj.Connectors)
            {
                if (conn.ClientID != obj.ElementID) continue;
                if (PassedElements.Contains(conn.ConnectorID)) continue;
                PassedElements.Add(conn.ConnectorID);

                // Generate Script For Triggers
                if (conn.Type == ConnectorTypes.Transition || conn.Type == ConnectorTypes.StateFlow)
                {
                    
                    string sql = $@"
        SELECT c.Connector_ID, 
               CASE 
                   WHEN startObj.Object_Type = 'ProxyConnector' THEN c.End_Object_ID 
                   ELSE c.Start_Object_ID 
               END AS RealElementID
        FROM t_connector c
        INNER JOIN t_object startObj ON startObj.Object_ID = c.Start_Object_ID
        INNER JOIN t_object endObj   ON endObj.Object_ID   = c.End_Object_ID
        WHERE (startObj.Object_Type = 'ProxyConnector' AND startObj.Classifier_guid = '{conn.ConnectorGUID}')
           OR (endObj.Object_Type   = 'ProxyConnector' AND endObj.Classifier_guid   = '{conn.ConnectorGUID}')
    ";

                    string xml = Context.Repository.SQLQuery(sql);

                    var doc = new XmlDocument();
                    doc.LoadXml(xml);

                    foreach (XmlNode row in doc.SelectNodes("//Row"))
                    {
                        string connectorIdStr = row.SelectSingleNode("Connector_ID")?.InnerText;
                        string elementIdStr = row.SelectSingleNode("RealElementID")?.InnerText;

                        if (!int.TryParse(connectorIdStr, out int connectorId) ||
                            !int.TryParse(elementIdStr, out int elementId))
                            continue;

                        Connector associatedConnector = Context.Repository.GetConnectorByID(connectorId);
                        Element associatedElement = Context.Repository.GetElementByID(elementId);

                        if (associatedConnector == null || associatedElement == null)
                            continue;

                        if (associatedElement.Type == "ProxyConnector")
                            continue;

                        string triggerType = TagUtility.GetSafeTagValue(associatedConnector.TaggedValues, "Type", "");

                        stateBuilder.Append(
                            $"{associatedConnector.Stereotype} {conn.Stereotype} {triggerType} emxTriggerManager input {associatedElement.Name}\n");
                    }
                }


                if (conn.Type == ConnectorTypes.Usage)
                {
                    Element targetElement = Context.Repository.GetElementByID(conn.SupplierID);
                    if (targetElement != null)
                    {
                        string accesses = TagUtility.GetSafeTagValue(targetElement.TaggedValues, "Accesses", "");
                        string filterVal = TagUtility.GetSafeTagValue(targetElement.TaggedValues, "Filter", "");
                        string filter = !string.IsNullOrEmpty(filterVal) ? $"Filter {Quote(filterVal)}" : "";

                        if (string.Equals(targetElement.Name, "owner", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(targetElement.Name, "public", StringComparison.OrdinalIgnoreCase))
                            stateBuilder.Append($"{targetElement.Name} {accesses} {filter}".Trim() + "\n");
                        else
                            stateBuilder.Append($"{targetElement.Stereotype} {targetElement.Name} {accesses} {filter}"
                                .Trim() + "\n");
                    }
                }
            }

            if (stateBuilder[stateBuilder.Length - 1] == '\n') stateBuilder.Length--;
            
            Script.Add(stateBuilder.ToString());
            Register.Add(string.Format(RegisterTemplate, obj.Name, policy.Name));
            
            foreach (Connector conn in obj.Connectors)
            {
                if ((conn.Type == ConnectorTypes.Transition || conn.Type == ConnectorTypes.StateFlow))
                {
                    Element targetElement = Context.Repository.GetElementByID(conn.SupplierID);
                    if (targetElement != null)
                    {
                        Generate(targetElement);
                    }
                }
            }
        }
    }
}