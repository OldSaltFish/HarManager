using System;
using System.IO;
using System.Threading.Tasks;
using HarManager.Models;
using Newtonsoft.Json;

namespace HarManager.Services
{
    public class HarService
    {
        public async Task<HarRoot?> ParseHarFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("HAR file not found.", filePath);
            }

            try
            {
                using var streamReader = new StreamReader(filePath);
                var jsonContent = await streamReader.ReadToEndAsync();
                return JsonConvert.DeserializeObject<HarRoot>(jsonContent);
            }
            catch (Exception ex)
            {
                // In a real app, log this error
                Console.WriteLine($"Error parsing HAR file: {ex.Message}");
                return null;
            }
        }
    }
}

