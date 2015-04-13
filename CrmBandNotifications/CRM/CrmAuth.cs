using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace CrmBandNotifications.CRM
{
    public class CrmAuth
    {
        /// <summary>
        /// Gets a CRM Online SOAP header & expiration.
        /// </summary>
        /// <param name="username">Username of a valid CRM user.</param>
        /// <param name="password">Password of a valid CRM user.</param>
        /// <param name="url">The Url of the CRM Online organization (https://org.crm.dynamics.com).</param>
        /// <returns>An object containing the SOAP header and expiration date/time of the header.</returns>
        public CrmAuthenticationHeader GetHeaderOnline(string username, string password, string url)
        {
            if (!url.EndsWith("/"))
                url += "/";

            string urnAddress = GetUrnOnline(url);
            DateTime now = DateTime.Now;

            StringBuilder xml = new StringBuilder();
            xml.Append("<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\" xmlns:a=\"http://www.w3.org/2005/08/addressing\" xmlns:u=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd\">");
            xml.Append("<s:Header>");
            xml.Append("<a:Action s:mustUnderstand=\"1\">http://schemas.xmlsoap.org/ws/2005/02/trust/RST/Issue</a:Action>");
            xml.Append("<a:MessageID>urn:uuid:" + Guid.NewGuid() + "</a:MessageID>");
            xml.Append("<a:ReplyTo>");
            xml.Append("<a:Address>http://www.w3.org/2005/08/addressing/anonymous</a:Address>");
            xml.Append("</a:ReplyTo>");
            xml.Append("<a:To s:mustUnderstand=\"1\">https://login.microsoftonline.com/RST2.srf</a:To>");
            xml.Append("<o:Security s:mustUnderstand=\"1\" xmlns:o=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd\">");
            xml.Append("<u:Timestamp u:Id=\"_0\">");
            xml.Append("<u:Created>" + now.ToUniversalTime().ToString("o") + "</u:Created>");
            xml.Append("<u:Expires>" + now.AddMinutes(60).ToUniversalTime().ToString("o") + "</u:Expires>");
            xml.Append("</u:Timestamp>");
            xml.Append("<o:UsernameToken u:Id=\"uuid-" + Guid.NewGuid() + "-1\">");
            xml.Append("<o:Username>" + username + "</o:Username>");
            xml.Append("<o:Password>" + password + "</o:Password>");
            xml.Append("</o:UsernameToken>");
            xml.Append("</o:Security>");
            xml.Append("</s:Header>");
            xml.Append("<s:Body>");
            xml.Append("<trust:RequestSecurityToken xmlns:trust=\"http://schemas.xmlsoap.org/ws/2005/02/trust\">");
            xml.Append("<wsp:AppliesTo xmlns:wsp=\"http://schemas.xmlsoap.org/ws/2004/09/policy\">");
            xml.Append("<a:EndpointReference>");
            xml.Append("<a:Address>urn:" + urnAddress + "</a:Address>");
            xml.Append("</a:EndpointReference>");
            xml.Append("</wsp:AppliesTo>");
            xml.Append("<trust:RequestType>http://schemas.xmlsoap.org/ws/2005/02/trust/Issue</trust:RequestType>");
            xml.Append("</trust:RequestSecurityToken>");
            xml.Append("</s:Body>");
            xml.Append("</s:Envelope>");

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://login.microsoftonline.com/RST2.srf");
            ASCIIEncoding encoding = new ASCIIEncoding();

            byte[] bytesToWrite = encoding.GetBytes(xml.ToString());
            request.Method = "POST";
            request.ContentLength = bytesToWrite.Length;
            request.ContentType = "application/soap+xml; charset=UTF-8";

            Stream newStream = request.GetRequestStream();
            newStream.Write(bytesToWrite, 0, bytesToWrite.Length);
            newStream.Close();

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream dataStream = response.GetResponseStream();

            if (dataStream == null)
                return null;

            StreamReader reader = new StreamReader(dataStream);

            XmlDocument x = new XmlDocument();
            x.Load(reader);

            XmlNodeList cipherElements = x.GetElementsByTagName("CipherValue");
            string token1 = cipherElements[0].InnerText;
            string token2 = cipherElements[1].InnerText;

            XmlNodeList keyIdentiferElements = x.GetElementsByTagName("wsse:KeyIdentifier");
            string keyIdentifer = keyIdentiferElements[0].InnerText;

            XmlNodeList tokenExpiresElements = x.GetElementsByTagName("wsu:Expires");
            string tokenExpires = tokenExpiresElements[0].InnerText;

            CrmAuthenticationHeader authHeader = new CrmAuthenticationHeader
            {
                Header = CreateSoapHeaderOnline(url, keyIdentifer, token1, token2),
                Expires = DateTimeOffset.Parse(tokenExpires).UtcDateTime
            };

            return authHeader;
        }

        /// <summary>
        /// Gets a CRM Online SOAP header.
        /// </summary>
        /// <param name="url">The Url of the CRM Online organization (https://org.crm.dynamics.com).</param>
        /// <param name="keyIdentifer">The KeyIdentifier from the initial request.</param>
        /// <param name="token1">The first token from the initial request.</param>
        /// <param name="token2">The second token from the initial request.</param>
        /// <returns>The XML SOAP header to be used in future requests.</returns>
        private static string CreateSoapHeaderOnline(string url, string keyIdentifer, string token1, string token2)
        {
            StringBuilder xml = new StringBuilder();
            xml.Append("<s:Header>");
            xml.Append("<a:Action s:mustUnderstand=\"1\">http://schemas.microsoft.com/xrm/2011/Contracts/Services/IOrganizationService/Execute</a:Action>");
            xml.Append("<Security xmlns=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd\">");
            xml.Append("<EncryptedData Id=\"Assertion0\" Type=\"http://www.w3.org/2001/04/xmlenc#Element\" xmlns=\"http://www.w3.org/2001/04/xmlenc#\">");
            xml.Append("<EncryptionMethod Algorithm=\"http://www.w3.org/2001/04/xmlenc#tripledes-cbc\"/>");
            xml.Append("<ds:KeyInfo xmlns:ds=\"http://www.w3.org/2000/09/xmldsig#\">");
            xml.Append("<EncryptedKey>");
            xml.Append("<EncryptionMethod Algorithm=\"http://www.w3.org/2001/04/xmlenc#rsa-oaep-mgf1p\"/>");
            xml.Append("<ds:KeyInfo Id=\"keyinfo\">");
            xml.Append("<wsse:SecurityTokenReference xmlns:wsse=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd\">");
            xml.Append("<wsse:KeyIdentifier EncodingType=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary\" ValueType=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-x509-token-profile-1.0#X509SubjectKeyIdentifier\">" + keyIdentifer + "</wsse:KeyIdentifier>");
            xml.Append("</wsse:SecurityTokenReference>");
            xml.Append("</ds:KeyInfo>");
            xml.Append("<CipherData>");
            xml.Append("<CipherValue>" + token1 + "</CipherValue>");
            xml.Append("</CipherData>");
            xml.Append("</EncryptedKey>");
            xml.Append("</ds:KeyInfo>");
            xml.Append("<CipherData>");
            xml.Append("<CipherValue>" + token2 + "</CipherValue>");
            xml.Append("</CipherData>");
            xml.Append("</EncryptedData>");
            xml.Append("</Security>");
            xml.Append("<a:MessageID>urn:uuid:" + Guid.NewGuid() + "</a:MessageID>");
            xml.Append("<a:ReplyTo>");
            xml.Append("<a:Address>http://www.w3.org/2005/08/addressing/anonymous</a:Address>");
            xml.Append("</a:ReplyTo>");
            xml.Append("<a:To s:mustUnderstand=\"1\">" + url + "XRMServices/2011/Organization.svc</a:To>");
            xml.Append("</s:Header>");
            return xml.ToString();
        }

        /// <summary>
        /// Gets the correct URN Address based on the Online region.
        /// </summary>
        /// <param name="url">The Url of the CRM Online organization (https://org.crm.dynamics.com).</param>
        /// <returns>URN Address.</returns>
        private static string GetUrnOnline(string url)
        {
            if (url.ToUpper().Contains("CRM2.DYNAMICS.COM"))
                return "crmsam:dynamics.com";
            if (url.ToUpper().Contains("CRM4.DYNAMICS.COM"))
                return "crmemea:dynamics.com";
            if (url.ToUpper().Contains("CRM5.DYNAMICS.COM"))
                return "crmapac:dynamics.com";

            return "crmna:dynamics.com";
        }

        /// <summary>
        /// Gets a CRM On Premise SOAP header & expiration.
        /// </summary>
        /// <param name="username">Username of a valid CRM user.</param>
        /// <param name="password">Password of a valid CRM user.</param>
        /// <param name="url">The Url of the CRM On Premise (IFD) organization (https://org.domain.com).</param>
        /// <returns>An object containing the SOAP header and expiration date/time of the header.</returns>
        public CrmAuthenticationHeader GetHeaderOnPremise(string username, string password, string url)
        {
            if (!url.EndsWith("/"))
                url += "/";
            string adfsUrl = GetAdfs(url);
            if (adfsUrl == null) 
                return null;

            DateTime now = DateTime.Now;
            string urnAddress = url + "XRMServices/2011/Organization.svc";
            string usernamemixed = adfsUrl + "/13/usernamemixed";

            StringBuilder xml = new StringBuilder();
            xml.Append("<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\" xmlns:a=\"http://www.w3.org/2005/08/addressing\">");
            xml.Append("<s:Header>");
            xml.Append("<a:Action s:mustUnderstand=\"1\">http://docs.oasis-open.org/ws-sx/ws-trust/200512/RST/Issue</a:Action>");
            xml.Append("<a:MessageID>urn:uuid:" + Guid.NewGuid() + "</a:MessageID>");
            xml.Append("<a:ReplyTo>");
            xml.Append("<a:Address>http://www.w3.org/2005/08/addressing/anonymous</a:Address>");
            xml.Append("</a:ReplyTo>");
            xml.Append("<Security s:mustUnderstand=\"1\" xmlns:u=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd\" xmlns=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd\">");
            xml.Append("<u:Timestamp  u:Id=\"" + Guid.NewGuid() + "\">");
            xml.Append("<u:Created>" + now.ToUniversalTime().ToString("o") + "</u:Created>");
            xml.Append("<u:Expires>" + now.AddMinutes(60).ToUniversalTime().ToString("o") + "</u:Expires>");
            xml.Append("</u:Timestamp>");
            xml.Append("<UsernameToken u:Id=\"" + Guid.NewGuid() + "\">");
            xml.Append("<Username>" + username + "</Username>");
            xml.Append("<Password Type=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordText\">" + password + "</Password>");
            xml.Append("</UsernameToken>");
            xml.Append("</Security>");
            xml.Append("<a:To s:mustUnderstand=\"1\">" + usernamemixed + "</a:To>");
            xml.Append("</s:Header>");
            xml.Append("<s:Body>");
            xml.Append("<trust:RequestSecurityToken xmlns:trust=\"http://docs.oasis-open.org/ws-sx/ws-trust/200512\">");
            xml.Append("<wsp:AppliesTo xmlns:wsp=\"http://schemas.xmlsoap.org/ws/2004/09/policy\">");
            xml.Append("<a:EndpointReference>");
            xml.Append("<a:Address>" + urnAddress + "</a:Address>");
            xml.Append("</a:EndpointReference>");
            xml.Append("</wsp:AppliesTo>");
            xml.Append("<trust:RequestType>http://docs.oasis-open.org/ws-sx/ws-trust/200512/Issue</trust:RequestType>");
            xml.Append("</trust:RequestSecurityToken>");
            xml.Append("</s:Body>");
            xml.Append("</s:Envelope>");

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(usernamemixed);
            ASCIIEncoding encoding = new ASCIIEncoding();

            byte[] bytesToWrite = encoding.GetBytes(xml.ToString());
            request.Method = "POST";
            request.ContentLength = bytesToWrite.Length;
            request.ContentType = "application/soap+xml; charset=UTF-8";

            Stream newStream = request.GetRequestStream();
            newStream.Write(bytesToWrite, 0, bytesToWrite.Length);
            newStream.Close();

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream dataStream = response.GetResponseStream();

            if (dataStream == null)
                return null;

            StreamReader reader = new StreamReader(dataStream);

            XmlDocument x = new XmlDocument();
            x.Load(reader);

            XmlNodeList cipherValue1 = x.GetElementsByTagName("e:CipherValue");
            string token1 = cipherValue1[0].InnerText;

            XmlNodeList cipherValue2 = x.GetElementsByTagName("xenc:CipherValue");
            string token2 = cipherValue2[0].InnerText;

            XmlNodeList keyIdentiferElements = x.GetElementsByTagName("o:KeyIdentifier");
            string keyIdentifer = keyIdentiferElements[0].InnerText;

            XmlNodeList x509IssuerNameElements = x.GetElementsByTagName("X509IssuerName");
            string x509IssuerName = x509IssuerNameElements[0].InnerText;

            XmlNodeList x509SerialNumberElements = x.GetElementsByTagName("X509SerialNumber");
            string x509SerialNumber = x509SerialNumberElements[0].InnerText;

            XmlNodeList binarySecretElements = x.GetElementsByTagName("trust:BinarySecret");
            string binarySecret = binarySecretElements[0].InnerText;

            string created = now.AddMinutes(-1).ToUniversalTime().ToString("o");
            string expires = now.AddMinutes(60).ToUniversalTime().ToString("o");
            string timestamp = "<u:Timestamp xmlns:u=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd\" u:Id=\"_0\"><u:Created>" + created + "</u:Created><u:Expires>" + expires + "</u:Expires></u:Timestamp>";

            SHA1CryptoServiceProvider sha1Hasher = new SHA1CryptoServiceProvider();
            byte[] hashedDataBytes = sha1Hasher.ComputeHash(Encoding.UTF8.GetBytes(timestamp));
            string digestValue = Convert.ToBase64String(hashedDataBytes);

            string signedInfo = "<SignedInfo xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><CanonicalizationMethod Algorithm=\"http://www.w3.org/2001/10/xml-exc-c14n#\"></CanonicalizationMethod><SignatureMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#hmac-sha1\"></SignatureMethod><Reference URI=\"#_0\"><Transforms><Transform Algorithm=\"http://www.w3.org/2001/10/xml-exc-c14n#\"></Transform></Transforms><DigestMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#sha1\"></DigestMethod><DigestValue>" + digestValue + "</DigestValue></Reference></SignedInfo>";
            byte[] signedInfoBytes = Encoding.UTF8.GetBytes(signedInfo);
            HMACSHA1 hmac = new HMACSHA1();
            byte[] binarySecretBytes = Convert.FromBase64String(binarySecret);
            hmac.Key = binarySecretBytes;
            byte[] hmacHash = hmac.ComputeHash(signedInfoBytes);
            string signatureValue = Convert.ToBase64String(hmacHash);

            XmlNodeList tokenExpiresElements = x.GetElementsByTagName("wsu:Expires");
            CrmAuthenticationHeader authHeader = new CrmAuthenticationHeader
            {
                Expires =
                    DateTime.ParseExact(tokenExpiresElements[0].InnerText, "yyyy-MM-ddTHH:mm:ss.fffK", null)
                        .ToUniversalTime(),
                Header = CreateSoapHeaderOnPremise(url, keyIdentifer, token1, token2, x509IssuerName,
                    x509SerialNumber, signatureValue, digestValue, created, expires)
            };

            return authHeader;
        }

        /// <summary>
        /// Gets a CRM On Premise (IFD) SOAP header.
        /// </summary>
        /// <param name="url">The CRM On Premise URL ("https://org.domain.com/").</param>
        /// <param name="keyIdentifer">The KeyIdentifier from the initial request.</param>
        /// <param name="token1">The first token from the initial request.</param>
        /// <param name="token2">The second token from the initial request.</param>
        /// <param name="issuerNameX509">The certificate issuer.</param>
        /// <param name="serialNumberX509">The certificate serial number.</param>
        /// <param name="signatureValue">The hashsed value of the header signature.</param>
        /// <param name="digestValue">The hashed value of the header timestamp.</param>
        /// <param name="created">The header created date/time.</param>
        /// <param name="expires">The header expiration date/tim.</param>
        /// <returns>SOAP Header XML.</returns>
        private static string CreateSoapHeaderOnPremise(string url, string keyIdentifer, string token1, string token2, string issuerNameX509, string serialNumberX509, string signatureValue, string digestValue, string created, string expires)
        {
            StringBuilder xml = new StringBuilder();
            xml.Append("<s:Header>");
            xml.Append("<a:Action s:mustUnderstand=\"1\">http://schemas.microsoft.com/xrm/2011/Contracts/Services/IOrganizationService/Execute</a:Action>");
            xml.Append("<a:MessageID>urn:uuid:" + Guid.NewGuid() + "</a:MessageID>");
            xml.Append("<a:ReplyTo>");
            xml.Append("<a:Address>http://www.w3.org/2005/08/addressing/anonymous</a:Address>");
            xml.Append("</a:ReplyTo>");
            xml.Append("<a:To s:mustUnderstand=\"1\">" + url + "XRMServices/2011/Organization.svc</a:To>");
            xml.Append("<o:Security xmlns:o=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd\">");
            xml.Append("<u:Timestamp xmlns:u=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd\" u:Id=\"_0\">");
            xml.Append("<u:Created>" + created + "</u:Created>");
            xml.Append("<u:Expires>" + expires + "</u:Expires>");
            xml.Append("</u:Timestamp>");
            xml.Append("<xenc:EncryptedData Type=\"http://www.w3.org/2001/04/xmlenc#Element\" xmlns:xenc=\"http://www.w3.org/2001/04/xmlenc#\">");
            xml.Append("<xenc:EncryptionMethod Algorithm=\"http://www.w3.org/2001/04/xmlenc#aes256-cbc\"/>");
            xml.Append("<KeyInfo xmlns=\"http://www.w3.org/2000/09/xmldsig#\">");
            xml.Append("<e:EncryptedKey xmlns:e=\"http://www.w3.org/2001/04/xmlenc#\">");
            xml.Append("<e:EncryptionMethod Algorithm=\"http://www.w3.org/2001/04/xmlenc#rsa-oaep-mgf1p\">");
            xml.Append("<DigestMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#sha1\"/>");
            xml.Append("</e:EncryptionMethod>");
            xml.Append("<KeyInfo>");
            xml.Append("<o:SecurityTokenReference xmlns:o=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd\">");
            xml.Append("<X509Data>");
            xml.Append("<X509IssuerSerial>");
            xml.Append("<X509IssuerName>" + issuerNameX509 + "</X509IssuerName>");
            xml.Append("<X509SerialNumber>" + serialNumberX509 + "</X509SerialNumber>");
            xml.Append("</X509IssuerSerial>");
            xml.Append("</X509Data>");
            xml.Append("</o:SecurityTokenReference>");
            xml.Append("</KeyInfo>");
            xml.Append("<e:CipherData>");
            xml.Append("<e:CipherValue>" + token1 + "</e:CipherValue>");
            xml.Append("</e:CipherData>");
            xml.Append("</e:EncryptedKey>");
            xml.Append("</KeyInfo>");
            xml.Append("<xenc:CipherData>");
            xml.Append("<xenc:CipherValue>" + token2 + "</xenc:CipherValue>");
            xml.Append("</xenc:CipherData>");
            xml.Append("</xenc:EncryptedData>");
            xml.Append("<Signature xmlns=\"http://www.w3.org/2000/09/xmldsig#\">");
            xml.Append("<SignedInfo>");
            xml.Append("<CanonicalizationMethod Algorithm=\"http://www.w3.org/2001/10/xml-exc-c14n#\"/>");
            xml.Append("<SignatureMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#hmac-sha1\"/>");
            xml.Append("<Reference URI=\"#_0\">");
            xml.Append("<Transforms>");
            xml.Append("<Transform Algorithm=\"http://www.w3.org/2001/10/xml-exc-c14n#\"/>");
            xml.Append("</Transforms>");
            xml.Append("<DigestMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#sha1\"/>");
            xml.Append("<DigestValue>" + digestValue + "</DigestValue>");
            xml.Append("</Reference>");
            xml.Append("</SignedInfo>");
            xml.Append("<SignatureValue>" + signatureValue + "</SignatureValue>");
            xml.Append("<KeyInfo>");
            xml.Append("<o:SecurityTokenReference xmlns:o=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd\">");
            xml.Append("<o:KeyIdentifier ValueType=\"http://docs.oasis-open.org/wss/oasis-wss-saml-token-profile-1.0#SAMLAssertionID\">" + keyIdentifer + "</o:KeyIdentifier>");
            xml.Append("</o:SecurityTokenReference>");
            xml.Append("</KeyInfo>");
            xml.Append("</Signature>");
            xml.Append("</o:Security>");
            xml.Append("</s:Header>");

            return xml.ToString();
        }

        /// <summary>
        /// Gets the name of the ADFS server CRM uses for authentication.
        /// </summary>
        /// <param name="url">The Url of the CRM On Premise (IFD) organization (https://org.domain.com).</param>
        /// <returns>The AD FS server url.</returns>
        private static string GetAdfs(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url + "/XrmServices/2011/Organization.svc?wsdl=wsdl0");
            request.Method = "GET";
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            if (dataStream == null)
                return null;

            StreamReader reader = new StreamReader(dataStream);

            XmlDocument x = new XmlDocument();
            x.Load(reader);

            XmlNodeList nodes = x.GetElementsByTagName("ms-xrm:Identifier");
            foreach (XmlNode node in nodes)
            {
                return node.FirstChild.InnerText.Replace("http://", "https://");
            }

            return null;
        }
    }
}