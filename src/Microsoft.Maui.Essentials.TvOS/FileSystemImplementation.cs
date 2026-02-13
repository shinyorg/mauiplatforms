using System.IO;
using Foundation;
using Microsoft.Maui.Storage;

namespace Microsoft.Maui.Essentials.TvOS;

class FileSystemImplementation : IFileSystem
{
    public string CacheDirectory =>
        GetDirectory(NSSearchPathDirectory.CachesDirectory);

    public string AppDataDirectory =>
        GetDirectory(NSSearchPathDirectory.LibraryDirectory);

    public Task<Stream> OpenAppPackageFileAsync(string filename)
    {
        var file = GetFullAppPackageFilePath(filename);
        return Task.FromResult((Stream)File.OpenRead(file));
    }

    public Task<bool> AppPackageFileExistsAsync(string filename)
    {
        var file = GetFullAppPackageFilePath(filename);
        return Task.FromResult(File.Exists(file));
    }

    static string GetFullAppPackageFilePath(string filename)
    {
        ArgumentNullException.ThrowIfNull(filename);
        filename = filename.Replace('\\', '/');
        return Path.Combine(NSBundle.MainBundle.BundlePath, filename);
    }

    static string GetDirectory(NSSearchPathDirectory directory)
    {
        var dirs = NSSearchPath.GetDirectories(directory, NSSearchPathDomain.User);
        return dirs?.Length > 0 ? dirs[0] : string.Empty;
    }
}
