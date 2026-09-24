using System;
using EA;

namespace Dastan.org.ed.ea.util
{
    public class PackageUtil
    {
        public static Package GetOrCreateJiraProjectPackage(Package parentPackage, string projectKey, string projectName, string projectId)
        {
            foreach (Package pkg in parentPackage.Packages)
            {
                if (string.Equals(pkg.Name, projectKey + " - " + projectName, StringComparison.OrdinalIgnoreCase))
                {
                    return pkg;
                }
            }

            Package newPkg = (Package)parentPackage.Packages.AddNew(projectKey + " - " + projectName, "Package");
            newPkg.Alias = projectId;
            newPkg.Update();
            parentPackage.Packages.Refresh();

            return newPkg;
        }
    }
}