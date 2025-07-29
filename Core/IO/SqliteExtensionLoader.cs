using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Data.Sqlite;

public class SqliteExtensionLoader
{
    private static string GetPlatformExtensionPath()
    {
        // Get the directory where the executable is running
        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        
        // Build platform-specific path
        string platformFolder;
        string extensionFile;
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            platformFolder = "MacOs";
            extensionFile = "vss0.dylib";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            platformFolder = "Linux";
            extensionFile = "vss0.so";
        }
        else
        {
            throw new PlatformNotSupportedException($"Platform {RuntimeInformation.OSDescription} is not supported");
        }
        
        return Path.Combine(baseDirectory, "SQLite-Extensions", platformFolder, extensionFile);
    }
    
    private static string GetVectorExtensionPath()
    {
        string vssPath = GetPlatformExtensionPath();
        string directory = Path.GetDirectoryName(vssPath);
        string vectorFile = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "vector0.dylib" : "vector0.so";
        
        return Path.Combine(directory, vectorFile);
    }
    
    private static string GetArchitecture()
    {
        return RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.Arm64 => "aarch64",
            Architecture.Arm => "arm",
            _ => throw new PlatformNotSupportedException($"Architecture {RuntimeInformation.ProcessArchitecture} is not supported")
        };
    }
    
    public static string GetPlatformInfo()
    {
        string os = "unknown";
        string arch = GetArchitecture();
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            os = "windows";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            os = "macos";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            os = "linux";
            
        return $"{os}-{arch}";
    }
    
    public static void LoadVssExtension(SqliteConnection connection)
    {
        try
        {
            string vssPath = GetPlatformExtensionPath();
            string vectorPath = GetVectorExtensionPath();
            
            if (!File.Exists(vssPath))
            {
                string platformInfo = GetPlatformInfo();
                throw new FileNotFoundException(
                    $"VSS extension not found at {vssPath}. " +
                    $"Please download the appropriate extension for {platformInfo} from " +
                    $"https://github.com/asg017/sqlite-vss/releases and place it in the correct sqlite-extensions folder."
                );
            }
            
            // Enable extensions
            connection.EnableExtensions(true);
            
            // Load vector0 extension
            connection.LoadExtension(vectorPath);
            Console.WriteLine($"Successfully loaded Vector extension from: {vectorPath}");

            // Load VSS extension
            connection.LoadExtension(vssPath);
            Console.WriteLine($"Successfully loaded VSS extension from: {vssPath}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load VSS extension: {ex.Message}", ex);
        }
    }
}