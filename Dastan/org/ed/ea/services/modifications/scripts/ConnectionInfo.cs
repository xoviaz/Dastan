using System;

namespace Dastan.org.ed.ea.services.modifications.scripts
{
    public static class ConnectionInfo
    {
        public static bool TryParse(string connectionInfo, out string relationshipStereotype,
            out string targetStereotype, out string targetName)
        {
            relationshipStereotype = targetStereotype = targetName = "";
            if (string.IsNullOrEmpty(connectionInfo)) return false;

            string[] parts = connectionInfo.Split(new[] { " » " }, StringSplitOptions.None);
            if (parts.Length != 3) return false;

            relationshipStereotype = parts[0];
            targetStereotype = parts[1];
            targetName = parts[2];
            return true;
        }
    }
}