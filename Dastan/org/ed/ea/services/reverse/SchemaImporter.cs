using System;
using System.Collections.Generic;
using Dastan.org.ed.ea.constants;
using Dastan.org.ed.ea.services.compare;
using Dastan.org.ed.ea.services.enovia;
using EA;
using Attribute = EA.Attribute;

namespace Dastan.org.ed.ea.services.reverse
{
    // Creates the part of an ENOVIA export the model does not have.
    //
    // It only ever creates. Nothing existing is edited or deleted, so the worst an import
    // can do is add elements -- which the modeller can delete -- rather than change ones
    // they have already worked on. That is deliberate: EA has no undo for anything done
    // through its API.
    //
    // The order below is a dependency order, not a preference. A generalization needs both
    // types to exist; an association class needs its connector; a connector needs both
    // ends. Each pass therefore finishes before the next begins.
    public class SchemaImporter
    {
        private const string StateType = "State";
        private const string StateDiagramType = "Statechart";

        private const string StartStateStereotype = "Start State";
        private const string StateStereotype = "State";
        private const string FinalStateStereotype = "Final State";

        // Laid out in one column, far enough apart to read. EA will not arrange a diagram
        // built through the API, and a pile of states on top of each other is worse than
        // a plain list.
        private const int StateLeft = 120;
        private const int StateWidth = 180;
        private const int StateHeight = 60;
        private const int StateGap = 110;

        private const string ClassDiagramType = "Logical";
        private const string DiagramName = "Imported Schema";

        private const int Margin = 40;
        private const int BoxWidth = 180;
        private const int BoxHeight = 90;
        private const int ColumnGap = 120;
        private const int RowGap = 110;

        private readonly Dictionary<string, Element> _types =
            new Dictionary<string, Element>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, Element> _relationships =
            new Dictionary<string, Element>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, Package> _folders =
            new Dictionary<string, Package>(StringComparer.OrdinalIgnoreCase);

        private readonly HashSet<string> _createdTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // What to put on the diagram afterwards, in creation order. An association class
        // is only recognisable as one when its two ends are on the diagram with it, so the
        // ends are kept alongside it rather than looked up again later.
        private readonly List<Element> _drawnTypes = new List<Element>();
        private readonly List<Drawn> _drawnRelationships = new List<Drawn>();

        private class Drawn
        {
            public Element Element;
            public Element From;
            public Element To;
        }

        public SchemaImportResult Import(Repository repository, Package target, SchemaImportPlan plan)
        {
            var result = new SchemaImportResult();
            if (repository == null || target == null || plan == null) return result;

            // Indexed across the whole model, not just the target package: a new
            // relationship may well connect types that live somewhere else entirely.
            IndexModel(repository);

            foreach (string name in plan.Unplaced)
            {
                result.Skip("Attribute " + name,
                    "nothing being created carries it, and adding it to a type the model already " +
                    "has would mean changing that type");
            }

            ImportRoles(target, plan, result);
            ImportTypes(target, plan, result);
            ImportTypeRelations(plan, result);
            ImportRelationships(target, plan, result);
            ImportPolicies(target, plan, result);
            Draw(target, result);

            target.Packages.Refresh();
            target.Elements.Refresh();
            return result;
        }

        private void ImportRoles(Package target, SchemaImportPlan plan, SchemaImportResult result)
        {
            if (plan.Roles.Count == 0) return;

            Package folder = Folder(target, "Roles");

            foreach (EnoviaRole role in plan.Roles)
            {
                Element element = NewElement(folder, role.Name, "Class", Stereotypes.Role);
                if (element == null)
                {
                    result.Skip("Role " + role.Name, "EA would not create the element");
                    continue;
                }

                element.Notes = role.Description;
                element.Update();
                result.Roles++;
            }
        }

