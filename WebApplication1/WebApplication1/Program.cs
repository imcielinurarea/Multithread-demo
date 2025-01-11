using WebApplication1;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
// Đăng ký MultiFileDownload trong DI container, sử dụng factory để tạo đối tượng với các tham số
builder.Services.AddTransient<MultiFileDownload>(provider =>
{
    // Bạn có thể lấy tham số từ cấu hình hoặc từ một nguồn khác
    string url = "http://example.com/yourfile.ext"; // Thay đổi URL này theo nhu cầu
    int connections = 4; // Hoặc lấy từ một cấu hình khác

    return new MultiFileDownload(url, connections);
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
