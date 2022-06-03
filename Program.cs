using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace GooglePhotosAPISimpleDemo
{
    using Arch2;
    class Program
    {
        static void Main(string[] args)
        {

            string jsonUserCreditalPath = @"..\..\..\credentials.json";
            string storeUserCreditalPath = @"C:\StorePath\";
            string accountName = "Milk";

            string[] scopes = new string[]
            {
                "https://www.googleapis.com/auth/photoslibrary"
            };




            string nameAlbum = "Self-Prod Disc";
            string path = @"C:\Users\F\Desktop\MyDisc\";
            string tPath = @"C:\Users\F\Desktop\MyDisc\ms.txt";
            GooglePhotoUserCredentialHendler cHendler = new GooglePhotoUserCredentialHendler(jsonUserCreditalPath, storeUserCreditalPath, accountName, scopes);
            GooglePhotoUserCredential credential = (GooglePhotoUserCredential)cHendler.GetCrendital();

            Album album = new Album(nameAlbum, credential);
            WindowsFolder folder = new WindowsFolder(path);

            var aMs = album.GetMediaitems();
            for (int i = 0; i < aMs.Length; i++)
            {
                var m = aMs[i];
                var rName = new WindowsMediaitemRandomNameHendler();
                rName.GetName(m);
            }
            folder.UploadMediaitems(aMs);
            //var fMs = folder.GetMediaitems();
            //OverallMediaitem[] oMs = new OverallMediaitem[fMs.Length];
            //for (int i = 0; i < fMs.Length; i++)
            //{
            //    var wM = (WindowsMediaitem)((ByteMediaitem)fMs[i]).GetHendler();
            //    var gM = (GoogleMediaitem)aMs[i];

            //    OverallMediaitem oM = new OverallMediaitem(wM, gM);
            //    oMs[i] = oM;
            //}


            FromJsonFileOverallMediaitemFactory factory = new FromJsonFileOverallMediaitemFactory(tPath, credential);
            var res = (OverallMediaitem[])factory.GetMediaitems();

        }
        
        
        
    }
   
}



namespace Arch2
{
    #region юзер крендитал и его(их) фабрика
    public abstract class MyUserCrendital
    {
        public string ClientID { get; set; }
        public string ClientSecret { get; set; }

    }
    public class GooglePhotoUserCredential : MyUserCrendital
    {
        public UserCredential User { get; set; }

    }
    public abstract class AbstractUserCreditalHendler
    {
        public abstract MyUserCrendital GetCrendital();
    }
    public class GooglePhotoUserCredentialHendler : AbstractUserCreditalHendler
    {
        public GooglePhotoUserCredentialHendler(string jsonPath, string storePath, string userName, string[] scopes)
        {
            _jsonPath = jsonPath ?? "";
            _storePath = storePath ?? "";
            _userName = userName ?? "";
            _scopes = scopes ?? new string[] { };
        }
        private string _jsonPath = "";
        private string _storePath = "";
        private string _userName = "";
        string[] _scopes = new string[] { };

        public override MyUserCrendital GetCrendital()
        {
            UserCredential credential = null;
            try
            {
                using (var stream = new FileStream(_jsonPath, FileMode.Open, FileAccess.Read))
                {
                    credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.FromStreamAsync(stream).Result.Secrets,
                        _scopes,
                        _userName,
                        CancellationToken.None,
                        new FileDataStore(_storePath, true)).Result;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;

            }
            
            JObject jObject = JObject.Parse(File.ReadAllText(_jsonPath));
            string ClientID = jObject["installed"]["client_id"].ToString();
            string ClientSecret = jObject["installed"]["client_secret"].ToString();

            GooglePhotoUserCredential user = new GooglePhotoUserCredential { ClientID = ClientID, ClientSecret = ClientSecret, User = credential };

            return user;
        }
    }
    #endregion

    #region Фабрика голов запросов

    public abstract class BaseRequestHeadersBuilder
    {
        public abstract WebHeaderCollection Build();
        protected BaseRequestHeadersBuilder() { }
    }

    public class GooglePhotoRequestHeadBuilder : BaseRequestHeadersBuilder
    {
        WebHeaderCollection _header = new WebHeaderCollection();
        GooglePhotoUserCredential _credetial;
        public GooglePhotoRequestHeadBuilder(GooglePhotoUserCredential credential)
        {
            _credetial = credential;
        }
        public override WebHeaderCollection Build()
        {
            

            var clientID = _credetial.ClientID;
            var clientSecret = _credetial.ClientSecret;
            var credential = _credetial.User;

            _header.Add("client_id", clientID);
            _header.Add("client_secret", clientSecret);
            _header.Add("Authorization:" + credential.Token.TokenType + " " + credential.Token.AccessToken);

            return _header;
        }

        public void Set_XGoogUploadContentType(string contentType)
        {
            _header.Add("X-Goog-Upload-Content-Type", contentType);
        }
        public void Set_XGoogUploadProtocol(string protocol)
        {
            _header.Add("X-Goog-Upload-Protocol", protocol);
        }

    }

    #endregion

    #region Фабрика запросов
    public class DirectorRequest
    {
        RequestBuilder _builder;
        public DirectorRequest(RequestBuilder builder)
        {
            _builder = builder;
        }
        public void OverrideBuilder(RequestBuilder builder)
        {
            _builder = builder;
        }
        public virtual void Construct(string method, WebHeaderCollection head, string json = "", byte[] bytes = null, string contentType = "application/json")
        {
            if (_builder == null)
                return;

            _builder.AddHead(head);
            _builder.AddMethod(method);
            _builder.AddJson(json);
            _builder.AddBytes(bytes);
            _builder.SetContentType(contentType);
        }
    }

