namespace Dastan.org.ed.ea.services.validation
{
    public enum IssueSeverity
    {
        // The generated MQL will fail, or nothing will be generated at all.
        Error,

        // The script still runs, but the result is not what the model says.
        Warning
    }

    // One problem found in the model, named the way the lint engine names a problem in a
    // script: a rule, a message, and enough to point at what it is about.
    //
    // The script linter catches these at export, when the fix means going back into the
    // model and generating again. This catches the same mistakes while they are still
    // one edit away.
    public class ModelIssue
    {
        public IssueSeverity Severity { get; }
        public string Rule { get; }
        public string Message { get; }
        public string ObjectName { get; }
        public string Kind { get; }
        public string Path { get; }

        // GUID of the element this is about. An attribute reports its owning element,
        // since selecting the type that carries it always works and lands close enough.
        public string ElementGuid { get; }

        public ModelIssue(IssueSeverity severity, string rule, string message, ModelObject obj)
        {
            Severity = severity;
            Rule = rule;
            Message = message;
            ObjectName = obj.Display;
            Kind = obj.Label;
            Path = obj.Path;
            ElementGuid = obj.ElementGuid;
        }

        public override string ToString()
        {
            return "[" + Rule + "] " + Severity + ": " + Kind + " " + ObjectName + " -- " + Message +
                   (string.IsNullOrEmpty(Path) ? "" : " (" + Path + ")");
        }
    }
}
