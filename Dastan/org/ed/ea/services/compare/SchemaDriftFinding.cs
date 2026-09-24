using System.Collections.Generic;

namespace Dastan.org.ed.ea.services.compare
{
    public enum SchemaDriftKind
    {
        // In the export and not in the model. What reverse engineering would create.
        OnlyInExport,

        // Modelled but absent from the target system. Either not installed yet, or
        // deleted there behind the model's back.
        OnlyInModel,

        // Present on both sides, saying different things.
        Different
    }

    // One line of a comparison: an object that is missing on one side, or one field of one
    // object that the two sides disagree about.
    //
    // A SchemaComparisonResult groups by object, which is the right shape for applying a
    // change but the wrong shape for reading one -- an object with four differences is
    // four things to look at. Flattening lives here rather than in the window so the
    // output tab lists exactly what the window does.
    public class SchemaDriftFinding
    {
        public SchemaDriftKind Kind { get; }
        public string ObjectKind { get; }
        public string Name { get; }

        // Empty when the whole object is missing: there is no one field to blame.
        public string Field { get; }

        public string InExport { get; }
        public string InModel { get; }
        public string ElementGuid { get; }

        private SchemaDriftFinding(SchemaDriftKind kind, string objectKind, string name, string field,
            string inExport, string inModel, string elementGuid)
        {
            Kind = kind;
            ObjectKind = objectKind ?? "";
            Name = name ?? "";
            Field = field ?? "";
            InExport = inExport ?? "";
            InModel = inModel ?? "";
            ElementGuid = elementGuid ?? "";
        }

        public static List<SchemaDriftFinding> From(SchemaComparisonResult result)
        {
            var findings = new List<SchemaDriftFinding>();
            if (result == null) return findings;

            foreach (SchemaItem item in result.OnlyInExport)
            {
                findings.Add(new SchemaDriftFinding(SchemaDriftKind.OnlyInExport, item.Kind, item.Name, "",
                    "present", "", item.ElementGuid));
            }

            foreach (SchemaItem item in result.OnlyInModel)
            {
                findings.Add(new SchemaDriftFinding(SchemaDriftKind.OnlyInModel, item.Kind, item.Name, "",
                    "", "present", item.ElementGuid));
            }

            foreach (SchemaMismatch mismatch in result.Mismatched)
            {
                foreach (SchemaDifference difference in mismatch.Differences)
                {
                    findings.Add(new SchemaDriftFinding(SchemaDriftKind.Different, mismatch.Export.Kind,
                        mismatch.Export.Name, difference.Field, difference.InExport, difference.InModel,
                        mismatch.Model.ElementGuid));
                }
            }

            return findings;
        }

        public string Label
        {
            get
            {
                switch (Kind)
                {
                    case SchemaDriftKind.OnlyInExport: return "Only in ENOVIA";
                    case SchemaDriftKind.OnlyInModel: return "Only in the model";
                    default: return "Different";
                }
            }
        }

        public override string ToString()
        {
            string what = ObjectKind + " " + Name;

            switch (Kind)
            {
                case SchemaDriftKind.OnlyInExport:
                    return what + " is in the export but not in the model";
                case SchemaDriftKind.OnlyInModel:
                    return what + " is in the model but not in the export";
                default:
                    return what + " -- " + Field + ": export '" + InExport + "' vs model '" + InModel + "'";
            }
        }
    }
}