        // First pass: the elements alone. Parents and attributes come afterwards, because
        // a type may derive from another type in the same import that does not exist yet.
        private void ImportTypes(Package target, SchemaImportPlan plan, SchemaImportResult result)
        {
            if (plan.Types.Count == 0) return;

            Package folder = Folder(target, "Types");

            foreach (EnoviaType type in plan.Types)
            {
                if (_types.ContainsKey(type.Name))
                {
                    result.Skip("Type " + type.Name, "the model already has a type with that name");
                    continue;
                }

                Element element = NewElement(folder, type.Name, "Class", Stereotypes.Type);
                if (element == null)
                {
                    result.Skip("Type " + type.Name, "EA would not create the element");
                    continue;
                }

                element.Notes = type.Description;
                element.Abstract = type.Abstract ? "1" : "0";
                element.Update();

                _types[type.Name] = element;
                _createdTypes.Add(type.Name);
                _drawnTypes.Add(element);
                result.Types++;
            }
        }

        private void ImportTypeRelations(SchemaImportPlan plan, SchemaImportResult result)
        {
            foreach (EnoviaType type in plan.Types)
            {
                if (!_createdTypes.Contains(type.Name)) continue;

                Element element = _types[type.Name];

                if (type.DerivedFrom.Length > 0)
                {
                    Element parent;
                    if (_types.TryGetValue(type.DerivedFrom, out parent))
                        Generalize(element, parent);
                    else
                        result.Skip("Type " + type.Name + " parent",
                            "'" + type.DerivedFrom + "' is neither in the model nor in this export");
                }

                AddAttributes(element, type.AttributeNames, plan, result);
            }
        }

        private void ImportRelationships(Package target, SchemaImportPlan plan, SchemaImportResult result)
        {
            if (plan.Relationships.Count == 0) return;

            Package folder = Folder(target, "Relationships");

            foreach (EnoviaRelationship relationship in plan.Relationships)
            {
                if (_relationships.ContainsKey(relationship.Name))
                {
                    result.Skip("Relationship " + relationship.Name,
                        "the model already has a relationship with that name");
                    continue;
                }

                Element from = EndType(relationship, relationship.From, "from", result);
                Element to = EndType(relationship, relationship.To, "to", result);
                if (from == null || to == null) continue;

                // The element first, then the association. Either can fail, and an element
                // left behind is one the modeller can see and delete, where a connector
                // left behind between two types is all but invisible.
                Element element = NewElement(folder, relationship.Name, "Class", Stereotypes.Relationship);
                if (element == null)
                {
                    result.Skip("Relationship " + relationship.Name, "EA would not create the element");
                    continue;
                }

                element.Notes = relationship.Description;
                element.Abstract = relationship.Abstract ? "1" : "0";
                element.Update();

                Connector connector = from.Connectors.AddNew("", ConnectorTypes.Association) as Connector;
                if (connector == null)
                {
                    result.Skip("Relationship " + relationship.Name, "EA would not create the association");
                    continue;
                }

                connector.SupplierID = to.ElementID;
                connector.Update();
                from.Connectors.Refresh();

                // What turns a plain class into the association class of that connector,
                // which is how every relationship in this model is shaped.
                if (!element.CreateAssociationClass(connector.ConnectorID))
                {
                    result.Skip("Relationship " + relationship.Name,
                        "EA would not attach it to the association as its class");
                    continue;
                }

                SetCardinality(connector.ClientEnd, relationship.From.Cardinality);
                SetCardinality(connector.SupplierEnd, relationship.To.Cardinality);

                Collection tags = element.TaggedValues;
                SetTag(tags, "Prevent Duplicate", Truth(relationship.PreventDuplicates));
                AddEndTags(tags, "From", relationship.From);
                AddEndTags(tags, "To", relationship.To);

                AddAttributes(element, relationship.AttributeNames, plan, result);

                _relationships[relationship.Name] = element;
                _drawnRelationships.Add(new Drawn { Element = element, From = from, To = to });
                result.Relationships++;
            }

            // Derived relationships are a generalization between the two association
            // classes, so both have to exist before any of them can be wired.
            foreach (EnoviaRelationship relationship in plan.Relationships)
            {
                if (relationship.DerivedFrom.Length == 0) continue;

                Element element;
                if (!_relationships.TryGetValue(relationship.Name, out element)) continue;

                Element parent;
                if (_relationships.TryGetValue(relationship.DerivedFrom, out parent))
                    Generalize(element, parent);
                else
                    result.Skip("Relationship " + relationship.Name + " parent",
                        "'" + relationship.DerivedFrom + "' is neither in the model nor in this export");
            }
        }

