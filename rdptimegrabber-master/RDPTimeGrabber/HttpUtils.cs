using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RDPTimeGrabber
{
    public static class HttpUtils
    {
        public static HttpStatusCode Request(string url, string method, string data = null)
        {
            WebRequest request = WebRequest.Create(url);
            request.Method = method;
            if (data != null)
            {
                var req_bytes = Encoding.UTF8.GetBytes(data);
                request.ContentLength = req_bytes.Length;
                request.ContentType = "text/plain";
                var dataStream = request.GetRequestStream();
                dataStream.Write(req_bytes, 0, req_bytes.Length);
                dataStream.Close();
            }
            else
            {
                request.ContentLength = 0;
            }
            WebResponse webResponse = request.GetResponse();
            return ((HttpWebResponse)webResponse).StatusCode;
        }
    }
}
