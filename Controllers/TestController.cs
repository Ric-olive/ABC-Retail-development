using ABC_Retail.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABC_Retail.Controllers
{
    public class TestController : Controller
    {
        private readonly AzureFileService _fileService;

        public TestController(AzureFileService fileService)
        {
            _fileService = fileService;
        }

        [HttpGet]
        public async Task<IActionResult> TestAzureFiles()
        {
            try
            {
                Console.WriteLine("DEBUG: Testing Azure Files connection...");
                
                var testContent = $"Test log entry created at {DateTime.UtcNow}";
                var logPath = await _fileService.UploadLogFileAsync(
                    "test_connection.log", 
                    testContent, 
                    "test");
                
                Console.WriteLine($"DEBUG: Test file uploaded successfully to: {logPath}");
                
                // Try to list files to verify
                var files = await _fileService.ListLogFilesAsync("test");
                Console.WriteLine($"DEBUG: Found {files.Count} files in test directory");
                
                return Json(new { 
                    success = true, 
                    message = "Azure Files test successful", 
                    logPath = logPath,
                    filesInDirectory = files.Count
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Azure Files test failed: {ex.Message}");
                Console.WriteLine($"DEBUG: Stack trace: {ex.StackTrace}");
                
                return Json(new { 
                    success = false, 
                    message = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }
    }
}
