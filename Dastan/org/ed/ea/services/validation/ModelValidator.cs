using System.Collections.Generic;
using System.Linq;

namespace Dastan.org.ed.ea.services.validation
{
    // The model-side counterpart of ScriptLinter: same shape, same one-rule-per-file
    // layout, checking the objects a generator would read instead of the text it
    // eventually produces.
    public class ModelValidator
    {
        private readonly List<IModelRule> _rules = new List<IModelRule>
        {
            new MissingNameRule(),
            new QuoteInIdentifierRule(),
            new UnquotedValueRule(),
            new NumericFieldRule(),
            new DuplicateNameRule(),
            new MissingStereotypeRule(),
            new MissingDisplayNameRule()
        };

        public List<ModelIssue> Validate(IReadOnlyList<ModelObject> objects)
        {
            var issues = new List<ModelIssue>();
            foreach (IModelRule rule in _rules)
                issues.AddRange(rule.Check(objects));

            // Errors first, so the things that will actually break the export are at the
            // top of the list. OrderBy keeps each rule's own order within a severity.
            return issues.OrderBy(i => i.Severity).ToList();
        }
    }
}
