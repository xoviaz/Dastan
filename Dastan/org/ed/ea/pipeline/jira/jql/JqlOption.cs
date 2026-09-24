namespace Dastan.org.ed.ea.pipeline.jira.jql
{
    public class JqlOption : IJqlOption
    {
        public string Key { get; }
        public string Value { get; }
        public string Operate { get; }
        
        public JqlOption(string key, string operate, string value)
        {
            Key = key;
            Operate = operate;
            Value = value;
        }


        public string Apply()
        {
            return Key + " " +  Operate + " " + Value;
        }
    }
}