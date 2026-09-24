using System.Collections.Generic;
using System.Linq;

namespace Dastan.org.ed.ea.pipeline.jira.jql
{
    public class JqlBuilder
    {
        private readonly List<JqlOption> _options = new List<JqlOption>();

        public JqlBuilder(List<JqlOption> options)
        {
            if (options != null)
                _options.AddRange(options.ToList());
        }

        public string Build()
        {
            var clauses = _options
                .Select(option => option.Apply())
                .Where(clause => !string.IsNullOrWhiteSpace(clause));
            
            return string.Join(" AND ", clauses);
            
        }
    }
}