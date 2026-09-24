using System.Collections.Generic;

namespace Dastan.org.ed.ea.services.validation
{
    // MQL has no escape mechanism. A value can only be carried by delimiting it with the
    // quote character it does not contain, which works for free text and not at all for a
    // name -- MQL rejects a quote in any name, type, policy, vault or revision. Catching
    // it here means fixing one field instead of re-running an export.
    public class QuoteInIdentifierRule : IModelRule
    {
        public IEnumerable<ModelIssue> Check(IReadOnlyList<ModelObject> objects)
        {
            foreach (ModelObject obj in objects)
            {
                if (!obj.Generates) continue;

                foreach (ModelField field in obj.Fields)
                {
                    if (field.Kind == FieldKind.Number) continue;
                    if (field.Value.IndexOf('"') < 0 && field.Value.IndexOf('\'') < 0) continue;

                    yield return new ModelIssue(IssueSeverity.Error, "quote-in-identifier",
                        field.Name + " contains a quote character, which MQL rejects in a name, type, " +
                        "store or revision: " + field.Value, obj);
                }
            }
        }
    }
}
