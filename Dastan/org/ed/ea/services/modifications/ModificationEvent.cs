using EA;

namespace Dastan.org.ed.ea.services.modifications
{
    public class ModificationEvent
    {
        public Element Element { get; }
        public string Date { get; }
        public string ElementName { get; }
        public string Type { get; }
        public string Stereotype { get; }
        public string Property { get; }
        public string OldValue { get; }
        public string NewValue { get; }

        public ModificationEvent(Element element, string date, string elementName, string type, string stereotype, string property, string oldValue, string newValue)
        {
            Element = element;
            Date = date;
            ElementName = elementName;
            Type = type;
            Stereotype = stereotype;
            Property = property;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}