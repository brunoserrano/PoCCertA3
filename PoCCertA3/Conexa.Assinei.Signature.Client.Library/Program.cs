using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Conexa.Assinei.Signature.Client.Library
{
    class Program
    {
        static void Main(string[] args)
        {
            var assinadorDocumento = new AssinadorDocumento();
            assinadorDocumento.AssinarDocumentoAsync(new
            {
                UrlDocument = "https://projetoxdevstorage.blob.core.windows.net:443/assinei/documento/ca0444c3-d4df-453d-9dc1-feb64883d351/a76a18c7a26c43b695a1e3813563e4e4/8e020d7cabd5445e8c1727411fec8b03.pdf",

            });
        }
    }
}
