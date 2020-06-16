using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Conexa.Assinei.Signature.Client.Library.Enums;
using Conexa.Assinei.Signature.Client.Library.Extensions;
using Conexa.Assinei.Signature.Client.Library.Model;
using DFe.Utils;
using NFe.Classes.Servicos.Evento;
using NFe.Classes.Servicos.Inutilizacao;
using NFe.Classes.Servicos.Tipos;
using NFe.Utils;
using NFe.Utils.Assinatura;
using Shared.DFe.Utils;
using NotaFiscal = NFe.Classes.NFe;

namespace Conexa.Assinei.Signature.Client.Library
{
    public class AssinadorDocumento
    {
        private readonly StoreCertificado _storeCertificado;
        private readonly Util _util;

        public AssinadorDocumento()
        {
            _storeCertificado = new StoreCertificado();
            _util = new Util();
        }

        public async Task<object> AssinarDocumentoAsync(dynamic parametros)
        {
            var result = new AssinaturaResponse();
            if (parametros == null)
            {
                result.Notifications.Add("Configuração é requerida.");
                return Task.FromResult<object>(result.ToDynamic());
            }

            var source = (object)parametros;
            var arg = new AssinaturaRequest
            {
                UrlDocument = source.GetValue<string>("UrlDocument"),
                SerialNumber = source.GetValue<string>("SerialNumber"),
                Password = source.GetValue<string>("Password"),
                ActionType = (EnumTipoAcao)source.GetValue<int>("ActionType", 0),
                CertificateType = (EnumTipoCertificado)source.GetValue<int>("CertificateType", 0)
            };

            var servico = source.GetValue<int?>("Servico", null);
            if (servico != null)
                arg.Servico = (ServicoNFe)servico;

            result = await AssinarDocumentoAsync(arg);
            return Task.FromResult<object>(result.ToDynamic());
        }

        internal async Task<AssinaturaResponse> AssinarDocumentoAsync(AssinaturaRequest request)
        {
            try
            {
                var response = ValidarRequest(request);
                if (!response.Success)
                {
                    return response;
                }

                var document = await _util.DownloadDocumentAsync(request.UrlDocument);
                //var documentValido = ValidarDocumento(document, request.SerialNumber);

                //if (documentValido.Item1)
                //{
                    document = Signature(document, request);
                    return new AssinaturaResponse
                    {
                        Data = document
                    };
                //}

                //response.Notifications.Add(documentValido.Item2);
                //return response;
            }
            catch (Exception ex)
            {
                var response = new AssinaturaResponse();
                response.Notifications.Add("Erro ao assinar xml.");
                response.Details.Add($"Erro: {ex.ToString()}");
                return response;
            }
        }

        internal static AssinaturaResponse ValidarRequest(AssinaturaRequest request)
        {
            var response = new AssinaturaResponse();

            if (request == null)
            {
                response.Notifications.Add("Parametros não informado.");
                return response;
            }

            if (string.IsNullOrEmpty(request?.UrlDocument))
            {
                response.Notifications.Add("Documento é requerido.");
                return response;
            }

            if (request?.CertificateType == EnumTipoCertificado.Nenhum)
            {
                response.Notifications.Add("Certificado digital é requerido.");
                return response;
            }

            if (string.IsNullOrEmpty(request?.SerialNumber) || (request?.CertificateType == EnumTipoCertificado.A3 && string.IsNullOrEmpty(request?.Password)))
            {
                response.Notifications.Add($"{(string.IsNullOrEmpty(request?.SerialNumber) ? "Serial Number" : "Password")} do Certificado digital é requerido.");
                return response;
            }

            if (request?.ActionType == null || request?.ActionType == EnumTipoAcao.Nenhum)
            {
                response.Notifications.Add("Tipo da ação é requerida.");
                return response;
            }

            return response;
        }

        internal Tuple<bool, string> ValidarDocumento(string documentContent, string serialNumber)
        {
            if (string.IsNullOrEmpty(documentContent))
            {
                return new Tuple<bool, string>(false, "Documento sem conteudo.");
            }

            if (!_storeCertificado.Any(serialNumber))
            {
                return new Tuple<bool, string>(false, "Certificado não encontrado.");
            }

            return new Tuple<bool, string>(true, string.Empty);
        }

