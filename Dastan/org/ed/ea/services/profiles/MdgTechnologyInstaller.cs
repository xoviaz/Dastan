using System;
using System.Collections.Generic;
using EA;

namespace Dastan.org.ed.ea.services.profiles
{
    public enum MdgInstallOutcome
    {
        Installed,
        Replaced,
        Failed
    }

    public class MdgInstallReport
    {
        public MdgTechnology Technology { get; }
        public MdgInstallOutcome Outcome { get; }

        // What EA had before, when it had anything. Worth saying: the usual reason an
        // import is refused is that the same version is already there.
        public string PreviousVersion { get; }

        public string Detail { get; }

        public MdgInstallReport(MdgTechnology technology, MdgInstallOutcome outcome, string previousVersion,
            string detail)
        {
            Technology = technology;
            Outcome = outcome;
            PreviousVersion = previousVersion ?? "";
            Detail = detail ?? "";
        }

        public override string ToString()
        {
            switch (Outcome)
            {
                case MdgInstallOutcome.Installed:
                    return Technology.Name + " installed";
                case MdgInstallOutcome.Replaced:
                    return Technology.Name + " replaced (was version " + PreviousVersion + ")";
                default:
                    return Technology.Name + " was not installed -- " + Detail;
            }
        }
    }

    // Puts the add-in's MDG technologies into EA.
    //
    // EA refuses to import a technology it already holds, so one already there is removed
    // first. That is the documented way to update one, and it is why this reports what it
    // replaced rather than quietly succeeding.
    public class MdgTechnologyInstaller
    {
        public List<MdgInstallReport> Install(Repository repository, IEnumerable<MdgTechnology> technologies)
        {
            var reports = new List<MdgInstallReport>();
            if (repository == null) return reports;

            foreach (MdgTechnology technology in technologies)
            {
                reports.Add(Install(repository, technology));
            }

            return reports;
        }

        private static MdgInstallReport Install(Repository repository, MdgTechnology technology)
        {
            string previous = "";
            bool replacing = false;

            try
            {
                if (repository.IsTechnologyLoaded(technology.Id))
                {
                    replacing = true;
                    previous = Version(repository, technology.Id);

                    // Removed rather than imported over. EA will not overwrite a
                    // technology in place, so an import on top of one silently does
                    // nothing at all.
                    repository.DeleteTechnology(technology.Id);
                }

                string xml = technology.Read();

                if (!repository.ImportTechnology(xml))
                {
                    return new MdgInstallReport(technology, MdgInstallOutcome.Failed, previous,
                        "EA rejected the technology XML" +
                        (replacing ? ", and the version that was there has been removed" : ""));
                }

                // Asked rather than assumed: ImportTechnology can return true for a file
                // EA then does not list, and a report saying it worked when it did not is
                // worse than no report.
                if (!repository.IsTechnologyLoaded(technology.Id))
                {
                    return new MdgInstallReport(technology, MdgInstallOutcome.Failed, previous,
                        "EA accepted the file but does not list the technology afterwards");
                }

                return new MdgInstallReport(technology,
                    replacing ? MdgInstallOutcome.Replaced : MdgInstallOutcome.Installed, previous, "");
            }
            catch (Exception e)
            {
                return new MdgInstallReport(technology, MdgInstallOutcome.Failed, previous, e.Message);
            }
        }

        private static string Version(Repository repository, string id)
        {
            try
            {
                string version = repository.GetTechnologyVersion(id);
                return string.IsNullOrEmpty(version) ? "unknown" : version;
            }
            catch
            {
                return "unknown";
            }
        }
    }
}
