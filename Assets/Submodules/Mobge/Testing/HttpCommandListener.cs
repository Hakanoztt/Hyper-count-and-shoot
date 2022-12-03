#if TEST_MODE
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Mobge.Threading;
using UnityEngine;

namespace Mobge.Test {
    public class HttpCommandListener {
        private static char[] s_escapeChars = new char[] { '\\', '/' };
        private bool _listening;
        private HttpListener _listener;
        private Dictionary<string, Action<HttpListenerRequest, HttpListenerResponse>> _handlers = new Dictionary<string, Action<HttpListenerRequest, HttpListenerResponse>>();

        public HttpCommandListener() {
            _shared = this;
            ThreadSystem.InitializeMainThreadHandler();
            //initTypes();
        }

        private static HttpCommandListener _shared;
        public static HttpCommandListener Shared {
            get {
                if (_shared == null) {
                    _shared = new HttpCommandListener();
                }
                return _shared;
            }
        }
        public bool Start(int port) {
            if (!_listening) {
                _listening = true;
                _listener = new HttpListener();
                _listener.Prefixes.Add("http://*:" + port + "/");
                _listener.Start();
                Thread t = new Thread(Run);
                t.Start();
                Debug.Log(nameof(HttpCommandListener) + " is started on port: " + port);
                return true;
            }
            return false;
        }
        public void Stop() {
            if (_listening) {
                _listening = false;
                try {
                    _listener.Abort();
                }
                catch {

                }
                _listener = null;
            }
        }
        private string GetMethod(string rawUrl) {
            int index = rawUrl.IndexOf('?');
            if (index > 0) {
                return rawUrl.Substring(0, index).Trim(s_escapeChars);
            }
            return rawUrl.Trim(s_escapeChars);

        }
        private void RunOnMainThread(Action a, HttpListenerResponse response) {
            Threading.ThreadSystem.DoOnMainThread(() => {
                try {
                    a();
                }
                catch (Exception e) {
                    UnityEngine.Debug.Log(e);
                }
                finally {
                    response.Close();
                }
                return true;
            });
        }
        /// <summary>
        /// Registers the specified action as a web method. Specified handler is called from unity thread so it is safe to do unity thread spesific actions in that callback.
        /// </summary>
        /// <param name="method"></param>
        /// <param name="handler"></param>
        public void RegisterMethod(string method, Action<HttpListenerRequest, HttpListenerResponse> handler) {
            _handlers[method] = handler;
        }
        public void SendResponse(string o, HttpListenerResponse response) {

            var boutput = Encoding.UTF8.GetBytes(o);
            response.OutputStream.Write(boutput, 0, boutput.Length);
        }
        private string BasicResponse(string response, bool useAsValue = false, string title = "response") {
            if (useAsValue) {
                return "{\"" + title + "\":" + response + "}";
            }
            return "{\"" + title + "\":\"" + response + "\"}";
        }
        private void Run() {
            while (_listening) {
                MemoryStream sb = new MemoryStream();
                byte[] buffer = new byte[256];
                try {
                    var c = _listener.GetContext();
                    if (c == null) {
                        continue;
                    }
                    var s = c.Request.InputStream;
                    string method = GetMethod(c.Request.RawUrl);

                    switch (method) {
                        default:
                            Action<HttpListenerRequest, HttpListenerResponse> handler;
                            if (_handlers.TryGetValue(method, out handler)) {
                                RunOnMainThread(() => {
                                    handler(c.Request, c.Response);
                                }, c.Response);
                            }
                            else {
                                SendResponse(BasicResponse("no such method: " + method, false, "error"), c.Response);
                            }
                            break;
                    }


                }
                catch (Exception e) {
                    _listening = false;
                    UnityEngine.Debug.Log(e);
                    try {

                        _listener.Close();
                    }
                    catch {

                    }
                }
            }
        }
        public class Jsonable {
            SimpleJSON.JSONNode _source;
            public Jsonable(bool asArray = false) {
                if (asArray) {
                    _source = new SimpleJSON.JSONArray();
                }
                else {
                    _source = new SimpleJSON.JSONClass();
                }
            }
            public static Jsonable FromDictionary<T>(Dictionary<string, T> source, Func<T, SimpleJSON.JSONNode> converter) {
                var j = new Jsonable();
                foreach (var item in source) {
                    j._source.Add(item.Key, converter(item.Value));
                }
                return j;
            }

            public SimpleJSON.JSONNode this[string index] {
                get { return _source[index]; }
                set { _source[index] = value; }
            }
            public SimpleJSON.JSONNode this[int index] {
                get { return _source[index]; }
                set { _source[index] = value; }
            }
            public void Add(string key, SimpleJSON.JSONNode node) {
                _source.Add(key, node);
            }
            public void Add(SimpleJSON.JSONNode node) {
                _source.Add(node);
            }
            public void ToJson(StringBuilder sb) {

                _source.ToJSON(sb);
            }

        }
    }
}
#endif