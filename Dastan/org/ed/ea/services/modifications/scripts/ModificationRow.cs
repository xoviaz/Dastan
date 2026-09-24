using EA;

namespace Dastan.org.ed.ea.services.modifications.scripts
{
    public class ModificationRow
    {
        public Element Element { get; }
        public string Name  { get; }
        public string Property { get; }
        public string Stereotype { get; }
        public string OldValue { get; }
        public string NewValue { get; }
        public bool IsBusScript { get; }

        public ModificationRow(Element element, string name, string property, string stereotype, string oldValue, string newValue, bool isBusScript)
        {
            Element = element;
            Name = name;
            Property = property;
            Stereotype = stereotype;
            OldValue = oldValue;
            NewValue = newValue;
            IsBusScript = isBusScript;
        }
    }
}