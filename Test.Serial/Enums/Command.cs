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

    //��Ϣ��
    /// <summary>
    /// ��Ϣ��
    /// ���أ������豸��
    /// �ڵ㣨�ֱ�
    /// </summary>
    public enum MetaCommand : byte
    {
        #region PC���͸����ص�������Ϣ
        RefreshNodesInfo = 0x00,            //������ǰ���أ���ʼ��ӽڵ�
        SetDateTime = 0x01,                 //���õ�ǰ�ڵ��ʱ��
        GetNodesInfo = 0x02,                //���ص�ǰ�������е���ע��ڵ���Ϣ���ڵ����ݣ��ڵ�����б����磺0000��ʾ��һ���ڵ㣩
        GetNodesState = 0x03,               //���нڵ㣬�ڵ㷵������״̬��0,1,2��0���豸�����ڣ�1���豸���ߣ�2�����гɹ���
        GetGatewayState = 0x04,             //��������ʱ����
        RemoveNodes = 0x05,                  //ɾ���ڵ�
        #endregion

        #region
        CallGatewayFromNode = 0x80,         //�ֱ�ڵ��豸���������豸
        ReportNodesBatteryLevel = 0x81,     //�������нڵ�����ٷֱȣ����ݣ�node+level(ʾ����00 00 64����ʾ0�ýڵ����100%��
        #endregion
    }
}

