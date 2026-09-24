using Dastan.org.ed.ea.entity;
using Dastan.org.ed.ea.services.elements;
using EA;

namespace Dastan.org.ed.ea.services.wrapper.impl
{
    public class ElementWrapperScriptGenerator<TS> : WrapperScriptGenerator<Element, TS> where TS : ScriptGenerator<Element>
    {
        public ElementWrapperScriptGenerator(IScriptGenerator<Element> scriptGenerator, Context context) : base(scriptGenerator, context)
        {
        }

        public override void Execute(Element obj)
        {
            ScriptGenerator.Generate(obj);
        }
    }
}