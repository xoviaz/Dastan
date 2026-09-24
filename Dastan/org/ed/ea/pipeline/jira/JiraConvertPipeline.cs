using System.Collections.Generic;
using Dastan.org.ed.ea.entity;
using Dastan.org.ed.ea.services.log;
using Dastan.org.ed.ea.settings;
using Newtonsoft.Json.Linq;

namespace Dastan.org.ed.ea.pipeline.jira
{
    public class JiraConvertPipeline : Pipeline<JArray, List<JiraTaskDto>>
    {
        public JiraConvertPipeline(LogTabService logService, Context context, IPipeline<List<JiraTaskDto>> nextPipeline)
            : base(logService, context, nextPipeline)
        {
        }
        
        public override void Do(JArray issues)
        {
            List<JiraTaskDto> resultList = new List<JiraTaskDto>();

            foreach (var issue in issues)
            {
                if (issue is JObject issueObj)
                {
                    JiraTaskDto dto = MapToDto(issueObj);
                    if (dto != null)
                    {
                        resultList.Add(dto);
                    }
                }
            }

            NextPipeline?.Do(resultList);
        }

        private JiraTaskDto MapToDto(JObject issue)
        {
            JObject fields = issue["fields"] as JObject;
            if (fields == null) return null;

            string fixVersion = "";
            JArray fixVersionsArray = fields["fixVersions"] as JArray;
            if (fixVersionsArray != null && fixVersionsArray.Count > 0)
            {
                fixVersion = fixVersionsArray[0]?["name"]?.ToString() ?? "";
            }

            string rawStatus = fields["status"]?["name"]?.ToString() ?? "";
            string mappedStatus = MapStatus(rawStatus);

            return new JiraTaskDto.Builder()
                .WithKey(GetString(issue,"key"))
                .WithSummary(GetString(fields,"summary"))
                .WithDescription(GetString(fields,"description"))
                .WithBuildNumber(GetString(fields, "customfield_11401"))
                .WithVersion(GetString(fields, "customfield_11400"))
                .WithFixVersion(fixVersion)
                .WithStatus(mappedStatus)
                .WithAuthor(GetString(fields, "reporter", "name"))
                .WithAssignee(GetString(fields,"assignee", "name"))
                .WithProjectKey(GetString(fields, "project", "key"))
                .WithProjectName(GetString(fields, "project", "name"))
                .WithProjectId(GetString(fields, "project", "id"))
                .Build();
        }

        private string MapStatus(string rawStatus)
        {
            if (string.IsNullOrWhiteSpace(rawStatus))
                return "Proposed";

            switch (rawStatus.Trim().ToLowerInvariant())
            {
                case "draft":       return AppSettings.JiraDraftEquivalent;
                case "to do":       return AppSettings.JiraToDoEquivalent;
                case "in progress": return AppSettings.JiraInProgressEquivalent;
                case "in review":   return AppSettings.JiraInReviewEquivalent;
                case "done":        return AppSettings.JiraDoneEquivalent;
                default:            return AppSettings.JiraToDoEquivalent;
            }
        }
        
        private string GetString(JToken parent, params string[] path)
        {
            JToken current = parent;
            foreach (string part in path)
            {
                if (current == null || current.Type == JTokenType.Null)
                    return "";
                
                if (current.Type != JTokenType.Object)
                    return "";
                
                current = current[part];
            }
            
            if (current == null || current.Type == JTokenType.Null)
                return "";
            
            return current.ToString();
        }
    }
}