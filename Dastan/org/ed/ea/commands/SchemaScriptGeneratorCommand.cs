using System;
using System.Windows.Forms;
using Dastan.org.ed.ea.constants;
using Dastan.org.ed.ea.entity;
using Dastan.org.ed.ea.services.elements;
using Dastan.org.ed.ea.services.elements.impl.schema;
using Dastan.org.ed.ea.services.wrapper.impl;
using Dastan.org.ed.ea.util;
using EA;

namespace Dastan.org.ed.ea.commands
{
    public class SchemaScriptGeneratorCommand : ScriptGeneratorCommandBase
    {
        private AttributeScriptGenerator _attributeScriptGenerator;
        private RelationshipScriptGenerator _relationshipScriptGenerator;
        private TypeScriptGenerator _typeScriptGenerator;
        private RoleScriptGenerator _roleScriptGenerator;
        
        protected override bool TryPrepare(Context context)
        {
            ObjectType ot = context.Repository.GetContextItem(out var contextObj);

            if (ot != ObjectType.otElement)
            {
                MessageBox.Show("Please select an element", "Schema Script Generator");
                return false;
            }
            
            Element contextElement = contextObj as Element;
            if (contextElement == null)
            {
                MessageBox.Show("Please select an element",
                    "Schema Script Generator");
                return false;
            }
            
            _attributeScriptGenerator = new AttributeScriptGenerator(context);
            _relationshipScriptGenerator = new RelationshipScriptGenerator(context, _attributeScriptGenerator);
            _typeScriptGenerator = new TypeScriptGenerator(context, _relationshipScriptGenerator, _attributeScriptGenerator);
            _roleScriptGenerator = new RoleScriptGenerator(context);
            ElementWrapperScriptGenerator<ScriptGenerator<Element>> elementWrapper;

            if (contextElement.Stereotype == Stereotypes.Type)
            {
                elementWrapper = new ElementWrapperScriptGenerator<ScriptGenerator<Element>>(_typeScriptGenerator, context);
            } else if (contextElement.Stereotype == Stereotypes.Role)
            {
                elementWrapper = new ElementWrapperScriptGenerator<ScriptGenerator<Element>>(_roleScriptGenerator, context);
            }
            else
            {
                MessageBox.Show("Please select an element with stereotype 'Type' or 'Role'", "Schema Script Generator");
                return false;
            }
            
            elementWrapper.Execute(contextElement);
            return true;
        }

        protected override void WriteOutputs(Context context, ScriptExportUtility.OutputPaths paths)
        {
            string filePath = paths.FilePath("Schema_Output");
            string stringResourceFilePath = paths.FilePath("String_Resource_Output", "properties");
            
            string body = string.Join("\n\n", new[]
            {
                _attributeScriptGenerator.Scripts(),
                _attributeScriptGenerator.Registers(),
                _typeScriptGenerator.Scripts(),
                _typeScriptGenerator.Registers(),
                _relationshipScriptGenerator.Scripts(),
                _relationshipScriptGenerator.Registers(),
                _roleScriptGenerator.Scripts(),
                _roleScriptGenerator.Registers(),
            });

            if (!ScriptExportUtility.WriteScript(context, filePath, body)) return;
            
            string stringResources =
                _attributeScriptGenerator.StringResources() + "\n\n" +
                _typeScriptGenerator.StringResources() + "\n\n" +
                _roleScriptGenerator.StringResources() + "\n\n" +
                _relationshipScriptGenerator.StringResources();
            
            ScriptExportUtility.WriteFile(stringResourceFilePath, stringResources);
            
            ScriptExportUtility.NotifyExportCompleted(filePath, stringResourceFilePath);
        }

        protected override void ShowError(Exception e)
        {
            MessageBox.Show("Error: " + e.Message, "Schema Script Generator");
        }

        public override bool HasAccess(Repository repository)
        {
            var element = repository.GetContextObject() as Element;
            return (element?.Stereotype == Stereotypes.Type || element?.Stereotype == Stereotypes.Role);
        }
    }
}