using System.Collections.Generic;
using EA;

namespace Dastan.org.ed.ea.entity
{
    public class ScriptGeneratorContext
    {
        public Element Element { get; set; }
        
        public Collection Attributes { get; set; }
        
        public Collection Connectors  { get; set; }
        
        public Repository Repository { get; set; }

        public List<string> BusScript { get; set; } = new  List<string>();
        public List<string> AttrScript { get; set; } = new  List<string>();
        public List<string> RelScript { get; set; } = new  List<string>();
        public List<string> StringResource { get; set; } = new  List<string>();
        public List<string> ProcessedElements  { get; set; } = new  List<string>();
        public List<string> ProcessedAttributes  { get; set; } = new  List<string>();
        public List<int> ProcessedRelationships  { get; set; } = new  List<int>();
        
    }
}