using System.Collections.Generic;

namespace Dastan.org.ed.ea.services.validation
{
    // Every generator opens by testing the stereotype and returning when it does not
    // match, so something that was never given one produces nothing at all and says
    // nothing about why. That is the hardest kind of export problem to work out from the
    // output, because there is no output.
    //
    // The collector has already thrown away the notes and boundaries, and keeps an
    // element that generates nothing only when a generator should have handled it. So
    // anything that arrives here without a stereotype is missing one.
    public class MissingStereotypeRule : IModelRule
    {
        public IEnumerable<ModelIssue> Check(IReadOnlyList<ModelObject> objects)
        {
            foreach (ModelObject obj in objects)
            {
                if (obj.Generates) continue;

                // A different stereotype is a different problem, and not one this rule
                // can name.
                if (obj.Stereotype.Length > 0) continue;

                yield return new ModelIssue(IssueSeverity.Warning, "missing-stereotype",
                    obj.Kind == ModelObject.AttributeKind
                        ? "Has no stereotype, so the attribute generator skips it and the type it belongs " +
                          "to is written without it"
                        : "Has no stereotype, so every generator skips it and nothing is written for it",
                    obj);
            }
        }
    }
}
