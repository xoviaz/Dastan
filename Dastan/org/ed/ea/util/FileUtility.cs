using System.Windows.Forms;
using Dastan.org.ed.ea.settings;

namespace Dastan.org.ed.ea.util
{
    public class FileUtility
    {
        public static string GetFilePath()
        {
            if (string.IsNullOrEmpty(AppSettings.DefaultOutputFolder))
            {
                MessageBox.Show(Resources.NULL_OUTPUT_DIRECTORY_ERROR_MSG, Resources.ERROR_MESSAGE, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            string userPath = AppSettings.DefaultOutputFolder;
            if (!userPath.EndsWith("\\"))
                userPath += "\\";
            
            return userPath;
        }
    }
}