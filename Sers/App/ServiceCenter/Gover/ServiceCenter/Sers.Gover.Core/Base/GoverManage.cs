﻿using Sers.Core.Extensions;
using Sers.Core.Module.Api.ApiDesc;
using Sers.Core.Module.Api.Message;
using Sers.Core.Module.Api.RouteMap;
using Sers.Core.Module.Rpc;
using Sers.Core.Module.SsApiDiscovery.SersValid;
using Sers.Core.Util.SsError;
using System;
using System.Collections.Generic;
using Sers.Core.Module.Log;
using Sers.Gover.Core.Model;
using Sers.Gover.Core;
using Sers.Core.Module.Env;
using Newtonsoft.Json;
using Sers.Core.Module.Api.Data;
using Sers.Gover.Core.Persistence;
using Sers.Gover.Core.RateLimit;

namespace Sers.ServiceCenter.ApiCenter.Gover.Core
{
    [JsonObject(MemberSerialization.OptIn)]
    public class GoverManage : IApiCenterManage
    {
        #region static       
        public static readonly GoverManage Instance = LoadFromFile();
        static GoverManage LoadFromFile()
        {
            var mng=new GoverManage();

            Persistence_ApiDesc.ApiDesc_LoadAllFromJsonFile(mng.apiStationMng);
            Persistence_Counter.LoadCounterFromJsonFile(mng.apiStationMng);

            return mng;
        }
        public static void SaveToFile()
        {
            Persistence_Counter.SaveCounterToJsonFile(Instance.apiStationMng); 
        }
        #endregion


        /// <summary>
        /// BeforeCallApi(IRpcContextData rpcData, ApiMessage requestMessage)
        /// </summary>
        [JsonIgnore]
        public Action<IRpcContextData, ApiMessage> BeforeCallApi { get; set; }




        public GoverManage()
        {
            apiLoadBalancingMng = new ApiLoadBalancingMng(this);

            serviceStationMng = new ServiceStationMng();
            serviceStationMng.Init(this);

            apiStationMng = new ApiStationMng();
            apiStationMng.Init(this);
        }

        

        internal readonly ApiLoadBalancingMng apiLoadBalancingMng;

        [JsonProperty]
        internal ApiStationMng apiStationMng { get; private set; }

        [JsonIgnore]
        internal ServiceStationMng serviceStationMng { get; private set; }

        [JsonIgnore]
        public RateLimitMng rateLimitMng { get; private set; } = new RateLimitMng();

        public void PublishUsageInfo(EnvUsageInfo item)
        {
            serviceStationMng.PublishUsageInfo(item);
        }




        #region ServiceStation

        public List<SsApiDesc> ApiDesc_GetActive()
        {
            return apiLoadBalancingMng.GetAllApiDesc();
        
        }

        public List<SsApiDesc> ApiDesc_GetAll()
        {
            return apiStationMng.ApiDesc_GetAll();
        }





        #endregion






        #region CallApi