    public abstract class RequestBuilder
    {
        public abstract void AddMethod(string method);
        public abstract void AddHead(WebHeaderCollection head);
        public abstract void AddJson(string json);
        public abstract void AddBytes(byte[] bytes);
        public abstract void SetContentType(string contentType);
        public abstract Request GetResult();
    }

    public class Request
    {
        protected HttpWebRequest _request;
        protected WebHeaderCollection _header;
        protected string _method = "";
        protected string _json;
        protected string _url;
        protected byte[] _bytes;
        protected string _contentType;

        public virtual bool CheckInitializate()
        {
            if (_request == null)
                return false;

            return true;
        }
        public virtual void AddMethod(string method)
        {
            if (!CheckInitializate())
                return;

            _method = method;
            _request.Method = method;
        }
        public virtual void AddHeader(WebHeaderCollection header)
        {
            if (!CheckInitializate())
                return;

            _header = header;
            _request.Headers = header;
        }
        public virtual void AddJson(string json)
        {
            if (!CheckInitializate() || json == null || json == "" || _method == "GET")
                return;

            _json = json;
            using (var resStream = _request.GetRequestStream())
            {
                using (StreamWriter writer = new StreamWriter(resStream))
                {
                    writer.Write(json);
                }
            }
            //
            //
            //
        }
        public virtual void AddBytes(byte[] bytes)
        {
            if (!CheckInitializate() || bytes == null || bytes == new byte[] { } || _method == "GET")
                return;

            _bytes = bytes;

            using (MemoryStream stream  = new MemoryStream(bytes))
            {
                using (var reqStream = _request.GetRequestStream())
                {
                    stream.WriteTo(reqStream);
                }
            }

        }
        public virtual void SetContentType(string contentType)
        {
            if (!CheckInitializate())
                return;

            _contentType = contentType;

            _request.ContentType = _contentType;
        }
        public virtual void Initializate(string url)
        {
            _url = url;

            _request = (HttpWebRequest)WebRequest.Create(url);
        }
        public virtual HttpWebRequest GetRequest()
        {
            if (_method == "" || _request == null || _header == null)
                return null;

            return _request;
        }
    }

    


    public class GooglePhotoRequestBuilder : RequestBuilder
    {
        Request product = new Request();

        public GooglePhotoRequestBuilder(string uri)
        {
            product.Initializate(uri);
        }

        public override void AddBytes(byte[] bytes)
        {
            product.AddBytes(bytes);
        }

        public override void AddHead(WebHeaderCollection header)
        {
            product.AddHeader(header);
        }

        public override void AddJson(string json)
        {
            product.AddJson(json);
        }

        public override void AddMethod(string method)
        {
            product.AddMethod(method);
        }

        public override void SetContentType(string contentType)
        {
            product.SetContentType(contentType);
        }
        public override Request GetResult()
        {
            return product;
        }
        
    }

    #endregion

    #region Медиайтемы и все с ними свзязанное
    abstract public class Mediaitem
    {
        public const string IMAGE = "image";
        public const string VIDEO = "video";
        public readonly string[] Types;
        protected Mediaitem()
        {
            Types = new string[] { IMAGE, VIDEO };
        }
        public string Name { get; set; } = "";
        public string MimeType { get; set; } = "";

        public string GetNameWithExtention()
        {
            string n = Name;
            int index = 0;
            for (int i = 0; i < MimeType.Length; i++)
            {
                if (MimeType[i] == '/')
                {
                    index = i + 1;
                    break;
                }
            }
            n += $".{MimeType[index..]}";
            return n;
        }

        public abstract byte[] GetBytes();

        public abstract string GetId();
    }

    public class MediaitemComparer : IEqualityComparer<Mediaitem>
    {
        public bool Equals([AllowNull] Mediaitem x, [AllowNull] Mediaitem y)
        {
            if (Object.ReferenceEquals(x, y)) return true;

            //Check whether any of the compared objects is null.
            if (Object.ReferenceEquals(x, null) || Object.ReferenceEquals(y, null))
                return false;

            return x.MimeType == y.MimeType && x.Name == y.Name;
        }

        public int GetHashCode([DisallowNull] Mediaitem mediaitem)
        {
            //if (Object.ReferenceEquals(mediaitem, null)) return 0;

            ////Get hash code for the Name field if it is not null.
            //int hashMediaitemtName = mediaitem.Name == null ? 0 : mediaitem.Name.GetHashCode();

            ////Get hash code for the Code field.
            //int hashMediaitemMimeType = mediaitem.MimeType.GetHashCode();

            ////Calculate the hash code for the product.
            //return hashMediaitemtName ^ hashMediaitemMimeType;

            return mediaitem.GetId().GetHashCode();
        }
    }

    public class GoogleMediaitem : ByteMediaitem
    {
        public string AlbumId { get; set; } = "";
        public string Id { get; set; } = "";
        public string BaseUrl { get; set; } = "";
        public static GoogleMediaitem GetMediaitemById(string id, string albumId, GooglePhotoUserCredential credential)
        {
            GooglePhotoRequestBuilder rB = new GooglePhotoRequestBuilder($"https://photoslibrary.googleapis.com/v1/mediaItems/{id}");
            DirectorRequest dR = new DirectorRequest(rB);
            GooglePhotoRequestHeadBuilder hBuilder = new GooglePhotoRequestHeadBuilder(credential);
            var headersCollection = hBuilder.Build();
            dR.Construct("GET", headersCollection);
            var req = rB.GetResult().GetRequest();
            ResponseReader response = new ResponseReader();
            JToken jToken = response.GetJson(req);
            FromJsonGoogleMediaitemFactory f = new FromJsonGoogleMediaitemFactory(jToken, albumId);
            var m = (GoogleMediaitem)f.GetMediaitem();
            return m;
        }

