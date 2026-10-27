using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PSKCharms
{
    public class CharmMetadata
    {
        public string id { get; set; }
        public string name { get; set; }
        public string category { get; set; }
        public string path { get; set; }
        public string origin { get; set; }
        public string description { get; set; }
        public string actionText { get; set; }
        public string gradientStart { get; set; }
        public string gradientEnd { get; set; }
    }

    public class CharmItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string RelativePath { get; set; }
        public string Origin { get; set; }
        public string Description { get; set; }
        public string ActionText { get; set; }
        public string GradientStart { get; set; }
        public string GradientEnd { get; set; }
        public bool IsCustom { get; set; }

        public static string GetAssetsRoot()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            if (File.Exists(Path.Combine(baseDir, "charms.json")))
                return baseDir;
            string subDir = Path.Combine(baseDir, "PSK_Charms");
            if (File.Exists(Path.Combine(subDir, "charms.json")))
                return subDir;
            string parentDir = Path.GetDirectoryName(baseDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (parentDir != null && File.Exists(Path.Combine(parentDir, "charms.json")))
                return parentDir;
            return baseDir;
        }

        private BitmapImage _cachedFullBitmap;
        private BitmapImage _cachedThumbBitmap;

        public BitmapImage GetBitmap(int decodeWidth)
        {
            if (decodeWidth <= 150 && _cachedThumbBitmap != null)
                return _cachedThumbBitmap;
            if (decodeWidth > 150 && _cachedFullBitmap != null)
                return _cachedFullBitmap;

            try
            {
                string baseDir = GetAssetsRoot();
                string fullPath = Path.Combine(baseDir, RelativePath.Replace('/', Path.DirectorySeparatorChar));

                if (!File.Exists(fullPath))
                {
                    // Fallback 1: Assets/Charms/FileName
                    fullPath = Path.Combine(baseDir, "Assets", "Charms", Path.GetFileName(RelativePath));
                }

                if (!File.Exists(fullPath))
                {
                    // Fallback 2: Subfolder search under Assets/Charms
                    string charmsDir = Path.Combine(baseDir, "Assets", "Charms");
                    if (Directory.Exists(charmsDir))
                    {
                        string fileName = Path.GetFileName(RelativePath);
                        var match = Directory.GetFiles(charmsDir, fileName, SearchOption.AllDirectories).FirstOrDefault();
                        if (match != null) fullPath = match;
                    }
                }

                if (!File.Exists(fullPath))
                {
                    // Fallback 3: Try ASCII name without accent
                    string simplified = RemoveDiacritics(Path.GetFileName(RelativePath));
                    string charmsDir = Path.Combine(baseDir, "Assets", "Charms");
                    if (Directory.Exists(charmsDir))
                    {
                        var match = Directory.GetFiles(charmsDir, simplified, SearchOption.AllDirectories).FirstOrDefault();
                        if (match != null) fullPath = match;
                    }
                }

                if (File.Exists(fullPath))
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    if (decodeWidth > 0)
                    {
                        bmp.DecodePixelWidth = decodeWidth;
                    }
                    bmp.UriSource = new Uri(fullPath, UriKind.Absolute);
                    bmp.EndInit();
                    bmp.Freeze();

                    if (decodeWidth <= 150)
                        _cachedThumbBitmap = bmp;
                    else
                        _cachedFullBitmap = bmp;

                    return bmp;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading charm bitmap: " + ex.Message);
            }

            // Ultimate fallback to official PSK developer avatar or default icon so charm is never missing
            try
            {
                string baseDir = GetAssetsRoot();
                string fallback = Path.Combine(baseDir, "Assets", "psk_developer.png");
                if (File.Exists(fallback))
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    if (decodeWidth > 0) bmp.DecodePixelWidth = decodeWidth;
                    bmp.UriSource = new Uri(fallback, UriKind.Absolute);
                    bmp.EndInit();
                    bmp.Freeze();
                    return bmp;
                }
            }
            catch { }

            return null;
        }

        private Point? _attachPoint;

        // Normalized (0..1) point on the image where the cord should attach: the topmost
        // visible pixel, so transparent padding above the artwork never leaves a gap.
        public Point GetAttachPoint()
        {
            if (_attachPoint.HasValue) return _attachPoint.Value;
            Point result = new Point(0.5, 0.0);
            try
            {
                var bmp = GetBitmap(350);
                if (bmp != null)
                {
                    var fc = new FormatConvertedBitmap(bmp, PixelFormats.Bgra32, null, 0);
                    int w = fc.PixelWidth, h = fc.PixelHeight, stride = w * 4;
                    var px = new byte[stride * h];
                    fc.CopyPixels(px, stride, 0);

                    int top = -1;
                    for (int y = 0; y < h && top < 0; y++)
                        for (int x = 0; x < w; x++)
                            if (px[y * stride + x * 4 + 3] > 40) { top = y; break; }

                    if (top >= 0)
                    {
                        double sum = 0; int n = 0;
                        for (int y = top; y < Math.Min(h, top + 3); y++)
                            for (int x = 0; x < w; x++)
                                if (px[y * stride + x * 4 + 3] > 40) { sum += x; n++; }
                        result = new Point((sum / n) / w, (double)top / h);
                    }
                }
            }
            catch { }
            _attachPoint = result;
            return result;
        }

        private static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return text.Replace("ğ", "g").Replace("Ğ", "G")
                       .Replace("á", "a").Replace("é", "e")
                       .Replace("ö", "o").Replace("Ö", "O")
                       .Replace("ü", "u").Replace("Ü", "U")
                       .Replace("Pnchang Ji", "Panchang Jie")
                       .Replace("Thor Mjlnir", "Thor Mjolnir");
        }

        public BitmapImage GetBitmap()
        {
            return GetBitmap(320);
        }
    }

    public class CharmCatalog
    {
        public List<CharmItem> Charms { get; private set; }
        public List<string> Categories { get; private set; }

        public CharmCatalog()
        {
            Charms = new List<CharmItem>();
            Categories = new List<string>();
        }

        public static CharmCatalog Load()
        {
            var catalog = new CharmCatalog();
            string baseDir = CharmItem.GetAssetsRoot();
            string jsonPath = Path.Combine(baseDir, "charms.json");

            if (File.Exists(jsonPath))
            {
                try
                {
                    string json = File.ReadAllText(jsonPath);
                    var serializer = new JavaScriptSerializer();
                    var list = serializer.Deserialize<List<CharmMetadata>>(json);
                    if (list != null)
                    {
                        foreach (var item in list)
                        {
                            catalog.Charms.Add(new CharmItem
                            {
                                Id = item.id,
                                Name = item.name,
                                Category = item.category,
                                RelativePath = item.path,
                                Origin = !string.IsNullOrEmpty(item.origin) ? item.origin : item.category.ToUpperInvariant(),
                                Description = !string.IsNullOrEmpty(item.description) ? item.description : ("A handcrafted " + item.name + " desktop companion charm."),
                                ActionText = !string.IsNullOrEmpty(item.actionText) ? item.actionText : "✨ Swing",
                                GradientStart = !string.IsNullOrEmpty(item.gradientStart) ? item.gradientStart : "#1e293b",
                                GradientEnd = !string.IsNullOrEmpty(item.gradientEnd) ? item.gradientEnd : "#334155",
                                IsCustom = false
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error parsing charms.json: " + ex.Message);
                }
            }

            // Also check for user Custom charms
            string customDir = Path.Combine(baseDir, "Assets", "Charms", "Custom");
            if (Directory.Exists(customDir))
            {
                var customFiles = Directory.GetFiles(customDir, "*.*")
                    .Where(s => s.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                                s.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                s.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                                s.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) ||
                                s.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) ||
                                s.EndsWith(".gif", StringComparison.OrdinalIgnoreCase));

                foreach (var file in customFiles)
                {
                    string name = Path.GetFileNameWithoutExtension(file);
                    string relPath = "Assets/Charms/Custom/" + Path.GetFileName(file);
                    catalog.Charms.Add(new CharmItem
                    {
                        Id = "custom_" + name.ToLowerInvariant().Replace(" ", "_"),
                        Name = name,
                        Category = "Custom",
                        RelativePath = relPath,
                        Origin = "CUSTOM STICKER",
                        Description = "Personal custom charm created from your photos.",
                        ActionText = "✨ Hang custom",
                        GradientStart = "#0f172a",
                        GradientEnd = "#2563eb",
                        IsCustom = true
                    });
                }
            }

            // Build unique categories list
            var cats = catalog.Charms.Select(c => c.Category).Distinct().OrderBy(c => c).ToList();
            catalog.Categories.Add("All");
            foreach (var c in cats)
            {
                catalog.Categories.Add(c);
            }

            return catalog;
        }

        public CharmItem AddCustomCharm(string sourceFilePath)
        {
            try
            {
                if (!File.Exists(sourceFilePath))
                    return null;

                string baseDir = CharmItem.GetAssetsRoot();
                string customDir = Path.Combine(baseDir, "Assets", "Charms", "Custom");
                if (!Directory.Exists(customDir))
                {
                    Directory.CreateDirectory(customDir);
                }

                string ext = Path.GetExtension(sourceFilePath);
                string fileName = Path.GetFileName(sourceFilePath);
                string destFile = Path.Combine(customDir, fileName);

                int counter = 1;
                while (File.Exists(destFile))
                {
                    destFile = Path.Combine(customDir, Path.GetFileNameWithoutExtension(fileName) + "_" + counter + ext);
                    counter++;
                }

                File.Copy(sourceFilePath, destFile, true);

                string finalName = Path.GetFileNameWithoutExtension(destFile);
                var newItem = new CharmItem
                {
                    Id = "custom_" + finalName.ToLowerInvariant().Replace(" ", "_"),
                    Name = finalName,
                    Category = "Custom",
                    RelativePath = "Assets/Charms/Custom/" + Path.GetFileName(destFile),
                    Origin = "CUSTOM STICKER",
                    Description = "Personal custom charm created from your photos.",
                    ActionText = "✨ Hang custom",
                    GradientStart = "#0f172a",
                    GradientEnd = "#2563eb",
                    IsCustom = true
                };

                Charms.Insert(0, newItem);
                if (!Categories.Contains("Custom"))
                {
                    Categories.Add("Custom");
                }
                return newItem;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error adding custom charm: " + ex.Message);
                return null;
            }
        }
    }
}
