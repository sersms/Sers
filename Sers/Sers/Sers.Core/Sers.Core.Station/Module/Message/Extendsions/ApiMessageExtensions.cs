﻿using Sers.Core.Module.Api.Message;
using Sers.Core.Module.Api;
using Sers.Core.Module.Message;
using Sers.Core.Module.Serialization;
using Sers.Core.Util.SsError;
using Sers.Core.Module.Rpc;
using Sers.Core.Util.Ioc;
using System;
using Sers.Core.Module.Api.Data;

namespace Sers.Core.Extensions
{
    public static partial class ApiMessageExtensions
    {
        #region ApiReplyMessage Init

        public static ApiMessage InitByContent(this ApiMessage data, object content)
        {
            data.value_OriData = content.SerializeToBytes().BytesToArraySegmentByte();
            return data;
        }

        public static ApiMessage InitByError(this ApiMessage data, SsError error)
        {
            return data.InitByApiReturn(error);
        }
        public static ApiMessage SetSysErrToRpcData(this ApiMessage data, SsError error)
        {
            if (null != error)
            {
                var rpcData = RpcFactory.Instance.CreateRpcContextData();
                rpcData.error_Set(error);

                data.RpcContextData_OriData_Set(rpcData);
            } 
            return data;
        }

        public static ApiMessage InitByApiReturn(this ApiMessage data, ApiReturn ret)
        {          
            data.value_OriData = ret.SerializeToBytes().BytesToArraySegmentByte();
            return data;
        }
        #endregion


        #region ApiRequestMessage
        public static ApiMessage InitAsApiRequestMessage(this ApiMessage apiRequestMessage, string route, Object arg=null)
        {
            apiRequestMessage.value_OriData = arg.SerializeToBytes().BytesToArraySegmentByte();

            var rpcData = RpcFactory.Instance.CreateRpcContextData().InitFromRpcContext();

            rpcData.route = route;
            rpcData.http_url_Set(route);

            apiRequestMessage.RpcContextData_OriData_Set(rpcData);

            return apiRequestMessage;
        }
        #endregion
    }
}
