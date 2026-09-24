using System;
using System.Collections.Generic;
using Dastan.org.ed.ea.constants;
using Dastan.org.ed.ea.entity;
using Dastan.org.ed.ea.repository;
using Dastan.org.ed.ea.util;
using EA;
using Attribute = EA.Attribute;

namespace Dastan.org.ed.ea.services.compare
{
    // Reduces the EA model to SchemaItems, so it can be compared with an ENOVIA export.
    //
    // The other half of the contract EnoviaSchemaSnapshot starts: the same field names,
    // holding the same wording, or everything reports as drift. Where the two systems say
    // a thing differently, this side is translated into the export's wording -- the export
    // is the one that cannot be changed.
    //
    // What a value means is taken from the generators, not from the profile, because the
    // generators decide what actually reaches the target system. Where a generator
    // supplies a default for a tag left blank, that default is applied here too: the
    // model means what it would install.
    public class ModelSchemaSnapshot
    {
        // EA's own element type for the UML final-state marker. StateScriptGenerator
        // returns before writing a state statement for it, so it is not part of the
        // policy in the target system and must not be part of it here.
        private const string FinalStateType = "FinalState";

        private readonly ElementRepository _elementRepository;

        // What the current walk has already described. An element reached twice is the
        // same element, and an attribute is defined once however many types carry it.
        private readonly HashSet<int> _seenElements = new HashSet<int>();
        private readonly HashSet<string> _seenAttributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public ModelSchemaSnapshot(ElementRepository elementRepository)
        {
            _elementRepository = elementRepository;
        }

        // Everything under one package. The whole model is every model root walked the
        // same way, so there is only ever this one traversal.
        public List<SchemaItem> Build(Repository repository, Package scope)
        {
            // Cleared rather than assumed empty, so one instance can be built from twice.
            // The container hands out a fresh one today, but it would hand out the same
            // one the moment somebody registers this as a singleton, and a second run
            // that silently returned nothing would be very hard to see.
            _seenElements.Clear();
            _seenAttributes.Clear();

            var items = new List<SchemaItem>();
            if (repository == null) return items;

            if (scope == null)
            {
                foreach (Package model in repository.Models) WalkPackage(repository, model, items);
            }
            else
            {
                WalkPackage(repository, scope, items);
            }

            return items;
        }

        private void WalkPackage(Repository repository, Package package, List<SchemaItem> items)
        {
            if (package == null) return;

            foreach (Package child in package.Packages) WalkPackage(repository, child, items);
            foreach (Element element in package.Elements) WalkElement(repository, element, items);
        }

        private void WalkElement(Repository repository, Element element, List<SchemaItem> items)
        {
            if (element == null || !_seenElements.Add(element.ElementID)) return;

            string stereotype = element.Stereotype ?? "";

            if (string.Equals(stereotype, Stereotypes.Type, StringComparison.OrdinalIgnoreCase))
            {
                items.Add(TypeItem(repository, element));
                AddAttributes(element.Attributes, items);
            }
            else if (string.Equals(stereotype, Stereotypes.Role, StringComparison.OrdinalIgnoreCase))
            {
                items.Add(RoleItem(element));
            }
            else if (string.Equals(stereotype, Stereotypes.Relationship, StringComparison.OrdinalIgnoreCase))
            {
                SchemaItem relationship = RelationshipItem(repository, element);
                if (relationship != null) items.Add(relationship);

                AddAttributes(element.Attributes, items);
            }
            else if (string.Equals(stereotype, Stereotypes.Policy, StringComparison.OrdinalIgnoreCase) &&
                     string.Equals(element.Type, ObjectTypes.StateMachine, StringComparison.OrdinalIgnoreCase))
            {
                items.Add(PolicyItem(repository, element));
            }

            // Walked regardless: a class that generates nothing can still own ones that do.
            foreach (Element child in element.Elements) WalkElement(repository, child, items);
        }

        private static SchemaItem TypeItem(Repository repository, Element element)
        {
            return new SchemaItem(SchemaItem.TypeKind, element.Name, new Dictionary<string, string>
            {
                { "name", element.Name },
                { "description", element.Notes },
                { "abstract", SchemaItem.Flag(element.Abstract == "1") },
                { "derivedFrom", GeneralizationParentName(repository, element) },
                { "attributes", SchemaItem.List(AttributeNames(element.Attributes), ordered: false) }
            });
        }

        private static SchemaItem RoleItem(Element element)
        {
            return new SchemaItem(SchemaItem.RoleKind, element.Name, new Dictionary<string, string>
            {
                { "name", element.Name },
                { "description", element.Notes }
            });
        }

