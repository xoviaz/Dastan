using System;
using System.Collections.Generic;

namespace Dastan.org.ed.ea.services.lint
{
    public class QuoteInNameRule : IScriptRule
    {
        public IEnumerable<ScriptFinding> Check(IReadOnlyList<ScriptStatement> statements)
        {
            foreach (ScriptStatement statement in statements)
            {
                foreach (string value in statement.IdentifierValues)
                {
                    if (value.IndexOf('"') < 0 && value.IndexOf('\'') < 0) continue;

                    yield return new ScriptFinding("quote-in-name", "Object type or name contains a quote", statement);
                    break;
                }
            }
        }
    }
}