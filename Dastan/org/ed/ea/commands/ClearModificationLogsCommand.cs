using Dastan.org.ed.ea.entity;
using Dastan.org.ed.ea.repository;
using EA;

namespace Dastan.org.ed.ea.commands
{
    public class ClearModificationLogsCommand : ICommand
    {
        private readonly ElementRepository _elementRepository = new ElementRepository();
        
        public void Execute(Context context)
        {
            context.ListView.Items.Clear();
            Repository repository = context.Repository;
            Element targetElement = _elementRepository.FindModificationLogsElement(repository);
            if (targetElement == null) return;

            targetElement.Notes = "";
            targetElement.Update();
        }

        public bool HasAccess(Repository repository)
        {
            return true;
        }
    }
}