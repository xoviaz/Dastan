using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Dastan.org.ed.ea.entity;
using Dastan.org.ed.ea.services.log;
using Dastan.org.ed.ea.services.validation;
using Dastan.org.ed.ea.tool;
using Dastan.org.ed.ea.util;
using EA;

namespace Dastan.org.ed.ea.commands
{
    // Runs the model checks over whatever is selected -- a package, an element, a diagram,
    // or the whole model when nothing useful is.
    //
    // The script linter already catches most of these, but only once a script exists, when
    // the fix means going back into the model and exporting again. This is the same set of
    // mistakes caught while they are still one edit away from being fixed.
    public class ValidateModelCommand : ICommand
    {
        public void Execute(Context context)
        {
            try
            {
                List<ModelObject> objects = new ModelCollector().Collect(context.Repository, out string scope);
                List<ModelIssue> issues = new ModelValidator().Validate(objects);

                LogIssues(context, issues);

                if (issues.Count == 0)
                {
                    MessageBox.Show("Checked " + objects.Count + " object(s) in " + scope + ".\r\n\r\n" +
                                    "No problems found.", "Model Check",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                ModelValidationForm.ShowIssues(context.Repository, scope, objects.Count, issues);
            }
            catch (Exception e)
            {
                MessageBox.Show("Error: " + e.Message, "Model Check");
            }
        }

        public bool HasAccess(Repository repository)
        {
            return true;
        }

        // Keeps a record in the output tab that outlives the window, the same way the
        // script linter does with its findings.
        private static void LogIssues(Context context, List<ModelIssue> issues)
        {
            if (issues.Count == 0) return;

            try
            {
                LogTabService log = TabUtility.Create(context).LogTab;
                foreach (ModelIssue issue in issues)
                    log.Log("[Model Check] " + issue, 0);
            }
            catch
            {
                // Best-effort only; the window already shows the issues.
            }
        }
    }
}
