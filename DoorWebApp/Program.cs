using JWT;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NLog.Web;
using System.Text;
using DoorDB;
using Newtonsoft.Json;
using Quartz;
using static ScheduledJob;

namespace DoorWebApp
{
    public class Program
    {
        public static string TempDir { set; get; } = $@"{AppDomain.CurrentDomain.BaseDirectory}temp";
        // static string SPA_Server = "http://localhost:80";
        static string SPA_Server = "http://localhost:3000";

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
                    // �����ҥ��ѮɡA�^�����Y�|�]�t WWW-Authenticate ���Y�A�o�̷|��ܥ��Ѫ��Բӿ��~��]
                    options.IncludeErrorDetails = true; // �w�]�Ȭ� true�A���ɷ|�S�O����

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        // �z�L�o���ŧi�A�N�i�H�q "sub" ���Ȩó]�w�� User.Identity.Name
                        NameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier",
                        // �z�L�o���ŧi�A�N�i�H�q "roles" ���ȡA�åi�� [Authorize] �P�_����
                        RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",

                        // �@��ڭ̳��|���� Issuer
                        ValidateIssuer = true,
                        ValidIssuer = builder.Configuration.GetValue<string>("JwtSettings:Issuer"),

                        // �q�`���ӻݭn���� Audience
                        ValidateAudience = false,
                        //ValidAudience = "JwtAuthDemo", // �����ҴN���ݭn��g

                        // �@��ڭ̳��|���� Token �����Ĵ���
                        ValidateLifetime = true,

                        // �p�G Token ���]�t key �~�ݭn���ҡA�@�볣�u��ñ���Ӥw
                        ValidateIssuerSigningKey = false,

                        // "1234567890123456" ���ӱq IConfiguration ���o
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

            // �K�[ Quartz �A��
            builder.Services.AddQuartz(q =>
            {
                // �]�m�@�ӧ@�~�MĲ�o��
                var jobKey = new JobKey("ScheduledJob");
                q.AddJob<ScheduledJob>(opts => opts.WithIdentity(jobKey));
                
                // 定時觸發器 - 每 10 分鐘執行一次
                q.AddTrigger(opts => opts
                    .ForJob(jobKey)
                    .WithIdentity("ScheduledJob-trigger")
                    .WithSchedule(CronScheduleBuilder.CronSchedule("45 09,19,29,39,49,59 * * * ?")));
                
                // 啟動時立即執行一次的觸發器
                q.AddTrigger(opts => opts
                    .ForJob(jobKey)
                    .WithIdentity("ScheduledJob-startup-trigger")
                    .StartNow());

                // 設定第二個工作 - 晚上12點自動存檔關帳
                var closeAccountJobKey = new JobKey("ScheduledJobCloseAccount");
                q.AddJob<ScheduledJobCloseAccount>(opts => opts.WithIdentity(closeAccountJobKey));
                
                // 定時觸發器 - 每天晚上 12:00 執行一次 (Cron: 0 0 0 * * ? = 每天午夜)
                q.AddTrigger(opts => opts
                    .ForJob(closeAccountJobKey)
                    .WithIdentity("ScheduledJobCloseAccount-trigger")
                    .WithSchedule(CronScheduleBuilder.CronSchedule("0 0 0 * * ?")));
            });

            // �K�[ Quartz �D���A��
            builder.Services.AddQuartzHostedService();
            //builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

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

                // 開發模式下提供 docs 資料夾的靜態檔案
                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
                        System.IO.Path.Combine(Directory.GetCurrentDirectory(), "docs")),
                    RequestPath = "/docs"
                });
            }

            app.UseStaticFiles();
            app.UseRouting();

            app.UseCors("AllowAllOrigins");

            app.UseAuthentication();
            if (app.Environment.IsDevelopment())
                app.UseHttpsRedirection();
            app.UseAuthorization();
            //if (app.Environment.IsDevelopment())
                app.MapControllers();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            //��}�o���ҤU�i�����ﱵvue server (npm run serve)
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