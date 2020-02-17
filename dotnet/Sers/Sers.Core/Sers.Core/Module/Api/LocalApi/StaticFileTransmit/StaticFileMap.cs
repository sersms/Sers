﻿using Newtonsoft.Json.Linq;
using Vit.Extensions;
using Sers.Core.Module.Rpc;
using System;
using System.IO;
using System.Web;
using Vit.Core.Util.ComponentModel.SsError;
using Vit.Core.Util.ConfigurationManager;

namespace Sers.Core.Module.Api.LocalApi.StaticFileTransmit
{
    public class StaticFileMap
    { 

        public readonly FileExtensionContentTypeProvider contentTypeProvider = new FileExtensionContentTypeProvider();



        #region LoadContentTypeFromFile
        public bool LoadContentTypeFromFile(string filePath)
        {
            var jsonFile = new JsonFile(filePath);
            if (File.Exists(jsonFile.configPath))
            {
                 
                var map = contentTypeProvider.Mappings;
                foreach (var item in (jsonFile.root as JObject))
                {
                    map.Remove(item.Key);
                    map[item.Key] = item.Value.Value<string>();
                }
                return true;
            }
            return false;
        }
        #endregion


        #region fileBasePath
        private string _fileBasePath;
        /// <summary>
        /// D://fold1/wwwroot
        /// 静态文件路径。可为相对路径或绝对路径。若未指定存在的文件夹则默认为当前目录下的wwwroot文件夹。
        /// </summary>
        public string fileBasePath
        {
            get => _fileBasePath;
            set
            {
                #region (x.1) get fullPath
                string fullPath = value;
                if ("" == fullPath)
                {
                    fullPath = Path.Combine(AppContext.BaseDirectory, "wwwroot");
                }
                else if (fullPath != null)
                {
                    if (!Directory.Exists(fullPath))
                    {
                        fullPath = Path.Combine(AppContext.BaseDirectory, fullPath);
                    }
                }

                if (string.IsNullOrEmpty(fullPath))
                {
                    fullPath = null;
                }
                else
                {
                    var dir = new DirectoryInfo(fullPath);
                    if (dir.Exists)
                    {
                        fullPath = dir.FullName;
                    }
                    else
                    {
                        fullPath = null;
                    }
                }
                #endregion

                _fileBasePath = fullPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot");
            }
        }
        #endregion



        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileBasePath">静态文件路径。可为相对路径或绝对路径。若未指定存在的文件夹则默认为当前目录下的wwwroot文件夹。demo  D://fold1/wwwroot </param>
        public StaticFileMap(string fileBasePath = null)
        {
            this.fileBasePath = fileBasePath;
        }


        /// <summary>
        /// 获取当前url对应的相对文件路径(若不合法，则返回null)。demo:"rpc/2.html"
        /// <para>（若 route为"/Station1/fold1/a/*"，url为"http://127.0.0.1/Station1/fold1/a/1/2.html?c=9",则 relativePath为"1/2.html"）</para>
        /// </summary>
        /// <returns></returns>
        public string GetRelativePath()
        {
            return RpcContext.RpcData.http_url_RelativePath_Get();
        }

        /// <summary>
        /// 获取当前url映射到文件系统中的绝对路径。demo:"/root/netapp/FileStorage/wwwroot/fold2/a.html"
        /// </summary>
        /// <returns></returns>
        public string GetAbsFilePath()
        {
            var relativePath = GetRelativePath();

            if (string.IsNullOrWhiteSpace(relativePath)) return null;

            string absFilePath = Path.Combine(fileBasePath, relativePath);
            return absFilePath;
        }


        public byte[] TransmitFile()
        {
            return TransmitFile(GetAbsFilePath());
        }


        public byte[] TransmitFile(string absFilePath)
        {
            if (!File.Exists(absFilePath))
            {
                return SsError.Err_404.SerializeToBytes();
            }

            #region reply header
            var replyRpcData = RpcFactory.Instance.CreateRpcContextData();      

            if (contentTypeProvider.TryGetContentType(absFilePath, out var contentType))
            {
                replyRpcData.http_header_Set("Content-Type", contentType);             
            }
            replyRpcData.http_header_Set("Cache-Control", "public,max-age=6000");        
 
            RpcContext.Current.apiReplyMessage.rpcContextData_OriData= replyRpcData.PackageOriData();
            #endregion

          
            return File.ReadAllBytes(absFilePath);

            //Response.AppendHeader("Content-Disposition", "attachment;filename=" + HttpUtility.UrlEncode(responseFileName, Encoding.UTF8));
            //Response.AddHeader("Content-Length", new FileInfo(filePath).Length.ToString());
            //Response.ContentType = (ContentType ?? MimeMapping(Path.GetExtension(responseFileName)));
            //Response.ContentEncoding = Encoding.UTF8;
            //Response.Charset = "utf-8";

            //ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=600");
            //(小知识: max - age：表示当访问此网页后的max - age秒内再次访问不会去服务器请求，其功能与Expires类似，只是Expires是根据某个特定日期值做比较。一但缓存者自身的时间不准确.则结果可能就是错误的，而max - age,显然无此问题.。Max - age的优先级也是高于Expires的。)
        }


 

        public byte[] DownloadFile(string absFilePath, string fileName=null)
        {
            if (string.IsNullOrEmpty(absFilePath))
            {
                absFilePath = GetAbsFilePath();
            }

            contentTypeProvider.TryGetContentType(absFilePath, out var contentType);

            return DownloadFile(absFilePath, contentType, fileName);
        }


        #region static DownloadFile


        public static byte[] DownloadFile(string absFilePath, string contentType, string fileName = null)
        {
            var info = new FileInfo(absFilePath);
            if (!info.Exists)
            {
                return SsError.Err_404.SerializeToBytes();
            }

            #region reply header
            var replyRpcData = RpcFactory.Instance.CreateRpcContextData();

            var header = new JObject();

            if (!string.IsNullOrEmpty(contentType))
            {
                header["Content-Type"] = contentType;
            }

            if (string.IsNullOrEmpty(fileName))
            {
                fileName = Path.GetFileName(absFilePath);
            }

            #region 填充文件头
            header["Content-Disposition"] = "attachment;filename=" + HttpUtility.UrlEncode(fileName, Vit.Core.Module.Serialization.Serialization.Instance.encoding);
            header["Content-Length"] = info.Length.ToString();
            #endregion

            replyRpcData.http_headers_Set(header);
            RpcContext.Current.apiReplyMessage.rpcContextData_OriData = replyRpcData.PackageOriData();
            #endregion


            return File.ReadAllBytes(absFilePath);

            //Response.AppendHeader("Content-Disposition", "attachment;filename=" + HttpUtility.UrlEncode(responseFileName, Encoding.UTF8));
            //Response.AddHeader("Content-Length", new FileInfo(filePath).Length.ToString());
            //Response.ContentType = (ContentType ?? MimeMapping(Path.GetExtension(responseFileName)));
            //Response.ContentEncoding = Encoding.UTF8;
            //Response.Charset = "utf-8";
        }
        #endregion


    }
}