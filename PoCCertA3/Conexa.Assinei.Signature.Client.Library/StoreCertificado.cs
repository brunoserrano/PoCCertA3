using Conexa.Assinei.Signature.Client.Library.Model;
using DFe.Utils.Assinatura;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Conexa.Assinei.Signature.Client.Library
{
    public class StoreCertificado
    {
#pragma warning disable IDE0060 // Remove unused parameter

        public Task<object> ListAsync(dynamic input)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            var list = List();
            return Task.FromResult<object>(list);
        }

        public bool Any(string serialNumber)
        {
            return List().FindIndex(f => f.SerialNumber.Equals(serialNumber, StringComparison.InvariantCultureIgnoreCase)) > -1;
        }

        public List<Certificado> List()
        {
            try
            {
                using (var store = CertificadoDigital.ObterX509Store(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly))
                {
                    var result = new List<Certificado>();

                    foreach (var certificate in store.Certificates)
                    {
                        var friendlyName = certificate.FriendlyName;
                        if (string.IsNullOrEmpty(certificate.FriendlyName))
                        {
                            friendlyName = certificate.Subject;
                            if (friendlyName.Contains(", OU="))
                            {
                                friendlyName = friendlyName.Substring(0, friendlyName.IndexOf(", OU=")).Replace("CN=", string.Empty);
                            }
                        }
                        var emissor = certificate.IssuerName.Name;
                        if (emissor.Contains(", OU="))
                        {
                            emissor = emissor.Substring(0, emissor.IndexOf(", OU=")).Replace("CN=", string.Empty);
                        }

                        result.Add(new Certificado
                        {
                            Documento = friendlyName?.Split(':').LastOrDefault(),
                            Responsavel = friendlyName?.Split(':').FirstOrDefault(),
                            Emissor = emissor,
                            ValidadeInicial = certificate.NotBefore,
                            ValidadeFinal = certificate.NotAfter,
                            SerialNumber = certificate.SerialNumber,
                            HasPrivateKey = certificate.HasPrivateKey
                        });
                    }

                    store.Close();

                    return result;
                }
            }
            catch (Exception)
            {
                return new List<Certificado>();
            }
        }
    }
}
