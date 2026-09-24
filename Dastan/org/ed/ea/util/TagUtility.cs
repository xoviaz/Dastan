using System;
using EA;

namespace Dastan.org.ed.ea.util
{
    public class TagUtility
    {
        public static string GetSafeTagValue(Collection taggedValuesCollection, string tagName, string defaultValue, bool isAttribute = false)
        {
            if (taggedValuesCollection == null)
                return defaultValue;

            try
            {
                if (!isAttribute)
                {
                    foreach (TaggedValue tv in taggedValuesCollection)
                    {
                        // Match on short name or fully-qualified name
                        if (string.Equals(tv.Name, tagName, StringComparison.OrdinalIgnoreCase) ||
                            (tv.FQName != null && tv.FQName.EndsWith("::" + tagName, StringComparison.OrdinalIgnoreCase)))
                        {
                            // Handle memo-style tags
                            if (tv.Value == "<memo>")
                                return tv.Notes ?? defaultValue;

                            return string.IsNullOrEmpty(tv.Value) ? defaultValue : tv.Value;
                        }
                    }    
                }
                else
                {
                    foreach (AttributeTag tv in taggedValuesCollection)
                    {
                        // Match on short name or fully-qualified name
                        if (string.Equals(tv.Name, tagName, StringComparison.OrdinalIgnoreCase) ||
                            (tv.FQName != null && tv.FQName.EndsWith("::" + tagName, StringComparison.OrdinalIgnoreCase)))
                        {
                            // Handle memo-style tags
                            if (tv.Value == "<memo>")
                                return tv.Notes ?? defaultValue;

                            return string.IsNullOrEmpty(tv.Value) ? defaultValue : tv.Value;
                        }
                    }
                }
                
            }
            catch
            {
                // swallow
            }

            return defaultValue;
        }
        
        public static bool HasTaggedValue(Element element, string tagName)
        {
            if (element == null || string.IsNullOrEmpty(tagName))
                return false;

            foreach (TaggedValue tv in element.TaggedValues)
            {
                if (string.Equals(tv.Name, tagName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}