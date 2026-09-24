using Dastan.org.ed.ea.entity;
using Dastan.org.ed.ea.services.jira;
using EA;

namespace Dastan.org.ed.ea.commands
{
    public class JiraIssueSyncCommand : ICommand
    {
        public void Execute(Context context)
        {
            SyncJira syncJira = new SyncJira();
            syncJira.ExecuteJiraSync(context);
        }
        
        public bool HasAccess(Repository repository)
        {
            return true;
        }
    }
}