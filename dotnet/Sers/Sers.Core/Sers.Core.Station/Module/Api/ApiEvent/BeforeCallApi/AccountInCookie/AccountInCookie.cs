﻿using Newtonsoft.Json.Linq;
using Sers.Core.Extensions;
using Sers.Core.Module.Api.Message;
using Sers.Core.Module.Log;
using Sers.Core.Module.Rpc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sers.Core.Station.Module.Api.ApiEvent.BeforeCallApi.AccountInCookie
{
    /// <summary>
    /// 在调用接口前，会获取 rpcData.http.headers.Cookie(格式为 "user=xxx;c=7")中的user，在账号列表中比对userToken，回写 CallerSource(rpcData.caller.source) 和 userInfo(rpcData.user.userInfo)
    /// </summary>
    public class AccountInCookie : IBeforeCallApi
    {
        class Account
        {
            public string userToken;
            public string CallerSource;
            public JObject userInfo;
        }

        SortedDictionary<string, Account> userMap = new SortedDictionary<string, Account>();


        /// <summary>
        ///  Sers.AccountInCookie.account:            [ {"userToken":"admin_123","CallerSource":"Internal","userInfo":{}  }      ]  
        /// </summary>
        /// <param name="config"></param>
        public void Init(JObject config)
        {
            userMap.Clear();

            var acounts = config["account"].Deserialize<Account[]>();

            if (acounts == null || acounts.Length == 0) return;

            #region 构建 userMap
            foreach (var account in acounts)
            {
                try
                {
                    if (!string.IsNullOrEmpty(account.userToken))
                        userMap[account.userToken] = account;
                }
                catch (Exception)
                {
                }
            }
            #endregion

        }

        /// <summary>
        /// 转换Cookie为对应的用户
        /// </summary>
        /// <param name="rpcData"></param>
        /// <param name="requestMessage"></param>
        public void BeforeCallApi(IRpcContextData rpcData, ApiMessage requestMessage)
        {
            try
            {
                #region (x.x.x.1)获取cookie 中的用户令牌
                var cookie = rpcData.http_header_Get("Cookie");
                if (string.IsNullOrEmpty(cookie)) return;

                string authUserFromCookie = null;
                {
                    // "a=b;c=7"
                    string str = cookie;
                    char entrySeparator = ';';
                    char kvSeparator = '=';
                    Dictionary<string, string> dictionary;
                    {
                        //dictionary = str.Split(new string[] { entrySeparator }, StringSplitOptions.RemoveEmptyEntries)
                        //    .GroupBy(x => x.Split(new string[] { kvSeparator }, StringSplitOptions.None)[0], x => x.Split(new string[] { kvSeparator }, StringSplitOptions.None)[1])
                        //    .ToDictionary(x => x.Key, x => x.First());
                        dictionary = str.Split(new[] { entrySeparator }, StringSplitOptions.RemoveEmptyEntries)
                           //.GroupBy(x => x.Split(new string[] { kvSeparator }, StringSplitOptions.None)[0], x => x.Split(new string[] { kvSeparator }, StringSplitOptions.None)[1])
                           .Select(x => x.Split(new[] { kvSeparator })).ToDictionary(kv => kv[0]?.Trim(), kv => kv[1]?.Trim());
                    } 

                    authUserFromCookie = dictionary.TryGetValue("user", out var v) ? v : null;
                }
                #endregion

                #region (x.x.x.2) 转换用户身份并写入 rpcData
                if (userMap.TryGetValue(authUserFromCookie, out var account))
                {
                    if (account.CallerSource != null)
                    {
                        rpcData.caller_source_Set(account.CallerSource);
                    }
                    if (account.userInfo != null)
                    {
                        rpcData.user_userInfo_Set(account.userInfo);
                    }
                    requestMessage.rpcContextData_OriData = ArraySegmentByteExtensions.Null;
                }
                #endregion
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
    }
}