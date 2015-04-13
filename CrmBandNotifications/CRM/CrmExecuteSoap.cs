using System.IO;
using System.Net;
using System.Text;
using System.Xml;

namespace CrmBandNotifications.CRM
{
    public class CrmExecuteSoap
    {
        /// <summary>
        /// Executes the SOAP request.
        /// </summary>
        /// <param name="authHeader">CrmAuthenticationHeader.</param>
        /// <param name="requestBody">The SOAP request body.</param>
        /// <param name="url">The CRM URL.</param>
        /// <returns>SOAP response.</returns>
        public static XmlDocument ExecuteSoapRequest(CrmAuthenticationHeader authHeader, string requestBody, string url)
        {
            if (!url.EndsWith("/"))
                url += "/";

            StringBuilder xml = new StringBuilder();
            xml.Append("<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\" xmlns:a=\"http://www.w3.org/2005/08/addressing\">");
            xml.Append(authHeader.Header);
            xml.Append(requestBody);
            xml.Append("</s:Envelope>");

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url + "XRMServices/2011/Organization.svc");
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
            if (dataStream != null)
            {
                StreamReader reader = new StreamReader(dataStream);

                XmlDocument xDoc = new XmlDocument();
                xDoc.Load(reader);

                return xDoc;
            }

            return null;
        }
    }
}