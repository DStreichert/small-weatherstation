using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace WeatherStation.Net
{
    public class RESTWebserviceConnection : IDisposable, ICloneable
    {
        #region fields
        /// <summary>
        /// Base Uri for the Webservice Endpoint
        /// </summary>
        private string BaseUri = null;
        private string AccessUri = null;

        /// <summary>
        /// Request Object for the Webservice
        /// </summary>
        private HttpWebRequest Request = null;
        private HttpWebResponse WebResponse = null;
        private string username;
        private string password;
        private string type;
        public string Accept;

        public List<KeyValuePair<string, string>> AdditionalHeaders { get; set; } = new List<KeyValuePair<string, string>>();
        public Encoding Encoding { get; set; } = Encoding.UTF8;

        #endregion

        #region constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="RESTWebserviceConnection"/> class.
        /// </summary>
        /// <param name="baseUri">The base URI.</param>
        /// <param name="type">Type of the content.</param>
        public RESTWebserviceConnection(string baseUri, string type, string userAgent = null, Encoding encoding = null)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11;
            this.BaseUri = baseUri;
            this.type = type;
            if (encoding != null)
                this.Encoding = encoding;
        }

        public RESTWebserviceConnection(string baseUri, string type, string userName, string password, string userAgent = null) : this(baseUri, type, userAgent)
        {
            this.username = userName;
            this.password = password;
        }
        #endregion

        #region methods

        private void CreateNewRequest(string accessUri, string type)
        {
            this.AccessUri = accessUri;
            this.Request = WebRequest.Create(this.BaseUri + this.AccessUri) as HttpWebRequest;
            this.Request.KeepAlive = true;
            this.Request.Timeout = 240000;
            if (this.AdditionalHeaders?.Any() == true)
            {
                var m = this.Request.Headers.GetType().GetMethod("AddWithoutValidate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                foreach (KeyValuePair<string, string> item in this.AdditionalHeaders)
                {
                    if (!this.Request.Headers.AllKeys.Contains(item.Key))
                    {
                        m.Invoke(this.Request.Headers, new string[] { item.Key, item.Value });
                    }
                }
            }
            if (string.IsNullOrWhiteSpace(this.Request.ContentType))
            {
                this.Request.ContentType = type ?? this.type;
            }
            this.Request.Accept = this.Accept;
        }

        /// <summary>
        /// Sets the credentials.
        /// </summary>
        /// <param name="userName">Name of the user.</param>
        /// <param name="password">The password.</param>
        public void SetCredentials(string userName, string password)
        {
            this.Request.Credentials = new NetworkCredential(userName, password);
        }

        /// <summary>
        /// Gets the ressource.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns></returns>
        private Stream GetRessource()
        {
            this.Request.Method = nameof(HTTPRequestMethods.GET);
            this.WebResponse = this.Request.GetResponse() as HttpWebResponse;

            return this.GetResponse();
        }

        /// <summary>
        /// Patch the ressource.
        /// </summary>
        /// <returns></returns>
        private Stream PatchRessource(byte[] byteData)
        {
            if (byteData == null)
            {
                byteData = new byte[] { };
            }
            this.Request.Method = nameof(HTTPRequestMethods.PATCH);
            // Wenn die Zugangsdaten nicht stimmen wird hier eine Exception geworfen
            using (Stream reqStream = this.Request.GetRequestStream())
            {
                reqStream.Write(byteData, 0, byteData.Length);
            }

            return this.GetResponse();
        }

        private async System.Threading.Tasks.Task<Stream> GetRessourceAsync()
        {
            // Get response 
            this.Request.Method = nameof(HTTPRequestMethods.GET);
            this.WebResponse = await this.Request.GetResponseAsync() as HttpWebResponse;
            Stream result = this.GetResponse();
            this.WebResponse.Close();

            return result;
        }

        /// <summary>
        /// Posts the ressource.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="requestContentInJSON">The request content in json.</param>
        /// <returns></returns>
        private Stream PostRessource(byte[] byteData)
        {
            if (byteData == null)
            {
                byteData = new byte[] { };
            }

            this.Request.Method = nameof(HTTPRequestMethods.POST);

            // Wenn die Zugangsdaten nicht stimmen wird hier eine Exception geworfen
            using (Stream reqStream = this.Request.GetRequestStream())
            {
                reqStream.Write(byteData, 0, byteData.Length);
            }
            return this.GetResponse();
        }

        /// <summary>
        /// Deletes the ressource.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="requestContentInJSON">The request content in json.</param>
        /// <returns></returns>
        private Stream DeleteRessource(string requestContentInJSON)
        {
            this.Request.Method = nameof(HTTPRequestMethods.DELETE);
            if (!string.IsNullOrWhiteSpace(requestContentInJSON))
            {
                using (var streamWriter = new StreamWriter(this.Request.GetRequestStream()))
                {
                    streamWriter.Write(requestContentInJSON);
                    streamWriter.Flush();
                }
            }

            return this.GetResponse();
        }

        /// <summary>
        /// Puts the ressource.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="requestContentInJSON">The request content in json.</param>
        /// <returns></returns>
        private Stream PutRessource(byte[] sentData)
        {
            this.Request.Method = nameof(HTTPRequestMethods.PUT);
            // Get response 
            if (sentData != null)
            {
                this.Request.ContentLength = sentData.Length;
                using (Stream reqStream = this.Request.GetRequestStream())
                {
                    reqStream.Write(sentData, 0, sentData.Length);
                    reqStream.Flush();
                }
            }
            else
            {
                using (Stream reqStream = this.Request.GetRequestStream())
                { }
            }

            return this.GetResponse();
        }

        private Stream GetResponse()
        {
            this.WebResponse = this.Request.GetResponse() as HttpWebResponse;

            Stream ResponseStream = this.WebResponse.GetResponseStream();
            var responseCode = (int)this.WebResponse.StatusCode;
            var contentType = this.WebResponse.ContentType;

            return ResponseStream;
        }

        private T DeserializeToObject<T>(Stream ResponseStream)
        {
            var json = "";
            //ResponseStream.Position = 0;
            using (var sr = new StreamReader(ResponseStream))
            {
                json = sr.ReadToEnd();
            }
            T obj = typeof(T) == typeof(string) ? (T)(object)json : this.DeserializeToObject<T>(json);

            ResponseStream.Close(); ResponseStream.Dispose();
            return obj;
        }

        /// <summary>
        /// Serializes to Stream.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objectToSerialize">The object to serialize.</param>
        /// <returns></returns>
        private Stream SerializeToStream<T>(T objectToSerialize)
        {
            var serializer = new DataContractJsonSerializer(typeof(T));
            var stream = new MemoryStream();
            serializer.WriteObject(stream, objectToSerialize);
            return stream;
        }

        /// <summary>
        /// Serializes to Stream.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objectToSerialize">The object to serialize.</param>
        /// <returns></returns>
        private string SerializeToJSON<T>(T objectToSerialize, DataContractJsonSerializerSettings settings)
        {
            var serializer = new DataContractJsonSerializer(typeof(T), settings);

            var stream = new MemoryStream();
            serializer.WriteObject(stream, objectToSerialize);
            stream.Position = 0;
            var json = "";
            using (var sr = new StreamReader(stream))
            {
                json = sr.ReadToEnd();
            }
            return json;
        }

        /// <summary>
        /// Serializes to Stream.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objectToSerialize">The object to serialize.</param>
        /// <returns></returns>
        private string SerializeToJSON<T>(T objectToSerialize)
        {
            var serializer = new DataContractJsonSerializer(typeof(T));

            var stream = new MemoryStream();
            serializer.WriteObject(stream, objectToSerialize);
            stream.Position = 0;
            var json = "";
            using (var sr = new StreamReader(stream))
            {
                json = sr.ReadToEnd();
            }
            return json;
        }

        public string DateTimeFormat = null;
        private readonly object _lockObject = new object();

        public T DeserializeToObject<T>(string json)
        {
            T obj = default;
            if (!string.IsNullOrWhiteSpace(json))
            {
                var sentData = Encoding.UTF8.GetBytes(json);
                using (Stream stream = new MemoryStream(sentData))
                {
                    DataContractJsonSerializer serializer = null;
                    if (this.DateTimeFormat != null)
                    {
                        serializer = new DataContractJsonSerializer(typeof(T), new DataContractJsonSerializerSettings() { DateTimeFormat = new System.Runtime.Serialization.DateTimeFormat(this.DateTimeFormat) });
                    }
                    else
                    {
                        serializer = new DataContractJsonSerializer(typeof(T));
                    }
                    obj = (T)serializer.ReadObject(stream);
                }
            }
            return obj;
        }

        public bool IsValidJsonObject<T>(string strInput)
        {
            if (string.IsNullOrWhiteSpace(strInput)) { return false; }
            strInput = strInput.Trim();
            if ((strInput.StartsWith("{") && strInput.EndsWith("}")) || //For object
                (strInput.StartsWith("[") && strInput.EndsWith("]"))) //For array
            {
                try
                {
                    var obj = this.DeserializeToObject<T>(strInput);
                    return true;
                }
                catch (Exception) //some other exception
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public T SendRequest<T>(string accessUri, ProvidedRequestMethods method, object requestObject = null)
        {
            //lock (_lockObject)
            //{
            T result = default;
            if (accessUri != this.AccessUri)
            {
                if (this.AccessUri != null)
                {
                    this.Dispose();
                }
            }
            this.CreateNewRequest(accessUri, this.type);
            this.SetCredentials(this.username, this.password);

            var data = new byte[0];
            var json = "";
            if (requestObject != null)
            {
                var list = new List<Type>();
                list.Add(requestObject.GetType());
                var settings = new DataContractJsonSerializerSettings()
                {
                    KnownTypes = list,
                    DateTimeFormat = new System.Runtime.Serialization.DateTimeFormat("yyyy-MM-dd'T'HH:mm:ssZ"),
                    EmitTypeInformation = System.Runtime.Serialization.EmitTypeInformation.Never
                };
                json = this.SerializeToJSON(requestObject, settings);
                data = this.Encoding.GetBytes(json);
            }
            this.Request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            Stream stream = null;
            switch (method)
            {
                case ProvidedRequestMethods.GET:
                    stream = this.GetRessource();
                    break;
                case ProvidedRequestMethods.POST:
                    //this.Request.ContentType = "application/x-www-form-urlencoded";
                    this.Request.ContentLength = data.LongLength;
                    stream = this.PostRessource(data);
                    break;
                case ProvidedRequestMethods.PUT:
                    this.Request.ContentLength = data.LongLength;
                    stream = this.PutRessource(data);
                    break;
                case ProvidedRequestMethods.DELETE:
                    this.Request.ContentLength = data.LongLength;
                    stream = this.DeleteRessource(json);
                    break;
                case ProvidedRequestMethods.PATCH:
                    this.Request.ContentLength = data.LongLength;
                    stream = this.PatchRessource(data);
                    break;
                default:
                    break;
            }
            if (stream != null)
            {
                result = this.DeserializeToObject<T>(stream);
            }
            this.WebResponse.Close();
            return result;
            //}
        }

        public void Dispose()
        {
            try
            {
                this.WebResponse?.Close();
                this.WebResponse?.Dispose();
            }
#pragma warning disable RCS1075 // Avoid empty catch clause that catches System.Exception.
            catch (Exception) { }
#pragma warning restore RCS1075 // Avoid empty catch clause that catches System.Exception.
        }

        public object Clone()
        {
            return this.Copy();
        }

        private RESTWebserviceConnection Copy()
        {
            var WS = new RESTWebserviceConnection(this.BaseUri, this.type);
            foreach (System.Reflection.PropertyInfo prop in this.GetType().GetProperties())
            {
                if (!(prop.CanWrite && prop.CanRead))
                {
                    continue;
                }

                var val = prop.GetValue(this, System.Reflection.BindingFlags.GetProperty, null, null, null);
                if (val == null)
                {
                    continue;
                }

                prop.SetValue(WS, val, System.Reflection.BindingFlags.SetProperty, null, null, null);
            }
            var headers = new KeyValuePair<string, string>[this.AdditionalHeaders.Count];
            headers = (KeyValuePair<string, string>[])this.AdditionalHeaders.ToArray().Clone();
            WS.AdditionalHeaders = headers.ToList();
            return WS;
        }

        public TOutput Get<TOutput>(string uri = "", List<string> parameters = null)
            where TOutput : class
        {
            if (parameters == null)
            {
                parameters = new List<string>();
            }

            if (parameters.Count > 0)
            {
                uri = string.Concat(uri, "?", string.Join("&", parameters));
            }

            TOutput response = this.SendRequest<TOutput>(uri, RESTWebserviceConnection.ProvidedRequestMethods.GET, null);
            return response;
        }

        #endregion

        public enum ProvidedRequestMethods
        {
            GET, POST, PUT, DELETE, PATCH
        }

        public struct ProvidedContentTypes
        {
            public const string json = "application/json";
        }
    }

    public enum HTTPRequestMethods
    {
        GET, POST, PUT, PATCH, DELETE, HEAD, TRACE, OPTIONS, CONNECT
    }
}
