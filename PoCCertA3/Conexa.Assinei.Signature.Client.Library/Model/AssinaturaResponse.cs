using System.Collections.Generic;

namespace Conexa.Assinei.Signature.Client.Library.Model
{
    public class AssinaturaResponse
    {
        public AssinaturaResponse()
        {
            Notifications = new List<string>();
            Details = new List<string>();
        }

        public object Data { get; set; }
        public bool Success => Notifications.Count == 0;
        public List<string> Notifications { get; private set; }
        public List<string> Details { get; private set; }
    }
}
