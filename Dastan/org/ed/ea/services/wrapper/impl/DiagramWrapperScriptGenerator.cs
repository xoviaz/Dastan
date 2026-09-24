using Dastan.org.ed.ea.entity;
using Dastan.org.ed.ea.services.elements;
using EA;

namespace Dastan.org.ed.ea.services.wrapper.impl
{
    public class DiagramWrapperScriptGenerator<TS> : WrapperScriptGenerator<Diagram, TS> where TS : ScriptGenerator<Element>
    {
        public DiagramWrapperScriptGenerator(IScriptGenerator<Element> scriptGenerator, Context context) : base(scriptGenerator, context)
        {
            
        }

        public override void Execute(Diagram obj)
        {
            foreach (DiagramObject dObj in obj.DiagramObjects)
            {
                Element el = Context.Repository.GetElementByID(dObj.ElementID);
                ScriptGenerator.Generate(el);
            }
        }
    }
}