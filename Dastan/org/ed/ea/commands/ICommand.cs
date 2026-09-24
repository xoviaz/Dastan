using Dastan.org.ed.ea.entity;
using EA;

namespace Dastan.org.ed.ea.commands
{
    public interface ICommand
    {
        void Execute(Context context);

        bool HasAccess(Repository repository);
    }
}