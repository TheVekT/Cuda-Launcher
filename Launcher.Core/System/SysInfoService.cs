using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using Launcher.Core.System.Abstractions;
// ReSharper disable InconsistentNaming

namespace Launcher.Core.System;

public class SysInfoService : ISysInfoService
{
    // Windows API for monitor resolutions
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct Devmode
    {
        private const int CCHDEVICENAME = 32;
        private const int CCHFORMNAME = 32;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
        public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public int dmDisplayOrientation;
        public int dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
        public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool EnumDisplaySettings(string? deviceName, int modeNum, ref Devmode devMode);

    /// <summary>
    /// Returns the total physical RAM in MB using WMI. If WMI fails, it falls back to using GC info.
    /// </summary>
    public long GetTotalRAMInMB()
    {
        if (OperatingSystem.IsWindows())
        {
            try
            {
                // language=none
                using var searcher = new ManagementObjectSearcher("SELECT Capacity FROM Win32_PhysicalMemory");
                using var collection = searcher.Get();

                long totalCapacity = 0;
                foreach (var obj in collection)
                {
                    using (obj)
                    {
                        totalCapacity += Convert.ToInt64(obj["Capacity"]);
                    }
                }

                long totalRamMb = totalCapacity / (1024 * 1024);
                Debug.WriteLine($"Total RAM from WMI: {totalRamMb} MB");
                return totalRamMb;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to get RAM from WMI: {ex.Message}");
            }
        }

        // Fallback: Use GC info to get total available memory
        var fallbackRamMb = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024 * 1024);
        Debug.WriteLine($"Falling back to GC info. Total available memory: {fallbackRamMb} MB");
        return fallbackRamMb;
    }

    /// <summary>
    /// Returns a list of supported resolutions for the primary monitor, sorted from highest to lowest.
    /// </summary>
    public IEnumerable<string> GetPrimaryMonitorResolutions()
    {
        var resolutions = new HashSet<string>();

        if (OperatingSystem.IsWindows())
        {
            var devMode = new Devmode();
            devMode.dmSize = (short)Marshal.SizeOf<Devmode>();
            int modeIndex = 0;

            // Enumerate all display settings for the primary monitor
            while (EnumDisplaySettings(null, modeIndex, ref devMode))
            {
                // Include resolutions with width >= 800 and height >= 600
                if (devMode.dmPelsWidth >= 800 && devMode.dmPelsHeight >= 600)
                {
                    resolutions.Add($"{devMode.dmPelsWidth}x{devMode.dmPelsHeight}");
                }
                modeIndex++;
            }
        }

        // Sort the resolutions first by width, then by height, both in descending order
        return resolutions
            .Select(r => {
                var parts = r.Split('x');
                return new { Original = r, Width = int.Parse(parts[0]), Height = int.Parse(parts[1]) };
            })
            .OrderByDescending(r => r.Width)
            .ThenByDescending(r => r.Height)
            .Select(r => r.Original);
    }
}
