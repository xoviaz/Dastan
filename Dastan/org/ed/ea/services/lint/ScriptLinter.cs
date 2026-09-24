using System.Collections.Generic;

namespace Dastan.org.ed.ea.services.lint
{
    public class ScriptLinter
    {
        private readonly List<IScriptRule> _rules = new List<IScriptRule>
        {
            new UnterminatedStatementRule(),
            new UnclosedQuoteRule(),
            new EmptyNameRule(),
            new QuoteInNameRule(),
            new DuplicateObjectRule()
        };

        public List<ScriptFinding> Lint(string script)
        {
            List<ScriptStatement> statements = MqlStatementParser.Parse(script);
            
            var findings = new List<ScriptFinding>();
            foreach (IScriptRule rule in _rules)
                findings.AddRange(rule.Check(statements));
            return findings;
        }
    }
}