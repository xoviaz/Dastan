using Dastan.org.ed.ea.entity;
using Dastan.org.ed.ea.tool;
using EA;

namespace Dastan.org.ed.ea.commands
{
    public class SettingsCommand : ICommand
    {
        public void Execute(Context context)
        {
            using (var form = new Settings())
            {
                form.ShowDialog();
            }
        }
        
        public bool HasAccess(Repository repository)
        {
            return true;
        }
    }
}