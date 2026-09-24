using System;
using System.Collections.Generic;
using System.Linq;

namespace Dastan.org.ed.ea.services.lint
{
    public class DuplicateObjectRule : IScriptRule
    {
        public IEnumerable<ScriptFinding> Check(IReadOnlyList<ScriptStatement> statements)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (ScriptStatement statement in statements)
            {
                string key = KeyOf(statement);
                if (key == null) continue;
                if (seen.Add(key)) continue;
                
                yield return new ScriptFinding("duplicate-add", "This object is created more than once in the script", statement);
            }
        }

        private static string KeyOf(ScriptStatement statement)
        {
            if (statement.Verb != "add") return null;

            if (statement.Subject == "property" || statement.Subject == "connection") return null;

            int take = statement.Subject == "bus" ? 3 : 2;
            if (statement.QuotedValues.Count < take) return null;
            return string.Join("\n", statement.QuotedValues.Take(take));
        }
    }
}