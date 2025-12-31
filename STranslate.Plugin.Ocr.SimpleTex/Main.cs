using Newtonsoft.Json;

using STranslate.Plugin.Ocr.SimpleTex.View;
using STranslate.Plugin.Ocr.SimpleTex.ViewModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Control = System.Windows.Controls.Control;

namespace STranslate.Plugin.Ocr.SimpleTex;

public class Main : IOcrPlugin
{
    private Control? _settingUi;
    private SettingsViewModel? _viewModel;
    private Settings Settings { get; set; } = null!;
    private IPluginContext Context { get; set; } = null!;
    private readonly HttpClient _httpClient = new();

    public IEnumerable<LangEnum> SupportedLanguages =>
    [
        LangEnum.Auto,
    ];

    public Control GetSettingUI()
    {
        _viewModel ??= new SettingsViewModel(Context, Settings);
        _settingUi ??= new SettingsView { DataContext = _viewModel };
        return _settingUi;
    }

    public void Init(IPluginContext context)
    {
        Context = context;
        Settings.DefaultPath = Context.MetaData.PluginCacheDirectoryPath;
        Settings = context.LoadSettingStorage<Settings>();
    }

    public void Dispose() 
    {
        _httpClient.Dispose();
    }

    public async Task<OcrResult> RecognizeAsync(OcrRequest request, CancellationToken cancellationToken)
    {
        var result = new OcrResult();

        try
        {
            if (string.IsNullOrEmpty(Settings.AppId) || string.IsNullOrEmpty(Settings.AppSecret))
            {
                return result.Fail("请在设置中配置 AppID 和 AppSecret");
            }

            using var content = new MultipartFormDataContent();
            using var fileContent = new ByteArrayContent(request.ImageData);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg"); // Assuming jpeg or just octet-stream works
            content.Add(fileContent, "file", "image.jpg");
            content.Add(new StringContent("formula"), "rec_mode"); // Requesting formula specifically

            // Add headers
            // Note: HttpClient headers are usually set on the request message or default headers
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://server.simpletex.net/api/simpletex_ocr");
            httpRequest.Headers.Add("app-id", Settings.AppId);
            httpRequest.Headers.Add("app-secret", Settings.AppSecret);
            httpRequest.Content = content;

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var jsonStr = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return result.Fail($"请求失败: {response.StatusCode} {jsonStr}");
            }

            var respData = JsonConvert.DeserializeObject<SimpleTexResponse>(jsonStr);
            if (respData == null)
            {
                 return result.Fail("解析响应失败");
            }

            if (respData.Status)
            {
                 // Assuming res.latex exists for formula mode
                 string latex = respData.Res?.Latex ?? respData.Res?.Content ?? "";
                 if(string.IsNullOrEmpty(latex))
                 {
                      return result.Fail("未识别到内容");
                 }
                 result.OcrContents.Add(new OcrContent { Text = latex });
                 return result;
            }
            else
            {
                // Try to find error message?
                 return result.Fail($"API 错误: {jsonStr}");
            }
        }
        catch (OperationCanceledException)
        {
            return result.Fail("操作已取消");
        }
        catch (Exception ex)
        {
            return result.Fail($"识别错误: {ex.Message}");
        }
    }

    private class SimpleTexResponse
    {
        [JsonProperty("status")]
        public bool Status { get; set; }
        
        [JsonProperty("res")]
        public ResData? Res { get; set; }

        [JsonProperty("request_id")]
        public string? RequestId { get; set; }
    }

    private class ResData
    {
        [JsonProperty("latex")]
        public string? Latex { get; set; }
        
        [JsonProperty("content")]
        public string? Content { get; set; }
    }
}