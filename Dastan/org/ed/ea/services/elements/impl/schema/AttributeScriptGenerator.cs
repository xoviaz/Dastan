using System;
using System.Collections.Generic;
using Dastan.org.ed.ea.constants;
using Dastan.org.ed.ea.entity;
using Dastan.org.ed.ea.services.mql;
using Dastan.org.ed.ea.settings;
using Attribute = EA.Attribute;

namespace Dastan.org.ed.ea.services.elements.impl.schema
{
    public class AttributeScriptGenerator : ScriptGenerator<Attribute>
    {
        private const string RegisterTemplate = "add property installer on attribute \"{0}\" value ENOVIAEngineering;\nadd property application on attribute \"{0}\" value Framework;\nadd property \"installed date\" on attribute \"{0}\" value \"{1}\";\nadd property \"original name\" on attribute \"{0}\" value \"{0}\";\nadd property version on attribute \"{0}\" value R419;\nadd property attribute_{0} on program eServiceSchemaVariableMapping.tcl to Attribute \"{0}\";";
        private readonly List<string> PassAttributes = new  List<string>();
        public AttributeScriptGenerator(Context context) : base(context) {}

        public override void Generate(Attribute obj)
        {
            if (obj.Stereotype != Stereotypes.Attribute) return;

            if (string.IsNullOrEmpty(obj.Name) || PassAttributes.Contains(obj.Name)) return;
            PassAttributes.Add(obj.Name);

            string stringResource = string.Format(StringResourceTemplate, AppSettings.StringResourcePrefix, (obj.Stereotype ?? ""), obj.Name, obj.Alias);

            // The statement itself lives in AttributeStatements, because the modification
            // tracker has to write the identical one when a new attribute is attached to
            // a type.
            Script.Add(AttributeStatements.Add(obj));
            StringResource.Add(stringResource);
            Register.Add(string.Format(RegisterTemplate, obj.Name, DateTime.Now.ToString("M/d/yyyy h:mm:ss tt")));
        }
    }
}