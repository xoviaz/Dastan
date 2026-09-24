using System;
using System.Collections.Generic;
using Dastan.org.ed.ea.pipeline.jira.jql;
using Dastan.org.ed.ea.settings;

namespace Dastan.org.ed.ea.util
{
    public class JqlUtil
    {
        public static string GetJql()
        {
            List<JqlOption> options = new List<JqlOption> { new JqlOption("project", "=", AppSettings.JiraProject) };
            
            if (AppSettings.JiraScanIncremental && DateTime.TryParse(AppSettings.JiraLastSync, out DateTime lastSync))
                options.Add(new JqlOption("updated", ">", lastSync.ToString("yyyy-MM-dd")));

            if (AppSettings.JiraCustomJql.Length > 0)
                options.Add(new JqlOption("","", AppSettings.JiraCustomJql));

            if (AppSettings.JiraTaskType != null && AppSettings.JiraTaskType != "All")
                options.Add(new JqlOption("issueType", "=", AppSettings.JiraTaskType));
            
            return new JqlBuilder(options).Build();
        }
    }
}