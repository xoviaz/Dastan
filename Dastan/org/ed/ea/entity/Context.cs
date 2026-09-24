using System.Windows.Forms;
using EA;

namespace Dastan.org.ed.ea.entity
{
    public class Context
    {
        public Context(Repository repository, ObjectType type = ObjectType.otElement, string guid = null, ListView listView = null)
        {
            Repository = repository;
            Type = type;
            Guid = guid;
            ListView = listView;
        }

        public Repository Repository { get; }
        public ObjectType Type { get; }
        public string Guid { get; }
        public ListView ListView { get; }
    }
}