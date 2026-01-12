using System.Windows.Media.Imaging;

namespace KIOSK.Infrastructure.Common.Utils
{
    public static class BitmapSafe
    {
        public static BitmapImage LoadBitmap(Uri uri)
        {
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.UriSource = uri;
            bi.CacheOption = BitmapCacheOption.OnLoad; // 스트림 닫아도 내부 데이터 유지
            bi.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
            bi.EndInit();
            bi.Freeze(); // 스레드 안전, Freezable 문제 예방
            return bi;
        }
    }
}
