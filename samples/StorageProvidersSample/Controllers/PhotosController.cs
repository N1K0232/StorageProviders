using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using StorageProvidersSample.BusinessLayer;
using StorageProvidersSample.BusinessLayer.Models;
using StorageProvidersSample.DataAccessLayer.Entities;

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

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await photoService.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Photo>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList()
    {
        var photos = await photoService.GetListAsync();
        return Ok(photos);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Read(Guid id)
    {
        StreamFileContent content = await photoService.ReadAsync(id);
        if (content != null)
        {
            return File(content.Stream, content.ContentType);
        }

        return NotFound();
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Upload(IFormFile file, string description)
    {
        await photoService.SaveAsync(file.OpenReadStream(), file.FileName, description);
        return NoContent();
    }
}