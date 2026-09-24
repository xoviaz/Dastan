using System.Collections.Generic;
using Dastan.org.ed.ea.entity;
using Dastan.org.ed.ea.util;

namespace Dastan.org.ed.ea.services.elements
{
    public abstract class ScriptGenerator<T> : IScriptGenerator<T>
    {
        protected readonly List<string> Script = new List<string>();
        protected readonly List<string> StringResource = new List<string>();
        protected readonly List<string> Register = new List<string>();
        protected readonly List<int> PassedElements = new List<int>();
        protected readonly List<int> PassedRelationships = new List<int>();
        protected const string StringResourceTemplate = "{0}.{1}.{2} = {3}";
        protected readonly Context Context;

        protected ScriptGenerator(Context context)
        {
            Context = context;
        }

        public abstract void Generate(T obj);

        protected static string Quote(string value)
        {
            return ScriptEscapeUtility.Quote(value);
        }
        
        public string StringResources()
        {
            return string.Join("\n", StringResource.ToArray());
        }

        public string Scripts()
        {
            return string.Join("\n", Script.ToArray());
        }

        public string Registers()
        {
            return string.Join("\n", Register.ToArray());
        }
    }
}