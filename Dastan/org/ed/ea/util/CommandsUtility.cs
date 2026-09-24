using System;
using System.Collections.Generic;
using Dastan.org.ed.ea.commands;
using Dastan.org.ed.ea.di;
using EA;

namespace Dastan.org.ed.ea.util
{
    public static class CommandsUtility
    {
        private static readonly ServiceContainer Container = BuildContainer();
        private static readonly Dictionary<string, Type> Commands = new Dictionary<string, Type>()
        {
            {Schema, typeof(SchemaScriptGeneratorCommand)},
            {Policy, typeof(PolicyScriptGeneratorCommand)},
            {Ui, typeof(UiScriptGeneratorCommand)},
            {Trigger, typeof(TriggerScriptGeneratorCommand)},
            {ModLogs, typeof(ShowModificationLogCommand)},
            {GenerateModifiedScript, typeof(GenerateModifiedScriptCommand)},
            {ClearModificationLogs, typeof(ClearModificationLogsCommand)},
            {ValidateModel, typeof(ValidateModelCommand)},
            {CompareWithExport, typeof(CompareWithExportCommand)},
            {ImportFromExport, typeof(ImportFromExportCommand)},
            {InstallProfiles, typeof(InstallProfilesCommand)},
            {Settings, typeof(SettingsCommand)},
            {NameGenerator, typeof(NameGeneratorCommand)},
            {JiraIssueSync, typeof(JiraIssueSyncCommand)},
            {OpenInJira, typeof(OpenInJiraCommand)}
        };

        private const string MenuHeader = "-&Dastan";
        private const string ScriptMenu = "-&Script";
        private const string JiraMenu = "-&Jira";
        private const string Schema = "&Schema Script Generator";
        private const string Ui = "&UI Script Generator";
        private const string Policy = "&Policy Script Generator";
        private const string Trigger = "&Trigger Script Generator";
        private const string NameGenerator = "&Name Generator Script Generator";
        private const string ModLogs = "&Modification Logs Panel";
        // S is taken by the Script submenu one level up, so Settings answers to E.
        private const string Settings = "S&ettings";
        private const string JiraIssueSync = "&Jira Issue Sync";
        private const string OpenInJira = "&Open In Jira";
        private const string GenerateModifiedScript = "Generate Script";
        private const string ValidateModel = "&Validate Model";
        private const string CompareWithExport = "&Compare With ENOVIA Export";
        private const string ImportFromExport = "&Import From ENOVIA Export";
        private const string InstallProfiles = "Install &Profiles";
        // Public so the panel can tell when its Clear button ran and drop its own copy of
        // the log to match.
        public const string ClearModificationLogs = "Clear";

        private static ServiceContainer BuildContainer()
        {
            var container = new ServiceContainer();
            
            container.RegisterSingleton<SchemaScriptGeneratorCommand>();
            container.RegisterSingleton<PolicyScriptGeneratorCommand>();
            container.RegisterSingleton<UiScriptGeneratorCommand>();
            container.RegisterSingleton<TriggerScriptGeneratorCommand>();
            container.RegisterSingleton<ShowModificationLogCommand>();
            container.RegisterSingleton<GenerateModifiedScriptCommand>();
            container.RegisterSingleton<ClearModificationLogsCommand>();
            container.RegisterSingleton<SettingsCommand>();
            container.RegisterSingleton<NameGeneratorCommand>();
            container.RegisterSingleton<JiraIssueSyncCommand>();
            container.RegisterSingleton<OpenInJiraCommand>();
            container.RegisterSingleton<ModifiedElementDetectionCommand>();
            container.RegisterSingleton<ValidateModelCommand>();
            container.RegisterSingleton<CompareWithExportCommand>();
            container.RegisterSingleton<ImportFromExportCommand>();
            container.RegisterSingleton<InstallProfilesCommand>();
            
            return container;
        }

        public static T Resolve<T>()
        {
            return Container.Resolve<T>();
        }

        public static object GetCommands(string menuName)
        {
            // Matched without the mnemonic markers, because the name EA hands back is
            // the one it was given and carrying '&' into every case label would make the
            // menu silently empty the day that changes.
            switch (WithoutMnemonic(menuName))
            {
                case "":
                    return MenuHeader;
                case "-Script":
                    return new string[] { Schema, Ui, Policy, Trigger, NameGenerator };
                case "-Jira":
                    return new string[] { JiraIssueSync, OpenInJira };
                case "-Dastan":
                    return new string[]
                    {
                        ScriptMenu, JiraMenu, "-", ModLogs, ValidateModel, CompareWithExport,
                        ImportFromExport, "-", InstallProfiles, Settings
                    };
                default:
                    return "";
            }
        }

        private static string WithoutMnemonic(string name)
        {
            return (name ?? "").Replace("&", "");
        }

        public static void CheckMenuState(Repository repository, string location, string menuName, string itemName,
            ref bool isEnabled, ref bool isChecked)
        {
            isChecked = false;

            ICommand command = GetCommand(itemName);
            isEnabled = command != null && command.HasAccess(repository);
        }

        public static ICommand GetCommand(string name)
        {
            if (!Commands.TryGetValue(name, out Type type))
                return null;
            
            return (ICommand) Container.Resolve(type);
        }
    }
}