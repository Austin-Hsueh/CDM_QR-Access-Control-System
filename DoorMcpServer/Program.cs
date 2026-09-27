using DoorMcpServer.DoorApi;
using McpKit;

// 啟動、驗證、scope、遮罩、確認、稽核都在 McpKit；這裡只註冊門禁系統自己的服務。
// 模式：預設 HTTP（API key）；--stdio 給本機開發 / 測試；--new-key 產生一把 API key。
return await McpKitHost.RunAsync(args, typeof(Program).Assembly, (services, configuration) =>
{
    services.Configure<DoorApiOptions>(configuration.GetSection(DoorApiOptions.SectionName));

    var api = configuration.GetSection(DoorApiOptions.SectionName).Get<DoorApiOptions>() ?? new DoorApiOptions();
    services.AddHttpClient(DoorApiClient.HttpClientName, http =>
    {
        if (Uri.TryCreate(api.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute, out var baseAddress))
            http.BaseAddress = baseAddress;
        http.Timeout = TimeSpan.FromSeconds(Math.Max(5, api.TimeoutSeconds));
    });

    services.AddSingleton<DoorApiClient>();
    services.AddSingleton<DoorLookups>();
});

public partial class Program;
