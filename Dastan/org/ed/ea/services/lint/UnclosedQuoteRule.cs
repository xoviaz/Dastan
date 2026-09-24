using System;
using System.Collections.Generic;

namespace Dastan.org.ed.ea.services.lint
{
    public class UnclosedQuoteRule : IScriptRule
    {
        public IEnumerable<ScriptFinding> Check(IReadOnlyList<ScriptStatement> statements)
        {
            foreach (ScriptStatement statement in statements)
            {
                if (!statement.HasUnclosedQuote) continue;

                yield return new ScriptFinding("unclosed-quote", "A quoted value is opened but never closed",
                    statement);
            }
        }
    }
}