using System;
using System.Collections.Generic;
using Dastan.org.ed.ea.services.compare;
using Dastan.org.ed.ea.services.enovia;

namespace Dastan.org.ed.ea.services.reverse
{
    // What an import would create: the part of an export the model does not have.
    //
    // Built from a comparison rather than from the export alone, so importing twice
    // creates nothing the second time. The comparison is what knows the model.
    public class SchemaImportPlan
    {
        public List<EnoviaType> Types { get; } = new List<EnoviaType>();
        public List<EnoviaRelationship> Relationships { get; } = new List<EnoviaRelationship>();
        public List<EnoviaPolicy> Policies { get; } = new List<EnoviaPolicy>();
        public List<EnoviaRole> Roles { get; } = new List<EnoviaRole>();

        // Every attribute definition in the export, by name -- not only the missing ones.
        //
        // An EA attribute belongs to the class that carries it, so a type being created
        // needs the definition of every attribute it names, including ones that already
        // exist elsewhere in the model.
        public Dictionary<string, EnoviaAttribute> Attributes { get; } =
            new Dictionary<string, EnoviaAttribute>(StringComparer.OrdinalIgnoreCase);

        // Attributes the model does not have that nothing being created carries. There is
        // nowhere to put them: the model has no standalone attribute, only attributes on a
        // type or a relationship. Reported rather than silently dropped.
        public List<string> Unplaced { get; } = new List<string>();

        public int Count
        {
            get { return Types.Count + Relationships.Count + Policies.Count + Roles.Count; }
        }

        public bool IsEmpty
        {
            get { return Count == 0; }
        }

        public static SchemaImportPlan From(EnoviaExport export, SchemaComparisonResult result)
        {
            var plan = new SchemaImportPlan();
            if (export == null || result == null) return plan;

            foreach (EnoviaAttribute attribute in export.Attributes)
            {
                if (attribute.Name.Length > 0) plan.Attributes[attribute.Name] = attribute;
            }

            HashSet<string> missing = MissingNames(result);

            foreach (EnoviaType type in export.Types)
            {
                if (missing.Contains(Key(SchemaItem.TypeKind, type.Name))) plan.Types.Add(type);
            }

            foreach (EnoviaRelationship relationship in export.Relationships)
            {
                if (missing.Contains(Key(SchemaItem.RelationshipKind, relationship.Name)))
                    plan.Relationships.Add(relationship);
            }

            foreach (EnoviaPolicy policy in export.Policies)
            {
                if (missing.Contains(Key(SchemaItem.PolicyKind, policy.Name))) plan.Policies.Add(policy);
            }

            foreach (EnoviaRole role in export.Roles)
            {
                if (missing.Contains(Key(SchemaItem.RoleKind, role.Name))) plan.Roles.Add(role);
            }

            plan.FindUnplaced(export, missing);
            return plan;
        }

        // A missing attribute is only created as part of the type or relationship that
        // carries it. One whose owner the model already has would mean changing that
        // owner, which an import that only ever creates does not do.
        private void FindUnplaced(EnoviaExport export, HashSet<string> missing)
        {
            var carried = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (EnoviaType type in Types)
            {
                foreach (string name in type.AttributeNames) carried.Add(name);
            }

            foreach (EnoviaRelationship relationship in Relationships)
            {
                foreach (string name in relationship.AttributeNames) carried.Add(name);
            }

            foreach (EnoviaAttribute attribute in export.Attributes)
            {
                if (!missing.Contains(Key(SchemaItem.AttributeKind, attribute.Name))) continue;
                if (carried.Contains(attribute.Name)) continue;

                Unplaced.Add(attribute.Name);
            }
        }

        private static HashSet<string> MissingNames(SchemaComparisonResult result)
        {
            var missing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (SchemaItem item in result.OnlyInExport) missing.Add(Key(item.Kind, item.Name));

            return missing;
        }

        private static string Key(string kind, string name)
        {
            return kind + "\n" + name;
        }
    }
}
