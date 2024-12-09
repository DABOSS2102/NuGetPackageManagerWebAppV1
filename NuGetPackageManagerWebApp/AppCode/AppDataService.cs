using System.IO;

namespace NuGetPackageManagerWebApp.AppCode
{
    public class AppDataService
    {
        private readonly string _appDataPath;

        public AppDataService()
        {
            // Get the App_Data folder path
            _appDataPath = Path.Combine(Directory.GetCurrentDirectory(), "App_Data");

            // Ensure the folder exists
            if (!Directory.Exists(_appDataPath))
            {
                Directory.CreateDirectory(_appDataPath);
            }
        }

        // Write data to a file
        public void WriteToFile(string fileName, string content)
        {
            var filePath = Path.Combine(_appDataPath, fileName);
            File.WriteAllText(filePath, content);
        }

        // Read data from a file
        public string ReadFromFile(string fileName)
        {
            var filePath = Path.Combine(_appDataPath, fileName);
            return File.Exists(filePath) ? File.ReadAllText(filePath) : null;
        }

        // List all files in the App_Data folder
        public IEnumerable<string> GetFiles()
        {
            return Directory.EnumerateFiles(_appDataPath);
        }
    }
}
