using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Dastan.org.ed.ea.services.profiles
{
    // An MDG technology shipped inside the add-in.
    //
    // The profiles are what give an element its stereotype, its tagged values and its
    // colour, and every generator reads those tagged values -- so a model built without
    // them produces nothing. Carrying them in the assembly means the add-in and the
    // profiles it expects can never be different versions of each other.
    public class MdgTechnology
    {
        // The id from the technology's own <Documentation id="..."> element, which is what
        // EA knows it by afterwards -- for ImportTechnology, ActivateTechnology,
        // IsTechnologyLoaded and IsTechnologyEnabled alike.
        public string Id { get; }

        public string Name { get; }
        public string Resource { get; }
        public string FileName { get; }
        public string Notes { get; }

        // The id of each <UIToolboxes><UMLProfile><Documentation id="..."> page the
        // technology carries. Empty when it ships no toolbox page.
        public IReadOnlyList<string> ToolboxIds { get; }

        private MdgTechnology(string id, string name, string resource, string fileName, string notes)
        {
            Id = id;
            Name = name;
            Resource = resource;
            FileName = fileName;
            Notes = notes;
        }

        public static IReadOnlyList<MdgTechnology> All
        {
            get { return Shipped; }
        }

        private static readonly List<MdgTechnology> Shipped = new List<MdgTechnology>
        {
            new MdgTechnology("DastanTech", "Dastan Technology",
                "Dastan.DastanMDGTechnology.xml", "DastanMDGTechnology.xml",
                "Types, attributes, relationships, policies, states, roles, widget, menu, command, parameter, settings,... with their toolbox.")
        };

        // The file exactly as it is carried, declaration and all. This is what gets written
        // to disk for EA to import from a file.
        //
        // Read as UTF-8 with the byte-order mark stripped: a mark left at the front sits
        // before the declaration, where a parser reads it as content and rejects the
        // document.
        public string Read()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream(Resource))
            {
                if (stream == null)
                    throw new FileNotFoundException(
                        "The add-in does not carry " + Resource + ". It has to be built as an " +
                        "embedded resource.");

                using (var reader = new StreamReader(stream, Encoding.UTF8, true))
                {
                    return reader.ReadToEnd().TrimStart('\uFEFF');
                }
            }
        }

        // Written with the declaration intact and as windows-1252, so the file on disk
        // matches what it says it is.
        public string Save(string folder)
        {
            Directory.CreateDirectory(folder);

            string path = Path.Combine(folder, FileName);
            File.WriteAllText(path, Read(), Encoding.GetEncoding(1252));
            return path;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}