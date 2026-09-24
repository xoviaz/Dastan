using System;

namespace Dastan.org.ed.ea.services.mql
{
    public class FlagMqlTagTerm : IMqlTagTerm
    {
        private readonly string _whenTrue;
        private readonly string _whenFalse;
        
        public string TagName { get; }

        public FlagMqlTagTerm(string tagName, string whenTrue, string whenFalse)
        {
            TagName = tagName;
            _whenTrue = whenTrue;
            _whenFalse = whenFalse;
        }

        public string ForAdd(string value)
        {
            return string.Equals(value, "True", StringComparison.Ordinal) ? _whenTrue  : _whenFalse;
        }

        public string ForModify(string value)
        {
            return ForAdd(value);
        }
    }
}