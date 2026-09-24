using System;
using System.Collections.Generic;

namespace Dastan.org.ed.ea.services.mql
{
    public class RangeMqlTagTerm : IMqlTagTerm
    {
        private readonly string _keyword;
        
        public string TagName { get; }

        public RangeMqlTagTerm(string tagName, string keyword)
        {
            TagName = tagName;
            _keyword = keyword;
        }

        public string ForAdd(string value)
        {
            var clauses = new List<string>();
            foreach (string entry in (value ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string text = entry.Trim();
                if (text.Length == 0) continue;
                
                clauses.Add(_keyword + " = " + text);
            }
            
            return string.Join(" ", clauses.ToArray());
        }

        public string ForModify(string value)
        {
            return "";
        }
    }
}