using StorageProvidersSample.BusinessLayer;
using StorageProvidersSample.DataAccessLayer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("SqlConnection");
builder.Services.AddSqlServer<DataContext>(connectionString);

var azureStorageConnectionString = builder.Configuration.GetConnectionString("AzureStorage");
var storageFolder = builder.Configuration.GetValue<string>("AppSettings:StorageFolder");

if (!string.IsNullOrWhiteSpace(azureStorageConnectionString))
{
    builder.Services.AddAzureStorage(options =>
    {
        options.ConnectionString = azureStorageConnectionString;
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