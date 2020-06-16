using System;
using System.Linq;

namespace Conexa.Assinei.Signature.Client.Library.Model
{
    public class Certificado
    {
        public string Responsavel { get; set; }
        public string Documento { get; set; }
        public DateTime ValidadeInicial { get; set; }
        public DateTime ValidadeFinal { get; set; }
        public bool Ativo => ValidadeInicial <= DateTime.Now && ValidadeFinal >= DateTime.Now;
        public string Emissor { get; set; }
        public string SerialNumber { get; set; }
        public bool HasPrivateKey { get; set; }

        public override string ToString()
        {
            return string.Join(", ", this.GetType().GetProperties().Select(prop => $"{prop.Name}: {prop.GetValue(this, null)}"));
        }
    }
}