        private void ImportPolicies(Package target, SchemaImportPlan plan, SchemaImportResult result)
        {
            if (plan.Policies.Count == 0) return;

            Package folder = Folder(target, "Policies");

            foreach (EnoviaPolicy policy in plan.Policies)
            {
                Element element = NewElement(folder, policy.Name, ObjectTypes.StateMachine, Stereotypes.Policy);
                if (element == null)
                {
                    result.Skip("Policy " + policy.Name, "EA would not create the element");
                    continue;
                }

                element.Notes = policy.Description;
                element.Update();

                Collection tags = element.TaggedValues;
                SetTag(tags, "Store", policy.Store);
                SetTag(tags, "Sequence", policy.Sequence);
                SetTag(tags, "Default Format", policy.DefaultFormat);
                SetTag(tags, "Type", string.Join(", ", policy.Types.ToArray()));
                SetTag(tags, "Format", string.Join(", ", policy.Formats.ToArray()));

                result.Policies++;
                AddStates(element, policy, result);
            }
        }

        // A policy's states have to go on a diagram, in order, joined by transitions --
        // that is where the generator reads them from, and where a later comparison reads
        // them from. States created as children alone would look right in the browser and
        // produce a policy with no lifecycle at all.
        private void AddStates(Element policy, EnoviaPolicy definition, SchemaImportResult result)
        {
            if (definition.States.Count == 0) return;

            Diagram diagram = policy.Diagrams.AddNew(policy.Name, StateDiagramType) as Diagram;
            if (diagram == null)
            {
                result.Skip("Policy " + policy.Name + " states",
                    "EA would not create the state diagram to put them on");
                return;
            }

            diagram.Update();

            int last = definition.States.Count - 1;
            int top = 40;
            Element previous = null;

            for (int i = 0; i <= last; i++)
            {
                EnoviaState state = definition.States[i];

                // The first state is where the walk starts, so it keeps the start
                // stereotype even when it is also the last one.
                string stereotype = i == 0
                    ? StartStateStereotype
                    : i == last ? FinalStateStereotype : StateStereotype;

                Element element = policy.Elements.AddNew(state.Name, StateType) as Element;
                if (element == null)
                {
                    result.Skip("State " + state.Name + " in " + policy.Name, "EA would not create the element");
                    continue;
                }

                Stereotype(element, stereotype);
                element.Update();
                policy.Elements.Refresh();

                Collection stateTags = element.TaggedValues;
                SetTag(stateTags, "Minor Revisionable", Truth(state.Revisionable));
                SetTag(stateTags, "Major Revisionable", Truth(state.MajorRevisionable));
                SetTag(stateTags, "Versionable", Truth(state.Versionable));
                SetTag(stateTags, "Promote", Truth(state.AutoPromotion));

                DiagramObject shape = diagram.DiagramObjects.AddNew(
                    "l=" + StateLeft + ";r=" + (StateLeft + StateWidth) +
                    ";t=" + top + ";b=" + (top + StateHeight) + ";", "") as DiagramObject;

                if (shape != null)
                {
                    shape.ElementID = element.ElementID;
                    shape.Update();
                }

                top += StateGap;

                if (previous != null)
                {
                    Connector transition = previous.Connectors.AddNew("", ConnectorTypes.Transition) as Connector;
                    if (transition != null)
                    {
                        transition.SupplierID = element.ElementID;
                        transition.Update();
                        previous.Connectors.Refresh();
                    }
                }

                previous = element;
                result.States++;
            }

            diagram.DiagramObjects.Refresh();
        }

