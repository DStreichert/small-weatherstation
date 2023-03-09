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
    /// <summary>
    /// The REST webservice connection object.
    /// </summary>
    public class RESTWebserviceConnection : IDisposable, ICloneable
    {
        #region fields
        /// <summary>
        /// Base Uri for the Webservice Endpoint
        /// </summary>
        private string _baseUri = null;
        private string _accessUri = null;

        /// <summary>
        /// Request Object for the Webservice
        /// </summary>
        private HttpWebRequest _request = null;
        private HttpWebResponse _webResponse = null;
        private string _username;
        private string _password;
        private string _type;

        /// <summary>
        /// Gets or sets the value of the Accept HTTP header.
        /// </summary>
        public string Accept { get; set; }

        /// <summary>
        /// Gets or sets the additional headers.
        /// </summary>
        public List<KeyValuePair<string, string>> AdditionalHeaders { get; set; } = new List<KeyValuePair<string, string>>();
        /// <summary>
        /// Gets or sets the character encoding.
        /// </summary>
        public Encoding Encoding { get; set; } = Encoding.UTF8;

        #endregion

        #region constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="RESTWebserviceConnection"/> class.
        /// </summary>
        /// <param name="baseUri">The base uri.</param>
        /// <param name="type">The type.</param>
        /// <param name="encoding">The encoding.</param>
        /// 
        public RESTWebserviceConnection(string baseUri, string type, Encoding encoding = null)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11;
            this._baseUri = baseUri;
            this._type = type;
            if (encoding != null)
                this.Encoding = encoding;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RESTWebserviceConnection"/> class.
        /// </summary>
        /// <param name="baseUri">The base uri.</param>
        /// <param name="type">The type.</param>
        /// <param name="userName">The user name.</param>
        /// <param name="password">The password.</param>
        /// 
        public RESTWebserviceConnection(string baseUri, string type, string userName, string password) : this(baseUri, type)
        {
            this._username = userName;
            this._password = password;
        }
        #endregion

        #region methods

        /// <summary>
        /// Initializes a new System.Net.WebRequest instance for the specified URI scheme.
        /// </summary>
        /// <param name="accessUri">The access uri.</param>
        /// <param name="type">The type.</param>
        private void CreateNewRequest(string accessUri, string type)
        {
            this._accessUri = accessUri;
            this._request = WebRequest.Create(this._baseUri + this._accessUri) as HttpWebRequest;
            this._request.KeepAlive = true;
            this._request.Timeout = 240000;
            if (this.AdditionalHeaders?.Any() == true)
            {
                var m = this._request.Headers.GetType().GetMethod("AddWithoutValidate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                foreach (KeyValuePair<string, string> item in this.AdditionalHeaders)
                {
                    if (!this._request.Headers.AllKeys.Contains(item.Key))
                    {
                        m.Invoke(this._request.Headers, new string[] { item.Key, item.Value });
                    }
                }
            }
            if (string.IsNullOrWhiteSpace(this._request.ContentType))
            {
                this._request.ContentType = type ?? this._type;
            }
            this._request.Accept = this.Accept;
        }

        /// <summary>
        /// Sets the credentials.
        /// </summary>
        /// <param name="userName">Name of the user.</param>
        /// <param name="password">The password.</param>
        public void SetCredentials(string userName, string password)
        {
            this._request.Credentials = new NetworkCredential(userName, password);
        }

        /// <summary>
        /// Gets the ressource from the endpoint with the get method.
        /// </summary>
        /// <returns>A <see cref="Stream"/>.</returns>
        private Stream GetRessource()
        {
            this._request.Method = nameof(HTTPRequestMethods.GET);
            this._webResponse = this._request.GetResponse() as HttpWebResponse;

            return this.GetResponse();
        }

        /// <summary>
        /// Sends the ressource zu the endpoint with the patch method.
        /// </summary>
        /// <param name="byteData">The patch data.</param>
        /// <returns>A <see cref="Stream"/>.</returns>
        private Stream PatchRessource(byte[] byteData)
        {
            if (byteData == null)
            {
                byteData = new byte[] { };
            }
            this._request.Method = nameof(HTTPRequestMethods.PATCH);
            // Wenn die Zugangsdaten nicht stimmen wird hier eine Exception geworfen
            using (Stream reqStream = this._request.GetRequestStream())
            {
                reqStream.Write(byteData, 0, byteData.Length);
            }

            return this.GetResponse();
        }

        /// <summary>
        /// Gets the ressource async.
        /// </summary>
        /// <returns>A System.Threading.Tasks.Task&lt;Stream&gt;.</returns>
        private async System.Threading.Tasks.Task<Stream> GetRessourceAsync()
        {
            // Get response 
            this._request.Method = nameof(HTTPRequestMethods.GET);
            this._webResponse = await this._request.GetResponseAsync() as HttpWebResponse;
            Stream result = this.GetResponse();
            this._webResponse.Close();

            return result;
        }

        /// <summary>
        /// Sends the ressource zu the endpoint with the post method.
        /// </summary>
        /// <param name="byteData">The data to post.</param>
        /// <returns>A <see cref="Stream"/>.</returns>
        private Stream PostRessource(byte[] byteData)
        {
            if (byteData == null)
            {
                byteData = new byte[] { };
            }

            this._request.Method = nameof(HTTPRequestMethods.POST);

            // If the access data is not correct, an exception is thrown here
            using (Stream reqStream = this._request.GetRequestStream())
            {
                reqStream.Write(byteData, 0, byteData.Length);
            }
            return this.GetResponse();
        }

        /// <summary>
        /// Sends the ressource zu the endpoint with the delete method.
        /// </summary>
        /// <param name="requestContentInJSON">The request string in JavaScript Object Notation (JSON).</param>
        /// <returns>A <see cref="Stream"/>.</returns>
        private Stream DeleteRessource(string requestContentInJSON)
        {
            this._request.Method = nameof(HTTPRequestMethods.DELETE);
            if (!string.IsNullOrWhiteSpace(requestContentInJSON))
            {
                using (var streamWriter = new StreamWriter(this._request.GetRequestStream()))
                {
                    streamWriter.Write(requestContentInJSON);
                    streamWriter.Flush();
                }
            }

            return this.GetResponse();
        }

        /// <summary>
        /// Sends the ressource zu the endpoint with the put method.
        /// </summary>
        /// <param name="sentData">The sent data.</param>
        /// <returns>A <see cref="Stream"/>.</returns>
        private Stream PutRessource(byte[] sentData)
        {
            this._request.Method = nameof(HTTPRequestMethods.PUT);
            // Get response 
            if (sentData != null)
            {
                this._request.ContentLength = sentData.Length;
                using (Stream reqStream = this._request.GetRequestStream())
                {
                    reqStream.Write(sentData, 0, sentData.Length);
                    reqStream.Flush();
                }
            }
            else
            {
                using (Stream reqStream = this._request.GetRequestStream())
                { }
            }

            return this.GetResponse();
        }

        /// <summary>
        /// Gets the stream used to read the body of the server response.
        /// </summary>
        /// <returns>A <see cref="Stream"/>.</returns>
        private Stream GetResponse()
        {
            this._webResponse = this._request.GetResponse() as HttpWebResponse;

            Stream ResponseStream = this._webResponse.GetResponseStream();
            var responseCode = (int)this._webResponse.StatusCode;
            var contentType = this._webResponse.ContentType;

            return ResponseStream;
        }

        /// <summary>
        /// Deserializes the <paramref name="ResponseStream"/> to <typeparamref name="T"/> object.
        /// </summary>
        /// <param name="ResponseStream">The response of type <see cref="Stream"/>.</param>
        /// <returns>A <typeparamref name="T"/> object.</returns>
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

        /// <summary>
        /// Reads a string in JavaScript Object Notation (JSON) format and returns the deserialized object.
        /// </summary>
        /// <param name="json">The json.</param>
        /// <returns>A <typeparamref name="T"/>.</returns>
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

        /// <summary>
        /// Is the json string valid and deserializable to neccessary object?
        /// If yes returns true if not returns false.
        /// </summary>
        /// <param name="json">The JavaScript Object Notation (JSON) string.</param>
        /// <returns>true if valid false if not.</returns>
        public bool IsJsonObjectValid<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) { return false; }
            json = json.Trim();
            if ((json.StartsWith("{") && json.EndsWith("}")) || //For object
                (json.StartsWith("[") && json.EndsWith("]"))) //For array
            {
                try
                {
                    var obj = this.DeserializeToObject<T>(json);
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

        /// <summary>
        /// Sends the <paramref name="requestObject"/> as string to the <paramref name="accessUri"/> with the <paramref name="method"/>.
        /// </summary>
        /// <param name="accessUri">The access uri.</param>
        /// <param name="method">The method.</param>
        /// <param name="requestObject">The request object.</param>
        /// <returns>A <typeparamref name="T"/>.</returns>
        public T SendRequest<T>(string accessUri, ProvidedRequestMethods method, object requestObject = null)
        {
            T result = default;
            if (accessUri != this._accessUri)
            {
                if (this._accessUri != null)
                {
                    this.Dispose();
                }
            }
            this.CreateNewRequest(accessUri, this._type);
            this.SetCredentials(this._username, this._password);

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
            this._request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            Stream stream = null;
            switch (method)
            {
                case ProvidedRequestMethods.GET:
                    stream = this.GetRessource();
                    break;
                case ProvidedRequestMethods.POST:
                    //this.Request.ContentType = "application/x-www-form-urlencoded";
                    this._request.ContentLength = data.LongLength;
                    stream = this.PostRessource(data);
                    break;
                case ProvidedRequestMethods.PUT:
                    this._request.ContentLength = data.LongLength;
                    stream = this.PutRessource(data);
                    break;
                case ProvidedRequestMethods.DELETE:
                    this._request.ContentLength = data.LongLength;
                    stream = this.DeleteRessource(json);
                    break;
                case ProvidedRequestMethods.PATCH:
                    this._request.ContentLength = data.LongLength;
                    stream = this.PatchRessource(data);
                    break;
                default:
                    break;
            }
            if (stream != null)
            {
                result = this.DeserializeToObject<T>(stream);
            }
            this._webResponse.Close();
            return result;
        }

        /// <summary>
        /// Releases the unmanaged resources used by the WeatherStation.Net.RESTWebserviceConnection object.
        /// </summary>
        public void Dispose()
        {
            try
            {
                this._webResponse?.Close();
                this._webResponse?.Dispose();
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
            var WS = new RESTWebserviceConnection(this._baseUri, this._type);
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

        /// <summary>
        /// Querys the <paramref name="uri"/> with the <paramref name="parameters"/>.
        /// </summary>
        /// <param name="uri">The uri.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>A TOutput.</returns>
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

            return this.SendRequest<TOutput>(uri, RESTWebserviceConnection.ProvidedRequestMethods.GET, null);
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
