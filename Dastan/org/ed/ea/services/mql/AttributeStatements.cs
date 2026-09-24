using System.Collections.Generic;
using Dastan.org.ed.ea.constants;
using Dastan.org.ed.ea.util;
using Attribute = EA.Attribute;

namespace Dastan.org.ed.ea.services.mql
{
    // How an attribute is written into MQL.
    //
    // This used to sit inside AttributeScriptGenerator, which was fine while a full schema
    // export was the only thing that created attributes. It no longer is: attaching a new
    // attribute to a type produces "modify type X add attribute Y", and MQL cannot attach
    // a Y that does not exist yet, so the tracking path needs the very same definition the
    // export would have written.
    public static class AttributeStatements
    {
        private const string AddTemplate = "add attribute {0} type {1} description {2} default {3} {4};";

        public static string Add(Attribute attribute)
        {
            return string.Format(AddTemplate,
                ScriptEscapeUtility.Quote(attribute.Name),
                ScriptEscapeUtility.Quote(attribute.Type),
                ScriptEscapeUtility.Quote(attribute.Notes ?? ""),
                ScriptEscapeUtility.Quote(attribute.Default),
                TagClauses(attribute));
        }

        // The clauses the attribute's tagged values contribute, in the order MQL expects
        // them. What each tag turns into lives in MqlTagTerms, so the tracker writing
        // "modify attribute X multiline;" reads the same translation this statement is
        // built from.
        public static string TagClauses(Attribute attribute)
        {
            var clauses = new List<string>();

            foreach (IMqlTagTerm term in MqlTagTerms.For(Stereotypes.Attribute))
            {
                string clause = term.ForAdd(TagUtility.GetSafeTagValue(attribute.TaggedValues, term.TagName, "", true));
                if (clause.Length > 0) clauses.Add(clause);
            }

            return string.Join(" ", clauses.ToArray());
        }
    }
}