        // A class diagram of what was just imported.
        //
        // Without one, an import is a list of names in the browser: the association class
        // that makes a relationship a relationship, and the colours the profile gives each
        // stereotype, are only visible on a diagram. Both ends of every relationship go on
        // it even when the end type was already in the model, because an association class
        // with one end missing does not read as one.
        private void Draw(Package target, SchemaImportResult result)
        {
            if (_drawnTypes.Count == 0 && _drawnRelationships.Count == 0) return;

            var diagram = target.Diagrams.AddNew(DiagramName, ClassDiagramType) as Diagram;
            if (diagram == null)
            {
                result.Skip("Diagram", "EA would not create it; everything else was still imported");
                return;
            }

            diagram.Update();
            target.Diagrams.Refresh();

            var centres = new Dictionary<int, int[]>();

            // Types first, in a square-ish grid. Square rather than a long row because a
            // diagram twenty boxes wide is no more readable than a list.
            int columns = (int) Math.Ceiling(Math.Sqrt(_drawnTypes.Count));
            if (columns < 1) columns = 1;

            for (int i = 0; i < _drawnTypes.Count; i++)
            {
                Place(diagram, centres, _drawnTypes[i],
                    Margin + i % columns * (BoxWidth + ColumnGap),
                    Margin + i / columns * (BoxHeight + RowGap));
            }

            int spare = Margin + (_drawnTypes.Count / columns + 1) * (BoxHeight + RowGap);

            foreach (Drawn drawn in _drawnRelationships)
            {
                // An end that was already in the model has no place on the grid yet, so it
                // is added below it rather than left off.
                spare = Ensure(diagram, centres, drawn.From, spare);
                spare = Ensure(diagram, centres, drawn.To, spare);

                int[] from = centres[drawn.From.ElementID];
                int[] to = centres[drawn.To.ElementID];

                // Beside the midpoint of the two ends, which is where EA draws the dashed
                // link to. Offset sideways so it does not land on the association line.
                Place(diagram, centres, drawn.Element,
                    (from[0] + to[0]) / 2 + BoxWidth / 2 + ColumnGap / 2,
                    (from[1] + to[1]) / 2 - BoxHeight / 2);
            }

            diagram.DiagramObjects.Refresh();
        }

        // A type that has to appear but was not part of this import. Stacked down the left
        // under the grid, which keeps it out of the way and still on the diagram.
        private static int Ensure(Diagram diagram, Dictionary<int, int[]> centres, Element element, int top)
        {
            if (centres.ContainsKey(element.ElementID)) return top;

            Place(diagram, centres, element, Margin, top);
            return top + BoxHeight + RowGap;
        }

        private static void Place(Diagram diagram, Dictionary<int, int[]> centres, Element element,
            int left, int top)
        {
            if (left < Margin) left = Margin;
            if (top < Margin) top = Margin;

            var shape = diagram.DiagramObjects.AddNew(
                "l=" + left + ";r=" + (left + BoxWidth) +
                ";t=" + top + ";b=" + (top + BoxHeight) + ";", "") as DiagramObject;

            // Recorded either way, so a shape EA refused is not placed twice and the
            // midpoint arithmetic still has somewhere to aim.
            centres[element.ElementID] = new[] { left + BoxWidth / 2, top + BoxHeight / 2 };
            if (shape == null) return;

            shape.ElementID = element.ElementID;
            shape.Update();
        }

        private void AddAttributes(Element owner, List<string> names, SchemaImportPlan plan,
            SchemaImportResult result)
        {
            foreach (string name in names)
            {
                EnoviaAttribute definition;
                if (!plan.Attributes.TryGetValue(name, out definition))
                {
                    result.Skip("Attribute " + name + " on " + owner.Name,
                        "the export names it but holds no definition for it");
                    continue;
                }

                Attribute attribute = owner.Attributes.AddNew(definition.Name, definition.PrimitiveType)
                    as Attribute;

                if (attribute == null)
                {
                    result.Skip("Attribute " + name + " on " + owner.Name, "EA would not create it");
                    continue;
                }

                Stereotype(attribute, Stereotypes.Attribute);
                attribute.Notes = definition.Description;
                attribute.Default = definition.DefaultValue;
                attribute.Update();
                owner.Attributes.Refresh();

                AddAttributeTags(attribute, definition, owner.Name, result);
                result.Attributes++;
            }
        }

