using System;
using System.Collections.Generic;
using Dastan.org.ed.ea.entity;
using EA;

namespace Dastan.org.ed.ea.repository
{
    public class ElementRepository
    {
        private const string ModificationLogsElementName = "DastanModificationLogs";

        public Element FindModificationLogsElement(Repository repository) =>
            FindModificationLogsElement(repository, out _);

        public Element FindModificationLogsElement(Repository repository, out bool repositoryReady)
        {
            repositoryReady = false;
            if (repository?.Models == null || repository.Models.Count == 0) return null;
            
            Package package = repository.Models.GetAt(0) as Package;
            if (package == null) return null;
            
            repositoryReady = true;

            foreach (Element element in package.Elements)
            {
                if (string.Equals(element.Name, ModificationLogsElementName, StringComparison.Ordinal))
                    return element;
            }

            return null;
        }

        public Element GetOrCreateModificationLogsElement(Repository repository)
        {
            Element target = FindModificationLogsElement(repository);
            if (target != null) return target;
            
            if (repository?.Models == null || repository.Models.Count == 0) return null;
            
            Package package = repository.Models.GetAt(0) as Package;
            if (package == null) return null;
            
            target = package.Elements.AddNew(ModificationLogsElementName, "Artifact") as Element;
            if (target == null) return null;

            target.Update();
            package.Elements.Refresh();
            return target;
        }

        public Element SafeGetElementById(Repository repository, int id)
        {
            try
            {
                return repository.GetElementByID(id);
            }
            catch
            {
                return null;
            }
        }
        
        public List<Element> GetStandaloneElementByDiagramIdAndTypeAndStereotype(Context context, int diagramId, string type, string stereotype)
        {
            string sql = $@"
        SELECT o.Object_ID
        FROM t_diagramobjects do
        INNER JOIN t_object o ON o.Object_ID = do.Object_ID
        WHERE do.Diagram_ID = {diagramId}
          AND o.Object_Type = '{Escape(type)}'
          AND o.Stereotype = '{Escape(stereotype)}'
          AND o.Object_ID NOT IN (
              SELECT c.Start_Object_ID
              FROM t_diagramlinks dl
              INNER JOIN t_connector c ON c.Connector_ID = dl.ConnectorID
              WHERE dl.DiagramID = {diagramId}
                AND dl.Hidden = 0

              UNION

              SELECT c.End_Object_ID
              FROM t_diagramlinks dl
              INNER JOIN t_connector c ON c.Connector_ID = dl.ConnectorID
              WHERE dl.DiagramID = {diagramId}
                AND dl.Hidden = 0
          )";

            // GetElementSet is the fastest way to turn SQL into Elements
            Collection elements = context.Repository.GetElementSet(sql, 2); // 2 = SQL mode

            var result = new List<Element>();
            foreach (Element el in elements)
                result.Add(el);

            return result;
        }
        public List<Element> GetElementByDiagramIdAndTypeAndStereotype(Context context, int diagramId, string type, string stereotype)
        {
            string sql = $@"
        SELECT o.Object_ID
        FROM t_diagramobjects do
        INNER JOIN t_object o ON o.Object_ID = do.Object_ID
        WHERE do.Diagram_ID = {diagramId}
          AND o.Object_Type = '{Escape(type)}'
          AND o.Stereotype = '{Escape(stereotype)}'";

            // GetElementSet is the fastest way to turn SQL into Elements
            Collection elements = context.Repository.GetElementSet(sql, 2); // 2 = SQL mode

            var result = new List<Element>();
            foreach (Element el in elements)
                result.Add(el);

            return result;
        }
        
        private string Escape(string value) => value?.Replace("'", "''") ?? "";
    }
}