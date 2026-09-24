using System;
using System.Collections.Generic;

namespace Dastan.org.ed.ea.services.validation
{
    // Two things that share a name are a problem in two different ways, so they are not
    // one check.
    //
    // Elements are de-duplicated by ID, so two type elements both named CW_Widget produce
    // two 'add type "CW_Widget"' statements and the second one fails outright.
    //
    // Attributes are de-duplicated by name -- the first one wins and the rest are dropped
    // silently. Reusing one attribute across several types is how the model is meant to
    // work, so that is only worth reporting when the copies disagree about what the
    // attribute actually is, because then the export quietly picks one of them.
    public class DuplicateNameRule : IModelRule
    {
        public IEnumerable<ModelIssue> Check(IReadOnlyList<ModelObject> objects)
        {
            var issues = new List<ModelIssue>();

            foreach (var group in GroupByName(objects))
            {
                if (group.Value.Count < 2) continue;

                ModelObject first = group.Value[0];
                for (int i = 1; i < group.Value.Count; i++)
                {
                    ModelObject other = group.Value[i];

                    if (other.Kind == ModelObject.AttributeKind)
                    {
                        if (other.Signature() == first.Signature()) continue;

                        issues.Add(new ModelIssue(IssueSeverity.Error, "conflicting-duplicate",
                            "Another attribute named " + other.Display + " is defined differently in " +
                            Where(first) + ". Only the first one reached is written, so one of the two " +
                            "definitions is silently dropped", other));
                        continue;
                    }

                    issues.Add(new ModelIssue(IssueSeverity.Error, "duplicate-name",
                        "A " + other.Label.ToLowerInvariant() + " named " + other.Display + " already exists in " +
                        Where(first) + ", so the script creates it twice and the second add fails", other));
                }
            }

            return issues;
        }

        // Elements collide within their own stereotype and their own naming scope;
        // attributes collide by name alone, because that is the key the generator itself
        // de-duplicates on.
        private static Dictionary<string, List<ModelObject>> GroupByName(IReadOnlyList<ModelObject> objects)
        {
            var groups = new Dictionary<string, List<ModelObject>>(StringComparer.OrdinalIgnoreCase);

            foreach (ModelObject obj in objects)
            {
                if (!obj.Generates || obj.Name.Length == 0) continue;

                string key = obj.Kind == ModelObject.AttributeKind
                    ? ModelObject.AttributeKind + "\n" + obj.Name
                    : obj.UniqueWithin + "\n" + obj.Kind + "\n" + obj.Stereotype + "\n" + obj.Name;

                if (!groups.TryGetValue(key, out List<ModelObject> group))
                {
                    group = new List<ModelObject>();
                    groups[key] = group;
                }

                group.Add(obj);
            }

            return groups;
        }

        private static string Where(ModelObject obj)
        {
            return obj.Path.Length > 0 ? obj.Path : "the same scope";
        }
    }
}