        private static void AddAttributeTags(Attribute attribute, EnoviaAttribute definition, string owner,
            SchemaImportResult result)
        {
            Collection tags = attribute.TaggedValues;

            // The export omits maxlength when it is unlimited; the model writes that as 0.
            SetAttributeTag(tags, "Max Length", definition.MaxLength.Length == 0 ? "0" : definition.MaxLength);
            SetAttributeTag(tags, "Multi Line", Truth(definition.Multiline));
            SetAttributeTag(tags, "Reset On Clone", Truth(definition.ResetOnClone));
            SetAttributeTag(tags, "Reset On Revision", Truth(definition.ResetOnRevision));

            var allowed = new List<string>();

            foreach (EnoviaRange range in definition.Ranges)
            {
                // Allowed Values is a list of values the attribute may equal, and that is
                // the only rule it can express. A range with any other operator has no
                // home in the model, so it is reported instead of being written as if it
                // were an equality.
                if (string.Equals(range.RangeType, "equal", StringComparison.OrdinalIgnoreCase))
                {
                    allowed.Add(range.Value);
                    continue;
                }

                result.Skip("Range '" + range + "' on attribute " + definition.Name + " in " + owner,
                    "the model can only express ranges that are an equality");
            }

            if (allowed.Count > 0) SetAttributeTag(tags, "Allowed Values", string.Join(", ", allowed.ToArray()));
        }

        private static void AddEndTags(Collection tags, string prefix, EnoviaRelationshipEnd end)
        {
            SetTag(tags, prefix + " Revision", end.RevisionAction);
            SetTag(tags, prefix + " Clone", end.CloneAction);
            SetTag(tags, prefix + " Propagate connection", Truth(end.PropagateConnection));
            SetTag(tags, prefix + " Propagate modify", Truth(end.PropagateModify));
        }

        // One end of a modelled relationship is one class. An export end that accepts
        // everything, or several types, cannot be drawn as one association, so it is left
        // for someone to decide rather than guessed at.
        private Element EndType(EnoviaRelationship relationship, EnoviaRelationshipEnd end, string side,
            SchemaImportResult result)
        {
            if (end.AllowAllTypes)
            {
                result.Skip("Relationship " + relationship.Name,
                    "its " + side + " end accepts all types, which one association cannot say");
                return null;
            }

            if (end.Types.Count == 0)
            {
                result.Skip("Relationship " + relationship.Name, "its " + side + " end names no type");
                return null;
            }

            if (end.Types.Count > 1)
            {
                result.Skip("Relationship " + relationship.Name,
                    "its " + side + " end accepts " + end.Types.Count +
                    " types, and one association can only join two elements");
                return null;
            }

            Element type;
            if (_types.TryGetValue(end.Types[0], out type)) return type;

            result.Skip("Relationship " + relationship.Name,
                "its " + side + " type '" + end.Types[0] + "' is neither in the model nor in this export");
            return null;
        }

        private static void SetCardinality(ConnectorEnd end, string exported)
        {
            if (end == null) return;

            // Back the way the model writes it. Anything the generator would read as many
            // is written as many, so a re-comparison comes out in step.
            end.Cardinality = SchemaItem.Cardinality(exported) == "one" ? "1" : "*";
            end.Update();
        }

        private static void Generalize(Element child, Element parent)
        {
            Connector connector = child.Connectors.AddNew("", ConnectorTypes.Generalization) as Connector;
            if (connector == null) return;

            connector.SupplierID = parent.ElementID;
            connector.Update();
            child.Connectors.Refresh();
        }

