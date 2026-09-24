using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Windows.Forms;
using Dastan.org.ed.ea.entity;
using Dastan.org.ed.ea.services.log;
using Dastan.org.ed.ea.settings;
using Dastan.org.ed.ea.util;
using Newtonsoft.Json.Linq;

namespace Dastan.org.ed.ea.pipeline.jira
{
    public class JiraFetchPipeline : Pipeline<ValueTuple, JArray>
    {
        public JiraFetchPipeline(LogTabService logService, Context context, IPipeline<JArray> nextPipeline)
            : base(logService, context, nextPipeline)
        {
        }

        public override void Do(ValueTuple input)
        {
            JArray totalIssues = new JArray();
            int startAt = 0;

            if (!int.TryParse(AppSettings.JiraPageSize, out int maxResults) || maxResults <= 0)
            {
                LogService.Log($"[CONFIGURATION ERROR] Invalid Jira Page Size '{AppSettings.JiraPageSize}'. Please set a valid page size in Settings.", 0);
                return;
            }

            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AppSettings.JiraToken?.Trim());

                    int total = 0;
                    do
                    {
                        string jql = JqlUtil.GetJql();
                        
                        string jiraUrl = $"{AppSettings.JiraDomain}/rest/api/2/search?jql={Uri.EscapeDataString(jql)}&startAt={startAt}&maxResults={maxResults}";
                        HttpResponseMessage response = client.GetAsync(jiraUrl).GetAwaiter().GetResult();

                        if (!response.IsSuccessStatusCode)
                        {
                            string errorBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                            LogService.Log($"[HTTP ERROR] Status: {response.StatusCode} - {response.ReasonPhrase} - {errorBody}", 0);
                            break;
                        }

                        string jsonResponse = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        JObject json = JObject.Parse(jsonResponse);

                        total = json["total"]?.Value<int>() ?? 0;
                        JArray issues = json["issues"] as JArray;
                        if (issues == null || issues.Count == 0)
                        {
                            MessageBox.Show("No issues found", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        // Correct way: merge the issues
                        totalIssues.Merge(issues);

                        startAt += issues.Count;
                        LogService.Log($"Fetched {startAt} / {total} tasks from Jira...", 0);

                    } while (startAt < total);
                }
            }
            catch (Exception ex)
            {
                LogService.Log($"[FETCH EXCEPTION] {ex.Message}", 0);
            }

            NextPipeline?.Do(totalIssues);
        }
    }
}