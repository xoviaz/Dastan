using System;
using System.Collections.Generic;

namespace Dastan.org.ed.ea.services.lint
{
    public class UnterminatedStatementRule : IScriptRule
    {
        public IEnumerable<ScriptFinding> Check(IReadOnlyList<ScriptStatement> statements)
        {
            foreach (ScriptStatement statement in statements)
            {
                if (statement.Terminated) continue;

                yield return new ScriptFinding("unterminated",
                    "Statement does not end with ';', so it runs into whatever follows", statement);
            }
        }
    }
}