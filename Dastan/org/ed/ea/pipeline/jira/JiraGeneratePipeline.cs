using System;
using System.Collections.Generic;
using Dastan.org.ed.ea.entity;
using Dastan.org.ed.ea.services.jira;
using Dastan.org.ed.ea.services.log;
using Dastan.org.ed.ea.util;
using EA;

namespace Dastan.org.ed.ea.pipeline.jira
{
    public class JiraGeneratePipeline : TerminalPipeline<List<JiraTaskDto>>
    {
        public JiraGeneratePipeline(LogTabService logService, Context context) 
            : base(logService, context) { }

        public override void Do(List<JiraTaskDto> tasks)
        {
            if (tasks == null || tasks.Count == 0)
            {
                LogService.Log("[COMPLETE] No jira tasks to process.", 0);
                return;
            }
            
            Package targetPackage = Context.Repository.GetTreeSelectedPackage();
            if (targetPackage == null)
            {
                LogService.Log("[ERROR] Please select a package in the Project Browser before syncing.", 0);
                return;
            }
            
            Package projectPackage = null;
            
            // Check if user has selected the package which already has project tasks
            if (string.Compare(targetPackage.Name, tasks[0].ProjectKey + " - " + tasks[0].ProjectName, StringComparison.OrdinalIgnoreCase) == 0)
            {
                projectPackage = targetPackage;
            }
            else
            {
                projectPackage = PackageUtil.GetOrCreateJiraProjectPackage(targetPackage, tasks[0].ProjectKey, tasks[0].ProjectName, tasks[0].ProjectId);
            }
            
            using (ProgressForm progress = new ProgressForm())
            {
                progress.Show();

                int createdCount = 0;
                int updatedCount = 0;

                for (int i = 0; i < tasks.Count; i++)
                {
                    JiraTaskDto task = tasks[i];

                    // Update UI progress bar
                    progress.UpdateProgress(i + 1, tasks.Count, $"Processing [{task.Key}] {task.Summary}");

                    // Check if element exists in target package by Alias
                    Element targetElement = ElementExistsByAlias(projectPackage, task.Key);
                    if (targetElement == null)
                    {
                        // Create EA Element (Requirement)
                        Element newElement = (Element)projectPackage.Elements.AddNew(task.Key + " - " + task.Summary, JiraTaskDto.Type);
                        newElement.Alias = task.Key;
                        newElement.Notes = task.Description;
                        newElement.Status = task.Status;
                        newElement.Author = task.Author;
                        newElement.Phase = task.FixVersion;
                        newElement.Version = task.Version;
                        newElement.Update();
                        
                        SetTagValue(newElement, "Build Number", task.BuildNumber);
                        SetTagValue(newElement, "Assignee", task.Assignee);

                        createdCount++;
                        LogService.Log($"[CREATED] Element for {task.Key}: {task.Summary}", 0);
                    }
                    else
                    {
                        targetElement.Alias = task.Key;
                        targetElement.Notes = task.Description;
                        targetElement.Status = task.Status;
                        targetElement.Author = task.Author;
                        targetElement.Phase = task.FixVersion;
                        targetElement.Version = task.Version;
                        targetElement.Update();
                        
                        SetTagValue(targetElement, "Build Number", task.BuildNumber);
                        SetTagValue(targetElement, "Assignee", task.Assignee);
                        updatedCount++;
                        LogService.Log($"[UPDATED] {task.Key} already exists in target package.", 0);
                    }
                }

                // Batch refresh Project Browser tree view once at the end
                projectPackage.Elements.Refresh();
                LogService.Log($"[COMPLETE] Done! Created: {createdCount}, Updated: {updatedCount}.", 0);

                progress.Close();
            }
        }
        
        private void SetTagValue(Element element, string tagKey, string tagValue)
        {
            if (element == null || string.IsNullOrEmpty(tagKey))
                return;

            tagValue = tagValue ?? "";

            // Try to find existing tag
            foreach (TaggedValue tv in element.TaggedValues)
            {
                if (string.Equals(tv.Name, tagKey, StringComparison.OrdinalIgnoreCase) ||
                    (tv.FQName != null && tv.FQName.EndsWith("::" + tagKey, StringComparison.OrdinalIgnoreCase)))
                {
                    // Update existing
                    if (tv.Value == "<memo>")
                    {
                        tv.Notes = tagValue;   // memo-style tag
                    }
                    else
                    {
                        tv.Value = tagValue;
                    }

                    tv.Update();
                    return;
                }
            }

            // Not found → create new
            TaggedValue newTv = (TaggedValue)element.TaggedValues.AddNew(tagKey, "");
            newTv.Value = tagValue;
            newTv.Update();
        }
        
        private Element ElementExistsByAlias(Package package, string aliasKey)
        {
            foreach (Element el in package.Elements)
            {
                if (string.Equals(el.Alias, aliasKey, StringComparison.OrdinalIgnoreCase))
                {
                    return el;
                }
            }

            return null;
        }
    }
}