namespace Dastan.org.ed.ea.entity
{
    public class JiraTaskDto
    {
        public string Key { get; private set; }
        public string Summary { get; private set; }
        public string Description { get; private set; }
        public const string Type = "Task";
        public string Version { get; private set; }
        public string BuildNumber { get; private set; }
        public string FixVersion { get; private set; }
        public string Status { get; private set; }
        public string Author { get; private set; }
        public string Assignee { get; private set; }
        public string ProjectKey { get; private set; }
        public string ProjectName { get; private set; }
        public string ProjectId { get; private set; }
        
        public override string ToString()
        {
            return $"Key: {Key}\n" +
                   $"Summary: {Summary}\n" +
                   $"Description: {Description}\n" +
                   $"Version: {Version}\n" +
                   $"Build Number: {BuildNumber}\n" +
                   $"Fix Version: {FixVersion}\n" +
                   $"Status: {Status}\n" +
                   $"Author: {Author}\n" +
                   $"Assignee: {Assignee}\n" +
                   $"Project Key: {ProjectKey}\n" +
                   $"Project Name: {ProjectName}\n" +
                   $"Project Id: {ProjectId}";
        }
        
        private JiraTaskDto() {}

        public class Builder
        {
            private string _key;
            private string _summary;
            private string _description;
            private string _version;
            private string _buildNumber;
            private string _fixVersion;
            private string _status;
            private string _author;
            private string _assignee;
            private string _projectKey;
            private string _projectName;
            private string _projectId;

            public Builder WithKey(string key)
            {
                _key = key;
                return this;
            }

            public Builder WithSummary(string summary)
            {
                _summary = summary;
                return this;
            }

            public Builder WithDescription(string description)
            {
                if (!string.IsNullOrWhiteSpace(description))
                    _description = description;
                return this;
            }

            public Builder WithBuildNumber(string buildNumber)
            {
                _buildNumber = buildNumber;
                return this;
            }

            public Builder WithVersion(string version)
            {
                _version = version;
                return this;
            }

            public Builder WithFixVersion(string fixVersion)
            {
                _fixVersion = fixVersion;
                return this;
            }

            public Builder WithStatus(string status)
            {
                if (!string.IsNullOrWhiteSpace(status))
                    _status = status;
                return this;
            }

            public Builder WithAuthor(string author)
            {
                _author = author;
                return this;
            }

            public Builder WithAssignee(string assignee)
            {
                _assignee = assignee;
                return this;
            }

            public Builder WithProjectKey(string projectKey)
            {
                _projectKey = projectKey;
                return this;
            }

            public Builder WithProjectName(string projectName)
            {
                _projectName = projectName;
                return this;
            }

            public Builder WithProjectId(string projectId)
            {
                _projectId = projectId;
                return this;
            }

            public JiraTaskDto Build()
            {
                return new JiraTaskDto
                {
                    Key = _key,
                    Summary = _summary,
                    Description = _description,
                    BuildNumber = _buildNumber,
                    Version = _version,
                    FixVersion = _fixVersion,
                    Status = _status,
                    Author = _author,
                    Assignee = _assignee,
                    ProjectName =  _projectName,
                    ProjectKey =  _projectKey,
                    ProjectId = _projectId
                };
            }
        }
    }
}