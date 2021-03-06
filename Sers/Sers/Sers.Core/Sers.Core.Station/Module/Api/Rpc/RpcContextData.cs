﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sers.Core.Extensions;
using Sers.Core.Module.Rpc;
using System;


namespace Sers.Core.Module.Api.Rpc
{
    /// <summary>
    ///
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class RpcContextData: IRpcContextData
    {

        #region static


        public static RpcContextData Unpack(ArraySegment<byte> oriData)
        {
            var rpcData = new RpcContextData();
            rpcData.UnpackOriData(oriData);
            return rpcData;
        }
         
        #endregion


         


        public RpcContextData() { }

      


        /// <summary>
        /// oriData为ArraySegment<byte>类型
        /// </summary>
        /// <param name="oriData"></param>
        public IRpcContextData UnpackOriData(ArraySegment<byte> oriData)
        {
            Clear(oriData);
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ArraySegment<byte> PackageOriData()
        {
            string strOriData = (_oriJson ?? new JObject()).ToString();
            return strOriData.StringToArraySegmentByte(); 
        }


        ArraySegment<byte> oriData;
 

        private JObject GetJsonFromOriData()
        {
            if (null == oriData) return new JObject();

            try
            {           
                string oriString = oriData.ArraySegmentByteToString();
                return JObject.Parse(oriString);                
            }
            catch (Exception)
            {
            }
            return new JObject();

        }


        #region oriJson        

        JObject _oriJson;
        public JObject oriJson=>(_oriJson??(_oriJson = GetJsonFromOriData()));
        #endregion

        void Clear(ArraySegment<byte> oriData)
        {
            this.oriData = oriData;
     
            _oriJson = null;
      
        }


        #region route
        [JsonProperty]
        public string route
        {
            get
            {
                return oriJson["route"].ConvertToString();
            }
            set
            {
                oriJson["route"] = value;
            }
        }
        #endregion       

 








        #region caller

        [JsonProperty]
        public JObject caller
        {
            get => this.caller_Get();
            set => this.caller_Set(value);
        }
        #endregion

        #region caller_source
        /// <summary>
        /// 调用来源
        /// </summary>    
        ECallerSource? caller_source
        {
            get
            {                
                return this.caller_source_GetEnum();
            }
            set
            {
                this.caller_source_Set(value);
            }
        }
        #endregion


        #region http

        [JsonProperty]
        public JObject http
        {
            get => this.http_Get();
            set=> this.http_Set(value);
        }
        #endregion


        #region user

        [JsonProperty]
        public JObject user
        {
            get => this.user_Get();
            set => this.user_Set(value);
        }
        #endregion


    }
}