        // A relationship is modelled as an association class: the element carries the name,
        // the description and the tags, and the connector it classifies carries the ends.
        // Without that connector there are no ends to compare, so there is nothing useful
        // to say about it.
        private static SchemaItem RelationshipItem(Repository repository, Element element)
        {
            Connector connector = ConnectorOf(repository, element);
            if (connector == null) return null;

            Collection tags = element.TaggedValues;

            var values = new Dictionary<string, string>
            {
                { "name", element.Name },
                { "description", element.Notes },
                { "abstract", SchemaItem.Flag(element.Abstract == "1") },
                // The generator's default, and MQL's: a relationship prevents duplicates
                // unless the model says otherwise.
                { "preventDuplicates", Flag(tags, "Prevent Duplicate", "True") },
                { "derivedFrom", GeneralizationParentName(repository, element) },
                { "attributes", SchemaItem.List(AttributeNames(element.Attributes), ordered: false) }
            };

            AddEnd(repository, values, "from", tags, "From", connector.ClientEnd, connector.ClientID);
            AddEnd(repository, values, "to", tags, "To", connector.SupplierEnd, connector.SupplierID);

            return new SchemaItem(SchemaItem.RelationshipKind, element.Name, values);
        }

        private static void AddEnd(Repository repository, Dictionary<string, string> values, string prefix,
            Collection tags, string tagPrefix, ConnectorEnd end, int elementId)
        {
            // EA, MQL and the export each word multiplicity differently; SchemaItem
            // reduces all three to the same two words.
            values[prefix + "Cardinality"] = SchemaItem.Cardinality(end == null ? "" : end.Cardinality);

            // The profile offers replicate/float/none, which is the export's wording
            // already. Blank means the profile default rather than a missing value.
            values[prefix + "Revision"] = Tag(tags, tagPrefix + " Revision", "none");
            values[prefix + "Clone"] = Tag(tags, tagPrefix + " Clone", "none");
            values[prefix + "PropagateConnection"] = Flag(tags, tagPrefix + " Propagate connection", "False");
            values[prefix + "PropagateModify"] = Flag(tags, tagPrefix + " Propagate modify", "False");

            // One end of a modelled relationship is one class, so this is always a single
            // type where the export may list several. A relationship that really does
            // accept several types reads as drift, and it is: the model does not say so.
            Element at = SafeElement(repository, elementId);
            values[prefix + "Types"] = at == null ? "" : at.Name ?? "";
        }

        private SchemaItem PolicyItem(Repository repository, Element element)
        {
            Collection tags = element.TaggedValues;

            return new SchemaItem(SchemaItem.PolicyKind, element.Name, new Dictionary<string, string>
            {
                { "name", element.Name },
                { "description", element.Notes },
                // These defaults are the generator's. A policy whose Store tag is blank
                // still installs with STORE, so that is what the model means.
                { "store", Tag(tags, "Store", "STORE") },
                { "sequence", Tag(tags, "Sequence", "-") },
                { "defaultFormat", Tag(tags, "Default Format", "generic") },
                { "types", SchemaItem.List(Split(Tag(tags, "Type", "")), ordered: false) },
                { "formats", SchemaItem.List(Split(Tag(tags, "Format", "generic")), ordered: false) },
                { "states", SchemaItem.List(StateNames(repository, element), ordered: true) }
            });
        }

        // The policy's states in lifecycle order, which is the order the generator walks
        // them: from the start state on the policy's diagram, along the transitions.
        // Reading them off the diagram in any other order would compare one lifecycle
        // against a different one and call it drift.
        private List<string> StateNames(Repository repository, Element policy)
        {
            var names = new List<string>();

            Diagram diagram = FirstDiagram(policy);
            if (diagram == null) return names;

            List<Element> start = _elementRepository.GetElementByDiagramIdAndTypeAndStereotype(
                new Context(repository), diagram.DiagramID, "State", "Start State");

            if (start.Count == 0) return names;

            var visited = new HashSet<int>();
            var pending = new Stack<Element>();
            pending.Push(start[0]);

            // Depth-first along the transitions, which is what StateScriptGenerator's
            // recursion amounts to.
            while (pending.Count > 0)
            {
                Element state = pending.Pop();
                if (state == null || !visited.Add(state.ElementID)) continue;

                // The final-state marker is not a state in the target system, and the walk
                // does not continue through it.
                if (string.Equals(state.Type, FinalStateType, StringComparison.OrdinalIgnoreCase)) continue;

                names.Add(state.Name ?? "");

                var next = new List<Element>();
                foreach (Connector connector in state.Connectors)
                {
                    if (connector.Type != ConnectorTypes.Transition &&
                        connector.Type != ConnectorTypes.StateFlow) continue;

                    if (connector.ClientID != state.ElementID) continue;

                    Element target = SafeElement(repository, connector.SupplierID);
                    if (target != null) next.Add(target);
                }

                // Pushed in reverse so the first transition is followed first, which is the
                // order the recursive walk uses.
                for (int i = next.Count - 1; i >= 0; i--) pending.Push(next[i]);
            }

            return names;
        }

