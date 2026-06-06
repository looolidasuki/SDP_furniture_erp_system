using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FurnitureERP.Helpers
{
    public static class ProductImageHelper
    {
        private const string LocalPrefix = "local:";
        private static readonly object CacheLock = new object();
        private static readonly Dictionary<string, Image> Cache = new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);
        private static Image _placeholder;

        public static string ProductImagesDirectory =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FurnitureERP",
                "ProductImages");

        public static Image PlaceholderImage
        {
            get
            {
                if (_placeholder != null) return _placeholder;
                var bmp = new Bitmap(220, 180);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.FromArgb(245, 245, 245));
                    using (var pen = new Pen(Color.FromArgb(210, 210, 210)))
                        g.DrawRectangle(pen, 0, 0, bmp.Width - 1, bmp.Height - 1);
                    var text = "No Image";
                    using (var font = new Font("Segoe UI", 10f, FontStyle.Regular))
                    {
                        var size = g.MeasureString(text, font);
                        g.DrawString(
                            text,
                            font,
                            Brushes.Gray,
                            (bmp.Width - size.Width) / 2f,
                            (bmp.Height - size.Height) / 2f);
                    }
                }
                _placeholder = bmp;
                return _placeholder;
            }
        }

        /// <summary>Saves bytes under AppData and returns a short DB token (local:filename).</summary>
        public static string SaveProductImage(long productId, byte[] bytes, string originalFileName = null)
        {
            if (bytes == null || bytes.Length == 0)
                throw new ArgumentException("Image data is empty.", nameof(bytes));

            Directory.CreateDirectory(ProductImagesDirectory);

            string ext = Path.GetExtension(originalFileName);
            if (string.IsNullOrWhiteSpace(ext))
                ext = ".jpg";
            ext = ext.ToLowerInvariant();
            if (ext != ".jpg" && ext != ".jpeg" && ext != ".png" && ext != ".bmp" && ext != ".gif")
                ext = ".jpg";

            string fileName = productId + ext;
            string fullPath = Path.Combine(ProductImagesDirectory, fileName);
            File.WriteAllBytes(fullPath, bytes);

            string token = LocalPrefix + fileName;
            Invalidate(token);
            Invalidate(productId);
            return token;
        }

        public static void Invalidate(long productId)
        {
            lock (CacheLock)
            {
                var keysToRemove = new List<string>();
                string pidKey = BuildProductIdKey(productId);
                foreach (var key in Cache.Keys)
                {
                    if (key.Equals(pidKey, StringComparison.OrdinalIgnoreCase)
                        || key.StartsWith(LocalPrefix + productId, StringComparison.OrdinalIgnoreCase))
                        keysToRemove.Add(key);
                }
                foreach (var key in keysToRemove)
                    RemoveCacheEntry(key);
            }
        }

        public static void Invalidate(string imageRef)
        {
            if (string.IsNullOrWhiteSpace(imageRef)) return;
            lock (CacheLock)
                RemoveCacheEntry(NormalizeCacheKey(imageRef));
        }

        public static Image LoadImage(string imageRef, long? productId = null)
        {
            return CloneForDisplay(LoadImageCore(imageRef, productId));
        }

        public static void LoadImageAsync(string imageRef, long? productId, Action<Image> onComplete, Control invokeTarget)
        {
            if (onComplete == null) throw new ArgumentNullException(nameof(onComplete));
            if (invokeTarget == null) throw new ArgumentNullException(nameof(invokeTarget));

            Task.Run(() =>
            {
                Image img = null;
                try
                {
                    img = CloneForDisplay(LoadImageCore(imageRef, productId));
                }
                catch
                {
                    img?.Dispose();
                    img = CloneForDisplay(PlaceholderImage);
                }

                if (invokeTarget.IsDisposed)
                {
                    img?.Dispose();
                    return;
                }

                try
                {
                    invokeTarget.BeginInvoke(new Action(() =>
                    {
                        if (invokeTarget.IsDisposed)
                        {
                            img?.Dispose();
                            return;
                        }
                        onComplete(img);
                    }));
                }
                catch (ObjectDisposedException)
                {
                    img?.Dispose();
                }
            });
        }

        public static void SetPictureBoxImage(PictureBox pictureBox, Image newImage)
        {
            if (pictureBox == null) return;
            var old = pictureBox.Image;
            pictureBox.Image = newImage;
            DisposeIfOwned(old);
        }

        public static void DisposeIfOwned(Image image)
        {
            if (image == null || ReferenceEquals(image, _placeholder)) return;
            image.Dispose();
        }

        private static Image LoadImageCore(string imageRef, long? productId)
        {
            string cacheKey = BuildCacheKey(imageRef, productId);
            lock (CacheLock)
            {
                if (Cache.TryGetValue(cacheKey, out var cached))
                    return cached;
            }

            Image loaded = TryLoadFromSources(imageRef, productId);
            if (loaded == null)
                loaded = PlaceholderImage;

            lock (CacheLock)
            {
                if (!Cache.ContainsKey(cacheKey))
                    Cache[cacheKey] = loaded;
                else
                {
                    if (!ReferenceEquals(loaded, PlaceholderImage) && loaded != null)
                        loaded.Dispose();
                    loaded = Cache[cacheKey];
                }
            }

            return loaded;
        }

        private static Image TryLoadFromSources(string imageRef, long? productId)
        {
            if (!string.IsNullOrWhiteSpace(imageRef))
            {
                if (IsRemoteUrl(imageRef))
                {
                    try
                    {
                        using (var wc = new WebClient())
                        using (var ms = new MemoryStream(wc.DownloadData(imageRef.Trim())))
                            return Image.FromStream(ms);
                    }
                    catch
                    {
                        // fall through to local / placeholder
                    }
                }

                string localPath = ResolveLocalPath(imageRef);
                if (localPath != null)
                {
                    try { return Image.FromFile(localPath); }
                    catch { }
                }
            }

            if (productId.HasValue)
            {
                foreach (var ext in new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif" })
                {
                    string fallback = Path.Combine(ProductImagesDirectory, productId.Value + ext);
                    if (!File.Exists(fallback)) continue;
                    try { return Image.FromFile(fallback); }
                    catch { }
                }
            }

            return null;
        }

        public static string ResolveLocalPath(string imageRef)
        {
            if (string.IsNullOrWhiteSpace(imageRef)) return null;

            string trimmed = imageRef.Trim();
            if (trimmed.StartsWith(LocalPrefix, StringComparison.OrdinalIgnoreCase))
            {
                string fileName = trimmed.Substring(LocalPrefix.Length);
                return Path.Combine(ProductImagesDirectory, fileName);
            }

            if (Path.IsPathRooted(trimmed) && File.Exists(trimmed))
                return trimmed;

            string combined = Path.Combine(ProductImagesDirectory, trimmed);
            if (File.Exists(combined))
                return combined;

            return null;
        }

        private static bool IsRemoteUrl(string value)
        {
            return value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildCacheKey(string imageRef, long? productId)
        {
            if (!string.IsNullOrWhiteSpace(imageRef))
                return NormalizeCacheKey(imageRef);
            if (productId.HasValue)
                return BuildProductIdKey(productId.Value);
            return "placeholder";
        }

        private static string NormalizeCacheKey(string imageRef) => imageRef.Trim();

        private static string BuildProductIdKey(long productId) => "pid:" + productId;

        private static Image CloneForDisplay(Image source)
        {
            if (source == null) return (Image)PlaceholderImage.Clone();
            return (Image)source.Clone();
        }

        private static void RemoveCacheEntry(string key)
        {
            if (!Cache.TryGetValue(key, out var img)) return;
            if (!ReferenceEquals(img, _placeholder))
                img?.Dispose();
            Cache.Remove(key);
        }
    }
}
