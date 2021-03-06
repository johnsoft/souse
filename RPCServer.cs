﻿using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Text;
using System.Threading;

namespace Souse
{
    internal class RPCServer
    {
        private string bindPrefix;
        private Thread thread;
        private HttpListener listener;

        public RPCServer(string bindPrefix) {
            this.bindPrefix = bindPrefix;
        }

        public void Start()
        {
            if (thread != null)
                return;
            listener = new HttpListener();
            listener.Prefixes.Add(bindPrefix);
            thread = new Thread(ThreadEntry);
            thread.Start();
        }

        public void Stop()
        {
            if (thread == null)
                return;
            thread.Abort();
            thread = null;
            listener.Stop();
            listener = null;
        }

        private void ThreadEntry()
        {
            listener.Start();
            for (; ; )
            {
                var context = listener.GetContext();
                HandleContext(context);
            }
        }

        private void HandleContext(HttpListenerContext context)
        {
            try
            {
                var length = int.Parse(context.Request.Headers["Content-Length"]);
                var buffer = new byte[length];
                context.Request.InputStream.Read(buffer, 0, length);
                var json = Encoding.UTF8.GetString(buffer);

                var request = JObject.Parse(json);
                var method = request["method"].Value<string>();
                var param = request["params"].Value<JArray>();
                var result = HandleRequest(method, param);

                var response = new JObject();
                response["result"] = result;
                response["error"] = null;
                response["id"] = request["id"];

                Console.WriteLine(response.ToString());
                var buffer2 = Encoding.UTF8.GetBytes(response.ToString());
                context.Response.OutputStream.Write(buffer2, 0, buffer2.Length);
            }
            catch (Exception)
            {
                var buffer = Encoding.UTF8.GetBytes("ERROR");
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            }
            finally
            {
                context.Response.Close();
            }
        }

        private JToken HandleRequest(string method, JArray param) {
            if (method == "setRunning") {
                var running = param[0].Value<bool>();
                App.audioMaster.Enabled = running;
                return JValue.CreateNull();
            } else if (method == "reloadConfig") {
                App.config.Reload();
                return JValue.CreateNull();
            } else {
                throw new ArgumentException("");
            }
        }
    }
}
