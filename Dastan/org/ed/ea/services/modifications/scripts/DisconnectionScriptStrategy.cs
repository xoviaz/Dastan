namespace Dastan.org.ed.ea.services.modifications.scripts
{
    public class DisconnectionScriptStrategy : IModificationScriptStrategy
    {
        public bool CanHandle(ModificationRow row) => row.Property == ModificationProperties.Disconnection;

        public string Generate(ModificationRow row)
        {
            if (!ConnectionInfo.TryParse(row.OldValue, out string relationshipStereotype, out string targetStereotype,
                    out string targetName))
                return "";
            
            return "disconnect bus " + row.Stereotype + " " + row.Name + " - relationship " + relationshipStereotype +
                   " to " + targetStereotype + " " + targetName + " -;";
        }
    }
}