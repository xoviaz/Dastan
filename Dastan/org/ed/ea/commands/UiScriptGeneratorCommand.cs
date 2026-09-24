using System;
using System.Windows.Forms;
using Dastan.org.ed.ea.constants;
using Dastan.org.ed.ea.entity;
using Dastan.org.ed.ea.services.elements.impl.business;
using Dastan.org.ed.ea.util;
using EA;

namespace Dastan.org.ed.ea.commands
{
    public class UiScriptGeneratorCommand : ScriptGeneratorCommandBase
    {
        private BusScriptGenerator _generator;
        private LabelScriptGenerator _labelScriptGenerator;
        private TooltipScriptGenerator _tooltipScriptGenerator;
        private ConnectionScriptGenerator _connectionScriptGenerator;

        protected override bool TryPrepare(Context context)
        {
            object contextObject = context.Repository.GetContextObject();
            Element contextElement = contextObject as Element;

            if (contextElement == null || contextElement.ObjectType != ObjectType.otElement)
            {
                MessageBox.Show(Resources.NO_ELEMENT_SELECTED_ERROR,
                    Resources.INVALID_SELECTION, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            
            _connectionScriptGenerator = new ConnectionScriptGenerator(context);
            _labelScriptGenerator = new LabelScriptGenerator(context);
            _tooltipScriptGenerator = new TooltipScriptGenerator(context);
            _generator = new BusScriptGenerator(context, _connectionScriptGenerator, _labelScriptGenerator, _tooltipScriptGenerator);
            _generator.Generate(contextElement);
            return true;
        }

        protected override void WriteOutputs(Context context, ScriptExportUtility.OutputPaths paths)
        {
            string body = string.Join("\n\n", new[]
            {
                _generator.Scripts(),
                _labelScriptGenerator.Scripts(),
                _tooltipScriptGenerator.Scripts(),
                _connectionScriptGenerator.Scripts()
            });
            
            string filePath = paths.FilePath("UI_Output");
            if (!ScriptExportUtility.WriteScript(context, filePath, body)) return;
            ScriptExportUtility.NotifyExportCompleted(filePath);
        }

        protected override void ShowError(Exception e)
        {
            MessageBox.Show($"Error: {e.Message}", "Execution Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public override bool HasAccess(Repository repository)
        {
            var element = repository.GetContextObject() as Element;
            string fq = element?.FQStereotype ?? "";
            int sep = fq.IndexOf("::", StringComparison.Ordinal);
            if (sep <= 0) return false;

            string profile = fq.Substring(0, sep);
            return profile == Profiles.Widget;
        }
    }
}