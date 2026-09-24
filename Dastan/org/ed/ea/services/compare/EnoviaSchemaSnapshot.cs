using System.Collections.Generic;
using Dastan.org.ed.ea.services.enovia;

namespace Dastan.org.ed.ea.services.compare
{
    // Reduces an ENOVIA export to SchemaItems, so it can be compared with the model.
    //
    // Everything that translates between ENOVIA's vocabulary and the field names the
    // comparison uses lives here. The comparer stays ignorant of both, and the model side
    // has its own translation that has to agree with this one -- that agreement is the
    // whole contract, and the round-trip test is what proves it.
    public class EnoviaSchemaSnapshot
    {
        public List<SchemaItem> Build(EnoviaExport export)
        {
            var items = new List<SchemaItem>();
            if (export == null) return items;

            foreach (EnoviaType type in export.Types)
            {
                items.Add(new SchemaItem(SchemaItem.TypeKind, type.Name, new Dictionary<string, string>
                {
                    { "name", type.Name },
                    { "description", type.Description },
                    { "abstract", SchemaItem.Flag(type.Abstract) },
                    { "derivedFrom", type.DerivedFrom },
                    // Order is not meaningful, so it is sorted: the same attributes listed
                    // differently must not read as drift.
                    { "attributes", SchemaItem.List(type.AttributeNames, ordered: false) }
                }));
            }

            foreach (EnoviaAttribute attribute in export.Attributes)
            {
                items.Add(new SchemaItem(SchemaItem.AttributeKind, attribute.Name, new Dictionary<string, string>
                {
                    { "name", attribute.Name },
                    { "description", attribute.Description },
                    { "primitiveType", attribute.PrimitiveType },
                    { "default", attribute.DefaultValue },
                    // ENOVIA omits maxlength when it is unlimited; MQL writes that as 0.
                    // Normalised here so an unlimited attribute does not read as drift
                    // against a model that says 0.
                    { "maxlength", attribute.MaxLength.Length == 0 ? "0" : attribute.MaxLength },
                    { "multiline", SchemaItem.Flag(attribute.Multiline) },
                    { "resetOnClone", SchemaItem.Flag(attribute.ResetOnClone) },
                    { "resetOnRevision", SchemaItem.Flag(attribute.ResetOnRevision) },
                    { "ranges", Ranges(attribute) }
                }));
            }

            foreach (EnoviaRelationship relationship in export.Relationships)
            {
                var values = new Dictionary<string, string>
                {
                    { "name", relationship.Name },
                    { "description", relationship.Description },
                    { "abstract", SchemaItem.Flag(relationship.Abstract) },
                    { "preventDuplicates", SchemaItem.Flag(relationship.PreventDuplicates) },
                    { "derivedFrom", relationship.DerivedFrom },
                    { "attributes", SchemaItem.List(relationship.AttributeNames, ordered: false) }
                };

                AddEnd(values, "from", relationship.From);
                AddEnd(values, "to", relationship.To);
                items.Add(new SchemaItem(SchemaItem.RelationshipKind, relationship.Name, values));
            }

            foreach (EnoviaPolicy policy in export.Policies)
            {
                var states = new List<string>();
                foreach (EnoviaState state in policy.States) states.Add(state.Name);

                items.Add(new SchemaItem(SchemaItem.PolicyKind, policy.Name, new Dictionary<string, string>
                {
                    { "name", policy.Name },
                    { "description", policy.Description },
                    { "store", policy.Store },
                    { "sequence", policy.Sequence },
                    { "defaultFormat", policy.DefaultFormat },
                    { "types", SchemaItem.List(policy.Types, ordered: false) },
                    { "formats", SchemaItem.List(policy.Formats, ordered: false) },
                    // Ordered: a policy's states are a lifecycle, and the same states in a
                    // different order are a different policy.
                    { "states", SchemaItem.List(states, ordered: true) }
                }));
            }

            foreach (EnoviaRole role in export.Roles)
            {
                items.Add(new SchemaItem(SchemaItem.RoleKind, role.Name, new Dictionary<string, string>
                {
                    { "name", role.Name },
                    { "description", role.Description }
                }));
            }

            return items;
        }

        private static void AddEnd(Dictionary<string, string> values, string prefix, EnoviaRelationshipEnd end)
        {
            values[prefix + "Cardinality"] = SchemaItem.Cardinality(end.Cardinality);
            values[prefix + "Revision"] = end.RevisionAction;
            values[prefix + "Clone"] = end.CloneAction;
            values[prefix + "PropagateConnection"] = SchemaItem.Flag(end.PropagateConnection);
            values[prefix + "PropagateModify"] = SchemaItem.Flag(end.PropagateModify);

            // allowAllTypes is its own flag and means something quite different from a
            // list that happens to name everything, so it is kept as a word rather than
            // being flattened into the list.
            values[prefix + "Types"] = end.AllowAllTypes ? "all" : SchemaItem.List(end.Types, ordered: false);
        }

        // ENOVIA states a range as a word -- equal, notequal, lessthan -- where MQL wants
        // an operator. The model side has to say it the same way round for these to
        // compare, so the export's wording is what both agree on and translating to MQL
        // stays the writer's job.
        private static string Ranges(EnoviaAttribute attribute)
        {
            var ranges = new List<string>();
            foreach (EnoviaRange range in attribute.Ranges) ranges.Add(range.ToString());

            return SchemaItem.List(ranges, ordered: false);
        }
    }
}
