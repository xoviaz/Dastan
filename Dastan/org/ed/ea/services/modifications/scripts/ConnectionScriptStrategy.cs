namespace Dastan.org.ed.ea.services.modifications.scripts
{
    public class ConnectionScriptStrategy : IModificationScriptStrategy
    {
        public bool CanHandle(ModificationRow row) => row.Property == ModificationProperties.Connection;

        public string Generate(ModificationRow row)
        {
            if (!ConnectionInfo.TryParse(row.NewValue, out string relationshipStereotype, out string targetStereotype,
                    out string targetName))
                return "";
            
            return "connect bus " + row.Stereotype + " " + row.Name + " - relationship " + relationshipStereotype +
                   " to " + targetStereotype + " " + targetName + " -;";
        }
    }
}