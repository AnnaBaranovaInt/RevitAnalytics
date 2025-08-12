using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAnalytics
{
    public static class PathManager
    {
        public enum PathOption
        {
            Debug,
            Desktop,
            ProgramFiles
        }

        // Establece la opción de ruta aquí
        private static readonly PathOption currentPathOption = PathOption.Debug;

        public static string GetBaseFolderPath()
        {
#if DEBUG
            switch (currentPathOption)
            {
                case PathOption.Debug:
                    return @"C:\Users\baraa\source\repos\RevitAnalytics\RevitAnalytics";
                case PathOption.ProgramFiles:
                    return @"C:\Program Files (x86)\LapLengthCalculator";
                case PathOption.Desktop:
                default:
                    return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Automation");
            }
        }
#else
            return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "GridManager");
            }
#endif

        public static string GetImagePath(string imageName)
        {
            string imagePath = System.IO.Path.Combine(GetBaseFolderPath(), "Images", imageName);
            DebugHandler.Log($"Image Path: {imagePath}", DebugHandler.LogLevel.INFO);
            return imagePath;
        }

        public static string GetFilePath(string fileName)
        {
            return System.IO.Path.Combine(GetBaseFolderPath(), fileName);
        }
        public static string GetLogFilePath()
        {
            string logFilePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "DebugRevitAnalyticsLog.txt");
            return logFilePath;
        }
    }
}



