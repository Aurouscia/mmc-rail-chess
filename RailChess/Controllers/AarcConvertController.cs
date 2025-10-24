using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RailChess.Utils;
using RestSharp;

namespace RailChess.Controllers
{
    public class AarcConvertController: Controller
    {
        private const int saveMaxMb = 10;
        private const int saveMaxBytes = saveMaxMb * 1000 * 1000;
        private static readonly string SaveSizeExceededMsg = $"文件不应超过{saveMaxMb}MB";
        private const string saveStoreDir = "./Data/Convert/AARC";

        private static bool _converterConfigRead = false;
        private static RestClient? _client;
        private static string? _svcUrl;
        private readonly ILogger<AarcConvertController> _logger;
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
            _client = new RestClient(url);
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
                return this.ApiFailedResp("文件应为json");
            if (save.Length > saveMaxBytes)
                return this.ApiFailedResp(SaveSizeExceededMsg);
            var dirPath = GetDir()?.FullName;
            if(dirPath is null)
                return this.ApiFailedResp("存储失败，请联系管理员");
            var tempPath = Path.Combine(dirPath, "temp_" + Path.GetRandomFileName());
            var tempInfo = new FileInfo(tempPath);
            FileStream? tempS = null;
            try
            {
                tempS = tempInfo.Create();
                using var saveS = save.OpenReadStream();
                saveS.CopyTo(tempS);
                tempS.Flush();
                saveS.Close();
                if (tempInfo.Length > saveMaxBytes)
                    return this.ApiFailedResp(SaveSizeExceededMsg);
                tempS.Seek(0, SeekOrigin.Begin);
                var md5 = MD5Helper.GetMD5Of(tempS);
                tempS.Close();
                var finalPath = Path.Combine(dirPath, Path.ChangeExtension(md5, "json"));
                var finalInfo = new FileInfo(finalPath);
                if (!finalInfo.Exists)
                    tempInfo.MoveTo(finalPath);
                return this.ApiResp(new { md5 });
            }
            finally
            {
                tempS?.Close();
                if(System.IO.File.Exists(tempPath))
                    System.IO.File.Delete(tempPath);
            }
        }

        [HttpPost]
        public IActionResult CreateTask(string md5, string? configJson)
        {
            if (_client is null)
                return this.ApiFailedResp("缺少转换器配置");
            var dirPath = GetDir()?.FullName;
            if(dirPath is null)
                return this.ApiFailedResp("存储失败，请联系管理员");
            var filePath = Path.Combine(dirPath, Path.ChangeExtension(md5, "json"));
            if (!System.IO.File.Exists(filePath))
                return this.ApiFailedResp("超时，请刷新页面并重新上传");
            var fileContent = System.IO.File.ReadAllText(filePath);
            RestRequest req = new("/create");
            req.Timeout = TimeSpan.FromSeconds(10);
            req.AddJsonBody(new
            {
                aarc = fileContent,
                //config = configJson ?? "{}"
            });
            try
            {
                var resp = _client.Post(req);
                var res = resp.Content;
                var jObj = JObject.Parse(res ?? "{}");
                var key = jObj["key"];
                if (key is not null && key.Type == JTokenType.String)
                    return this.ApiResp(new { key });
                return this.ApiFailedResp("aarc转换器接口返回异常");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "aarc转换器接口调用出错");
                return this.ApiFailedResp("接口异常: " + e.Message);
            }
        }

        public IActionResult GetSvcUrl()
        {
            return this.ApiResp(new{ svcUrl = _svcUrl});
        }

        [NonAction]
        private static DirectoryInfo? GetDir()
        {
            DirectoryInfo directoryInfo = new(saveStoreDir);
            if (!directoryInfo.Exists)
                directoryInfo.Create();
            var thrs = DateTime.Now.AddMinutes(-30);
            var oldFiles = directoryInfo
                .EnumerateFiles()
                .Where(x => x.CreationTime < thrs)
                .ToList();
            foreach(var f in oldFiles)
            {
                try 
                {
                    f.Delete();
                }
                catch { }
            }
            var totalBytes = directoryInfo.EnumerateFiles().Select(x => x.Length).Sum();
            if (totalBytes > 500 * 1000 * 1000)
                return null;
            return directoryInfo;
        }
    }
}
