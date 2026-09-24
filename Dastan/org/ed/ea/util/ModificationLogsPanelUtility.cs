using System;
using Dastan.org.ed.ea.tool;
using EA;

namespace Dastan.org.ed.ea.util
{
    public class ModificationLogsPanelUtility
    {
        private static ModificationLogsPanelUtility _instance;
        private bool _show = false;

        private ModificationLogsPanelUtility(ModificationLogs modificationLogs)
        {
            this.ModificationLogs = modificationLogs;
        }

        public static ModificationLogsPanelUtility GetInstance(Repository repository = null, string title = "", string id = "", bool toggle = false)
        {
            if (repository == null) return _instance;

            // Re-attempt window creation if it's never succeeded, instead of permanently
            // caching a broken (null ModificationLogs) instance from a failed AddWindow call.
            if (_instance == null || _instance.ModificationLogs == null || _instance.ModificationLogs.IsDisposed)
            {
                object ctrl = repository.AddWindow(title, id);
                ModificationLogs modificationLogs = ctrl as ModificationLogs;
                if (modificationLogs == null)
                {
                    throw new InvalidOperationException(
                        "Failed to create the Modification Logs panel (AddWindow did not return a ModificationLogs instance). " +
                        "The Dastan COM component may not be registered correctly — try rebuilding and re-registering the add-in.");
                }

                _instance = new ModificationLogsPanelUtility(modificationLogs);
            }

            _instance._show = _instance.ModificationLogs.IsVisibleInEa();

            if (toggle)
            {
                _instance._show = !_instance._show;
            }
            
            if (!_instance._show)
            {
                repository.HideAddinWindow();
            }
            else
            {
                repository.ShowAddinWindow(id);
            }
            return _instance;
        }
        
        public ModificationLogs ModificationLogs { get; }
    }
}