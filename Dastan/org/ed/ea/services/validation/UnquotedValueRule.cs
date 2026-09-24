using System.Collections.Generic;

namespace Dastan.org.ed.ea.services.validation
{
    // Some values go into the statement bare -- an attribute's allowed values become
    // "range X" with nothing around X. A space there does not widen the value, it ends
    // it, and the rest becomes stray MQL that fails in a way that points nowhere useful.
    public class UnquotedValueRule : IModelRule
    {
        public IEnumerable<ModelIssue> Check(IReadOnlyList<ModelObject> objects)
        {
            foreach (ModelObject obj in objects)
            {
                if (!obj.Generates) continue;

                foreach (ModelField field in obj.Fields)
                {
                    if (field.Kind != FieldKind.Unquoted) continue;
                    if (field.Value.Length == 0) continue;
                    if (!HasWhitespace(field.Value)) continue;

                    yield return new ModelIssue(IssueSeverity.Error, "space-in-unquoted-value",
                        field.Name + " is written into the script without quotes, so the space in it " +
                        "ends the value early: " + field.Value, obj);
                }
            }
        }

        private static bool HasWhitespace(string value)
        {
            foreach (char c in value)
            {
                if (char.IsWhiteSpace(c)) return true;
            }

            return false;
        }
    }
}