        public override byte[] GetBytes()
        {
            if (BaseUrl == "")
                return new byte[] { };

            var baseUrl = BaseUrl;

            if (MimeType.Contains("video"))
            {
                baseUrl += "=dv";
            }
            else if (MimeType.Contains("image"))
            {
                baseUrl += "=d";
            }
            GooglePhotoRequestBuilder rBuilder = new GooglePhotoRequestBuilder(baseUrl);

            DirectorRequest director = new DirectorRequest(rBuilder);
            director.Construct("GET", new WebHeaderCollection());

            ResponseReader responseReader = new ResponseReader();
            var bytes = responseReader.GetBytes(rBuilder.GetResult().GetRequest());

            return bytes;
        }

        public override string GetId()
        {
            return Id;
        }
    }

    public class WindowsMediaitem : ByteMediaitem
    {
        string[] extentions = new string[] { };
        string[] vExt = new string[] { "3GP", "3G2", "ASF", "AVI", "DIVX", "M2T", "M2TS", "M4V", "MKV", "MMV", "MOD", "MOV", "MP4", "MPG", "MTS", "TOD", "WMV" };
        string[] imgExt = new string[] { "BMP", "GIF", "HEIC", "ICO", "JPG", "PNG", "TIFF", "WEBP" };

        private void InitExtentions() => extentions = vExt.Union(imgExt).ToArray();
        public WindowsMediaitem() 
        { 
            InitExtentions(); 
            InitFields(); 
        }
        public WindowsMediaitem(string path = "")
        {
            Path = path;
            InitExtentions();
            InitFields();
        }
        private bool FileIsFitedOrFound()
        {
            if (!CheckInitalizate())
                return false;
            bool fileIsExist = File.Exists(Path);
            if (fileIsExist)
            {
                bool extIsFound = false;
                for (int i = 0; i < extentions.Length; i++)
                {
                    if (Path.ToUpper().EndsWith(extentions[i]))
                    {
                        extIsFound = true;
                        break;
                    }
                }

                if (!extIsFound)
                    return false;


                return true;

               

            }
            return false;
        }
        void InitFields()
        {

            if (!FileIsFitedOrFound())
                return;

            FileInfo file = new FileInfo(Path);
            string ext = file.Extension[1..];
            if (vExt.Contains(file.Extension))
                MimeType = "video/";
            else
                MimeType = "image/";
            MimeType += ext;

            int t = (file.Name.Length - ext.Length - 1);

            Name = file.Name[0..t];
        }

        public bool CheckInitalizate()
        {
            if (Path == "")
                return false;

            return true;
        }
        public string Path { get; set; }

        public override byte[] GetBytes()
        {
            if (!FileIsFitedOrFound())
                return new byte[] { };

            byte[] bytes = new byte[] { };
            using (var stream = File.OpenRead(Path))
            {
                bytes = new byte[stream.Length];
                int numBytesToRead = (int)stream.Length;
                int numBytesRead = 0;
                while (numBytesToRead > 0)
                {
                    // Read may return anything from 0 to numBytesToRead.
                    int n = stream.Read(bytes, numBytesRead, numBytesToRead);

                    // Break when the end of the file is reached.
                    if (n == 0)
                        break;

                    numBytesRead += n;
                    numBytesToRead -= n;
                }
            }
            return bytes;
        }

        public override string GetId()
        {
            return Path;
        }
    }

    public class ByteMediaitem : Mediaitem
    {
        protected Mediaitem _methodHendler = null;
        public ByteMediaitem() { }
        public Mediaitem GetHendler()
        {
            return _methodHendler;
        }
        public ByteMediaitem(Mediaitem methodHendler) 
        {
            _methodHendler = methodHendler;

            Name = _methodHendler.Name;
            MimeType = _methodHendler.MimeType;
            
        }


        public override byte[] GetBytes()
        {
            byte[] bytes = new byte[] { };
            if(_methodHendler != null)
            {
                bytes = _methodHendler.GetBytes();
            }
            return bytes;
        }
        public override string GetId()
        {
            string id = "";
            if (_methodHendler != null)
            {
                id = _methodHendler.GetId();
            }
            return id;
        }
    }

    public class UploadMediaitem : ByteMediaitem
    {
        public bool UploadTokenIsCreated { get; set; } = false;
        public UploadMediaitem() { }
        public UploadMediaitem(Mediaitem methodHendler) : base(methodHendler) {  }
        public string Description { get; set; } = "";
        public string UploadToken { get; set; } = "";
        public override byte[] GetBytes()
        {
            return _methodHendler.GetBytes();
        }

        public bool MakeUploadToken(GooglePhotoUserCredential credential, Mediaitem methodHendler = null)
        {
            if (methodHendler != null)
                _methodHendler = methodHendler;

            if (!new CheckerInitializate(MimeType, Name, _methodHendler).CheckByValue("", "", null))
            {
                return false;
            }
            
            if(Description == "")
                _ = "fromWindowsFolder";

            GooglePhotoRequestHeadBuilder headBuilder = new GooglePhotoRequestHeadBuilder(credential);
            headBuilder.Set_XGoogUploadContentType(MimeType);
            headBuilder.Set_XGoogUploadProtocol("raw");
            var head = headBuilder.Build();

            GooglePhotoRequestBuilder builderUploadRequest = new GooglePhotoRequestBuilder("https://photoslibrary.googleapis.com/v1/uploads");

            DirectorRequest dRequest = new DirectorRequest(builderUploadRequest);
            string cType = "application/octet-stream";

            var bytes = GetBytes();
            dRequest.Construct("POST", head, bytes: bytes, contentType: cType);
            string uploadToken = new ResponseReader().GetString(builderUploadRequest.GetResult().GetRequest());

            if (uploadToken == "" || uploadToken == null)
                return false;



            UploadToken = uploadToken;
            UploadTokenIsCreated = true;

            return true;
            

        }


        
    }

