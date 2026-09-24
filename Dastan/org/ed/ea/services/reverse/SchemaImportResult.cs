using System.Collections.Generic;

namespace Dastan.org.ed.ea.services.reverse
{
    // What an import actually did, and what it could not do.
    //
    // The second half matters more than the first. An import that creates 40 objects and
    // quietly drops 3 leaves a model that looks complete and is not, and the next
    // comparison is the only thing that would ever say so.
    public class SchemaImportResult
    {
        public int Types { get; set; }
        public int Attributes { get; set; }
        public int Relationships { get; set; }
        public int Policies { get; set; }
        public int States { get; set; }
        public int Roles { get; set; }

        // One line per thing that was not created, saying why.
        public List<string> Skipped { get; } = new List<string>();

        public int Created
        {
            get { return Types + Attributes + Relationships + Policies + States + Roles; }
        }

        public void Skip(string what, string reason)
        {
            Skipped.Add(what + " -- " + reason);
        }

        public string Summary()
        {
            var parts = new List<string>();

            Add(parts, Types, "type");
            Add(parts, Attributes, "attribute");
            Add(parts, Relationships, "relationship");
            Add(parts, Policies, "policy", "policies");
            Add(parts, States, "state");
            Add(parts, Roles, "role");

            return parts.Count == 0 ? "Nothing was created." : string.Join(", ", parts.ToArray());
        }

        private static void Add(List<string> parts, int count, string singular, string plural = null)
        {
            if (count == 0) return;

            parts.Add(count + " " + (count == 1 ? singular : plural ?? singular + "s"));
        }
    }
}