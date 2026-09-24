using System;
using Dastan.org.ed.ea.constants;
using Dastan.org.ed.ea.entity;
using Dastan.org.ed.ea.settings;
using EA;

namespace Dastan.org.ed.ea.services.elements.impl.schema
{
    public class RoleScriptGenerator : ScriptGenerator<Element>
    {        
        private const string Template = "add {0} {1} description {2};";
        private const string RegisterTemplate = "add property installer on role \"{0}\" value ENOVIAEngineering;\nadd property application on role \"{0}\" value Framework;\nadd property \"installed date\" on role \"{0}\" value \"{1}\";\nadd property \"original name\" on role \"{0}\" value \"{0}\";\nadd property version on role \"{0}\" value R419;\nadd property role_{0} on program eServiceSchemaVariableMapping.tcl to Role \"{0}\";";

        public RoleScriptGenerator(Context context) : base(context)
        {
        }

        public override void Generate(Element obj)
        {
            if (obj.Stereotype != Stereotypes.Role) return;
            
            if (PassedElements.Contains(obj.ElementID)) return;
            PassedElements.Add(obj.ElementID);
            
            string stringResource = string.Format(StringResourceTemplate, AppSettings.StringResourcePrefix,
                (obj.Stereotype ?? ""), obj.Name, obj.Alias);

            Script.Add(string.Format(Template, Quote(obj.Stereotype), Quote(obj.Name), Quote(obj.Notes)));
            StringResource.Add(stringResource);
            Register.Add(string.Format(RegisterTemplate,obj.Name, DateTime.Now.ToString("M/d/yyyy h:mm:ss tt")));
            
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
        }
    }
}