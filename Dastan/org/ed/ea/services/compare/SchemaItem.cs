using System;
using System.Collections.Generic;

namespace Dastan.org.ed.ea.services.compare
{
    // One schema object reduced to a kind, a name and a bag of named values.
    //
    // Both sides of a comparison reduce to this: an ENOVIA export on one side, the EA
    // model on the other. That is what lets the comparer know nothing about XML or about
    // EA -- it only ever sees these.
    public class SchemaItem
    {
        public const string TypeKind = "Type";
        public const string AttributeKind = "Attribute";
        public const string RelationshipKind = "Relationship";
        public const string PolicyKind = "Policy";
        public const string RoleKind = "Role";

        public string Kind { get; }
        public string Name { get; }
        public IReadOnlyDictionary<string, string> Values { get; }

        // GUID of the element this came from, so a result can be walked back to the object
        // in the project browser. Empty on the export side, which has no element to show:
        // an export names objects that may not exist in the model at all, and that is the
        // whole reason for comparing.
        public string ElementGuid { get; }

        public SchemaItem(string kind, string name, Dictionary<string, string> values, string elementGuid = "")
        {
            Kind = kind ?? "";
            Name = name ?? "";
            Values = values ?? new Dictionary<string, string>();
            ElementGuid = elementGuid ?? "";
        }

        // Matched case-insensitively on purpose. MQL names are case-sensitive, so a name
        // that differs only in case is a real difference -- but it is a difference
        // between two objects that are plainly the same one, not two unrelated objects
        // where each is missing from the other side. Pairing them first and reporting the
        // case as a difference says what actually happened.
        public string Key
        {
            get { return Kind + "\n" + Name.ToLowerInvariant(); }
        }

        public string Value(string field)
        {
            return Values.TryGetValue(field, out string value) ? value ?? "" : "";
        }

        public bool Has(string field)
        {
            return Values.ContainsKey(field);
        }

        public override string ToString()
        {
            return Kind + " " + Name;
        }

        // A list written as one value, so a comparison can treat it like any other field.
        // Order is kept where it means something -- policy states are a lifecycle -- and
        // sorted where it does not, so two sides that merely list the same attributes in
        // a different order do not read as drift.
        public static string List(IEnumerable<string> values, bool ordered)
        {
            var items = new List<string>();
            foreach (string value in values)
            {
                string text = (value ?? "").Trim();
                if (text.Length > 0) items.Add(text);
            }

            if (!ordered) items.Sort(StringComparer.OrdinalIgnoreCase);

            return string.Join(", ", items.ToArray());
        }

        public static string Flag(bool value)
        {
            return value ? "true" : "false";
        }

        // One end's multiplicity, said the one way.
        //
        // Three vocabularies describe the same two facts: MQL writes one and many, an
        // export writes 1 and n, and EA writes 1 and *. Reduced to MQL's words, because
        // those are the ones the generated script uses and the ones a reader recognises.
        //
        // Anything that is not exactly one is many, which is also how the relationship
        // generator reads EA -- so a model saying 0..1 compares as what it would install,
        // not as what it might have meant.
        public static string Cardinality(string value)
        {
            string text = (value ?? "").Trim();

            return text == "1" || string.Equals(text, "one", StringComparison.OrdinalIgnoreCase)
                ? "one"
                : "many";
        }
    }
}
