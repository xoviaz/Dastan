using System;
using System.Collections.Generic;

namespace Dastan.org.ed.ea.services.enovia
{
    public class EnoviaExport
    {
        public string Release { get; set; } = "";
        public string ExportedAt { get; set; } = "";

        public List<EnoviaType> Types { get; } = new List<EnoviaType>();
        public List<EnoviaAttribute> Attributes { get; } = new List<EnoviaAttribute>();
        public List<EnoviaRelationship> Relationships { get; } = new List<EnoviaRelationship>();
        public List<EnoviaPolicy> Policies { get; } = new List<EnoviaPolicy>();
        public List<EnoviaRole> Roles { get; } = new List<EnoviaRole>();
        
        public Dictionary<string, int> Skipped { get; } = new Dictionary<string, int>();

        public int Count
        {
            get { return Types.Count + Attributes.Count + Relationships.Count + Policies.Count + Roles.Count; }
        }
        
    }
}