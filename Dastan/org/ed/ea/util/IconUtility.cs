using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace Dastan.org.ed.ea.util
{
    // Artwork compiled into the assembly.
    //
    // EA gives an add-in no say over its menu or ribbon icons -- those come back from
    // EA_GetMenuItems as plain strings -- so the only places the Dastan icon can actually
    // appear are the windows the add-in owns itself.
    public static class IconUtility
    {
        private const string AppIconResource = "Dastan.icons.dastan.ico";

        // One shared instance for the life of the process. A Form does not dispose an icon
        // handed to it, so sharing is safe, and handing each window its own copy would
        // leak one per dialog instead.
        private static Icon _appIcon;
        private static bool _appIconLoaded;

        public static Icon AppIcon
        {
            get
            {
                if (_appIconLoaded) return _appIcon;
                _appIconLoaded = true;

                try
                {
                    Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(AppIconResource);
                    if (stream == null) return null;

                    using (stream)
                    {
                        _appIcon = new Icon(stream);
                    }
                }
                catch
                {
                    // A resource that is missing or that this runtime cannot parse is a
                    // cosmetic problem. Letting it escape would turn every dialog that
                    // wants an icon into a command that fails to open.
                    _appIcon = null;
                }

                return _appIcon;
            }
        }

        // Puts the icon in the window's title bar and in Alt-Tab. Silently does nothing
        // when the resource is missing, because a dialog without an icon is a cosmetic
        // problem and throwing here would turn it into a broken command.
        public static void Apply(Form form)
        {
            if (form == null) return;

            Icon icon = AppIcon;
            if (icon != null) form.Icon = icon;
        }

        public static Image LoadImage(string resourceName)
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stream == null) return null;

                return Image.FromStream(stream);
            }
        }
    }
}
