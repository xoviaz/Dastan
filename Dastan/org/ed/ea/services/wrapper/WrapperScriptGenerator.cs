using Dastan.org.ed.ea.entity;
using Dastan.org.ed.ea.services.elements;
using EA;

namespace Dastan.org.ed.ea.services.wrapper
{
    public abstract class WrapperScriptGenerator<T, TS> : IWrapperScriptGenerator<T> where TS : ScriptGenerator<Element>
    {
        protected readonly IScriptGenerator<Element> ScriptGenerator;
        protected readonly Context Context;

        protected WrapperScriptGenerator(IScriptGenerator<Element> scriptGenerator, Context context)
        {
            ScriptGenerator = scriptGenerator;
            Context = context;
        }

        public abstract void Execute(T obj);
    }
}