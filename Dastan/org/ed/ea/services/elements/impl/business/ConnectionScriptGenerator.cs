using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Dastan.org.ed.ea.constants;
using Dastan.org.ed.ea.entity;
using EA;

namespace Dastan.org.ed.ea.services.elements.impl.business
{
    public class ConnectionScriptGenerator : ScriptGenerator<Connector>
    {
        private const string Template = "connect bus {0} {1} - relationship {2} to {3} {4} - {5};";

        public ConnectionScriptGenerator(Context context) : base(context)
        {
        }

        public override void Generate(Connector obj)
        {
            if (obj == null) return;
            string fq = obj?.FQStereotype ?? "";
            int sep = fq.IndexOf("::", StringComparison.Ordinal);
            if (sep <= 0) return;

            string profile = fq.Substring(0, sep);
            if (profile != Profiles.Widget) return;

            if (!(obj.Type == ConnectorTypes.Aggregation || obj.Type == ConnectorTypes.Composition)) return;
            Element child = Context.Repository.GetElementByID(obj.SupplierID);
            Element parent = Context.Repository.GetElementByID(obj.ClientID);

            string childStereo = child.Stereotype ?? "";
            if (string.IsNullOrEmpty(childStereo))
            {
                MessageBox.Show(
                    $"No Stereotype has set for relationship between {parent.Stereotype} {parent.Name} and {child.Stereotype} {child.Name}");
                throw new Exception();
            }

            string relationship = obj.Stereotype;

            if (PassedElements.Contains(obj.ConnectorID)) return;
            PassedElements.Add(obj.ConnectorID);

            IEnumerable<string> rangeStatement = from ConnectorTag tag in obj.TaggedValues
                where !string.IsNullOrEmpty(tag.Name) && !string.IsNullOrEmpty(tag.Value)
                select $"{Quote(tag.Name)} {Quote(tag.Value)}";

            Script.Add(string.Format(Template, Quote(parent.Stereotype), Quote(parent.Name), Quote(relationship), Quote(child.Stereotype),
                Quote(child.Name), string.Join(" ", rangeStatement)));
        }
    }
}