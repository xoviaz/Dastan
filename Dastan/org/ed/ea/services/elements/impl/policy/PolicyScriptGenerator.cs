using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Dastan.org.ed.ea.constants;
using Dastan.org.ed.ea.entity;
using Dastan.org.ed.ea.repository;
using Dastan.org.ed.ea.settings;
using Dastan.org.ed.ea.util;
using EA;

namespace Dastan.org.ed.ea.services.elements.impl.policy
{
    public class PolicyScriptGenerator : ScriptGenerator<Element>
    {
        private const string Template = "add {0} {1} {2}{3}{4}{5}{6}";
        private readonly ElementRepository _elementRepository;
        private readonly AllStateScriptGenerator _allStateScriptGenerator;
        private readonly StateScriptGenerator _stateScriptGenerator;

        private const string RegisterTemplate =
            "add property installer on policy \"{0}\" value ENOVIAEngineering;\nadd property application on policy \"{0}\" value Framework;\nadd property \"installed date\" on policy \"{0}\" value \"{1}\";\nadd property \"original name\" on policy \"{0}\" value \"{0}\";\nadd property version on policy \"{0}\" value R419;\nadd property policy_{0} on program eServiceSchemaVariableMapping.tcl to policy \"{0}\";";
        
        public PolicyScriptGenerator(Context context, ElementRepository elementRepository, AllStateScriptGenerator allStateScriptGenerator, StateScriptGenerator stateScriptGenerator) : base(context)
        {
            _elementRepository = elementRepository;
            _allStateScriptGenerator = allStateScriptGenerator;
            _stateScriptGenerator = stateScriptGenerator;
        }

        public override void Generate(Element obj)
        {
            if (obj == null || obj.Type != ObjectTypes.StateMachine || obj.Stereotype != Stereotypes.Policy)
            {
                MessageBox.Show(Resources.STATE_MACHINE_NOT_SELECTED_ERROR,
                    Resources.INVALID_SELECTION, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string[] typesArray = TagUtility.GetSafeTagValue(obj.TaggedValues, "Type", "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            string formattedTypes = string.Join(", ", typesArray.Select(t => Quote(t.Trim())));
            string type = string.IsNullOrEmpty(formattedTypes) ? "" : $"type {formattedTypes} ";

            string[] formatArray = TagUtility.GetSafeTagValue(obj.TaggedValues, "Format", "generic").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            string format = string.Join(" ", formatArray.Select(f => $"format {Quote(f.Trim())} "));

            string store = $"store {Quote(TagUtility.GetSafeTagValue(obj.TaggedValues, "Store", "STORE"))} ";
            string defaultFormat = $"defaultformat {Quote(TagUtility.GetSafeTagValue(obj.TaggedValues, "Default Format", "generic"))} ";
            string sequence = $"minorsequence {Quote(TagUtility.GetSafeTagValue(obj.TaggedValues, "Sequence", "-"))} ";

            string displayName = TagUtility.GetSafeTagValue(obj.TaggedValues, "Display Name", "");
            
            Script.Add(string.Format(Template, Quote(obj.Stereotype), Quote(obj.Name), type, store, format, defaultFormat, sequence));
            StringResource.Add(string.Format(StringResourceTemplate, AppSettings.StringResourcePrefix, "Policy", obj.Name, displayName));
            Register.Add(string.Format(RegisterTemplate, obj.Name, DateTime.Now.ToString("M/d/yyyy h:mm:ss tt")));
            
            _allStateScriptGenerator.Generate(obj);

            // A policy with no diagram has no states to walk, and asking for its states
            // would have thrown here rather than said so.
            Diagram diagram = obj.Diagrams.GetAt(0) as Diagram;
            if (diagram == null) return;

            List<Element> initialElements = _elementRepository.GetElementByDiagramIdAndTypeAndStereotype(Context,
                diagram.DiagramID, "State", "Start State");

            if (initialElements.Count == 0) return;
            
            _stateScriptGenerator.Generate(initialElements[0]);
        }
    }
}