    public class OverallMediaitem : Mediaitem
    {
        protected WindowsMediaitem _wMediaitem;
        protected GoogleMediaitem _gMediaitem;
        public GoogleMediaitem GMediaitem { get => _gMediaitem; }
        public WindowsMediaitem WMediaitem { get => _wMediaitem; }
        protected KeyValuePair<string, string>  _saveMediaitem;
        public OverallMediaitem(WindowsMediaitem wMediaitem, GoogleMediaitem gMediaitem)
        {
            _gMediaitem = gMediaitem;
            _wMediaitem = wMediaitem;
            if (_gMediaitem != null || _wMediaitem != null)
            {
                _saveMediaitem = new KeyValuePair<string, string>(_wMediaitem.GetId(), _gMediaitem.GetId());

                Name = _gMediaitem.Name ?? _wMediaitem.Name;
                MimeType = _gMediaitem.MimeType ?? _wMediaitem.MimeType;
            }

            
        }
        public string GetWindowsMediaitemId()
        {
            return _wMediaitem?.GetId() ?? "";
        }
        public string GetGoogleMediaitemId()
        {
            return _gMediaitem?.GetId() ?? "";
        }
        public override byte[] GetBytes()
        {
            if (_wMediaitem != null) 
            {
                var bs = _wMediaitem.GetBytes();

                if (_gMediaitem != null && bs.Length == 0)
                    return _gMediaitem.GetBytes();

                return bs;
            }

            return _gMediaitem.GetBytes();
        }
        public override string GetId()
        {
            return _saveMediaitem.ToString();
            
        }
    }


    #region Фабрика медиаайтемов

    public interface IGetMediaitemByName
    {
        public Mediaitem GetMediaitemByName(string name);
    }
    public abstract class MediaitemFactory : IGetMediaitemByName
    {
        public abstract Mediaitem GetMediaitem();

        public abstract Mediaitem[] GetMediaitems();


        protected virtual Mediaitem UniteMediaitem(Mediaitem mediaitem1, Mediaitem mediaitem2)
        {
            if (mediaitem1 == null || mediaitem2 == null)
                return new ByteMediaitem { };

            mediaitem2.MimeType = mediaitem1.MimeType;
            mediaitem2.Name = mediaitem1.Name;
            return mediaitem2;
        }

        protected virtual Mediaitem[] UniteMediaitems(Mediaitem[] mediaitems1, Mediaitem[] mediaitems2)
        {
            if (mediaitems1.Length != mediaitems2.Length)
            {
                return new ByteMediaitem[] { };
            }

            for (int i = 0; i < mediaitems1.Length; i++)
            {
                Mediaitem mediaitem1 = mediaitems1[i];
                Mediaitem mediaitem2 = mediaitems2[i];

                UniteMediaitem(mediaitem1, mediaitem2);
            }


            return mediaitems2;
        }

        public virtual Mediaitem GetMediaitemByName(string name)
        {
            var ms = GetMediaitems();
            Mediaitem res = null;
            for (int i = 0; i < ms.Length; i++)
            {
                var m = ms[i];

                if (m.Name == name)
                {
                    res = m;
                }
            }
            return res;
        }
    }




    public class FromJsonGoogleMediaitemFactory : MediaitemFactory
    {
        protected JToken _json;
        protected string _albumId;
        public FromJsonGoogleMediaitemFactory(JToken json, string albumId)
        {
            _json = json;
            _albumId = albumId;
        }
        public const string mimeTypeCell = "mimeType";
        public const string filenameCell = "filename";
        public const string baseUrlCell = "baseUrl";
        public const string idCell = "id";
        public const string mediaItemsCell = "mediaItems";
        public override Mediaitem GetMediaitem()
        {
            GoogleMediaitem mediaitem = new GoogleMediaitem();
            mediaitem.MimeType = _json?[mimeTypeCell]?.ToString() ?? "";
            mediaitem.Name = _json?[filenameCell]?.ToString() ?? "";
            mediaitem.BaseUrl = _json?[baseUrlCell]?.ToString() ?? "";
            mediaitem.Id = _json?[idCell]?.ToString() ?? "";
            mediaitem.AlbumId = _albumId;
            return mediaitem;
        }

        public override Mediaitem[] GetMediaitems()
        {
            var jsonArr = _json[mediaItemsCell]?.ToArray() ?? new JToken[] { };
            GoogleMediaitem[] ts = new GoogleMediaitem[jsonArr.Length];
            var tJ = JObject.Parse(_json.ToString());
            for (int i = 0; i < ts.Length; i++)
            {
                
                _json = jsonArr[i];
                ts[i] = (GoogleMediaitem)GetMediaitem();

                _json = tJ;
            }



            return ts;
        }
    } // работает

    public class FromFolderByteMediaitemFactory : MediaitemFactory
    {
        protected string _path = "";
        string[] extentions = new string[] { };
        string[] vExt = new string[] { "3GP", "3G2", "ASF", "AVI", "DIVX", "M2T", "M2TS", "M4V", "MKV", "MMV", "MOD", "MOV", "MP4", "MPG", "MTS", "TOD", "WMV" };
        string[] imgExt = new string[] { "BMP", "GIF", "HEIC", "ICO", "JPG", "PNG", "TIFF", "WEBP" };
        public FromFolderByteMediaitemFactory(string path)
        {
            _path = path;
            extentions = vExt.Union(imgExt).ToArray();
        }

