using Microsoft.AspNetCore.Mvc;
using RailChess.Utils;

namespace RailChess.Controllers.Convert
{
    public class AarcConvertController : ControllerBase
    {
        private const int saveMaxMB = 10;
        private const int saveMaxBytes = saveMaxMB * 1000 * 1000;
        private readonly static string saveSizeExceededMsg = $"文件不应超过{saveMaxMB}MB";
        private const string saveStoreDir = "./Data/Convert/AARC";

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
                return BadRequest(new { errmsg = "缺少上传文件" });
            if (!save.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { errmsg = "文件应为json" });
            if (save.Length > saveMaxBytes)
                return BadRequest(new { errmsg = saveSizeExceededMsg });
            var dirPath = GetDir()?.FullName;
            if(dirPath is null)
                return BadRequest(new { errmsg = "存储失败，请联系管理员" });
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
                    return BadRequest(new { errmsg = saveSizeExceededMsg });
                tempS.Seek(0, SeekOrigin.Begin);
                var md5 = MD5Helper.GetMD5Of(tempS);
                tempS.Close();
                var finalPath = Path.Combine(dirPath, Path.ChangeExtension(md5, "json"));
                var finalInfo = new FileInfo(finalPath);
                if (!finalInfo.Exists)
                    tempInfo.MoveTo(finalPath);
                return Ok(new { md5 });
            }
            finally
            {
                tempS?.Close();
                if(System.IO.File.Exists(tempPath))
                    System.IO.File.Delete(tempPath);
            }
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