        private void AddAttributes(Collection attributes, List<SchemaItem> items)
        {
            if (attributes == null) return;

            foreach (Attribute attribute in attributes)
            {
                if (!string.Equals(attribute.Stereotype, Stereotypes.Attribute, StringComparison.OrdinalIgnoreCase))
                    continue;

                string name = attribute.Name ?? "";

                // An attribute is defined once however many types carry it, which is what
                // the generator does and what MQL requires.
                if (name.Length == 0 || !_seenAttributes.Add(name)) continue;

                items.Add(AttributeItem(attribute));
            }
        }

        private static SchemaItem AttributeItem(Attribute attribute)
        {
            Collection tags = attribute.TaggedValues;
            string maxLength = AttributeTag(tags, "Max Length", "");

            return new SchemaItem(SchemaItem.AttributeKind, attribute.Name, new Dictionary<string, string>
            {
                { "name", attribute.Name },
                { "description", attribute.Notes },
                { "primitiveType", attribute.Type },
                { "default", attribute.Default },
                // Blank and 0 both mean unlimited, and the export omits the element
                // entirely. All three have to read as the same thing.
                { "maxlength", maxLength.Length == 0 ? "0" : maxLength },
                { "multiline", AttributeFlag(tags, "Multi Line") },
                { "resetOnClone", AttributeFlag(tags, "Reset On Clone") },
                { "resetOnRevision", AttributeFlag(tags, "Reset On Revision") },
                { "ranges", Ranges(tags) }
            });
        }

        // Allowed Values is a comma-separated list of values the attribute may equal --
        // RangeMqlTagTerm writes every entry as "range = X" and the model has no way to
        // say anything else. The export words that same rule as "equal", so that is what
        // it is written as here. A range the target system holds with any other operator
        // reads as drift, and it is: the model cannot express it.
        private static string Ranges(Collection tags)
        {
            var ranges = new List<string>();
            foreach (string value in Split(AttributeTag(tags, "Allowed Values", "")))
                ranges.Add("equal " + value);

            return SchemaItem.List(ranges, ordered: false);
        }

        private static List<string> AttributeNames(Collection attributes)
        {
            var names = new List<string>();
            if (attributes == null) return names;

            // Every attribute, not only the stereotyped ones, because the type statement
            // names every one of them. An attribute listed on a type and never defined is
            // worth seeing rather than hiding.
            foreach (Attribute attribute in attributes) names.Add(attribute.Name ?? "");

            return names;
        }

        private static string GeneralizationParentName(Repository repository, Element element)
        {
            foreach (Connector connector in element.Connectors)
            {
                if (connector.Type != ConnectorTypes.Generalization) continue;
                if (connector.ClientID != element.ElementID) continue;

                Element parent = SafeElement(repository, connector.SupplierID);
                if (parent != null) return parent.Name ?? "";
            }

            return "";
        }

        private static Connector ConnectorOf(Repository repository, Element associationClass)
        {
            try
            {
                int id = associationClass.AssociationClassConnectorID;
                return id <= 0 ? null : repository.GetConnectorByID(id);
            }
            catch
            {
                return null;
            }
        }

        private static Element SafeElement(Repository repository, int id)
        {
            try
            {
                return id <= 0 ? null : repository.GetElementByID(id);
            }
            catch
            {
                return null;
            }
        }

        private static Diagram FirstDiagram(Element element)
        {
            try
            {
                return element.Diagrams.Count == 0 ? null : element.Diagrams.GetAt(0) as Diagram;
            }
            catch
            {
                return null;
            }
        }

        private static string Tag(Collection tags, string name, string fallback)
        {
            return TagUtility.GetSafeTagValue(tags, name, fallback);
        }

        private static string AttributeTag(Collection tags, string name, string fallback)
        {
            return TagUtility.GetSafeTagValue(tags, name, fallback, true);
        }

        // "True" exactly, because that is the test FlagMqlTagTerm makes when it decides
        // between multiline and notmultiline. Anything else installs as false, so
        // anything else has to compare as false.
        private static string Flag(Collection tags, string name, string fallback)
        {
            return SchemaItem.Flag(string.Equals(Tag(tags, name, fallback), "True", StringComparison.Ordinal));
        }

        private static string AttributeFlag(Collection tags, string name)
        {
            return SchemaItem.Flag(string.Equals(AttributeTag(tags, name, ""), "True", StringComparison.Ordinal));
        }

        private static IEnumerable<string> Split(string value)
        {
            foreach (string entry in (value ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string text = entry.Trim();
                if (text.Length > 0) yield return text;
            }
        }
    }
}
