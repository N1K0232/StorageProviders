using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using StorageProvidersSample.BusinessLayer;
using StorageProvidersSample.BusinessLayer.Models;

namespace StorageProvidersSample.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class PhotosController : ControllerBase
{
    private readonly IPhotoService photoService;

    public PhotosController(IPhotoService photoService)
    {
        this.photoService = photoService;
    }

    [HttpDelete("{fileName}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(string fileName)
    {
        await photoService.DeleteAsync(fileName);
        return NoContent();
    }

    [HttpGet("{fileName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Read(string fileName)
    {
        StreamFileContent content = await photoService.ReadAsync(fileName);
        if (content != null)
        {
            return File(content.Stream, content.ContentType);
        }

        return NotFound();
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        await photoService.SaveAsync(file.FileName, file.OpenReadStream());
        return NoContent();
    }
}