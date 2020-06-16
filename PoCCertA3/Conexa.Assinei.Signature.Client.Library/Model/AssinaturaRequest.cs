using Conexa.Assinei.Signature.Client.Library.Enums;
using NFe.Classes.Servicos.Tipos;

namespace Conexa.Assinei.Signature.Client.Library.Model
{
    public class AssinaturaRequest
    {
        public string UrlDocument { get; set; }
        public string SerialNumber { get; set; }
        public string Password { get; set; }
        public EnumTipoCertificado CertificateType { get; set; }
        public EnumTipoAcao ActionType { get; set; }
        public ServicoNFe? Servico { get; set; }
    }
}
