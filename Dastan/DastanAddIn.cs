using System;
using System.Runtime.InteropServices;
using Dastan.org.ed.ea.commands;
using Dastan.org.ed.ea.entity;
using Dastan.org.ed.ea.settings;
using Dastan.org.ed.ea.util;
using EA;

namespace Dastan
{
    
    [ComVisible(true)]
    [Guid("399F1857-4C6C-4557-9870-2E03E3CE3A1A")]
    [ClassInterface(ClassInterfaceType.None)]
    public class DastanAddIn
    {
        private readonly ModifiedElementDetectionCommand _modifiedElementDetectionCommand = CommandsUtility.Resolve<ModifiedElementDetectionCommand>();
        
        public string EA_Connect(Repository repository)
        {
            return "";
        }

        public void EA_Disconnect()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public object EA_GetMenuItems(Repository repository, string location, string menuName)
        {
            return CommandsUtility.GetCommands(menuName);
        }

        public void EA_GetMenuState(Repository repository, string location, string menuName, string itemName, ref bool isEnabled, ref bool isChecked)
        {
            CommandsUtility.CheckMenuState(repository, location, menuName, itemName, ref isEnabled, ref isChecked);
        }

        public void EA_MenuClick(Repository repository, string location, string menuName, string itemName)
        {
            ICommand command = CommandsUtility.GetCommand(itemName);
            command?.Execute(new Context(repository));
        }

        public bool EA_OnPostNewConnector(Repository repository, EventProperties info)
        {
            if (AppSettings.TrackModifications)
                _modifiedElementDetectionCommand.OnConnectorCreated(repository, info);
            return false;
        }

        public bool EA_OnPreDeleteConnector(Repository repository, EventProperties info)
        {
            if (AppSettings.TrackModifications)
                _modifiedElementDetectionCommand.OnConnectorDeleted(repository, info);
            return true;
        }

        // Fired while the attribute still exists, which is the only moment its name and
        // owner can be read. Returning true lets the deletion go ahead.
        public bool EA_OnPreDeleteAttribute(Repository repository, EventProperties info)
        {
            if (AppSettings.TrackModifications)
                _modifiedElementDetectionCommand.OnAttributeDeleted(repository, info);
            return true;
        }

        public bool EA_OnPreDeleteMethod(Repository repository, EventProperties info)
        {
            if (AppSettings.TrackModifications)
                _modifiedElementDetectionCommand.OnOperationDeleted(repository, info);
            return true;
        }

        public void EA_OnContextItemChanged(Repository repository, string guid, ObjectType ot)
        {
            if (!AppSettings.TrackModifications) return;
            _modifiedElementDetectionCommand.OnContextItemChanged(repository, guid, ot);
        }

        public void EA_OnNotifyContextItemModified(Repository repository, string guid, ObjectType ot)
        {
            if (!AppSettings.TrackModifications) return;
            _modifiedElementDetectionCommand.Execute(new Context(repository, ot, guid));
        }

        public String EA_GetRibbonCategory(Repository repository)
        {
            return "Develop";
        }
    }
}