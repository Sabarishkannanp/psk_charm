using System;
using System.IO;
using System.Reflection;
using System.Web.Script.Serialization;

namespace PSKCharms
{
    public class AppSettings
    {
        public string ActiveCharmId { get; set; }
        public string ActiveCharmName { get; set; }
        public string ActiveCharmPath { get; set; }
        public string CordStyle { get; set; } // Classic, BeadChain, GoldenThread, NeonGlow, Invisible
        public string CordColorHex { get; set; }
        public double CordLength { get; set; }
        public double CordThickness { get; set; }
        public double CharmScale { get; set; }
        public double AnchorPercentX { get; set; }
        public double AnchorOffsetY { get; set; }
        public bool AmbientBreeze { get; set; }
        public double BreezeIntensity { get; set; }
        public double Gravity { get; set; }
        public double Damping { get; set; }
        public bool AlwaysOnTop { get; set; }
        public bool ClickThrough { get; set; }
        public bool StartWithWindows { get; set; }
        public bool GlowBackdrop { get; set; }

        public AppSettings()
        {
            GlowBackdrop = true;
            // Sensible defaults
            ActiveCharmId = "psk_developer";
            ActiveCharmName = "PSK Developer";
            ActiveCharmPath = "Assets/Charms/PSK Developer.png";
            CordStyle = "Classic";
            CordColorHex = "#FFD700"; // Rich Gold
            CordLength = 220.0;
            CordThickness = 3.0;
            CharmScale = 1.0;
            AnchorPercentX = 0.50; // Top center of screen
            AnchorOffsetY = 0.0;
            AmbientBreeze = true;
            BreezeIntensity = 1.0;
            Gravity = 9.8;
            Damping = 0.985;
            AlwaysOnTop = true;
            ClickThrough = false;
            StartWithWindows = false;
        }

        public static string GetSettingsFilePath()
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            if (Directory.Exists(Path.Combine(appDir, "Assets")))
                return Path.Combine(appDir, "settings.json");
            string subDir = Path.Combine(appDir, "PSK_Charms");
            if (Directory.Exists(subDir))
                return Path.Combine(subDir, "settings.json");
            return Path.Combine(appDir, "settings.json");
        }

        public static AppSettings Load()
        {
            try
            {
                string path = GetSettingsFilePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var serializer = new JavaScriptSerializer();
                    var settings = serializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                    {
                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading settings: " + ex.Message);
            }
            var defaultSettings = new AppSettings();
            defaultSettings.Save();
            return defaultSettings;
        }

        public void Save()
        {
            try
            {
                string path = GetSettingsFilePath();
                var serializer = new JavaScriptSerializer();
                string json = serializer.Serialize(this);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error saving settings: " + ex.Message);
            }
        }
    }
}
