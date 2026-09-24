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

        // What EA says about it afterwards. Listed is not the same as working: a
        // technology EA holds but has not enabled gives no stereotypes and no toolbox,
        // and reporting it as installed would be a lie the modeller only finds out about
        // when nothing appears.
        public bool Listed { get; }
        public bool Enabled { get; }

        public MdgInstallReport(MdgTechnology technology, MdgInstallOutcome outcome, string previousVersion,
            string detail, bool listed, bool enabled)
        {
            Technology = technology;
            Outcome = outcome;
            PreviousVersion = previousVersion ?? "";
            Detail = detail ?? "";
            Listed = listed;
            Enabled = enabled;
        }

        public bool Working
        {
            get { return Outcome != MdgInstallOutcome.Failed && Listed && Enabled; }
        }

        public override string ToString()
        {
            if (Outcome == MdgInstallOutcome.Failed)
                return Technology.Name + " was not installed -- " + Detail;

            string what = Outcome == MdgInstallOutcome.Replaced
                ? Technology.Name + " replaced (was version " + PreviousVersion + ")"
                : Technology.Name + " installed";

            if (Enabled) return what;

            return what + ", but EA does not report it as enabled -- its stereotypes and toolbox " +
                   "will not appear until it is ticked on in Manage Technologies";
        }
    }

    // Puts the add-in's MDG technologies into EA.
    //
    // EA refuses to import a technology it already holds, so one already there is removed
    // first. That is the documented way to update one, and it is why this reports what it
    // replaced rather than quietly succeeding.
    //
    // Importing and enabling are two different things to EA. ImportTechnology only puts
    // the technology where Manage Technologies can see it -- it is ActivateTechnology
    // that ticks the box, and without that call the profile is registered but produces no
    // stereotype and no toolbox page, which is exactly the "listed but not there" result
    // an import otherwise leaves behind.
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
                if (Ask(repository, technology.Id, true))
                {
                    replacing = true;
                    previous = Version(repository, technology.Id);

                    // Removed rather than imported over. EA will not overwrite a
                    // technology in place, so an import on top of one silently does
                    // nothing at all.
                    repository.DeleteTechnology(technology.Id);
                }

                if (!repository.ImportTechnology(technology.ReadForImport()))
                {
                    return Failed(technology, previous, replacing,
                        "EA rejected the technology XML" +
                        (replacing ? ", and the version that was there has been removed" : ""));
                }

                bool listed = Ask(repository, technology.Id, true);
                if (!listed)
                {
                    return Failed(technology, previous, replacing,
                        "EA accepted the file but does not list the technology afterwards");
                }

                // The step that actually turns it on. Without it a technology sits in
                // Manage Technologies imported but unchecked, and an unchecked technology
                // contributes no stereotype and no toolbox page -- indistinguishable from
                // one that was never imported at all, except that EA lists it.
                Activate(repository, technology);

                bool enabled = Ask(repository, technology.Id, false);

                return new MdgInstallReport(technology,
                    replacing ? MdgInstallOutcome.Replaced : MdgInstallOutcome.Installed,
                    previous, "", true, enabled);
            }
            catch (Exception e)
            {
                return Failed(technology, previous, replacing, e.Message);
            }
        }

        // ActivateTechnology for the profile itself, then ActivateToolbox for each
        // toolbox page it carries. The toolbox call is best-effort: it only decides
        // whether the page is open in the Toolbox window, not whether the technology
        // works, so a failure here does not change the report's Enabled state.
        private static void Activate(Repository repository, MdgTechnology technology)
        {
            try
            {
                repository.ActivateTechnology(technology.Id);
            }
            catch
            {
                // Ask(..., enabled: false) below is what actually decides whether this
                // took; a thrown Activate does not by itself mean it failed.
            }

            foreach (string toolboxId in technology.ToolboxIds)
            {
                try
                {
                    repository.ActivateToolbox(toolboxId, 1);
                }
                catch
                {
                    // Cosmetic: worst case the toolbox page has to be opened by hand from
                    // the Toolbox window's own menu.
                }
            }
        }

        private static MdgInstallReport Failed(MdgTechnology technology, string previous, bool replacing,
            string detail)
        {
            return new MdgInstallReport(technology, MdgInstallOutcome.Failed, previous, detail, false, false);
        }

        // Both questions throw the same way when EA does not know the id, and a thrown
        // question is a no.
        private static bool Ask(Repository repository, string id, bool loaded)
        {
            try
            {
                return loaded ? repository.IsTechnologyLoaded(id) : repository.IsTechnologyEnabled(id);
            }
            catch
            {
                return false;
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