        internal string Signature(string xmlNFe, AssinaturaRequest request)
        {
            if (request.ActionType == EnumTipoAcao.AssinaturaDocumento)
            {
                if (request.Servico == ServicoNFe.NFeAutorizacao)
                    return AssinarXmlTransmissao(xmlNFe, request.SerialNumber, request.Password, request.CertificateType);
                else
                {
                    if (request.Servico == ServicoNFe.RecepcaoEventoCancelmento || request.Servico == ServicoNFe.RecepcaoEventoCartaCorrecao)
                    {
                        return AssinarXml<envEvento>(xmlNFe, request.SerialNumber, request.Password, request.CertificateType);
                    }
                    else
                    {
                        return AssinarXml<inutNFe>(xmlNFe, request.SerialNumber, request.Password, request.CertificateType);
                    }
                }
            }
            return default;
        }

        private string AssinarXml<T>(string xmlNFe, string certifiacadoSerialNumber, string certificadoSenha, EnumTipoCertificado certificateType)
        {
            var reader = new StringReader(xmlNFe);
            var desserializador = new XmlSerializer(typeof(T));
            var eventosEnvio = ((T)desserializador.Deserialize(reader));
            var configuracao = ObterConfiguracao(certificateType, certifiacadoSerialNumber, string.IsNullOrEmpty(certificadoSenha) ? string.Empty : Cypher.Decrypt(certificadoSenha));
            var eventos = typeof(T).GetProperty("evento")?.GetValue(eventosEnvio);

            if (eventos != null)
            {
                foreach (var eventoLocal in eventos as List<evento>)
                    eventoLocal.Signature = Assinador.ObterAssinatura(eventoLocal, eventoLocal.infEvento.Id);
            }
            else
            {
                var infInut = typeof(T).GetProperty("infInut").GetValue(eventosEnvio) as infInutEnv;
                var inut = (inutNFe)(object)eventosEnvio;
                inut.Signature = Assinador.ObterAssinatura<inutNFe>(inut, infInut.Id);
            }

            return configuracao.RemoverAcentos
                    ? FuncoesXml.ClasseParaXmlString(eventosEnvio).RemoverAcentos()
                    : FuncoesXml.ClasseParaXmlString(eventosEnvio);
        }

        private string AssinarXmlTransmissao(string xmlNFe, string certifiacadoSerialNumber, string certificadoSenha, EnumTipoCertificado certificateType)
        {
            var reader = new StringReader(xmlNFe);
            var desserializador = new XmlSerializer(typeof(NotaFiscal));
            var nfe = ((NotaFiscal)desserializador.Deserialize(reader));
            var configuracao = ObterConfiguracao(certificateType, certifiacadoSerialNumber, string.IsNullOrEmpty(certificadoSenha) ? string.Empty : Cypher.Decrypt(certificadoSenha));

            nfe.Signature = Assinador.ObterAssinatura(nfe, nfe.infNFe.Id, configuracao);

            return FuncoesXml.ClasseParaXmlString<NotaFiscal>(nfe);
        }

        internal ConfiguracaoServico ObterConfiguracao(EnumTipoCertificado certificateType, string serialNumber, string password)
        {
            var config = ConfiguracaoServico.Instancia;
            config.RemoverAcentos = true;
            config.Certificado = new ConfiguracaoCertificado
            {
                TipoCertificado = certificateType == EnumTipoCertificado.A1 ? TipoCertificado.A1Repositorio : TipoCertificado.A3,
                ManterDadosEmCache = false,
                Serial = serialNumber,
                Senha = certificateType == EnumTipoCertificado.A1 ? string.Empty : password,
                SignatureMethodSignedXml = "http://www.w3.org/2000/09/xmldsig#rsa-sha1",
                DigestMethodReference = "http://www.w3.org/2000/09/xmldsig#sha1"
            };
            return config;
        }
    }
}
