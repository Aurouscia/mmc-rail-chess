using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RailChess.Utils;

namespace RailChess.Controllers
{
    public class AarcConvertController: Controller
    {
        private const int saveMaxMb = 10;
        private const int saveMaxBytes = saveMaxMb * 1024 * 1024;
        private static readonly string SaveSizeExceededMsg = $"文件不应超过{saveMaxMb}MB";
        private const string storeDir = "./Data/Convert/AARC";
        private const string filesDir = "files";
        private const string configsDir = "configs";
        private const string saveFileName = "aarc.json";
        private const long storeDirMaxBytes = 500 * 1024 * 1024;

        private static bool _converterConfigRead;
        private static readonly HttpClient Client = new();
        private static string? _svcUrl;
        private const string createTaskEndpoint = "/create";
        private readonly ILogger<AarcConvertController> _logger;
        private static readonly Lock Locker = new();
        public AarcConvertController(IConfiguration config, ILogger<AarcConvertController> logger)
        {
            _logger = logger;
            if (_converterConfigRead)
                return;
            _converterConfigRead = true;
            var url = config["AarcConverter:Url"];
            if (url is null)
                return;
            _svcUrl = url;
        }
        
        /// <summary>
        /// 上传aarc存档文件，上传成功后返回其md5  
        /// 失败：返回json中有errmsg键  
        /// 成功：返回json中有md5键
        /// </summary>
        /// <param name="save">aarc存档json文件</param>
        /// <returns>{md5:xxx}</returns>
        [HttpPost]
        public IActionResult UploadSave(IFormFile? save)
        {
            if (save is null)
                return this.ApiFailedResp("缺少上传文件");
            if (!save.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                return this.ApiFailedResp("请上传json文件");
            if (save.Length > saveMaxBytes)
                return this.ApiFailedResp(SaveSizeExceededMsg);
            if (!CleanAndCheckStoreDir())
                return this.ApiFailedResp("服务器存储空间不足，请联系管理员");

            using var stream = save.OpenReadStream();
            using var md5Stream = new MemoryStream();
            stream.CopyTo(md5Stream);
            md5Stream.Position = 0;
            var md5 = Convert.ToHexStringLower(System.Security.Cryptography.MD5.HashData(md5Stream));
            if (md5.Length != 32)
                return this.ApiFailedResp("计算md5失败");
            
            var finalPath = Path.Combine(storeDir, filesDir, md5, saveFileName);
            var finalInfo = new FileInfo(finalPath);
            if (!finalInfo.Exists)
            {
                md5Stream.Position = 0;
                var finalDir = Path.GetDirectoryName(finalPath);
                if (!string.IsNullOrEmpty(finalDir) && !Directory.Exists(finalDir))
                    Directory.CreateDirectory(finalDir);
                using (var finalStream = System.IO.File.Create(finalPath))
                {
                    md5Stream.CopyTo(finalStream);
                }
            }
            return this.ApiResp(new { md5 });
        }

        [HttpPost]
        public async Task<IActionResult> CreateTask(string md5, string? configJson, string? sourceDomain, int? sourceId)
        {                
            if (_svcUrl is null)
                return this.ApiFailedResp("缺少转换器配置");
            var filePath = Path.Combine(storeDir, filesDir, md5, saveFileName);
            if (!System.IO.File.Exists(filePath))
                return this.ApiFailedResp("超时，请刷新页面并重新上传");

            // 如果提供了域名和存档ID，保存配置
            string? configFilePath = null;
            if (!string.IsNullOrEmpty(configJson) && !string.IsNullOrEmpty(sourceDomain) && sourceId.HasValue)
            {
                // 验证域名格式（只允许字母、数字、英文句点和连字符）
                if (!Regex.IsMatch(sourceDomain, @"^[a-zA-Z0-9.-]+$"))
                    return this.ApiFailedResp("域名格式不正确");
                
                var configsDirPath = Path.Combine(storeDir, configsDir);
                if (!Directory.Exists(configsDirPath))
                    Directory.CreateDirectory(configsDirPath);
                
                var domainDirPath = Path.Combine(configsDirPath, sourceDomain);
                if (!Directory.Exists(domainDirPath))
                    Directory.CreateDirectory(domainDirPath);
                configFilePath = Path.Combine(domainDirPath, $"{sourceId.Value}.json");
                System.IO.File.WriteAllText(configFilePath, configJson);
            }

            using var reqStream = new MemoryStream();
            try
            {
                using (var writer = new Utf8JsonWriter(reqStream, new JsonWriterOptions { Indented = false }))
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName("aarc");
                    using (var aarcStream = System.IO.File.OpenRead(filePath))
                    using (var aarcDoc = JsonDocument.Parse(aarcStream))
                    {
                        aarcDoc.RootElement.WriteTo(writer);
                    }
                    if (!string.IsNullOrEmpty(configJson))
                    {
                        writer.WritePropertyName("config");
                        using (var configDoc = JsonDocument.Parse(configJson))
                        {
                            configDoc.RootElement.WriteTo(writer);
                        }
                    }
                    writer.WriteEndObject();
                    writer.Flush();
                }

                reqStream.Position = 0;
                var content = new StreamContent(reqStream);
                content.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Application.Json);
                var url = _svcUrl + createTaskEndpoint;
                var resp = await Client.PostAsync(url, content);
                resp.EnsureSuccessStatusCode();
                var res = resp.Content;
                await using var resStream = await res.ReadAsStreamAsync();
                await using var reader = new JsonTextReader(new StreamReader(resStream));
                var jObj = await JObject.LoadAsync(reader);
                var key = jObj["key"];
                if (key is not null && key.Type == JTokenType.String)
                    return this.ApiResp(new { key });
                return this.ApiFailedResp("aarc转换器接口返回异常：" + res);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "aarc转换器接口调用出错");
                return this.ApiFailedResp("aarc转换器接口返回异常: " + e.Message);
            }
        }

        public IActionResult GetSvcUrl()
        {
            return this.ApiResp(new { svcUrl = _svcUrl });
        }

        /// <summary>
        /// 通过域名和ID获取config.json内容
        /// </summary>
        /// <param name="sourceDomain">域名</param>
        /// <param name="sourceId">存档ID</param>
        /// <returns>config.json内容，找不到返回空对象</returns>
        [HttpGet]
        public IActionResult GetConfig(string sourceDomain, int sourceId)
        {
            // 验证域名格式
            if (!Regex.IsMatch(sourceDomain, @"^[a-zA-Z0-9.-]+$"))
                return this.ApiFailedResp("域名格式不正确");

            var configFilePath = Path.Combine(storeDir, configsDir, sourceDomain, $"{sourceId}.json");
            if (!System.IO.File.Exists(configFilePath))
                return this.ApiResp(new object());

            var configContent = System.IO.File.ReadAllText(configFilePath);
            var configObj = JsonConvert.DeserializeObject(configContent);
            return this.ApiResp(configObj);
        }

        [NonAction]
        private bool CleanAndCheckStoreDir()
        {
            DirectoryInfo directoryInfo = new(storeDir);
            if (!directoryInfo.Exists)
                directoryInfo.Create();
            var thrs = DateTime.Now.AddDays(-7);
            long totalLength = 0;
            foreach(var subDir in directoryInfo.EnumerateDirectories())
            {
                var subDirInfo = new DirectoryInfo(subDir.FullName);
                var files = subDirInfo.GetFiles();
                if (files.Length == 0)
                {
                    subDir.Delete(true);
                    continue;
                }
                var lastWrite = files.Max(f => f.LastWriteTime);
                if (lastWrite < thrs)
                {
                    subDir.Delete(true);
                    continue;
                }
                totalLength += files.Sum(f => f.Length);
            }
            if (totalLength > storeDirMaxBytes)
                return false;
            return true;
        }
    }
}
