using System.Collections.Generic;
using Dastan.org.ed.ea.entity;

namespace Dastan.org.ed.ea.services.modifications
{
    public interface IModificationObserver
    {

        bool OnModified(Context context, IReadOnlyList<ModificationEvent> changes);

    }
}