using System.Collections.Generic;

namespace Dastan.org.ed.ea.services.validation
{
    public interface IModelRule
    {
        IEnumerable<ModelIssue> Check(IReadOnlyList<ModelObject> objects);
    }
}
