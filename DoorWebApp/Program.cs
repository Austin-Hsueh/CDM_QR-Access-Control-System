using JWT;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NLog.Web;
using System.Text;
using DoorDB;
using Newtonsoft.Json;

namespace DoorWebApp
{
    public class Program
    {
        public static string TempDir { set; get; } = $@"{AppDomain.CurrentDomain.BaseDirectory}temp";
        static string SPA_Server = "http://localhost:8080";

        public static void Main(string[] args)
        {
            Directory.CreateDirectory(TempDir);

            #region builder
            var builder = WebApplication.CreateBuilder(args);
            builder.Host.UseSystemd();

            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddTransient<AuditLogWritter>();


            // NLog: Setup NLog for Dependency injection
            builder.Logging.SetMinimumLevel(LogLevel.Trace);
            builder.Host.UseNLog();

            

            var env = builder.Environment.EnvironmentName;
            

            // DB Context
            builder.Services.AddDbContext<DoorDbContext>(options =>
            {
                string DoorDBConectionString = builder.Configuration.GetConnectionString("DoorDB");
                options.UseMySql(DoorDBConectionString, ServerVersion.AutoDetect(DoorDBConectionString));
            });




            builder.Services.AddMemoryCache();
            

            // Authentication
            builder.Services.AddSingleton<JWTHelper>();
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    // 當驗證失敗時，回應標頭會包含 WWW-Authenticate 標頭，這裡會顯示失敗的詳細錯誤原因
                    options.IncludeErrorDetails = true; // 預設值為 true，有時會特別關閉

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        // 透過這項宣告，就可以從 "sub" 取值並設定給 User.Identity.Name
                        NameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier",
                        // 透過這項宣告，就可以從 "roles" 取值，並可讓 [Authorize] 判斷角色
                        RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",

                        // 一般我們都會驗證 Issuer
                        ValidateIssuer = true,
                        ValidIssuer = builder.Configuration.GetValue<string>("JwtSettings:Issuer"),

                        // 通常不太需要驗證 Audience
                        ValidateAudience = false,
                        //ValidAudience = "JwtAuthDemo", // 不驗證就不需要填寫

                        // 一般我們都會驗證 Token 的有效期間
                        ValidateLifetime = true,

                        // 如果 Token 中包含 key 才需要驗證，一般都只有簽章而已
                        ValidateIssuerSigningKey = false,

                        // "1234567890123456" 應該從 IConfiguration 取得
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration.GetValue<string>("JwtSettings:SignKey")))
                    };

                });

            // Add CORS policy
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAllOrigins",
                    builder =>
                    {
                        builder.AllowAnyOrigin()
                               .AllowAnyMethod()
                               .AllowAnyHeader();
                    });
            });

            #endregion

            #region App middleware
            var app = builder.Build();

            //Create DB
            using (var serviceScope = app.Services.GetService<IServiceScopeFactory>().CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetRequiredService<DoorDbContext>();
                context.Database.Migrate();
                context.Database.EnsureCreated();
                //SeedTestSiteData(context);
                //SeedTestProjectData(context);
                //SeedKaizenTestData(context);
            }


            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
                });
            }

            app.UseStaticFiles();
            app.UseRouting();

            app.UseCors("AllowAllOrigins");

            app.UseAuthentication();
            if (app.Environment.IsDevelopment())
                app.UseHttpsRedirection();
            app.UseAuthorization();
            if (app.Environment.IsDevelopment())
                app.MapControllers();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });




            //於開發環境下可直接對接vue server (npm run serve)
            //if (app.Environment.IsDevelopment())
            //{
            //    app.UseSpa(builder =>
            //    {
            //        builder.UseProxyToSpaDevelopmentServer(SPA_Server);
            //    });
            //}
            //else
            //{
            app.MapFallbackToFile("index.html");
            //}

            app.Run();
            #endregion
        }

        #region Test Data Seeding
        private static void SeedTestSiteData(DoorDbContext ctx)
        {

            
        }
        #endregion
    }
}