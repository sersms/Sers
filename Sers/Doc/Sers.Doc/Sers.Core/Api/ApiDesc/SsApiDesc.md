﻿# SsApiDesc


```javascript
//Demo:

{  
   "catagory":"ApiStation1/ss/ccc"
   ,"name":"获取用户信息"
   ,"description":"用户必须登录"

   //路由
   ,"route":"/ApiStation1/path1/path2/api1.html"




   //请求参数类型   SsModel类型
   ,"argType":  { SsModel }

   //返回数据类型   SsModel类型
   ,"returnType":  { SsModel }


   ,"rpcValidations":[
		//SsLimit format:  {"path":"path in RpcContext","ssError":{ssError} , "ssValid": {SsValid} }

		{  "path":"callerSource","ssError":{} , "ssValid": {"type":"Equal","value":"ServiceStation"}   }


		/*
		{  "path":"callerSource","ssError":{} , "ssValid": {"type":"Equal","value":"ServiceStation"}   }

		{"path":"user.userType", "ssValid": [
			{"type":"Equal","value":"Logined", "errorMessage": "必须为登陆用户"}
		] } ,

		{"path":"http.method", "ssValid": [
			{"type":"Equal","value":"POST", "errorMessage": "接口只接受Post请求"}
		] }  */
   ]


   //api调用限制
   ,"limit":[
		//SsLimit format:  {"path":"path in Arg","ssError":{ssError} , "ssValid": {SsValid} }

		
   ]

   //调用api时构建RpcContext 的配置参数
   ,"rpcContextBuildConfig":  { 
	
   
   }

}
```
 