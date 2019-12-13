using System;
using System.IO;
using Newtonsoft.Json.Linq;
using Windows.Storage;

namespace ApolloLensLibrary.Utilities
{
    public class UserConfig
    {
        #region Singleton
        public static UserConfig Instance { get; } = new UserConfig();

        static UserConfig() { }
        private UserConfig()
        {
            this.Configure();
        }

        #endregion

        #region PrivateProperties

        private ApplicationDataContainer localData = ApplicationData.Current.LocalSettings;
        private JObject defaultSettings;
        private JObject userSettings;

        private void Configure()
        {
            using (StreamReader r = File.OpenText("Library\\Utilities\\DefaultConfig.json"))
            {
                string json = r.ReadToEnd();
                defaultSettings = JObject.Parse(json);
            }

            if (localData.Values.ContainsKey("Settings"))
            {
                userSettings = JObject.Parse(localData.Values["Settings"] as string);
            } else
            {
                userSettings = defaultSettings;
                this.SaveSettings();
            }
        }

        #endregion

        #region Interface

        public void SaveSettings()
        {
            localData.Values["Settings"] = userSettings.ToString();
        }

        public JToken GetProperty(string propertyName)
        {
            if (userSettings.SelectToken(propertyName) == null || userSettings.SelectToken(propertyName).Type == JTokenType.Undefined)
            {
                if (defaultSettings.SelectToken(propertyName) != null && defaultSettings.SelectToken(propertyName).Type != JTokenType.Undefined)
                {
                    userSettings.Add(defaultSettings.SelectToken(propertyName));
                    this.SaveSettings();
                } else
                {
                    // Setting doesn't exist... oh no?
                    throw new Exception();
                }
            }
            return userSettings.SelectToken(propertyName);
        }

        public void SaveProperty(string propertyName, JToken value)
        {
            userSettings.SelectToken(propertyName).Replace(value);
        }

        public void ResetSettings()
        {
            userSettings = defaultSettings;
            this.SaveSettings();
        }

        #endregion

    }
}
