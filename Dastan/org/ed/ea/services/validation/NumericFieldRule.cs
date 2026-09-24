using System.Collections.Generic;

namespace Dastan.org.ed.ea.services.validation
{
    // A value the script uses as a number -- "maxlength 128" -- goes in exactly as it was
    // typed, so anything that is not a whole number becomes a malformed statement.
    public class NumericFieldRule : IModelRule
    {
        public IEnumerable<ModelIssue> Check(IReadOnlyList<ModelObject> objects)
        {
            foreach (ModelObject obj in objects)
            {
                if (!obj.Generates) continue;

                foreach (ModelField field in obj.Fields)
                {
                    if (field.Kind != FieldKind.Number) continue;
                    if (field.Value.Length == 0) continue;

                    if (int.TryParse(field.Value.Trim(), out int number) && number >= 0) continue;

                    yield return new ModelIssue(IssueSeverity.Error, "not-a-number",
                        field.Name + " has to be a whole number, and is: " + field.Value, obj);
                }
            }
        }
    }
}
