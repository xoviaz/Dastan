using System;
using System.Diagnostics;
using System.Windows.Forms;
using Dastan.org.ed.ea.entity;
using Dastan.org.ed.ea.settings;
using EA;

namespace Dastan.org.ed.ea.commands
{
    public class OpenInJiraCommand : ICommand
    {
        public void Execute(Context context)
        {
            try
            {
                object contextObject = context.Repository.GetContextObject();
                if (contextObject == null)
                {
                    MessageBox.Show("No context type found.");
                    return;
                }
            
                ObjectType contextType = context.Repository.GetContextItemType();
                if (contextType != ObjectType.otElement)
                {
                    MessageBox.Show("Please select an element");
                    return;
                }
            
                Element element = (Element) contextObject;
                string key = element.Alias;
                if (string.IsNullOrEmpty(key))
                {
                    MessageBox.Show("No key found.");
                    return;
                }
            
                string url = AppSettings.JiraDomain + "/browse/" + key;
                Process.Start(url);
            } catch (Exception e)
            {
                MessageBox.Show("Error: " + e.Message, "Open In Jira");
            }
        }
        
        public bool HasAccess(Repository repository)
        {
            var element = repository.GetContextObject() as Element;
            return element?.Type == JiraTaskDto.Type && !string.IsNullOrEmpty(element?.Alias) && element?.Alias.StartsWith("XW-", StringComparison.OrdinalIgnoreCase) == true;
        }
    }
}
