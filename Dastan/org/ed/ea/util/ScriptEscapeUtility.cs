using System.Text;


namespace Dastan.org.ed.ea.util
{
    public class ScriptEscapeUtility
    {

        public static string Quote(string value)
        {
            string text = value ?? "";

            return text.IndexOf('"') >= 0 && text.IndexOf('\'') < 0 ? "'" + text + "'" : "\"" + text + "\"";
        }
    }
}