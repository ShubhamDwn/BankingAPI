using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace BankingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdvertisementsController : ControllerBase
    {
        private readonly string adsFolderPath;

        public AdvertisementsController(IWebHostEnvironment env)
        {
            adsFolderPath = Path.Combine(env.WebRootPath, "ads");
            if (!Directory.Exists(adsFolderPath))
                Directory.CreateDirectory(adsFolderPath);
        }

        // Validate file signature
        private bool IsValidImageFile(IFormFile file)
        {
            try
            {
                byte[] buffer = new byte[4];
                using var stream = file.OpenReadStream();
                stream.Read(buffer, 0, buffer.Length);

                return (buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF) ||           // JPEG
                       (buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47) || // PNG
                       (buffer[0] == 0x47 && buffer[1] == 0x49 && buffer[2] == 0x46);            // GIF
            }
            catch { return false; }
        }

        // Upload Ad Image
        [HttpPost("upload")]
        public async Task<IActionResult> UploadAdImage(IFormFile image)
        {
            if (image == null || image.Length == 0)
                return BadRequest("No file uploaded.");
            if (image.Length > 5 * 1024 * 1024)
                return BadRequest("Image size should not exceed 5 MB.");
            if (image.FileName.Length > 100)
                return BadRequest("Filename too long.");

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
            var ext = Path.GetExtension(image.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(ext))
                return BadRequest("Unsupported file type.");
            if (!allowedMimeTypes.Contains(image.ContentType.ToLowerInvariant()))
                return BadRequest("Invalid MIME type.");
            if (!IsValidImageFile(image))
                return BadRequest("File signature mismatch.");

            var fileName = Path.GetFileNameWithoutExtension(image.FileName);
            fileName = string.Concat(fileName.Split(Path.GetInvalidFileNameChars())) + ext;

            var savePath = Path.Combine(adsFolderPath, fileName);
            using var stream = new FileStream(savePath, FileMode.Create);
            await image.CopyToAsync(stream);

            var imageUrl = $"{Request.Scheme}://{Request.Host}/ads/{fileName}";
            return Ok(new { imageUrl });
        }

        // List Ads
        [HttpGet("list")]
        public IActionResult GetAllAdImages()
        {
            var files = Directory.GetFiles(adsFolderPath);
            var baseUrl = $"{Request.Scheme}://{Request.Host}/ads/";
            var extensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".txt" };

            var urls = files
                .Where(f => extensions.Contains(Path.GetExtension(f).ToLower()))
                .Select(f => baseUrl + Path.GetFileName(f))
                .ToList();

            return Ok(urls);
        }

        // Delete Ad
        [HttpDelete("delete/{fileName}")]
        public IActionResult DeleteAdImage(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return BadRequest("Filename is required.");

            fileName = Path.GetFileName(fileName);
            var fullPath = Path.Combine(adsFolderPath, fileName);

            if (!System.IO.File.Exists(fullPath))
                return NotFound("File not found.");

            System.IO.File.Delete(fullPath);
            return Ok($"Deleted {fileName}");
        }

        // Upload Popup
        [HttpPost("upload-popup")]
        public async Task<IActionResult> UploadPopup(IFormFile popupFile)
        {
            if (popupFile == null || popupFile.Length == 0)
                return BadRequest("No file uploaded.");
            if (popupFile.Length > 5 * 1024 * 1024)
                return BadRequest("Popup size should not exceed 5 MB.");
            if (popupFile.FileName.Length > 100)
                return BadRequest("Filename too long.");

            var ext = Path.GetExtension(popupFile.FileName).ToLowerInvariant();
            if (!new[] { ".jpg", ".jpeg", ".png", ".txt", ".webp" }.Contains(ext))
                return BadRequest("Invalid extension for popup.");
            if (!IsValidImageFile(popupFile) && ext != ".txt")
                return BadRequest("Popup file is not a valid image.");

            var savePath = Path.Combine(adsFolderPath, "popup" + ext);
            using var stream = new FileStream(savePath, FileMode.Create);
            await popupFile.CopyToAsync(stream);

            return Ok("Popup uploaded.");
        }

        // Delete Popup
        [HttpDelete("delete-popup")]
        public IActionResult DeletePopup()
        {
            string[] files = Directory.GetFiles(adsFolderPath, "popup.*")
                .Where(f => f.EndsWith(".jpg") || f.EndsWith(".png") || f.EndsWith(".txt") || f.EndsWith(".webp")).ToArray();

            foreach (var file in files)
                System.IO.File.Delete(file);

            var metaFile = Path.Combine(adsFolderPath, "popup.meta.json");
            if (System.IO.File.Exists(metaFile))
                System.IO.File.Delete(metaFile);

            return Ok("Popup deleted.");
        }

        // Set Popup Duration
        [HttpPost("set-popup-duration")]
        public async Task<IActionResult> SetPopupDuration([FromForm] int hours)
        {
            if (hours <= 0)
                return BadRequest("Duration must be greater than 0.");

            var meta = new { durationHours = hours, lastShown = (string)null };
            var metaPath = Path.Combine(adsFolderPath, "popup.meta.json");
            var json = JsonSerializer.Serialize(meta);
            await System.IO.File.WriteAllTextAsync(metaPath, json);

            return Ok("Popup duration set.");
        }

        // Upload Logo
        [HttpPost("upload-logo")]
        public async Task<IActionResult> UploadLogo(IFormFile image)
        {
            if (image == null || image.Length == 0)
                return BadRequest("No image uploaded.");
            if (image.Length > 5 * 1024 * 1024)
                return BadRequest("Image size must not exceed 5 MB.");
            if (image.FileName.Length > 100)
                return BadRequest("Filename too long.");

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var ext = Path.GetExtension(image.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(ext))
                return BadRequest("Invalid logo format.");
            if (!IsValidImageFile(image))
                return BadRequest("Invalid image content.");

            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "logos");
            if (!Directory.Exists(uploadsPath))
                Directory.CreateDirectory(uploadsPath);

            foreach (var file in Directory.GetFiles(uploadsPath, "bank_logo.*"))
                System.IO.File.Delete(file);

            var filePath = Path.Combine(uploadsPath, "bank_logo" + ext);
            using var stream = new FileStream(filePath, FileMode.Create);
            await image.CopyToAsync(stream);

            var publicUrl = $"{Request.Scheme}://{Request.Host}/logos/{Path.GetFileName(filePath)}";
            return Ok(new { message = "Logo uploaded successfully", url = publicUrl });
        }

        // Delete Logo
        [HttpDelete("delete-logo")]
        public IActionResult DeleteLogo()
        {
            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "logos");

            if (!Directory.Exists(uploadsPath))
                return NotFound("Logo folder not found.");

            var logoFiles = Directory.GetFiles(uploadsPath, "bank_logo.*");
            if (logoFiles.Length == 0)
                return NotFound("No logo to delete.");

            foreach (var file in logoFiles)
                System.IO.File.Delete(file);

            return Ok(new { message = "Logo deleted successfully." });
        }
    }
}
