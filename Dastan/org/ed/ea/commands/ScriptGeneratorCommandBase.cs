using System;
using Dastan.org.ed.ea.entity;
using Dastan.org.ed.ea.services.elements;
using Dastan.org.ed.ea.services.wrapper.impl;
using Dastan.org.ed.ea.util;
using EA;

namespace Dastan.org.ed.ea.commands
{
    public abstract class ScriptGeneratorCommandBase : ICommand
    {
        public void Execute(Context context)
        {
            try
            {
                if (!TryPrepare(context)) return;

                ScriptExportUtility.OutputPaths? paths = ScriptExportUtility.ResolveOutputPaths();
                if (paths == null) return;

                WriteOutputs(context, paths.Value);
            }
            catch (Exception e)
            {
                ShowError(e);
            }
        }
        
        protected abstract bool TryPrepare(Context context);
        
        protected abstract void WriteOutputs(Context context, ScriptExportUtility.OutputPaths paths);
        
        protected abstract void ShowError(Exception e);
        
        public abstract bool HasAccess(Repository repository);

        protected static bool TryDispatchByObjectType<TGenerator>(Context context, ObjectType contextType, object contextObj, TGenerator generator, out string unsupportedMessage) where TGenerator : ScriptGenerator<Element>
        {
            unsupportedMessage = null;
            switch (contextType)
            {
                case ObjectType.otDiagram: 
                    new DiagramWrapperScriptGenerator<TGenerator>(generator, context).Execute((Diagram) contextObj);
                    return true;
                case ObjectType.otElement:
                    new ElementWrapperScriptGenerator<TGenerator>(generator, context).Execute((Element) contextObj);
                    return true;
                case ObjectType.otPackage:
                    new PackageWrapperScriptGenerator<TGenerator>(generator, context).Execute((Package) contextObj);
                    return true;
                default:
                    unsupportedMessage = "Please select a Diagram, Element or Package";
                    return false;
            }
        }
    }
}