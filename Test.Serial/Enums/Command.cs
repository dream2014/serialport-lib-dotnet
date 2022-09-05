/*
  This file is part of ZWaveLib (https://github.com/genielabs/zwave-lib-dotnet)

  Copyright (2012-2018) G-Labs (https://github.com/genielabs)

  Licensed under the Apache License, Version 2.0 (the "License");
  you may not use this file except in compliance with the License.
  You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

  Unless required by applicable law or agreed to in writing, software
  distributed under the License is distributed on an "AS IS" BASIS,
  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
  See the License for the specific language governing permissions and
  limitations under the License.
*/

/*
 *     Author: Generoso Martello <gene@homegenie.it>
 *     Project Homepage: https://github.com/genielabs/zwave-lib-dotnet
 */

namespace MetaLib
{

    //消息号
    /// <summary>
    /// 消息号
    /// 网关（中心设备）
    /// 节点（手表）
    /// </summary>
    public enum MetaCommand : byte
    {
        #region PC发送给网关的主动消息
        RefreshNodesInfo = 0x00,            //启动当前网关，开始添加节点
        SetDateTime = 0x01,                 //设置当前节点的时间
        GetNodesInfo = 0x02,                //返回当前网关所有的已注册节点信息，节点数据：节点序号列表（例如：0000表示第一个节点）
        GetNodesState = 0x03,               //呼叫节点，节点返回三种状态：0,1,2（0：设备不存在，1：设备离线，2：呼叫成功）
        GetGatewayState = 0x04,             //保留，暂时不用
        RemoveNodes = 0x05,                  //删除节点
        #endregion

        #region
        CallGatewayFromNode = 0x80,         //手表节点设备呼叫网关设备
        ReportNodesBatteryLevel = 0x81,     //返回所有节点电量百分比，数据：node+level(示例：00 00 64，表示0好节点电量100%）
        #endregion
    }
}

