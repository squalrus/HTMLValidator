using System;
using System.IO;
using System.Net;
using HTMLValidator.Models;
using Newtonsoft.Json;

public static class ValidateHtml
{
    public static string s_moduleApi = "https://sundog.azure.net/api/modules?status=1";

    public static ModuleSchema[] GetModuleSchemas()
    {
        ModuleSchema[] schemaJson = null;

        try
        {
            WebRequest request = WebRequest.Create(s_moduleApi);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string payload = reader.ReadToEnd();

            schemaJson = JsonConvert.DeserializeObject<ModuleSchema[]>(payload);

            response.Close();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return schemaJson;
    }
}
