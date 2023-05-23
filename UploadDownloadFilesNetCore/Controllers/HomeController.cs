using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System.Diagnostics;
using UploadDownloadFilesNetCore.Models;
using System.IO;
using Microsoft.AspNetCore.StaticFiles;

namespace UploadDownloadFilesNetCore.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Folder()
        {
            try
            {
                var folderName = Path.Combine("Resources", "Upload");
                var pathToRead = Path.Combine(Directory.GetCurrentDirectory(), folderName);

                var files = Directory.GetFiles(pathToRead)
                    .Select(fullPath => Path.Combine(folderName, Path.GetFileName(fullPath)));

                ViewBag.Files = files;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(500, $"Internal error: {ex.Message}");
            }
            
        }

        [HttpGet, DisableRequestSizeLimit]
        public async Task<IActionResult> Download(string filepath, CancellationToken cancellationToken)
        {
            try
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), filepath);
                var fileName = Path.GetFileName(filePath);

                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound();
                }

                var memory = new MemoryStream();

                using (var stream = new FileStream(filePath, FileMode.Open))
                {
                    await stream.CopyToAsync(memory);
                }
                memory.Position = 0;

                return File(memory, GetContentType(filePath), fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(500, $"Internal error: {ex.Message}");
            }
        }

        [HttpPost, DisableRequestSizeLimit]
        public async Task<IActionResult> Upload(CancellationToken cancellationToken)
        {
            try
            {
                var form = await Request.ReadFormAsync(cancellationToken);
                var files = form.Files;
                var folderName = Path.Combine("Resources", "Upload");
                var pathToSave = Path.Combine(Directory.GetCurrentDirectory(), folderName);

                if (files.Any())
                {
                    foreach (var file in files)
                    {
                        var fileName = file.FileName.Trim();
                        var fullPath = Path.Combine(pathToSave, fileName);
                        var dbPath = Path.Combine(folderName, fileName);

                        using (var stream = new FileStream(fullPath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }
                    }
                    return Redirect("Index");
                }
                return BadRequest();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(500, $"Internal error: {ex.Message}");
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private string GetContentType(string path)
        {
            var provider = new FileExtensionContentTypeProvider();
            string contentType;

            if (!provider.TryGetContentType(path, out contentType))
            {
                contentType = "application/octet-stream";
            }

            return contentType;
        }
    }
}