        public bool CheckInit 
        {
            get
            {
                if (_path == "")
                {
                    return false;
                }
                return true;
            }
        }
        public override Mediaitem GetMediaitem()
        {
            ByteMediaitem mediaItem = new ByteMediaitem {MimeType = "", Name =  "" };
            if (!CheckInit)
                return mediaItem;

            bool fileIsExist = File.Exists(_path);
            if (fileIsExist)
            {
                bool extIsFound = false;
                for (int i = 0; i < extentions.Length; i++)
                {
                    if (_path.ToUpper().EndsWith(extentions[i]))
                    {
                        extIsFound = true;
                        break;
                    }
                }

                if (!extIsFound)
                    return new ByteMediaitem();

                string mT;
                FileInfo file = new FileInfo(_path);
                if (vExt.Contains(file.Extension))
                    mT = "video/" + file.Extension;
                else
                    mT = "image/" + file.Extension;

                var name = file.Name;
                WindowsMediaitem wMediaitem = new WindowsMediaitem(_path) {MimeType = mT, Name = name, Path = _path};
                mediaItem = new ByteMediaitem(wMediaitem);

            }
            return mediaItem;
        }

        

        public override Mediaitem[] GetMediaitems()
        {
            ByteMediaitem[] mediaitems = null;

            if(_path == "")
                return new ByteMediaitem[] { };

            if (Directory.Exists(_path))
            {
                string[] files = Directory.GetFiles(_path, "*.*", SearchOption.TopDirectoryOnly).Where(file =>
                {

                    for (int i = 0; i < extentions.Length; i++)
                    {
                        if (file.ToUpper().EndsWith(extentions[i]))
                        {
                            return true;
                        }
                    }
                    return false;
                }).ToArray();

                string tPath = _path;
                mediaitems = new ByteMediaitem[files.Length];
                for (int i = 0; i < files.Length; i++)
                {
                    _path = files[i];
                    var m = GetMediaitem();
                    mediaitems[i] = (ByteMediaitem)m;
                }
                _path = tPath;
            }

            if (mediaitems == null)
                return new ByteMediaitem[] { };

            return mediaitems;
        }
    } // работает

    public class FromAlbumGoogleMediaitemFactory : MediaitemFactory
    {
        private const string albumIdCell = "albumId";
        protected string _albumId;
        GooglePhotoUserCredential _credential;
        public FromAlbumGoogleMediaitemFactory(string albumId, GooglePhotoUserCredential credential)
        {
            _albumId = albumId;
            _credential = credential;
        }
        
        public override Mediaitem GetMediaitem()
        {
            var m = (GoogleMediaitem)GetMediaitems().FirstOrDefault() ?? new GoogleMediaitem {BaseUrl = "", Id = "", MimeType = "", Name = "" };

            return m;
        }

        public override Mediaitem[] GetMediaitems()
        {
            if(_credential == null || _albumId == "")
                return new GoogleMediaitem[] { };

            GooglePhotoRequestHeadBuilder bHeader = new GooglePhotoRequestHeadBuilder(_credential);

            GooglePhotoRequestBuilder rBuilder = new GooglePhotoRequestBuilder("https://photoslibrary.googleapis.com/v1/mediaItems:search");
            DirectorRequest directorRequest = new DirectorRequest(rBuilder);

            string json = "{" + $"'{albumIdCell}': '{_albumId}'" + "}";
            directorRequest.Construct("POST", bHeader.Build(), contentType: "application/json", json: json);


            var req = rBuilder.GetResult().GetRequest();
            ResponseReader responseReader = new ResponseReader();
            var jtoken = responseReader.GetJson(req);

            FromJsonGoogleMediaitemFactory factory = new FromJsonGoogleMediaitemFactory(jtoken, _albumId);
            var ms = (GoogleMediaitem[])factory.GetMediaitems();

            return ms;
        }
    } // работает

    public class FromJsonFileOverallMediaitemFactory : MediaitemFactory
    {
        protected string _path;
        GooglePhotoUserCredential _credential;
        public FromJsonFileOverallMediaitemFactory(string path, GooglePhotoUserCredential credential)
        {
            _path = path;
            _credential = credential;
        }
        public override Mediaitem GetMediaitem()
        {
            return null;
        }

        public override Mediaitem[] GetMediaitems()
        {
            string data = "";
            if (File.Exists(_path))
            {
                data = File.ReadAllText(_path);
            }
            else
                File.Create(_path);

            JObject jObject = JObject.Parse(data);
            
            var arr = jObject["meadiaitems"].ToArray();

            OverallMediaitem[] mediaitems = new OverallMediaitem[arr.Length];

            for (int i = 0; i < arr.Length; i++)
            {
                var t = arr[i];

                var path = t["path"].ToString();
                var id = t["id"].ToString();
                var albumId = t["albumId"].ToString();
                WindowsMediaitem wM = new WindowsMediaitem(path);
                GoogleMediaitem gM = GoogleMediaitem.GetMediaitemById(id, albumId, _credential);
                OverallMediaitem overallMediaitem = new OverallMediaitem(wM, gM);
                mediaitems[i] = overallMediaitem;
            }
            return mediaitems;
        }
    }
    #endregion


    #region Загрузчики медиаайтемов


    public abstract class UploaderMediaitem
    {
        public bool IsSuccess { get; protected set; } = false;
        protected readonly Mediaitem[] _mediaitems;
        protected UploaderMediaitem(params Mediaitem[] mediaitems)
        {

            _mediaitems = mediaitems;
        }

        public abstract void Upload();
    }

    public class ToAlbumUploader : UploaderMediaitem
    {
        GooglePhotoUserCredential _credential;
        string _albumId;
       
