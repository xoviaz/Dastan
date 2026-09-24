using System.Collections.Generic;

namespace Dastan.org.ed.ea.services.validation
{
    public class MissingNameRule : IModelRule
    {
        public IEnumerable<ModelIssue> Check(IReadOnlyList<ModelObject> objects)
        {
            foreach (ModelObject obj in objects)
            {
                if (!obj.Generates) continue;

                if (!string.IsNullOrWhiteSpace(obj.Name)) continue;

                yield return new ModelIssue(IssueSeverity.Error, "missing-name",
                    "Has no name, so it is either skipped or written into the script as an empty one", obj);
            }
        }
    }
}
