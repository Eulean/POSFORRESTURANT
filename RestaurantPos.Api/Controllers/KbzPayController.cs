using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantPos.Api.Models;

namespace RestaurantPos.Api.Controllers;

[ApiController]
[Route("api/kbzpay")]
public sealed class KbzPayController : ControllerBase
{
    private readonly KbzPayQrStore _qrStore;

    public KbzPayController(KbzPayQrStore qrStore)
    {
        _qrStore = qrStore;
    }

    [HttpGet]
    [Authorize(Roles = Roles.Cashier + "," + Roles.Admin)]
    public IActionResult GetSettings() => Ok(new
    {
        hasQr = _qrStore.Exists,
        qrUrl = _qrStore.Exists ? $"/api/kbzpay/qr?v={System.IO.File.GetLastWriteTimeUtc(_qrStore.PathOnDisk).Ticks}" : null
    });

    [HttpGet("qr")]
    [AllowAnonymous]
    public IActionResult GetQr()
    {
        if (!_qrStore.Exists) return NotFound();
        Response.Headers.CacheControl = "no-store";
        return PhysicalFile(_qrStore.PathOnDisk, "image/png");
    }

    [HttpPost("qr")]
    [Authorize(Roles = Roles.Admin)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadQr([FromForm] IFormFile file)
    {
        if (!await _qrStore.SaveAsync(file))
        {
            return BadRequest("Upload a JPEG, PNG, or WebP image smaller than 10 MB and 40 megapixels.");
        }
        return GetSettings();
    }

    [HttpDelete("qr")]
    [Authorize(Roles = Roles.Admin)]
    public IActionResult DeleteQr()
    {
        _qrStore.Delete();
        return NoContent();
    }
}
