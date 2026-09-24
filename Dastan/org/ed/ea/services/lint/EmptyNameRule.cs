using System;
using System.Collections.Generic;

namespace Dastan.org.ed.ea.services.lint
{
    public class EmptyNameRule : IScriptRule
    {
        public IEnumerable<ScriptFinding> Check(IReadOnlyList<ScriptStatement> statements)
        {
            foreach (ScriptStatement statement in statements)
            {
                foreach (string value in statement.IdentifierValues)
                {
                    if (!string.IsNullOrWhiteSpace(value)) continue;

                    yield return new ScriptFinding("empty-name", "Object type or name is empty", statement);
                }
            }
        }
    }
}