        public List<ArraySegment<byte>> CallApi(IRpcContextData rpcData, ApiMessage requestMessage)
        {
            try
            {
                #region (x.1)route 判空               
                if (string.IsNullOrWhiteSpace(rpcData.route))
                {
                    //返回api 不存在
                    return new ApiMessage().InitByError(SsError.Err_ApiNotExists).Package();
                }
                #endregion


                #region (x.2) 服务限流 BeforeLoadBalancing               
                var error = rateLimitMng.BeforeLoadBalancing(rpcData, requestMessage);
                if (null != error)
                {
                    return new ApiMessage().InitByError(error).Package();
                }
                #endregion

                #region (x.3) 负载均衡，获取对应服务端                
                var apiNode = apiLoadBalancingMng.GetCurApiNodeByLoadBalancing(rpcData.route, out var routeType);

                if (null == apiNode)
                {
                    //返回api 不存在
                    return new ApiMessage().InitByError(SsError.Err_ApiNotExists).Package();
                }
                #endregion

                #region (x.4) 服务限流 BeforeCallRemoteApi
                error = rateLimitMng.BeforeCallRemoteApi(rpcData, requestMessage, apiNode);
                if (null != error)
                {
                    return new ApiMessage().InitByError(error).Package();
                }
                #endregion

                #region (x.5) BeforeCallApi
                try
                {
                    BeforeCallApi?.Invoke(rpcData, requestMessage);
                }
                catch (Exception ex)
                {
                    Logger.log.Error(ex);
                }
                #endregion

                #region (x.6) 权限校验 SsValid
                // 权限校验不通过，调用次数也计数
                // TODO:应当有其他计数
                if (!SersValidMng.Valid(rpcData.oriJson, apiNode.apiDesc.rpcValidations, out var validError))
                {
                    return new ApiMessage().InitByError(validError).Package();
                }
                #endregion


                #region (x.7) RpcContextData 修正
                //(x.x.1) 修正route
                // 调用服务端 泛接口（如： "/station1/fold2/*"）时,route应修正为"/station1/fold2/*"，而不是原始 地址（如： "/station1/fold2/index.html"）
                if (routeType == ERouteType.genericRoute)
                {
                    rpcData.route = apiNode.apiDesc.route;
                    requestMessage.rpcContextData_OriData = ArraySegmentByteExtensions.Null;
                }


                //(x.2) 修正 requestMessage
                if (requestMessage.rpcContextData_OriData.Count <= 0) {
                    requestMessage.RpcContextData_OriData_Set(rpcData);
                }
                #endregion


                #region (x.8) 服务调用 
                return apiNode.CallApi(rpcData, requestMessage);
                #endregion
            }
            catch (Exception ex)
            {
                Logger.log.Error(ex);

                ApiSysError.LogSysError(rpcData, requestMessage, ex.SsError_Get());

                var err = SsError.Err_SysErr;
                return new ApiMessage().InitByError(err).SetSysErrToRpcData(err).Package();
            }
            finally
            {
                //服务限流 OnFinally
                rateLimitMng.OnFinally(rpcData, requestMessage);
            }

        }
        #endregion


        #region ServiceStation

        public List<ServiceStationData> ServiceStation_GetAll()
        {
            return serviceStationMng.ServiceStation_GetAll();
        }


        public void ServiceStation_Regist(ServiceStation serviceStation)
        {
            Logger.Info("[ApiCenter]注册站点Api,mqConnGuid:" + serviceStation.mqConnGuid);
            serviceStationMng.ServiceStation_Add(serviceStation);
        }

        public void ServiceStation_Remove(string connGuid)
        {
            Logger.Info("[ApiCenter]移除ServiceStation,connGuid:" + connGuid);
            serviceStationMng.ServiceStation_Remove(connGuid);
        }


        public bool ServiceStation_Pause(string connGuid)
        {
            Logger.Info("[ApiCenter]暂停ServiceStation,connGuid:" + connGuid);
            return serviceStationMng.ServiceStation_Pause(connGuid);
        }

        public bool ServiceStation_Start(string connGuid)
        {
            Logger.Info("[ApiCenter]启用ServiceStation,connGuid:" + connGuid);
            return serviceStationMng.ServiceStation_Start(connGuid);
        }
        #endregion



        #region ApiStation

        public List<ApiStationData> ApiStation_GetAll()
        {
            return apiStationMng.ApiStation_GetAll();
        }

        public bool ApiStation_Pause(string stationName)
        {
            Logger.Info("[ApiCenter]暂停ApiStation,stationName:" + stationName);
            return apiStationMng.ApiStation_Pause(stationName);
        }

        public bool ApiStation_Start(string stationName)
        {
            Logger.Info("[ApiCenter]启用ApiStation,stationName:" + stationName);
            return apiStationMng.ApiStation_Start(stationName);
        }
        #endregion


        

    }
}
