using System;
using System.Windows.Forms;
using Dastan.org.ed.ea.constants;
using Dastan.org.ed.ea.entity;
using Dastan.org.ed.ea.repository;
using Dastan.org.ed.ea.services.elements.impl.policy;
using Dastan.org.ed.ea.util;
using EA;

namespace Dastan.org.ed.ea.commands
{
    public class PolicyScriptGeneratorCommand : ScriptGeneratorCommandBase
    {
        private PolicyScriptGenerator _policyScriptGenerator;
        private AllStateScriptGenerator _allStateScriptGenerator;
        private StateScriptGenerator _stateScriptGenerator;

        protected override bool TryPrepare(Context context)
        {
            object contextObject = context.Repository.GetContextObject();
            Element smElement = contextObject as Element;

            if (smElement == null || smElement.Type != ObjectTypes.StateMachine)
            {
                MessageBox.Show(Resources.STATE_MACHINE_NOT_SELECTED_ERROR,
                    Resources.INVALID_SELECTION, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            ElementRepository elementRepository = new ElementRepository();
            _allStateScriptGenerator = new AllStateScriptGenerator(context, elementRepository);
            _stateScriptGenerator = new StateScriptGenerator(context);
            _policyScriptGenerator = new PolicyScriptGenerator(context, elementRepository, _allStateScriptGenerator,
                _stateScriptGenerator);
            _policyScriptGenerator.Generate(smElement);
            return true;
        }

        protected override void WriteOutputs(Context context, ScriptExportUtility.OutputPaths paths)
        {
            string filePath = paths.FilePath("Policy_Output");
            string stringResourceFilePath = paths.FilePath("Policy_Output_String_Resources", "properties");

            string body = _policyScriptGenerator.Scripts() + "\n" +
                          _allStateScriptGenerator.Scripts() + "\n" +
                          _stateScriptGenerator.Scripts() + ";\n" +
                          _policyScriptGenerator.Registers() + "\n" +
                          _stateScriptGenerator.Registers();

            if (!ScriptExportUtility.WriteScript(context, filePath, body, includeBom: false)) return;

            ScriptExportUtility.WriteFile(stringResourceFilePath, _policyScriptGenerator.StringResources() + "\n" + _stateScriptGenerator.StringResources(), includeBom: false);
            ScriptExportUtility.NotifyExportCompleted(filePath, stringResourceFilePath);
        }

        protected override void ShowError(Exception e)
        {
            MessageBox.Show(string.Format(Resources.ERROR_MESSAGE, e.Message), Resources.EXECUTION_ERROR, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public override bool HasAccess(Repository repository)
        {
            var element = repository.GetContextObject() as Element;
            return element?.Stereotype == Stereotypes.Policy;
        }
    }
}