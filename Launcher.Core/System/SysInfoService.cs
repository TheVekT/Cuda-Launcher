using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using Launcher.Core.System.Abstractions;

namespace Launcher.Core.System;

public class SysInfoService : ISysInfoService
{
    // --- WinAPI для разрешений монитора ---
    [StructLayout(LayoutKind.Sequential)]
    public struct DEVMODE
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

    [DllImport("user32.dll")]
    private static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);

    private const int ENUM_CURRENT_SETTINGS = -1;

    /// <summary>
    /// Возвращает общий объем физической ОЗУ в Мегабайтах
    /// </summary>
    public long GetTotalRAMInMB()
    {
        try
        {
            using (var searcher = new ManagementObjectSearcher("SELECT Capacity FROM Win32_PhysicalMemory"))
            {
                long totalCapacity = 0;
                foreach (var obj in searcher.Get())
                {
                    totalCapacity += Convert.ToInt64(obj["Capacity"]);
                }
                Debug.WriteLine($"Total RAM from WMI: {totalCapacity / (1024 * 1024)} MB");
                return totalCapacity / (1024 * 1024);
            }
        }
        catch
        {
            // Fallback: если WMI не сработал, берем то, что видит ОС (может быть чуть меньше физической)
            Debug.WriteLine($"Failed to get RAM from WMI, falling back to GC info. Total available memory: {GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024 * 1024)} MB");
            return (long)GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024 * 1024);
        }
    }

    /// <summary>
    /// Возвращает список доступных разрешений основного монитора в формате "1920x1080"
    /// </summary>
    public IEnumerable<string> GetPrimaryMonitorResolutions()
    {
        var resolutions = new HashSet<string>();
        var devMode = new DEVMODE();
        int modeIndex = 0;

        // Перебираем все поддерживаемые видеокартой режимы
        while (EnumDisplaySettings(null, modeIndex, ref devMode))
        {
            // Фильтруем: берем только те, где глубина цвета 32 бита (стандарт для игр)
            // и ширина экрана больше 800 (чтобы не предлагать совсем старые режимы)
            if (devMode.dmBitsPerPel == 32 && devMode.dmPelsWidth >= 800)
            {
                resolutions.Add($"{devMode.dmPelsWidth}x{devMode.dmPelsHeight}");
            }
            modeIndex++;
        }

        // Сортируем от большего к меньшему
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