        WebHeaderCollection _defaultHead;
        public ToAlbumUploader(GooglePhotoUserCredential credential, string albumId,params Mediaitem[] mediaitems) : base(mediaitems)
        {
            _credential = credential;
            _albumId = albumId;
            GooglePhotoRequestHeadBuilder builder = new GooglePhotoRequestHeadBuilder(credential);
            _defaultHead = builder.Build();
        }
        public override void Upload()
        {
            IsSuccess = false;
            JObject overallJobject = new JObject();
            JArray jArray = new JArray();
            for (int i = 0; i < _mediaitems.Length; i++)
            {
                var m = _mediaitems[i];

                if (m == null) continue;

                UploadMediaitem uM = new UploadMediaitem(m);

                MakeUploadToken(uM);
                
                JObject item = new JObject();
                item.Add("description", uM.Description);
                JObject simpleMediaItem = new JObject();
                simpleMediaItem.Add("fileName", uM.Name);
                simpleMediaItem.Add("uploadToken", uM.UploadToken);
                item.Add("simpleMediaItem", simpleMediaItem);
                jArray.Add(item);
            }
            overallJobject.Add("albumId", _albumId);
            overallJobject.Add("newMediaItems", jArray);

            string json = overallJobject.ToString();

            IsSuccess = UploadToken(json);
        }

        private void MakeUploadToken(UploadMediaitem uM)
        {
            uM.MakeUploadToken(_credential);
        }

        private bool UploadToken(string json)
        {
            GooglePhotoRequestBuilder rBuilder = new GooglePhotoRequestBuilder("https://photoslibrary.googleapis.com/v1/mediaItems:batchCreate");
            DirectorRequest directorRequest = new DirectorRequest(rBuilder);
            directorRequest.Construct("POST", _defaultHead, json);
            var req = rBuilder.GetResult().GetRequest();

            ResponseReader r = new ResponseReader();
            var str =  r.GetString(req);

            if (str == "")
                return false;

            return true;
        }
    }

    public class ToTextFileUploader : UploaderMediaitem
    {
        protected new OverallMediaitem[] _mediaitems;
        string _path;
        public ToTextFileUploader(string path, params OverallMediaitem[] mediaitems) : base(mediaitems)
        {
            _mediaitems = mediaitems;
            _path = path;
        }
        public override void Upload()
        {
            string partOfJson;

            JObject jo = new JObject();
            JArray jArr = new JArray();
            for (int i = 0; i < _mediaitems.Length; i++)
            {
                var m = _mediaitems[i];
                string path = m.WMediaitem.Path;
                string id = m.GMediaitem.Id;
                string albumId = m.GMediaitem.AlbumId;
                JObject lO = new JObject();
                lO.Add("path", path);
                lO.Add("id", id);
                lO.Add("albumId", albumId);
                jArr.Add(lO);
            }
            jo.Add("meadiaitems",jArr);
            partOfJson = jo.ToString();
            try
            {
                File.WriteAllText(_path, partOfJson);
                IsSuccess = true;
            }
            catch
            {
                IsSuccess = false;
            }
        }

    }

    public class ToWinFolderUploader : UploaderMediaitem
    {
        string _pathFolder;
        public ToWinFolderUploader(string pathFolder, params Mediaitem[] mediaitems) : base(mediaitems)
        {
            _pathFolder = pathFolder;
        }
        public override void Upload()
        {
            IsSuccess = true;
            for (int i = 0; i < _mediaitems.Length; i++ )
            {
                var m = _mediaitems[i];
                LoadMediaite(m);
            }
        }

