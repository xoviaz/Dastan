namespace Dastan.org.ed.ea.services.mql
{
    public class LogOnlyMqlTagTerm : IMqlTagTerm
    {
        public string TagName { get; }

        public LogOnlyMqlTagTerm(string tagName)
        {
            TagName = tagName;
        }

        public string ForAdd(string value)
        {
            return "";
        }

        public string ForModify(string value)
        {
            return "";
        }
    }
}