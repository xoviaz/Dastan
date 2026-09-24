using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Dastan.org.ed.ea.services.enovia
{
    // Reads an ematrix XML export into an EnoviaExport.
    //
    // Deliberately tolerant. The export format has not changed since V6R2008 even in an
    // R2024x file, and every optional part of it is marked optional in the DTD, so a
    // release difference shows up as an element this reader has not been told about
    // rather than as a structural change. Those are counted and reported instead of
    // failing the read, which is what lets one reader serve every version.
    //
    // Nothing here knows about EA. Reverse engineering turns this model into elements and
    // drift detection compares it against them; both read the same thing.
    public class EnoviaExportReader
    {
        // Element kinds that are part of an export and simply not read yet, so they are
        // not reported as surprises. Business objects will move out of this list when
        // they are implemented.
        private static readonly HashSet<string> Expected = new HashSet<string>(StringComparer.Ordinal)
        {
            "creationProperties", "businessObject", "relationship", "bulkExport"
        };

        public EnoviaExport Read(string path)
        {
            XmlDocument document = Load(path);
            var export = new EnoviaExport();

            XmlElement root = document.DocumentElement;
            if (root == null || root.Name != "ematrix")
                throw new InvalidDataException(
                    "This is not an ematrix export: the document's root element is " +
                    (root == null ? "missing" : "'" + root.Name + "'") + " rather than 'ematrix'.");

            foreach (XmlNode node in root.ChildNodes)
            {
                XmlElement element = node as XmlElement;
                if (element == null) continue;

                switch (element.Name)
                {
                    case "creationProperties":
                        export.Release = Text(element, "release");
                        export.ExportedAt = Text(element, "datetime");
                        break;

                    case "type": export.Types.Add(ReadType(element)); break;
                    case "attributeDef": export.Attributes.Add(ReadAttribute(element)); break;
                    case "relationshipDef": export.Relationships.Add(ReadRelationship(element)); break;
                    case "policy": export.Policies.Add(ReadPolicy(element)); break;
                    case "role": export.Roles.Add(ReadRole(element)); break;

                    default:
                        Skip(export, element.Name);
                        break;
                }
            }

            return export;
        }

        // The DTD is not optional. An export references &ematrixProductDtd;, an entity
        // declared in ematrixml.dtd, so a parser told to ignore the DTD fails on an
        // undefined entity. The file therefore has to be read with the .dtd beside it --
        // which is what ENOVIA's own documentation says to ship anyway.
        private static XmlDocument Load(string path)
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Parse,
                XmlResolver = new XmlUrlResolver(),
                IgnoreComments = true,
                IgnoreWhitespace = true
            };

            try
            {
                var document = new XmlDocument { XmlResolver = null };
                using (XmlReader reader = XmlReader.Create(path, settings))
                {
                    document.Load(reader);
                }

                return document;
            }
            catch (XmlException e)
            {
                string folder = Path.GetDirectoryName(path) ?? "";
                bool dtdPresent = File.Exists(Path.Combine(folder, "ematrixml.dtd"));

                throw new InvalidDataException(
                    "Could not read the export: " + e.Message +
                    (dtdPresent
                        ? ""
                        : "\r\n\r\nematrixml.dtd is not in the same folder as this file. An export " +
                          "refers to entities declared in it and cannot be read without it. Copy it " +
                          "from the XML folder of the ENOVIA installation that produced the export."));
            }
        }

        private EnoviaType ReadType(XmlElement element)
        {
            var type = new EnoviaType { Abstract = Has(element, "abstract") };
            ReadAdminProperties(element, type);

            XmlElement derived = Child(element, "derivedFrom");
            if (derived != null) type.DerivedFrom = FirstRef(derived, "typeRefList");

            AddRefs(element, "attributeDefRefList", "attributeDefRef", type.AttributeNames);
            AddRefs(element, "methodList", "programRef", type.Methods);
            return type;
        }

        private EnoviaAttribute ReadAttribute(XmlElement element)
        {
            var attribute = new EnoviaAttribute
            {
                PrimitiveType = Text(element, "primitiveType"),
                DefaultValue = Text(element, "defaultValue"),
                MaxLength = Text(element, "maxlength"),
                Multiline = Has(element, "multiline"),
                ResetOnClone = Has(element, "resetonclone"),
                ResetOnRevision = Has(element, "resetonrevision")
            };

            ReadAdminProperties(element, attribute);

            XmlElement ranges = Child(element, "rangeList");
            if (ranges != null)
            {
                foreach (XmlElement range in Children(ranges, "range"))
                {
                    attribute.Ranges.Add(new EnoviaRange
                    {
                        RangeType = Text(range, "rangeType"),
                        Value = Text(range, "rangeValue")
                    });
                }
            }

            return attribute;
        }

        private EnoviaRelationship ReadRelationship(XmlElement element)
        {
            var relationship = new EnoviaRelationship
            {
                Abstract = Has(element, "abstract"),
                PreventDuplicates = Has(element, "preventDuplicates")
            };

            ReadAdminProperties(element, relationship);

            XmlElement derived = Child(element, "derivedFromRelationship");
            if (derived != null) relationship.DerivedFrom = FirstRef(derived, "relationshipDefRefList");

            ReadEnd(Child(element, "fromSide"), relationship.From);
            ReadEnd(Child(element, "toSide"), relationship.To);
            AddRefs(element, "attributeDefRefList", "attributeDefRef", relationship.AttributeNames);
            return relationship;
        }

        private static void ReadEnd(XmlElement side, EnoviaRelationshipEnd end)
        {
            if (side == null) return;

            end.Meaning = Text(side, "meaning");
            end.Cardinality = Text(side, "cardinality");
            end.RevisionAction = Text(side, "revisionAction");
            end.CloneAction = Text(side, "cloneAction");
            end.PropagateModify = Has(side, "propagateModify");
            end.PropagateConnection = Has(side, "propagateConnection");
            end.AllowAllTypes = Has(side, "allowAllTypes");

            AddRefs(side, "typeRefList", "typeRef", end.Types);
        }

        private EnoviaPolicy ReadPolicy(XmlElement element)
        {
            var policy = new EnoviaPolicy
            {
                Sequence = Text(element, "sequence"),
                MajorSequence = Text(element, "majorsequence"),
                AllowAllTypes = Has(element, "allowAllTypes")
            };

            ReadAdminProperties(element, policy);

            XmlElement store = Child(element, "storeRef");
            if (store != null) policy.Store = store.InnerText.Trim();

            XmlElement defaultFormat = Child(element, "defaultFormat");
            if (defaultFormat != null) policy.DefaultFormat = FirstRef(defaultFormat, null);

            AddRefs(element, "typeRefList", "typeRef", policy.Types);
            AddRefs(element, "formatRefList", "formatRef", policy.Formats);

            XmlElement states = Child(element, "stateDefList");
            if (states != null)
            {
                // In file order, which is lifecycle order.
                foreach (XmlElement state in Children(states, "stateDef"))
                {
                    policy.States.Add(new EnoviaState
                    {
                        Name = Text(state, "name"),
                        Revisionable = Has(state, "revisionable"),
                        MajorRevisionable = Has(state, "majorrevisionable"),
                        Versionable = Has(state, "versionable"),
                        Published = Has(state, "published"),
                        AutoPromotion = Has(state, "autoPromotion")
                    });
                }
            }

            return policy;
        }

        private EnoviaRole ReadRole(XmlElement element)
        {
            var role = new EnoviaRole();
            ReadAdminProperties(element, role);
            return role;
        }

        private static void ReadAdminProperties(XmlElement element, EnoviaObject target)
        {
            target.Id = element.GetAttribute("id");

            XmlElement admin = Child(element, "adminProperties");
            if (admin == null) return;

            target.Name = Text(admin, "name");
            target.Description = Text(admin, "description");

            XmlElement properties = Child(admin, "propertyList");
            if (properties == null) return;

            foreach (XmlElement property in Children(properties, "property"))
            {
                string name = Text(property, "name");
                if (name.Length > 0) target.Properties[name] = Text(property, "value");
            }
        }

        private static void Skip(EnoviaExport export, string name)
        {
            if (Expected.Contains(name)) return;

            export.Skipped.TryGetValue(name, out int count);
            export.Skipped[name] = count + 1;
        }

        // A *RefList holding one kind of child, flattened to the names inside. Passing a
        // null list name reads the refs directly off the given element.
        private static void AddRefs(XmlElement parent, string listName, string refName, List<string> into)
        {
            XmlElement list = Child(parent, listName);
            if (list == null) return;

            foreach (XmlElement reference in Children(list, refName))
            {
                string value = reference.InnerText.Trim();
                if (value.Length > 0) into.Add(value);
            }
        }

        private static string FirstRef(XmlElement parent, string listName)
        {
            XmlElement scope = listName == null ? parent : Child(parent, listName);
            if (scope == null) return "";

            foreach (XmlNode node in scope.ChildNodes)
            {
                XmlElement element = node as XmlElement;
                if (element != null) return element.InnerText.Trim();
            }

            return "";
        }

        private static XmlElement Child(XmlElement parent, string name)
        {
            if (parent == null) return null;

            foreach (XmlNode node in parent.ChildNodes)
            {
                XmlElement element = node as XmlElement;
                if (element != null && element.Name == name) return element;
            }

            return null;
        }

        private static IEnumerable<XmlElement> Children(XmlElement parent, string name)
        {
            if (parent == null) yield break;

            foreach (XmlNode node in parent.ChildNodes)
            {
                XmlElement element = node as XmlElement;
                if (element != null && element.Name == name) yield return element;
            }
        }

        // Direct children only. A nested <name> inside a propertyList must never be
        // mistaken for the object's own name, which is exactly what a descendant search
        // would do.
        private static string Text(XmlElement parent, string name)
        {
            XmlElement element = Child(parent, name);
            return element == null ? "" : element.InnerText.Trim();
        }

        private static bool Has(XmlElement parent, string name)
        {
            return Child(parent, name) != null;
        }
    }
}
