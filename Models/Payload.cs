using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net;

namespace HTMLValidator.Models
{
    public static class Payload
    {
        public static string Get(string url, ILogger log)
        {
            try
            {
                log.LogInformation($"Starting request to {url}");
                var request = WebRequest.Create(url);
                var response = (HttpWebResponse)request.GetResponse();
                var dataStream = response.GetResponseStream();
                var reader = new StreamReader(dataStream);
                var data = reader.ReadToEnd();

                response.Close();
                return data;
            }
            catch (Exception ex)
            {
                log.LogInformation($"Failed to get payload for {url}: {ex}");
            }

            return string.Empty;
        }
    }
}
