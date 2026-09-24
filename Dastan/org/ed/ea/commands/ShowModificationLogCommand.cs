using System;
using System.Windows.Forms;
using Dastan.org.ed.ea.constants;
using Dastan.org.ed.ea.entity;
using Dastan.org.ed.ea.tool;
using Dastan.org.ed.ea.util;
using EA;

namespace Dastan.org.ed.ea.commands
{
    public class ShowModificationLogCommand : ICommand
    {
        public void Execute(Context context)
        {
            try
            {
                ModificationLogs modificationLogs = ModificationLogsPanelUtility
                    .GetInstance(context.Repository, "Modification Logs", Constants.UI_FORM_ID, true).ModificationLogs;

                if (modificationLogs == null)
                {
                    MessageBox.Show("Could not open the Modification Logs panel.", "Modification Logs Panel");
                    return;
                }
                
                modificationLogs.LoadItems(context.Repository);
            }
            catch (Exception e)
            {
                MessageBox.Show("Error: " + e.Message, "Modification Logs Panel");
            }
        }
        
        public bool HasAccess(Repository repository)
        {
            return true;
        }
    }
}