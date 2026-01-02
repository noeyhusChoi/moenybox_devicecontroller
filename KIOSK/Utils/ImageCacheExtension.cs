using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace KIOSK.Utils
{
    public static class ImageCacheExtension
    {
        private static ConcurrentDictionary<string, BitmapImage> _cache = new();

        public static BitmapImage GetOrAdd(string filePath, Func<BitmapImage> factory)
            => _cache.GetOrAdd(filePath, _ => factory());

        public static void ClearAll()
        {
            _cache.Clear();
        }
    }
}
