using System;
using System.Globalization;
using Dastan.org.ed.ea.entity;
using Dastan.org.ed.ea.pipeline.jira;
using Dastan.org.ed.ea.services.log;
using Dastan.org.ed.ea.settings;
using Dastan.org.ed.ea.util;

namespace Dastan.org.ed.ea.services.jira
{
    public class SyncJira
    {
        public void ExecuteJiraSync(Context context)
        {
            LogTabService logTabService = TabUtility.Create(context).LogTab;
            logTabService.Clear();
            JiraGeneratePipeline generate = new JiraGeneratePipeline(logTabService, context);
            JiraConvertPipeline convert = new JiraConvertPipeline(logTabService, context, generate);
            JiraFetchPipeline fetch = new JiraFetchPipeline(logTabService, context, convert);
            fetch.Do(default);
            AppSettings.JiraLastSync = DateTime.Now.ToLocalTime().ToString(CultureInfo.CurrentCulture);
        }
    }
}