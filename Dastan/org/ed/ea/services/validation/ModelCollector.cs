using System;
using System.Collections.Generic;
using Dastan.org.ed.ea.constants;
using Dastan.org.ed.ea.util;
using EA;
using Attribute = EA.Attribute;

namespace Dastan.org.ed.ea.services.validation
{
    // Walks whatever is selected and flattens it into the values the generators would
    // read. Everything the generators know about which tagged value ends up where in a
    // statement lives here, so the rules can stay about MQL and not about EA.
    public class ModelCollector
    {
        // Stereotypes whose generator writes a line into the .properties file, taking the
        // display name from the element's alias.
        private static readonly HashSet<string> AliasStringResources =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                Stereotypes.Type, Stereotypes.Role, Stereotypes.Relationship
            };

        // Tagged values a generator drops into a statement where MQL expects a name.
        private static readonly Dictionary<string, string[]> IdentifierTags =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                { Stereotypes.Policy, new[] { "Type", "Store", "Format", "Default Format", "Sequence" } },
                { Stereotypes.Relationship, new[] { "From Clone", "From Revision", "To Clone", "To Revision" } }
            };

        // Tags holding a comma-separated list, each entry of which is quoted separately.
        private static readonly HashSet<string> ListTags =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Type", "Format" };

        // EA types whose name goes into the policy script bare, with no quotes around it.
        private static readonly HashSet<string> StateTypes =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "State", "StateNode", "FinalState" };

        // EA types a generator would have handled if only a stereotype had been set.
        // Everything else with no stereotype is model furniture -- notes, boundaries, the
        // tasks the Jira sync creates, the log artifact -- and reporting on it would bury
        // the things that matter.
        private static readonly HashSet<string> Generatable =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Class", "StateMachine" };

        private readonly Dictionary<int, string> _packagePaths = new Dictionary<int, string>();
        private readonly HashSet<int> _seen = new HashSet<int>();

        public List<ModelObject> Collect(Repository repository, out string scopeName)
        {
            var objects = new List<ModelObject>();
            ObjectType type = repository.GetContextItem(out object item);

            switch (type)
            {
                case ObjectType.otPackage:
                    Package package = (Package) item;
                    scopeName = "package " + package.Name;
                    CollectPackage(package, PathOf(repository, package.PackageID), objects);
                    break;

                case ObjectType.otElement:
                    Element element = (Element) item;
                    scopeName = "element " + element.Name;
                    CollectElement(element, PathOf(repository, element.PackageID), objects);
                    break;

                case ObjectType.otDiagram:
                    Diagram diagram = (Diagram) item;
                    scopeName = "diagram " + diagram.Name;
                    CollectDiagram(repository, diagram, objects);
                    break;

                default:
                    // Nothing useful selected, so check everything, which is what the menu
                    // item says it does.
                    scopeName = "the whole model";
                    foreach (Package model in repository.Models)
                        CollectPackage(model, model.Name ?? "", objects);
                    break;
            }

            return objects;
        }

        private void CollectDiagram(Repository repository, Diagram diagram, List<ModelObject> objects)
        {
            foreach (DiagramObject shape in diagram.DiagramObjects)
            {
                Element element = repository.GetElementByID(shape.ElementID);
                if (element == null) continue;

                CollectElement(element, PathOf(repository, element.PackageID), objects);
            }
        }

        private void CollectPackage(Package package, string path, List<ModelObject> objects)
        {
            foreach (Package child in package.Packages)
                CollectPackage(child, Join(path, child.Name), objects);

            foreach (Element element in package.Elements)
                CollectElement(element, path, objects);
        }

        private void CollectElement(Element element, string path, List<ModelObject> objects)
        {
            if (element == null || !_seen.Add(element.ElementID)) return;

            string stereotype = element.Stereotype ?? "";
            string eaType = element.Type ?? "";
            bool isState = StateTypes.Contains(eaType);

            // Every generator opens by testing the stereotype, and a state is the one
            // thing generated without needing one.
            bool generates = stereotype.Length > 0 || isState;
            string childPath = Join(path, element.Name);

            // Worth describing either because something will be generated from it, or
            // because something should have been and no stereotype is the reason it was
            // not. A note on a diagram is neither.
            if (generates || Generatable.Contains(eaType))
            {
                var fields = new List<ModelField>
                {
                    // A state's name is written into the policy script with nothing around
                    // it, so a space in it ends the name and the rest becomes stray MQL.
                    new ModelField("Name", element.Name, isState ? FieldKind.Unquoted : FieldKind.Identifier)
                };

                AddTagFields(element.TaggedValues, stereotype, fields);

                objects.Add(new ModelObject(ModelObject.ElementKind, element.Name, stereotype, eaType, path,
                    element.ElementGUID, DisplayNameOf(element, stereotype, isState), generates,
                    isState ? path : "", fields));

                // Only from an element that is itself part of the schema. An attribute on
                // an unstereotyped class is not a missing attribute, it is a class that was
                // never meant to be exported.
                if (stereotype.Length > 0)
                {
                    foreach (Attribute attribute in element.Attributes)
                        objects.Add(Describe(attribute, childPath, element.ElementGUID));
                }
            }

            // Walked regardless: a skipped element can still own ones worth checking.
            foreach (Element child in element.Elements)
                CollectElement(child, childPath, objects);
        }

        private static ModelObject Describe(Attribute attribute, string path, string ownerGuid)
        {
            var fields = new List<ModelField>
            {
                new ModelField("Name", attribute.Name, FieldKind.Identifier),
                new ModelField("Type", attribute.Type, FieldKind.Identifier),
                new ModelField("Max Length",
                    TagUtility.GetSafeTagValue(attribute.TaggedValues, "Max Length", "", true), FieldKind.Number)
            };

            // Allowed values become "range X" with no quotes around X.
            string allowed = TagUtility.GetSafeTagValue(attribute.TaggedValues, "Allowed Values", "", true);
            foreach (string value in allowed.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                fields.Add(new ModelField("Allowed value", value.Trim(), FieldKind.Unquoted));

            // The attribute generator writes nothing for an attribute that is not
            // stereotyped as one, which also means there is no string-resource line and so
            // no display name to be missing.
            string stereotype = attribute.Stereotype ?? "";
            bool generates = string.Equals(stereotype, Stereotypes.Attribute, StringComparison.OrdinalIgnoreCase);
            string displayName = generates ? attribute.Alias ?? "" : null;

            return new ModelObject(ModelObject.AttributeKind, attribute.Name, stereotype, "", path, ownerGuid,
                displayName, generates, "", fields);
        }

        private static string DisplayNameOf(Element element, string stereotype, bool isState)
        {
            // A policy and its states carry their display name as a tagged value; the rest
            // use the element's alias. Anything else writes no string resource at all, so
            // it has no display name to be missing.
            if (isState || string.Equals(stereotype, Stereotypes.Policy, StringComparison.OrdinalIgnoreCase))
                return TagUtility.GetSafeTagValue(element.TaggedValues, "Display Name", "");

            return AliasStringResources.Contains(stereotype) ? element.Alias ?? "" : null;
        }

        private static void AddTagFields(Collection taggedValues, string stereotype, List<ModelField> fields)
        {
            if (!IdentifierTags.TryGetValue(stereotype, out string[] tagNames)) return;

            foreach (string tagName in tagNames)
            {
                string value = TagUtility.GetSafeTagValue(taggedValues, tagName, "");
                if (value.Length == 0) continue;

                if (!ListTags.Contains(tagName))
                {
                    fields.Add(new ModelField(tagName, value, FieldKind.Identifier));
                    continue;
                }

                foreach (string entry in value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    fields.Add(new ModelField(tagName, entry.Trim(), FieldKind.Identifier));
            }
        }

        // Package names from the selection up to the model root, so an issue says where to
        // go looking. Cached because the walk asks for the same ones repeatedly.
        private string PathOf(Repository repository, int packageId)
        {
            if (_packagePaths.TryGetValue(packageId, out string cached)) return cached;

            var names = new List<string>();
            int id = packageId;

            // The root's ParentID is 0; the depth cap is only there so a broken parent
            // chain cannot spin forever.
            for (int depth = 0; depth < 32 && id > 0; depth++)
            {
                Package package;
                try
                {
                    package = repository.GetPackageByID(id);
                }
                catch
                {
                    break;
                }

                if (package == null) break;

                names.Insert(0, package.Name ?? "");
                id = package.ParentID;
            }

            string path = string.Join(" / ", names.ToArray());
            _packagePaths[packageId] = path;
            return path;
        }

        private static string Join(string path, string name)
        {
            if (string.IsNullOrEmpty(name)) return path;
            return string.IsNullOrEmpty(path) ? name : path + " / " + name;
        }
    }
}
