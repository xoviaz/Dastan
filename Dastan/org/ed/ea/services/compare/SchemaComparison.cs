using System;
using System.Collections.Generic;

namespace Dastan.org.ed.ea.services.compare
{
    public class SchemaDifference
    {
        public string Field { get; }
        public string InExport { get; }
        public string InModel { get; }

        public SchemaDifference(string field, string inExport, string inModel)
        {
            Field = field;
            InExport = inExport ?? "";
            InModel = inModel ?? "";
        }

        public override string ToString()
        {
            return Field + ": export '" + InExport + "' vs model '" + InModel + "'";
        }
    }

    public class SchemaMismatch
    {
        public SchemaItem Export { get; }
        public SchemaItem Model { get; }
        public List<SchemaDifference> Differences { get; } = new List<SchemaDifference>();

        public SchemaMismatch(SchemaItem export, SchemaItem model)
        {
            Export = export;
            Model = model;
        }
    }

    public class SchemaComparisonResult
    {
        // In the ENOVIA export and not in the model. What reverse engineering would
        // create.
        public List<SchemaItem> OnlyInExport { get; } = new List<SchemaItem>();

        // Modelled but absent from the target system. Either not yet installed, or
        // deleted there behind the model's back.
        public List<SchemaItem> OnlyInModel { get; } = new List<SchemaItem>();

        public List<SchemaMismatch> Mismatched { get; } = new List<SchemaMismatch>();

        public int Matched { get; set; }

        public bool InStep
        {
            get { return OnlyInExport.Count == 0 && OnlyInModel.Count == 0 && Mismatched.Count == 0; }
        }
    }

    // Lines an ENOVIA export up against the EA model and says where they disagree.
    //
    // Knows nothing about XML or about EA -- it compares two lists of SchemaItem, which
    // is what makes it the shared half of drift detection and reverse engineering. Drift
    // detection shows the result; reverse engineering applies the OnlyInExport side.
    public class SchemaComparison
    {
        // Only fields both sides genuinely carry. Anything else would report as a
        // difference on every single object and drown the real ones.
        //
        // Multi Value is the case in point and deliberately absent: the DTD has no
        // multivalue element for attributeDef, only an attrValueType integer whose
        // encoding is not documented here. Until an export shows what it holds, comparing
        // it would mean comparing a value ENOVIA never stated.
        private static readonly Dictionary<string, string[]> Comparable =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                { SchemaItem.TypeKind, new[] { "name", "description", "abstract", "derivedFrom", "attributes" } },
                { SchemaItem.AttributeKind, new[] { "name", "description", "primitiveType", "default", "maxlength", "multiline", "resetOnClone", "resetOnRevision", "ranges" } },
                { SchemaItem.RelationshipKind, new[] { "name", "description", "abstract", "preventDuplicates", "derivedFrom", "fromCardinality", "fromTypes", "fromRevision", "fromClone", "fromPropagateConnection", "fromPropagateModify", "toCardinality", "toTypes", "toRevision", "toClone", "toPropagateConnection", "toPropagateModify", "attributes" } },
                { SchemaItem.PolicyKind, new[] { "name", "description", "store", "sequence", "defaultFormat", "types", "formats", "states" } },
                { SchemaItem.RoleKind, new[] { "name", "description" } }
            };

        public SchemaComparisonResult Compare(IReadOnlyList<SchemaItem> export, IReadOnlyList<SchemaItem> model)
        {
            var result = new SchemaComparisonResult();

            Dictionary<string, SchemaItem> modelByKey = ByKey(model);
            var matchedKeys = new HashSet<string>(StringComparer.Ordinal);

            foreach (SchemaItem exported in export ?? new List<SchemaItem>())
            {
                if (!modelByKey.TryGetValue(exported.Key, out SchemaItem modelled))
                {
                    result.OnlyInExport.Add(exported);
                    continue;
                }

                matchedKeys.Add(exported.Key);

                SchemaMismatch mismatch = Diff(exported, modelled);
                if (mismatch == null)
                    result.Matched++;
                else
                    result.Mismatched.Add(mismatch);
            }

            foreach (SchemaItem modelled in model ?? new List<SchemaItem>())
            {
                if (!matchedKeys.Contains(modelled.Key)) result.OnlyInModel.Add(modelled);
            }

            return result;
        }

        private static SchemaMismatch Diff(SchemaItem export, SchemaItem model)
        {
            if (!Comparable.TryGetValue(export.Kind, out string[] fields)) return null;

            var mismatch = new SchemaMismatch(export, model);

            foreach (string field in fields)
            {
                // A field neither side stated is not a difference. One side stating it and
                // the other not is, because the snapshots are supposed to produce the same
                // fields for the same kind -- a gap there is worth seeing.
                if (!export.Has(field) && !model.Has(field)) continue;

                string exported = Normalise(export.Value(field));
                string modelled = Normalise(model.Value(field));

                // Names are matched case-insensitively so the objects pair up at all, which
                // means a case difference reaches here rather than reading as two missing
                // objects. It is a real difference: MQL names are case-sensitive.
                bool same = field == "name"
                    ? string.Equals(exported, modelled, StringComparison.Ordinal)
                    : string.Equals(exported, modelled, StringComparison.OrdinalIgnoreCase);

                if (!same) mismatch.Differences.Add(new SchemaDifference(field, exported, modelled));
            }

            return mismatch.Differences.Count == 0 ? null : mismatch;
        }

        // Whitespace and line endings differ freely between a description typed into EA
        // and the same description round-tripped through XML, and that is never drift.
        private static string Normalise(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";

            string text = value.Replace("\r\n", "\n").Replace("\r", "\n").Trim();
            var builder = new System.Text.StringBuilder(text.Length);
            bool space = false;

            foreach (char c in text)
            {
                if (char.IsWhiteSpace(c))
                {
                    space = true;
                    continue;
                }

                if (space && builder.Length > 0) builder.Append(' ');
                space = false;
                builder.Append(c);
            }

            return builder.ToString();
        }

        private static Dictionary<string, SchemaItem> ByKey(IReadOnlyList<SchemaItem> items)
        {
            var map = new Dictionary<string, SchemaItem>(StringComparer.Ordinal);
            foreach (SchemaItem item in items ?? new List<SchemaItem>())
            {
                if (!map.ContainsKey(item.Key)) map[item.Key] = item;
            }

            return map;
        }
    }
}
