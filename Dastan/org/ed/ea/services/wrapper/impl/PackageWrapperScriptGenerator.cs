using Dastan.org.ed.ea.entity;
using Dastan.org.ed.ea.services.elements;
using EA;

namespace Dastan.org.ed.ea.services.wrapper.impl
{
    public class PackageWrapperScriptGenerator<TS> : WrapperScriptGenerator<Package, TS> where TS : ScriptGenerator<Element>
    {
        public PackageWrapperScriptGenerator(IScriptGenerator<Element> scriptGenerator, Context context) : base(scriptGenerator, context)
        {
        }

        public override void Execute(Package obj)
        {
            foreach (Element elem in obj.Elements)
            {
                ScriptGenerator.Generate(elem);
            }
        }
    }
}