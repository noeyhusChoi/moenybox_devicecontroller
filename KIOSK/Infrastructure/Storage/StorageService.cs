using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KIOSK.Infrastructure.Storage
{
    public class StorageInfo
    {
        public string DriveName { get; init; } = "";
        public string? VolumeLabel { get; init; }
        public string DriveFormat { get; init; } = "";
        public long TotalBytes { get; init; }
        public long FreeBytes { get; init; }
        public long AvailableBytes { get; init; }

        public double TotalGB => TotalBytes / 1024.0 / 1024 / 1024;
        public double FreeGB => FreeBytes / 1024.0 / 1024 / 1024;
        public double AvailableGB => AvailableBytes / 1024.0 / 1024 / 1024;

        public double FreePercent =>
            TotalBytes == 0 ? 0 : (FreeBytes * 100.0 / TotalBytes);
    }

    public interface IStorageService
    {
        /// <summary>모든 준비된 드라이브의 저장공간 정보</summary>
        IReadOnlyList<StorageInfo> GetAllDrives();

        /// <summary>특정 경로가 속한 드라이브의 저장공간 정보</summary>
        StorageInfo? GetDriveForPath(string path);

        /// <summary>해당 경로 드라이브에 최소 requiredBytes 만큼 여유 공간이 있는지 확인</summary>
        bool HasEnoughFreeSpace(string path, long requiredBytes);
    }

    public class StorageService : IStorageService
    {
        public IReadOnlyList<StorageInfo> GetAllDrives()
        {
            var drives = DriveInfo.GetDrives()
                                  .Where(d => d.IsReady)
                                  .Select(ToStorageInfo)
                                  .ToList();

            return drives;
        }

        public StorageInfo? GetDriveForPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("path is null or empty", nameof(path));

            var root = Path.GetPathRoot(path);
            if (string.IsNullOrEmpty(root))
                return null;

            var drive = DriveInfo.GetDrives()
                                 .FirstOrDefault(d => d.IsReady &&
                                                      string.Equals(d.Name, root,
                                                            StringComparison.OrdinalIgnoreCase));
            return drive == null ? null : ToStorageInfo(drive);
        }

        public bool HasEnoughFreeSpace(string path, long requiredBytes)
        {
            if (requiredBytes <= 0)
                return true;

            var info = GetDriveForPath(path);
            if (info == null)
                return false;

            return info.AvailableBytes >= requiredBytes;
        }

        private static StorageInfo ToStorageInfo(DriveInfo d)
        {
            return new StorageInfo
            {
                DriveName = d.Name,
                VolumeLabel = d.VolumeLabel,
                DriveFormat = d.DriveFormat,
                TotalBytes = d.TotalSize,
                FreeBytes = d.TotalFreeSpace,
                AvailableBytes = d.AvailableFreeSpace
            };
        }
    }
}
