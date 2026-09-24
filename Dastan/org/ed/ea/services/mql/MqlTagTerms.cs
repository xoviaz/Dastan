using System;
using System.Collections.Generic;
using Dastan.org.ed.ea.constants;

namespace Dastan.org.ed.ea.services.mql
{
    public class MqlTagTerms
    {
        private static readonly Dictionary<string, List<IMqlTagTerm>> Terms =
            new Dictionary<string, List<IMqlTagTerm>>(StringComparer.OrdinalIgnoreCase)
            {
                {
                    Stereotypes.Attribute, new List<IMqlTagTerm>
                    {
                        new FlagMqlTagTerm("Multi Line", "multiline", "notmultiline"),
                        new FlagMqlTagTerm("Multi Value", "multivalue", "notmultivalue"),
                        new ValueMqlTagTerm("Max Length", "maxlength", quoted: false),
                        new FlagMqlTagTerm("Reset On Clone", "resetonclone", "notresetonclone"),
                        new FlagMqlTagTerm("Reset On Revision", "resetonrevision", "notresetonrevision"),
                        new RangeMqlTagTerm("Allowed Values", "range"),
                        new LogOnlyMqlTagTerm("stereotype")
                    }
                },
                {
                    Stereotypes.Policy, new List<IMqlTagTerm>
                    {
                        new ValueMqlTagTerm("Store", "store", quoted: true),
                        new ValueMqlTagTerm("Default Format", "defaultformat", quoted: true),
                        new ValueMqlTagTerm("Sequence", "minorsequence", quoted: true),
                        new LogOnlyMqlTagTerm("stereotype"),
                        // Lists: changed by adding and removing entries, not by restating
                        // the whole thing.
                        new LogOnlyMqlTagTerm("Type"),
                        new LogOnlyMqlTagTerm("Format"),

                        // Goes to the .properties file, never to a statement.
                        new LogOnlyMqlTagTerm("Display Name")
                    }
                },
                {
                    Stereotypes.Relationship, new List<IMqlTagTerm>
                    {
                        new FlagMqlTagTerm("Prevent Duplicate", "preventduplicates", "notpreventduplicates"),

                        // These belong to one end of the relationship, so changing one
                        // needs a from/to qualifier that no statement in this codebase has
                        // ever emitted. Logged until the syntax is confirmed.
                        new LogOnlyMqlTagTerm("From Clone"),
                        new LogOnlyMqlTagTerm("From Revision"),
                        new LogOnlyMqlTagTerm("From Propagate connection"),
                        new LogOnlyMqlTagTerm("From Propagate modify"),
                        new LogOnlyMqlTagTerm("To Clone"),
                        new LogOnlyMqlTagTerm("To Revision"),
                        new LogOnlyMqlTagTerm("To Propagate connection"),
                        new LogOnlyMqlTagTerm("To Propagate modify"),
                        new LogOnlyMqlTagTerm("stereotype")
                    }
                }
            };
        
        private static readonly List<IMqlTagTerm> None =  new List<IMqlTagTerm>();

        public static IReadOnlyList<IMqlTagTerm> For(string stereotype)
        {
            if (stereotype == null) return None;
            
            return Terms.TryGetValue(stereotype, out List<IMqlTagTerm> terms) ? terms : None;
        }

        public static IMqlTagTerm Find(string stereotype, string tagName)
        {
            if (string.IsNullOrEmpty(tagName)) return null;

            foreach (IMqlTagTerm term in For(stereotype))
            {
                if (string.Equals(term.TagName, tagName, StringComparison.OrdinalIgnoreCase)) return term;
            }

            return null;
        }
    }
}