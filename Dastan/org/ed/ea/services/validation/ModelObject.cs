using System.Collections.Generic;

namespace Dastan.org.ed.ea.services.validation
{
    // One thing in the model that a generator would turn into MQL, flattened to just the
    // parts a rule can judge. Rules never touch EA objects, so they stay testable and the
    // knowledge of what each generator reads stays in ModelCollector.
    public class ModelObject
    {
        public const string ElementKind = "Element";
        public const string AttributeKind = "Attribute";

        public string Kind { get; }
        public string Name { get; }
        public string Stereotype { get; }

        // EA's own Type -- Class, StateMachine, Note, Boundary. Tells the elements a
        // generator could handle apart from the diagram furniture around them.
        public string EaType { get; }

        public string Path { get; }
        public string ElementGuid { get; }

        // The text that ends up on the right of '=' in the string-resource file. Null
        // when this object produces no string-resource line at all, so the rule that
        // wants a display name stays quiet about everything else.
        public string DisplayName { get; }

        public IReadOnlyList<ModelField> Fields { get; }

        // Whether a generator actually writes something for this object. Everything the
        // rules care about follows from it: there is no point reporting a name, a tag or a
        // duplicate on something that never reaches the script, and an object that should
        // generate and does not is itself the thing worth reporting.
        public bool Generates { get; }

        // Where this name has to be unique. Empty for everything MQL names globally -- a
        // type, a role, an attribute. A policy state is named inside its policy, so two
        // policies may each have a Preliminary state and neither is a duplicate.
        public string UniqueWithin { get; }

        public ModelObject(string kind, string name, string stereotype, string eaType, string path,
            string elementGuid, string displayName, bool generates, string uniqueWithin,
            IReadOnlyList<ModelField> fields)
        {
            Kind = kind;
            Name = name ?? "";
            Stereotype = stereotype ?? "";
            EaType = eaType ?? "";
            Path = path ?? "";
            ElementGuid = elementGuid ?? "";
            DisplayName = displayName;
            Generates = generates;
            UniqueWithin = uniqueWithin ?? "";
            Fields = fields ?? new List<ModelField>();
        }

        public string Display
        {
            get { return Name.Length == 0 ? "(unnamed)" : Name; }
        }

        // What to call this in a report. The stereotype, because that is what the modeller
        // set and what the generators dispatch on; EA's own type when there is none, since
        // that is the whole point of the object being reported at all.
        public string Label
        {
            get
            {
                if (Stereotype.Length > 0) return Stereotype;

                return EaType.Length > 0 ? EaType : Kind;
            }
        }

        // Every value the object contributes to the script, in order. Two objects with the
        // same name are only really the same object when these match.
        public string Signature()
        {
            var parts = new List<string>();
            foreach (ModelField field in Fields)
                parts.Add(field.Name + "=" + field.Value);

            return string.Join("\n", parts);
        }
    }
}
