using Dastan.org.ed.ea.util;

namespace Dastan.org.ed.ea.services.mql
{
    public class ValueMqlTagTerm : IMqlTagTerm
    {
        private readonly string _keyword;
        private readonly bool _quoted;
        
        public string TagName { get; }

        public ValueMqlTagTerm(string tagName, string keyword, bool quoted)
        {
            TagName = tagName;
            _keyword = keyword;
            _quoted = quoted;
        }

        public string ForAdd(string value)
        {
            string text = (value ?? "").Trim();
            if (text.Length == 0) return "";

            return _keyword + " " + (_quoted ? ScriptEscapeUtility.Quote(text) : text);
        }

        public string ForModify(string value)
        {
            return ForAdd(value);
        }
    }
}