        private Package Folder(Package target, string name)
        {
            Package cached;
            if (_folders.TryGetValue(name, out cached)) return cached;

            foreach (Package existing in target.Packages)
            {
                if (!string.Equals(existing.Name, name, StringComparison.OrdinalIgnoreCase)) continue;

                _folders[name] = existing;
                return existing;
            }

            var created = target.Packages.AddNew(name, "Package") as Package;
            if (created != null)
            {
                created.Update();
                target.Packages.Refresh();
            }

            // A null here means EA refused, and every caller reports the element it could
            // not create rather than failing the whole import.
            _folders[name] = created ?? target;
            return _folders[name];
        }

        private static Element NewElement(Package package, string name, string eaType, string stereotype)
        {
            var element = package.Elements.AddNew(name, eaType) as Element;
            if (element == null) return null;

            Stereotype(element, stereotype);
            element.Update();
            package.Elements.Refresh();
            return element;
        }

        // Applied through the profile so the element gets the profile's tagged values and
        // looks to a modeller exactly like one they drew themselves. If the MDG is not
        // loaded in this EA, StereotypeEx leaves the stereotype empty, and the plain name
        // is better than nothing -- the generators only ever read the plain name.
        private static void Stereotype(Element element, string stereotype)
        {
            element.StereotypeEx = Profiles.PlmSchema + "::" + stereotype;
            if (string.IsNullOrEmpty(element.Stereotype)) element.Stereotype = stereotype;
        }

        private static void Stereotype(Attribute attribute, string stereotype)
        {
            attribute.StereotypeEx = Profiles.PlmSchema + "::" + stereotype;
            if (string.IsNullOrEmpty(attribute.Stereotype)) attribute.Stereotype = stereotype;
        }

        // Updated in place when the profile already put the tag there, which it does for
        // every stereotype in the schema profile. Adding a second tag of the same name
        // would leave the element with two and the generators reading whichever came
        // first.
        private static void SetTag(Collection tags, string name, string value)
        {
            foreach (TaggedValue existing in tags)
            {
                if (!Matches(existing.Name, existing.FQName, name)) continue;

                existing.Value = value ?? "";
                existing.Update();
                return;
            }

            var created = tags.AddNew(name, value ?? "") as TaggedValue;
            if (created == null) return;

            created.Update();
            tags.Refresh();
        }

        private static void SetAttributeTag(Collection tags, string name, string value)
        {
            foreach (AttributeTag existing in tags)
            {
                if (!Matches(existing.Name, existing.FQName, name)) continue;

                existing.Value = value ?? "";
                existing.Update();
                return;
            }

            var created = tags.AddNew(name, value ?? "") as AttributeTag;
            if (created == null) return;

            created.Update();
            tags.Refresh();
        }

        // The same match TagUtility makes when it reads a tag back, so a tag written here
        // is one the generators will find.
        private static bool Matches(string tagName, string fqName, string wanted)
        {
            return string.Equals(tagName, wanted, StringComparison.OrdinalIgnoreCase) ||
                   (fqName != null && fqName.EndsWith("::" + wanted, StringComparison.OrdinalIgnoreCase));
        }

        private static string Truth(bool value)
        {
            return value ? "True" : "False";
        }

        private void IndexModel(Repository repository)
        {
            foreach (Package model in repository.Models) IndexPackage(model);
        }

        private void IndexPackage(Package package)
        {
            foreach (Package child in package.Packages) IndexPackage(child);
            foreach (Element element in package.Elements) IndexElement(element);
        }

        private void IndexElement(Element element)
        {
            string name = element.Name ?? "";
            string stereotype = element.Stereotype ?? "";

            if (name.Length > 0)
            {
                if (string.Equals(stereotype, Stereotypes.Type, StringComparison.OrdinalIgnoreCase) &&
                    !_types.ContainsKey(name))
                    _types[name] = element;

                if (string.Equals(stereotype, Stereotypes.Relationship, StringComparison.OrdinalIgnoreCase) &&
                    !_relationships.ContainsKey(name))
                    _relationships[name] = element;
            }

            foreach (Element child in element.Elements) IndexElement(child);
        }
    }
}
