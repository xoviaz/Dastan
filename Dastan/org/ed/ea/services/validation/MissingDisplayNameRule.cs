using System.Collections.Generic;

namespace Dastan.org.ed.ea.services.validation
{
    // Types, roles, attributes, relationships and policies each write a line into the
    // .properties file. With nothing on the right of the '=' the object shows up in the
    // UI under its internal name.
    public class MissingDisplayNameRule : IModelRule
    {
        public IEnumerable<ModelIssue> Check(IReadOnlyList<ModelObject> objects)
        {
            foreach (ModelObject obj in objects)
            {
                if (obj.DisplayName == null) continue;
                if (obj.DisplayName.Trim().Length > 0) continue;

                yield return new ModelIssue(IssueSeverity.Warning, "missing-display-name",
                    "Has no display name, so its string-resource line ends at the '=' and the UI " +
                    "falls back to the internal name", obj);
            }
        }
    }
}
