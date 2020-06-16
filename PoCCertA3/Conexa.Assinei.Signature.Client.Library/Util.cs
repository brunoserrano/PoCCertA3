using System.Net;
using System.Threading.Tasks;

namespace Conexa.Assinei.Signature.Client.Library
{
    public class Util
    {
        public async Task<string> DownloadDocumentAsync(string url)
        {
            using (var client = new WebClient())
            {
                var bytes = await client.DownloadDataTaskAsync(url);
                return System.Text.Encoding.ASCII.GetString(bytes);
            }
        }
    }
}