        private void LoadMediaite(Mediaitem m)
        {
            string path = _pathFolder + m.Name;

            var bs = m.GetBytes();
            try
            {
                using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write))
                {
                    stream.Write(bs, 0, bs.Length);
                }
            }
            catch
            {
                IsSuccess = false;
            }
            finally
            {

            }
        }
    }
    #endregion

    #endregion

    #region Хранилища
    public abstract class BaseOverallStorage : BaseStorage
    {
        protected BaseStorage[] _someStorages;
        protected BaseOverallStorage(params BaseStorage[] someStorages)
        {
            _someStorages = someStorages ?? new BaseStorage[] { };
        }
        
    }
    public delegate void  StorageChanger(BaseStorage hendler, params Mediaitem[] mediaitems); 
    public abstract class BaseStorage
    {
        protected StorageChanger uploader;
        protected StorageChanger deleter;
        public bool RecentUploadIsSuccefull { get; protected set; }
        public event StorageChanger UploadEvent
        {
            add
            {
                if (value != null)
                    uploader += value;
            }
            remove
            {
                if (uploader != null)
                    uploader -= value;
            }
        }
        public event StorageChanger DeleteEvent
        {
            add
            {
                if (value != null)
                    deleter += value;
            }
            remove
            {
                if (deleter != null)
                    deleter -= value;
            }
        }
        public abstract void UploadMediaitems(params Mediaitem[] mediaitems);
        public abstract Mediaitem[] GetMediaitems();
        public abstract string GetId();
        public abstract List<byte[]> GetMediaitemsInBytes();
        public virtual void UpdateImgs(Mediaitem[] storage)
        {
            Mediaitem[] albumsMediaItems = GetMediaitems();

            var intersectMediaItems = storage.Except(albumsMediaItems, new MediaitemComparer());

            ByteMediaitem[] bMs = new ByteMediaitem[intersectMediaItems.Count()];


            for (int i = 0; i < bMs.Count();)
            {
                var iM = intersectMediaItems.ElementAt(i);
                bMs[i] = new ByteMediaitem(iM);
                var bM = bMs[i];


                bM.MimeType = iM.MimeType;
                bM.Name = iM.Name;
            }

            UploadMediaitems(bMs);
        }
        public abstract void DeleteMediaitems(params Mediaitem[] mediaitems);
        

        public void InitEvents(StorageChanger uploader , StorageChanger deleter)
        {
            UploadEvent += uploader;
            DeleteEvent += deleter;
        }

        public void ClearEvents()
        {
            uploader = null;

            deleter = null;
        }
    }
    public class Album : BaseStorage
    {
        protected WebHeaderCollection _defaultHead;
        protected string _albumName = "";
        protected string _albumId = "";
        
        protected GooglePhotoUserCredential _credential = null;

        protected GoogleMediaitem[] _mediaitems = new GoogleMediaitem[] { };

        protected Dictionary<string, string> _urls = new Dictionary<string, string>();
        public Album(string nameAlbum, GooglePhotoUserCredential credential)
        {
            _albumName = nameAlbum;
            _credential = credential;
            _urls["albums"] = "https://photoslibrary.googleapis.com/v1/albums";
            _urls["uploadingBytes"] = "https://photoslibrary.googleapis.com/v1/uploads";
            _urls["creatingMediaItem"] = "https://photoslibrary.googleapis.com/v1/mediaItems:batchCreate";

            if (credential == null)
                return;

            GooglePhotoRequestHeadBuilder hBuilder = new GooglePhotoRequestHeadBuilder(credential);
            _defaultHead = hBuilder.Build();
        }

        private bool CheckInit()
        {
            if (_albumName == "" || _credential == null || _defaultHead == null)
                return false;

            return true;
        }

        public override string GetId()
        {
            if (_albumId != "")
                return _albumId;

            if (!CheckInit())
                return "";

            GooglePhotoRequestBuilder builderRequest = new GooglePhotoRequestBuilder(_urls["albums"]);
            DirectorRequest director = new DirectorRequest(builderRequest);
            director.Construct("GET", _defaultHead);
            var request = builderRequest.GetResult().GetRequest();
            string id = "";
            ResponseReader responseReader = new ResponseReader();
            var albums = responseReader.GetJson(request)["albums"]?.ToList() ?? new List<JToken>();
            if (albums.Count == 0)
            {
                GooglePhotoRequestBuilder bR = new GooglePhotoRequestBuilder(_urls["albums"]);
                director.OverrideBuilder(bR);
                string json = "{" + "album:" + "{" + $"'title': '{_albumName}'" + "}" + "}";
                director.Construct("POST", _defaultHead, json);

                var req = bR.GetResult().GetRequest();

                ResponseReader r = new ResponseReader();
                var t = r.GetString(req);

                if (t == "")
                    return "";

                JObject j = JObject.Parse(t);
                id = j["id"].ToString();
                _albumId = id;
                return id;
            }
            for (int i = 0; i < albums.Count; i++)
            {
                var currentName = albums[i]["title"].ToString();
                if (currentName == _albumName)
                    id = albums[i]["id"].ToString();
            }
            if(id == "")
            {
                GooglePhotoRequestBuilder bR = new GooglePhotoRequestBuilder(_urls["albums"]);
                director.OverrideBuilder(bR);
                string json = "{" + "album:" +"{" + $"'title': '{_albumName}'" + "}" + "}";
                director.Construct("POST", _defaultHead, json);

                var req = bR.GetResult().GetRequest();

                ResponseReader r = new ResponseReader();
                var t = r.GetString(req);

                JObject j = JObject.Parse(t);
                id = j["id"].ToString();
            }

            _albumId = id;
            return id;
        }

        public override Mediaitem[] GetMediaitems()
        {
            if (!CheckInit())
                return new Mediaitem[] { };

            if (_albumId == "")
                GetId();

            FromAlbumGoogleMediaitemFactory factory = new FromAlbumGoogleMediaitemFactory(_albumId, _credential);
            var res = (GoogleMediaitem[])factory.GetMediaitems();
            _mediaitems = res;
            return res;
        } // работает

        public override List<byte[]> GetMediaitemsInBytes()
        {
            if (!CheckInit())
                return new List<byte[]>();

            if (_mediaitems == null || _mediaitems == new Mediaitem[] { })
                GetMediaitems();

            return _mediaitems.Select((m) => m.GetBytes()).ToList();

            
        }

        public override void UpdateImgs(Mediaitem[] storage)
        {
            if (!CheckInit())
                return;

            base.UpdateImgs(storage);
        }

        public override void UploadMediaitems(params Mediaitem[] mediaitems)
        {
            if (!CheckInit() || mediaitems == null || mediaitems.Length == 0)
                return;

            if (_albumId == "")
                GetId();

            RecentUploadIsSuccefull = false;
            ToAlbumUploader upl = new ToAlbumUploader(_credential, _albumId, mediaitems);
            upl.Upload();
            RecentUploadIsSuccefull = upl.IsSuccess;

            if (RecentUploadIsSuccefull)
                uploader?.Invoke(this, mediaitems);
        }

        public override void DeleteMediaitems(params Mediaitem[] mediaitems)
        {
            
        }
    }
    public class WindowsFolder : BaseStorage
    {
        public WindowsFolder(string path)
        {
            _path = path ?? "";
        }

        protected string _path;

        public override string GetId()
        {
            return _path;
        }

        public override Mediaitem[] GetMediaitems()
        {
            if (_path == "")
                return new Mediaitem[] { };

            FromFolderByteMediaitemFactory mFactory = new FromFolderByteMediaitemFactory(_path);
            var m = (ByteMediaitem[])mFactory.GetMediaitems();

            return m;
        } // работает

        public override List<byte[]> GetMediaitemsInBytes()
        {


            var m = (ByteMediaitem[])GetMediaitems();
            return m?.Select((m, i) => m?.GetBytes())?.ToList() ?? new List<byte[]>();
        }

        public override void UploadMediaitems(params Mediaitem[] mediaitems)
        {

            RecentUploadIsSuccefull = false;
            if (mediaitems == null || _path == "")
                return;

            ToWinFolderUploader upl = new ToWinFolderUploader(_path, mediaitems);

            upl.Upload();

            RecentUploadIsSuccefull = upl.IsSuccess;

            if (RecentUploadIsSuccefull)
                uploader?.Invoke(this, mediaitems);

            
        }

        public override void DeleteMediaitems(params Mediaitem[] mediaitems)
        {
            
        }
    }

    public class OverallPhotoStorage : BaseOverallStorage 
    {
        protected Album _album;
        protected WindowsFolder _folder;
        protected string _jsonPath = "";
        GooglePhotoUserCredential _credential;
        public OverallPhotoStorage(Album album, WindowsFolder folder, string jsonPath, GooglePhotoUserCredential credential) : base(album, folder)
        {
            _album = album;
            _folder = folder;
            _album.InitEvents(UploadEventHendler, DeleteEventHendler);
            _jsonPath = jsonPath;
            _credential = credential;
        }

        protected void DeleteEventHendler(BaseStorage hendler, params Mediaitem[] mediaitems)
        {

        }
        protected void UploadEventHendler(BaseStorage hendler, params Mediaitem[] mediaitems)
        {

        }


        public void Delete(params Mediaitem[] mediaitems)
        {
            var overallsMs = mediaitems as OverallMediaitem[];
            if (overallsMs == null)
                return;

            FromJsonFileOverallMediaitemFactory fact = new FromJsonFileOverallMediaitemFactory(_jsonPath, _credential);

            var ms = (OverallMediaitem[])fact.GetMediaitems();

            List<OverallMediaitem> res = new List<OverallMediaitem>();
            for (int i = 0; i < ms.Length; i++)
            {
                var m = ms[i];
                bool isExist = false;
                for (int j = 0; j < overallsMs.Length; j++)
                {
                    var oM = overallsMs[j];
                    
                    if (m.GMediaitem.Id == oM.GMediaitem.Id)
                    {
                        isExist = true;
                        break;
                    }
                }
                if (!isExist)
                {
                    res.Add(m);
                }
            }
            ToTextFileUploader upl = new ToTextFileUploader(_jsonPath, res.ToArray());
            upl.Upload();
        }


        public void Save(params Mediaitem[] mediaitems)
        {
            var overallsMs = mediaitems as OverallMediaitem[];
            if (overallsMs == null)
                return;


            ToTextFileUploader upl = new ToTextFileUploader(_jsonPath, overallsMs);
            upl.Upload();
            

            
        }
        public override void UploadMediaitems(params Mediaitem[] mediaitems)
        {
            _album.UploadMediaitems(mediaitems);
            _folder.UploadMediaitems(mediaitems);
        }

        public override Mediaitem[] GetMediaitems()
        {
            throw new NotImplementedException();
        }

        public override string GetId()
        {
            throw new NotImplementedException();
        }

        public override List<byte[]> GetMediaitemsInBytes()
        {
            throw new NotImplementedException();
        }

        public override void DeleteMediaitems(params Mediaitem[] mediaitems)
        {
            
        }
    }

    #endregion

    #region Фабрика ответов

    public class ResponseReader
    {
        WebResponse _currentResponse;
        private Stream GetStream(HttpWebRequest request)
        {
            if (request == null)
                return null;

            Stream stream = null;
            try
            {
                _currentResponse = request.GetResponse();

                stream = _currentResponse.GetResponseStream();


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + " " + request.RequestUri.ToString());
            }

            return stream;

        }
        public byte[] GetBytes(HttpWebRequest request) 
        {
            var stream = GetStream(request);
            if (stream == null)
                return new byte[] { };

            byte[] buffer = new byte[16 * 1024];
            byte[] result;
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                result = ms.ToArray();
            }
            stream.Close();
            _currentResponse.Close();
            return result;
        }
        public string GetString(HttpWebRequest request)
        {
            var stream = GetStream(request);
            if (stream == null)
                return "";
            string res;
            using (StreamReader r = new StreamReader(stream))
            {
                res = r.ReadToEnd();
            }
            stream.Close();
            _currentResponse.Close();
            return res;
        }
        public JToken GetJson(HttpWebRequest request)
        {
            string str = GetString(request);
            if (str == "")
                return new JObject();
            JToken token = JToken.Parse(str);
            return token;
        }

    }


    #endregion

    #region
    #endregion

    public class CheckerInitializate
    {
        object[] _params;
        public CheckerInitializate(params object[] parms)
        {
            _params = parms;
        }

        public bool CheckNull()
        {
            for (int i = 0; i < _params.Length; i++)
            {
                var p = _params[i];

                if (p == null)
                {
                    return false;
                }

            }
            return true;
        }

        public bool CheckByValue(params object[] values)
        {
            if (values.Length != _params.Length || !CheckNull())
                return false;

            for (int i = 0; i < _params.Length; i++)
            {
                if (_params[i] == values[i])
                {
                    return false;
                }
            }
            return true;

        }

    }

    public class WindowsMediaitemRandomNameHendler 
    {
        public void GetName(Mediaitem m)
        {
            string rWords = "";
            for (int i = 0; i < 5; i++)
            {
                RandomNumberGenerator r = RandomNumberGenerator.Create();
                byte[] nums = new byte[4];
                r.GetBytes(nums);
                var a = (char)(Math.Abs(BitConverter.ToInt32(nums)) % 25 + 97);
                rWords += a;
            }

            var name = "";

            name += new string(m.Name.TakeWhile((c) => c != '.').ToArray());
            var ext = "";
            var l = false;
            for (int i = 0; i < m.MimeType.Length; i++)
            {
                var s = m.MimeType[i];
                if (l)
                {
                    ext += s;
                    continue;
                }

                if(s == '/')
                {
                    l = true;
                }
            }
            name += $"_{rWords}.{ext}";

            m.Name = name;
        }

    }
}