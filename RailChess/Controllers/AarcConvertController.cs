using System.Net.Http.Headers;
using System.Net.Mime;
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
        private const string saveFileName = "aarc.json";
        private const string reqFileName = "req.json";
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
                return this.ApiFailedResp("文件应为json");
            if (save.Length > saveMaxBytes)
                return this.ApiFailedResp(SaveSizeExceededMsg);
            bool okay;
            lock (Locker)
            {
                okay = CleanAndCheckStoreDir();
            }

            if (!okay)
                return this.ApiFailedResp("存储失败，请联系管理员");
            var tempPath = Path.Combine(storeDir , "temp_" + Path.GetRandomFileName());
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
                var finalDirPath = Path.Combine(storeDir, md5);
                if(!Directory.Exists(finalDirPath))
                    Directory.CreateDirectory(finalDirPath);
                var finalPath = Path.Combine(storeDir, md5, saveFileName);
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
        public async Task<IActionResult> CreateTask(string md5, string? configJson)
        {                
            if (_svcUrl is null)
                return this.ApiFailedResp("缺少转换器配置");
            var filePath = Path.Combine(storeDir, md5, saveFileName);
            var reqFilePath = Path.Combine(storeDir, md5, reqFileName);
            var reqFileInfo = new FileInfo(reqFilePath);
            lock (Locker)
            {
                if (!reqFileInfo.Exists)
                {
                    // 如果req文件不存在则创建
                    if (!System.IO.File.Exists(filePath))
                        return this.ApiFailedResp("超时，请刷新页面并重新上传");
                    try
                    {
                        using var reader = new JsonTextReader(System.IO.File.OpenText(filePath))
                        {
                            CloseInput = true,
                            SupportMultipleContent = false
                        };
                        using var reqFileStream = System.IO.File.Create(reqFilePath);
                        using var writer = new JsonTextWriter(new StreamWriter(reqFileStream));
                        writer.Formatting = Formatting.None;
                        writer.CloseOutput = true;
                        writer.WriteStartObject();
                        writer.WritePropertyName("aarc");
                        if (!reader.Read() || reader.TokenType != JsonToken.StartObject)
                            throw new JsonReaderException($"json格式异常");
                        writer.WriteToken(reader, true);
                        writer.WriteEndObject();
                        writer.Flush();
                    }
                    catch
                    {
                        if (System.IO.File.Exists(reqFilePath))
                            System.IO.File.Delete(reqFilePath);
                        return this.ApiFailedResp($"{saveFileName}格式异常");
                    }
                }
                else
                {
                    // 如果存在，则设置“上次写”时间，假装新创建了一次（避免被清理）
                    reqFileInfo.LastWriteTime = DateTime.Now;
                }
            }

            await using var reqBodyStream = System.IO.File.OpenRead(reqFilePath);
            try
            {
                    var content = new StreamContent(reqBodyStream);
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
                    try
                    {
                        reqFileInfo.Delete();
                        _logger.LogWarning("aarc转换器已删除出错req文件");
                    }
                    catch (Exception e1)
                    {
                        _logger.LogError(e1, "aarc转换器删除出错req文件失败");
                    }

                    return this.ApiFailedResp("aarc转换器接口返回异常: " + e.Message);
            }
        }

        public IActionResult GetSvcUrl()
        {
            return this.ApiResp(new { svcUrl = _svcUrl });
        }

        [NonAction]
        private bool CleanAndCheckStoreDir()
        {
            DirectoryInfo directoryInfo = new(storeDir);
            if (!directoryInfo.Exists)
                directoryInfo.Create();
            var thrs = DateTime.Now.AddDays(-7);
            var dirs = directoryInfo.EnumerateDirectories();
            long sizeSum = 0;
            foreach (var d in dirs)
            {
                long sizeSumHere = 0;
                var latest = DateTime.MinValue;
                var dFiles = d.EnumerateFiles();
                foreach (var f in dFiles)
                {
                    if (f.LastWriteTime > latest)
                        latest = f.LastWriteTime;
                    sizeSumHere += f.Length;
                }

                if (latest >= thrs) continue; // 如果足够新，那没事
                // 如果小于thrs（不够新）则删除
                try
                {
                    d.Delete(true);
                    sizeSumHere = 0; // 如果删除失败，不会变0
                }
                catch (Exception e)
                {
                    _logger.LogWarning("清理失败：{emsg}, {fname}", e.Message, d.Name);
                }

                sizeSum += sizeSumHere;
            }
            var files = directoryInfo.EnumerateFiles();
            foreach (var f in files)
            {
                if(f.LastWriteTime >= thrs) continue;
                try
                {
                    f.Delete();
                }
                catch (Exception e)
                {
                    _logger.LogWarning("清理失败：{emsg}, {fname}", e.Message, f.Name);
                }
            }

            if (sizeSum > storeDirMaxBytes)
                return false; // 超出上限
            return true;
        }
    }
}
