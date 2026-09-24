using System.Collections.Generic;

namespace Dastan.org.ed.ea.services.enovia
{
    // What every admin object in an ematrix export carries, from its adminProperties.
    //
    // Business objects will sit alongside these rather than beneath them: they have a
    // type, name and revision instead of a single admin name, so they are a different
    // shape and forcing them to share this base would distort both.
    public abstract class EnoviaObject
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";

        // The installer/application/version/original name block ENOVIA stamps on
        // everything, plus anything else the site has added.
        public Dictionary<string, string> Properties { get; } = new Dictionary<string, string>();

        public override string ToString()
        {
            return Name;
        }
    }
}