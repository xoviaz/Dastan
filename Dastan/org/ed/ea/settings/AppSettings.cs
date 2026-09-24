using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace Dastan.org.ed.ea.settings
{
    public static class AppSettings
    {
        private const string RegPath = @"Software\DastanSettings";

        public static string DefaultOutputFolder
        {
            get { return Get("OutputFolder", "C:\\temp"); }
            set { Set("OutputFolder", value ?? "C:\\temp"); }
        }

        public static string StringResourcePrefix
        {
            get { return Get("Prefix", "emxFramework"); }
            set { Set("Prefix", value ?? "emxFramework"); }
        }

        public static bool AutoOpenExport
        {
            get { return Get("AutoOpenExport", "0") == "1"; }
            set { Set("AutoOpenExport", value ? "1" : "0"); }
        }

        public static bool TrackModifications
        {
            get { return Get("TrackModifications", "1") == "1"; }
            set { Set("TrackModifications", value ? "1" : "0"); }
        }

        public static string JiraDomain
        {
            get { return Get("JiraDomain", ""); }
            set { Set("JiraDomain", value ?? ""); }
        }

        public static string JiraProject
        {
            get { return Get("JiraProject", ""); }
            set { Set("JiraProject", value ?? ""); }
        }

        public static string JiraToken
        {
            get { return Decrypt(Get("JiraToken", "")); }
            set { Set("JiraToken", Encrypt(value ?? "")); }
        }

        public static string JiraPageSize
        {
            get { return Get("JiraPageSize", ""); }
            set { Set("JiraPageSize", value ?? ""); }
        }

        public static bool JiraScanIncremental
        {
            get { return Get("JiraSearchIncremental", "0") == "1"; }
            set { Set("JiraSearchIncremental", value ? "1" : "0"); }
        }

        public static string JiraLastSync
        {
            get { return Get("JiraLastSync", ""); }
            set { Set("JiraLastSync", value ?? ""); }
        }

        public static string JiraDraftEquivalent
        {
            get { return Get("JiraDraftEquivalent", "Proposed"); }
            set { Set("JiraDraftEquivalent", value ?? "Proposed"); }
        }

        public static string JiraToDoEquivalent
        {
            get { return Get("JiraToDoEquivalent", "Proposed"); }
            set { Set("JiraToDoEquivalent", value ?? "Proposed"); }
        }

        public static string JiraInProgressEquivalent
        {
            get { return Get("JiraInProgressEquivalent", "Validated"); }
            set { Set("JiraInProgressEquivalent", value ?? "Validated"); }
        }

        public static string JiraInReviewEquivalent
        {
            get { return Get("JiraInReviewEquivalent", "Approved"); }
            set { Set("JiraInReviewEquivalent", value ?? "Approved"); }
        }

        public static string JiraDoneEquivalent
        {
            get { return Get("JiraDoneEquivalent", "Implemented"); }
            set { Set("JiraDoneEquivalent", value ?? "Implemented"); }
        }

        public static string JiraCustomJql
        {
            get { return Get("JiraCustomJql", ""); }
            set { Set("JiraCustomJql", value ?? ""); }
        }

        public static string JiraTaskType
        {
            get { return Get("JiraTaskType", "All"); }
            set { Set("JiraTaskType", value ?? "All"); }
        }

        public static bool SkipPreviewWhenClean
        {
            get { return Get("SkipPreviewWhenClean", "0") == "1"; }
            set { Set("SkipPreviewWhenClean", value ? "1" : "0"); }
        }

        public static string PreviewLayout
        {
            get { return Get("PreviewLayout", ""); }
            set { Set("PreviewLayout", value ?? ""); }
        }

        public static string ActiveChangeset
        {
            get { return  Get("ActiveChangeset", ""); }
            set { Set("ActiveChangeset", value ?? ""); }
        }
        
        private static string Get(string name, string defaultValue)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegPath))
            {
                if (key == null) return defaultValue;
                object val = key.GetValue(name, defaultValue);
                return val != null ? val.ToString() : defaultValue;
            }
        }

        private static void Set(string name, string value)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegPath))
            {
                if (key != null)
                    key.SetValue(name, value);
            }
        }

        // Encrypted with Windows DPAPI, scoped to the current Windows user, so the
        // Jira token is never written to the registry in plain text.
        private static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return "";

            try
            {
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(encryptedBytes);
            }
            catch
            {
                return plainText;
            }
        }

        private static string Decrypt(string storedValue)
        {
            if (string.IsNullOrEmpty(storedValue)) return "";

            try
            {
                byte[] encryptedBytes = Convert.FromBase64String(storedValue);
                byte[] plainBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch
            {
                return storedValue;
            }
        }
    }
}