using System.Collections.Generic;
using Dastan.org.ed.ea.constants;
using Dastan.org.ed.ea.entity;
using Dastan.org.ed.ea.tool;
using Dastan.org.ed.ea.util;

namespace Dastan.org.ed.ea.services.modifications
{
    public class ModificationLogObserver : IModificationObserver
    {
        public bool OnModified(Context context, IReadOnlyList<ModificationEvent> changes)
        {
            if (changes.Count == 0) return true;

            ModificationLogs modificationLogs = ModificationLogsPanelUtility
                .GetInstance(context.Repository, "Modification Logs", Constants.UI_FORM_ID).ModificationLogs;
            if (modificationLogs == null) return false;

            foreach (ModificationEvent modificationEvent in changes)
            {
                modificationLogs.AddRow(context, modificationEvent.Element, modificationEvent.Date, modificationEvent.ElementName, modificationEvent.Type, modificationEvent.Stereotype, modificationEvent.Property, modificationEvent.OldValue, modificationEvent.NewValue);
            }
            
            return true;
        }
    }
}