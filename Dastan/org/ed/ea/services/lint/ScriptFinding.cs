namespace Dastan.org.ed.ea.services.lint
{
    public class ScriptFinding
    {
        private const int PreviewLength = 120;

        public string Rule { get; }
        public string Message { get; }
        public int StatementNumber { get; }
        public string Statement { get; }

        // Where the statement starts in the script that was linted, so a caller can point
        // at this exact statement rather than the first one that happens to match its text.
        public int Offset { get; }

        public ScriptFinding(string rule, string message, ScriptStatement statement)
        {
            Rule = rule;
            Message = message;
            StatementNumber = statement.Number;
            Statement = statement.Text;
            Offset = statement.Offset;
        }

        public override string ToString()
        {
            return "[" + Rule + "] statement " + StatementNumber + ": " + Message + " -- " + Preview();
        }

        private string Preview()
        {
            string single = Statement.Replace("\r\n", " ").Replace("\n", " ").Replace("\t", " ");
            return single.Length <= PreviewLength ? single : single.Substring(0, PreviewLength) + "...";
        }
    }
}