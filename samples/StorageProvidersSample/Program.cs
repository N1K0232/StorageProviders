using StorageProvidersSample.BusinessLayer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var azureStorage = builder.Configuration.GetConnectionString("AzureStorage");
var storageFolder = builder.Configuration.GetValue<string>("AppSettings:StorageFolder");

if (!string.IsNullOrWhiteSpace(azureStorage))
{
    builder.Services.AddAzureStorage(options =>
    {
        options.ConnectionString = azureStorage;
        options.ContainerName = storageFolder;
    });
}
else
{
    builder.Services.AddFileSystemStorage(options =>
    {
        options.SiteRootFolder = builder.Environment.ContentRootPath;
        options.StorageFolder = storageFolder ?? string.Empty;
    });
}

builder.Services.AddScoped<IPhotoService, PhotoService>();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();
app.Run();