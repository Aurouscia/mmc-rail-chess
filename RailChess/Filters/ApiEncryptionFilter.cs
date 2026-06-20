using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RailChess.Models.COM;
using RailChess.Utils;

namespace RailChess.Filters
{
    /// <summary>
    /// 对成功的 API JSON 响应中的 data 字段进行 AES-256-GCM 加密。
    /// </summary>
    public class ApiEncryptionFilter : IActionFilter
    {
        private const string JsonContentType = "application/json";
        private readonly IApiEncryption _encryption;

        public ApiEncryptionFilter(IApiEncryption encryption)
        {
            _encryption = encryption;
        }

        public void OnActionExecuting(ActionExecutingContext context) { }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Result is not ContentResult contentResult)
                return;
            if (contentResult.ContentType != JsonContentType)
                return;
            if (string.IsNullOrEmpty(contentResult.Content))
                return;

            JObject? responseObj;
            try
            {
                responseObj = JObject.Parse(contentResult.Content);
            }
            catch
            {
                // 不是合法 JSON，跳过
                return;
            }

            // 只加密成功响应，且 data 字段有值
            if (responseObj["success"]?.Value<bool>() != true)
                return;

            var dataToken = responseObj["data"];
            if (dataToken is null || dataToken.Type == JTokenType.Null)
                return;

            try
            {
                var plainDataJson = dataToken.ToString(Formatting.None);
                var encryptedData = _encryption.Encrypt(plainDataJson);

                responseObj["data"] = encryptedData;
                responseObj["encrypted"] = true;

                contentResult.Content = responseObj.ToString(Formatting.None);
            }
            catch
            {
                // 加密失败时不应暴露异常，保持原响应不变
                // 生产环境建议记录日志
            }
        }
    }
}
