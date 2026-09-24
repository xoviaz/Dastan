using Dastan.org.ed.ea.entity;
using Dastan.org.ed.ea.services.log;


namespace Dastan.org.ed.ea.util
{
    public class TabUtility
    {
        public LogTabService LogTab { get; }

        private TabUtility(Context context)
        {
            LogTab = new LogTabService(context);
        }

        public static TabUtility Create(Context context)
        {
            return new TabUtility(context);
        }
    }
}