using Dastan.org.ed.ea.entity;

namespace Dastan.org.ed.ea.services.log
{
    public class LogTabService
    {
        private readonly string _logTab = "Script Generator Log";
        private readonly Context _context;

        public LogTabService(Context context)
        {
            _context = context;
            _context.Repository.CreateOutputTab(_logTab);
        }

        public void Clear()
        {
            _context.Repository.ClearOutput(_logTab);
        }

        public void Log(string title, int id)
        {
            _context.Repository.EnsureOutputVisible(_logTab);
            _context.Repository.WriteOutput(_logTab, title, id);
        }
    }
}