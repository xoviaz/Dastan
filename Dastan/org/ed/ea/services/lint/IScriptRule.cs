using System.Collections.Generic;

namespace Dastan.org.ed.ea.services.lint
{
    public interface IScriptRule
    {
        IEnumerable<ScriptFinding> Check( IReadOnlyList<ScriptStatement> statements);
    }
}