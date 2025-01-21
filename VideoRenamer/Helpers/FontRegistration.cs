using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Drishya.Helpers
{
    public static class FontRegistration
    {
        private static readonly string FontRegPath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty,
            "Tools",
            "FontReg.exe"
        );

        public static void RegisterFonts()
        {
            try
            {
                RegisterFont("Poppins_Regular", ".ttf");
            }
            catch (Exception ex)
            {
                // Log error or show message if needed
                Debug.WriteLine($"Font registration failed: {ex.Message}");
            }
        }

        private static void RegisterFont(string resourceName, string extension)
        {
            string tempPath = Path.Combine(
                Path.GetTempPath(),
                $"{resourceName}_{Guid.NewGuid()}{extension}"
            );

            try
            {
                // Convert byte array to stream
                using (var memoryStream = new MemoryStream(Properties.Resources.Poppins_Regular))
                using (var fileStream = File.Create(tempPath))
                {
                    memoryStream.CopyTo(fileStream);
                }

                var info = new ProcessStartInfo()
                {
                    FileName = FontRegPath,
                    Arguments = $"/copy \"{tempPath}\"",
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(info))
                {
                    process?.WaitForExit();
                }
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    try
                    {
                        File.Delete(tempPath);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
            }
        }
    }
}