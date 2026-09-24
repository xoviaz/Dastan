using System;
using System.Windows.Forms;
using Dastan.org.ed.ea.entity;
using Dastan.org.ed.ea.services.elements.impl.naming;
using Dastan.org.ed.ea.util;
using EA;

namespace Dastan.org.ed.ea.commands
{
    public class NameGeneratorCommand : ScriptGeneratorCommandBase
    {
        private NumberGeneratorConnectionScriptGenerator _numberGeneratorConnectionScriptGenerator;
        private NumberScriptGenerator _numberScriptGenerator;
        private ObjectScriptGenerator _objectScriptGenerator;

        protected override bool TryPrepare(Context context)
        {
            ObjectType contextType = context.Repository.GetContextItemType();
            context.Repository.GetContextItem(out object contextObj);
            
            _numberGeneratorConnectionScriptGenerator = new NumberGeneratorConnectionScriptGenerator(context);
            _numberScriptGenerator = new NumberScriptGenerator(context);
            _objectScriptGenerator = new ObjectScriptGenerator(context, _numberScriptGenerator, _numberGeneratorConnectionScriptGenerator);
            
            if (!TryDispatchByObjectType(context, contextType, contextObj, _objectScriptGenerator, out string unsupportedMessage))
            {
                MessageBox.Show(unsupportedMessage, "Unsupported Context");
                return false;
            }

            return true;
        }

        protected override void WriteOutputs(Context context, ScriptExportUtility.OutputPaths paths)
        {
            string body = string.Join("\n\n", new[]
            {
                _numberScriptGenerator.Scripts(),
                _objectScriptGenerator.Scripts(),
                _numberGeneratorConnectionScriptGenerator.Scripts(),
            });
            
            string filePath = paths.FilePath("Naming_Output");

            if (!ScriptExportUtility.WriteScript(context, filePath, body)) return;
            ScriptExportUtility.NotifyExportCompleted(filePath);
        }

        protected override void ShowError(Exception e)
        {
            MessageBox.Show("Error: " + e.Message, "Name Generator Script Generator");
        }

        // Generating name generator script has to be done using either one element, one diagrams elements or one packages elements, checking if the diagram contains
        // Name generator object or a package contains name generator object will affect heavily on performance, therefor, for element the command will be shown but
        // If the element is not of type name generator, no script will be generated
        public override bool HasAccess(Repository repository)
        {
            return true;
        }
    }
}