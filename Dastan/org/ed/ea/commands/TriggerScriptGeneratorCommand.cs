using System;
using System.Windows.Forms;
using Dastan.org.ed.ea.entity;
using Dastan.org.ed.ea.services.elements.impl.schema;
using Dastan.org.ed.ea.util;
using EA;

namespace Dastan.org.ed.ea.commands
{
    public class TriggerScriptGeneratorCommand : ScriptGeneratorCommandBase
    {
        private TriggerScriptGenerator _triggerScriptGenerator;

        protected override bool TryPrepare(Context context)
        {
            ObjectType contextType = context.Repository.GetContextItemType();
            context.Repository.GetContextItem(out object contextObj);
            _triggerScriptGenerator = new TriggerScriptGenerator(context);
            if (!TryDispatchByObjectType(context, contextType, contextObj, _triggerScriptGenerator, out string unsupportedMessage))
            {
                MessageBox.Show(unsupportedMessage, "Unsupported Context");
                return false;
            }
            
            return true;
        }

        protected override void WriteOutputs(Context context, ScriptExportUtility.OutputPaths paths)
        {
            string filePath = paths.FilePath("Trigger_Output");
            if (!ScriptExportUtility.WriteScript(context, filePath, _triggerScriptGenerator.Scripts())) return;
            ScriptExportUtility.NotifyExportCompleted(filePath);
        }

        protected override void ShowError(Exception e)
        {
            MessageBox.Show("Error: " + e.Message, "Schema Script Generator");
        }


        // Generating trigger script has to be done using either one element, one diagrams elements or one packages elements, checking if the diagram contains
        // trigger object or a package contains trigger object will affect heavily on performance, therefor, for element the command will be shown but
        // If the element is not of type trigger, no script will be generated
        public override bool HasAccess(Repository repository)
        {
            return true;
        }
    }
}