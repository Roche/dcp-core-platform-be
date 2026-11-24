using Microsoft.AspNetCore.Http;
using System;
using System.Linq;

namespace OpenDCP.Core.Core.Extensions
{
    public static class ResponseHeaderExtensions
    {
        public static void SetValidationHeader(this HttpResponse response, 
            bool api, bool? datasource, bool? module, bool? entity)
        {
            if (response.HasStarted)
                return;

            const string headerName = "isValidated";

            var headerValue = string.Join(",",
              api.ToString().ToLowerInvariant(),
              datasource?.ToString().ToLowerInvariant() ?? "undefined",
              module?.ToString().ToLowerInvariant() ?? "undefined",
              entity?.ToString().ToLowerInvariant() ?? "undefined");

            response.Headers[headerName] = headerValue;

            if (!response.Headers.TryGetValue("access-control-expose-headers", out var existing))
            {
                response.Headers["access-control-expose-headers"] = headerName;
            }
            else
            {
                var headers = existing.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(h => h.Trim())
                    .ToList();

                if (!headers.Contains(headerName, StringComparer.OrdinalIgnoreCase))
                {
                    headers.Add(headerName);
                    response.Headers["access-control-expose-headers"] = string.Join(", ", headers);
                }
            }
        }
    }
}
