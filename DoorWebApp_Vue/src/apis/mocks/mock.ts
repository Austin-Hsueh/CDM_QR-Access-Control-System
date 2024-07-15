import IReqManufMethodDTO from '@/models/dto/IReqManufMethodDTO';
import IResUserAuthInfoDTO from '@/models/dto/IResUserAuthInfoDTO';
import IAPIResponse from '@/models/IAPIResponse';
import MockAdapter from 'axios-mock-adapter';
import API from '@/apis/TPSAPI';
import IReqUserPermissionDTO from '@/models/dto/IReqUserPermissionDTO';
import { IBaseAPIResponse } from '@/models/IBaseAPIResponse';
import IResManufMethodDTO from '@/models/dto/IResManufMethodDTO';
import ManufMethodData from '@/assets/mockData/ManufMethods.json';
import IResFactoryAreaDTO from '../../models/dto/IResFactoryAreaDTO';
import IResKaizenDTO from '@/models/dto/IResKaizenItemDTO';
import IAPIServerSideResponse from '@/models/IAPIServerSideResponse';
import KaizenListData from '@/assets/mockData/KaizenList.json';
import { xor } from 'lodash';

const manufMethodData = ManufMethodData as IResManufMethodDTO[];

const mock = new MockAdapter(API.axiosInstance);

//#region UserController : 使用者相關
mock.onPost("v1/User/login").reply((ctx) => {
  console.log(`[Axios Mock] Call API : ${ctx.url}`);
  const loginInfo = JSON.parse(ctx.data);
  const username = loginInfo.username;
  const password = loginInfo.password;
  console.log('account : ' + username + '/' + password);

  const res: IAPIResponse<IResUserAuthInfoDTO> = {
    result: 0,
    msg: 'login fail',
    content: {
      token: '',
      userId: -1,
      username: '',
      displayName: '',
      locale: '',
      permissionIds: [],
      qrcode: ''
    }
  };

  if (username === 'admin' && password === 'admin') {
    res.result = 1;
    res.msg = 'success';
    res.content.token = 'hello world';
    res.content.username = 'admin@mwtest.com';
    res.content.locale = '';
  }
  return [200, res]
});

mock.onGet("v1/User/RefreshToken").reply((ctx) => {
  console.log(`[Axios Mock] Call API : ${ctx.url}`);

  const res: IAPIResponse<IResUserAuthInfoDTO> = {
    result: 1,
    msg: 'success',
    content: {
      token: 'hello world',
      userId: 1,
      username: 'admin',
      displayName: 'administartor',
      locale: '',
      permissionIds: [],
      qrcode: ''
    }
  };

  return [200, res]
});

mock.onGet("v1/User/Permission").reply((ctx) => {
  console.log(`[Axios Mock] Call API : ${ctx.url}`);

  const premission: IReqUserPermissionDTO = {
    grantId: [1, 3, 5, 7, 9]
  }
  const res: IAPIResponse<IReqUserPermissionDTO> = {
    result: 1,
    msg: 'success',
    content: premission
  }

  return [200, res];
})
//#endregion

//#region DDLController : 下拉式選單相關
/** 取得所有工藝類別 */
mock.onGet("v1/ManufMethods").reply((ctx) => {
  console.log(`[Axios Mock] Call API : [Get] ${ctx.url}`);

  const res: IAPIResponse<IResManufMethodDTO[]> = {
    result: 1,
    msg: 'success',
    content: manufMethodData
  };

  return [200, res];
});

/** 取得單一工藝類別(by Id) */
mock.onGet(/v1\/ManufMethod\/?.*/).reply((ctx) => {
  console.log(`[Axios Mock] Call API : [Get] ${ctx.url}`);
  const urlId = ctx.url?.split("v1/ManufMethod/")[1] as string;
  const idx = manufMethodData.findIndex((x) => x.id == parseInt(urlId)) as number;

  const res: IAPIResponse<IResManufMethodDTO> = {
    result: 1,
    msg: 'success',
    content: manufMethodData[idx]
  };

  return [200, res];
});

/** 新增一個工藝類別 */
mock.onPost("v1/ManufMethod").reply((ctx) => {
  console.log(`[Axios Mock] Call API : [Post] ${ctx.url}`);
  const dataFromPost = JSON.parse(ctx.data) as IReqManufMethodDTO;
  const maxId = Math.max(...manufMethodData.map(x => x.id)) as number;

  manufMethodData.push({
    id: maxId + 1,
    methodName: dataFromPost.methodName,
    creator: 'admin',
    createDate: new Date().toDateString(),
    remark: dataFromPost.remark,
  });

  const res: IBaseAPIResponse = {
    result: 1,
    msg: 'success'
  };
  return [200, res];
});

mock.onPut(/v1\/ManufMethod\/?.*/).reply((ctx) => {
  console.log(`[Axios Mock] Call API : [Put] ${ctx.url}`);
  const urlId = ctx.url?.split("v1/ManufMethod/")[1] as string;
  const idx = manufMethodData.findIndex((x) => x.id == parseInt(urlId)) as number;
  const dataFromPut = JSON.parse(ctx.data) as IReqManufMethodDTO;

  manufMethodData[idx].methodName = dataFromPut.methodName;
  manufMethodData[idx].remark = dataFromPut.remark;

  const res: IBaseAPIResponse = {
    result: 1,
    msg: 'success'
  };
  return [200, res];
});

mock.onDelete(/v1\/ManufMethod\/?.*/).reply((ctx) => {
  console.log(`[Axios Mock] Call API : [Delete] ${ctx.url}`);
  const urlId = ctx.url?.split("v1/ManufMethod/")[1] as string;
  const idx = manufMethodData.findIndex((x) => x.id == parseInt(urlId)) as number;

  manufMethodData.splice(idx, 1);

  const res: IBaseAPIResponse = {
    result: 1,
    msg: 'success'
  };
  return [200, res];
});
//#endregion

//#region FactoryAreaController : 廠區相關
import FactoryAreaData from '@/assets/mockData/FactoryAreas.json';
import IReqFactoryAreaListDTO from '@/models/dto/IReqFactoryAreaListDTO';
import IReqFactoryAreaDTO from '@/models/dto/IReqFactoryAreaDTO';
import IResFactoryAreaListDTO from '../../models/dto/IResFactoryAreaListDTO';
import IReqManufMethodProjectListDTO from '../../models/dto/IReqReportManufKaizenListSearchCriteriaDTO';

const factoryAreaData = FactoryAreaData as IResFactoryAreaDTO[];

/** 取得 廠區 列表 */
mock.onGet("v1/FactoryAreas").reply((ctx) => {
  console.log(`[Axios Mock] Call API : [Get] ${ctx.url}`);
  /* 取 資料 從 Query */
  const dataFromQuery = JSON.parse(ctx.data) as IReqFactoryAreaListDTO;

  /* 過濾 */
  const factoryAreas = factoryAreaData.filter((x) =>
    dataFromQuery.searchText == "" ? true : x.factoryAreaName.includes(dataFromQuery.searchText));

  /* 分頁 */
  const slicePageFactoryAreas = pagination(dataFromQuery.pageSize, dataFromQuery.page, factoryAreas);

  /* 回傳 */
  const res: IAPIResponse<IResFactoryAreaListDTO> = {
    result: 1,
    msg: 'success',
    content: {
      totalAmount: factoryAreas.length,
      totalPage: 0,
      factoryAreaList: slicePageFactoryAreas as IResFactoryAreaDTO[]
    }
  };
  return [200, res];
});

/** 分頁 function */
function pagination(pageNo: number, pageSize: number, array: any) {
  const offset = (pageNo - 1) * pageSize;
  return (offset + pageSize >= array.length) ? array.slice(offset, array.length) : array.slice(offset, offset + pageSize);
}

/** 新增 廠區 */
mock.onPost("v1/FactoryArea").reply((ctx) => {
  console.log(`[Axios Mock] Call API : [Post] ${ctx.url}`);
  /* 取 資料 從 Body */
  const dataFromBody = JSON.parse(ctx.data) as IReqFactoryAreaDTO;
  /* 用 max 取得 最大Id */
  const maxId = Math.max(...factoryAreaData.map(x => x.id)) as number;

  /* 檢查 */
  const factoryArea = factoryAreaData.filter((x) => x.factoryAreaName == dataFromBody.factoryAreaName)[0];
  if (factoryArea != null) {
    const res: IBaseAPIResponse = {
      result: 0,
      msg: '重複廠區'
    };
    return [200, res];
  }

  factoryAreaData.push({
    /* Id + 1 */
    id: maxId + 1,
    factoryAreaName: dataFromBody.factoryAreaName,
    factorys: dataFromBody.factorys,
    remark: dataFromBody.remark,
  });

  const res: IBaseAPIResponse = {
    result: 1,
    msg: 'success'
  };
  return [200, res];
});


/** 取得 一個 廠區(by Id) */
mock.onGet(/v1\/FactoryArea\/?.*/).reply((ctx) => {
  console.log(`[Axios Mock] Call API : [Get] ${ctx.url}`);
  /* 取 Id 從 Url */
  const IdFromUrl = ctx.url?.split("v1/FactoryArea/")[1] as string;
  /* find factoryAreaData Seq */
  const Seq = factoryAreaData.findIndex((x) => x.id == parseInt(IdFromUrl)) as number;

  /* 依 序列 找到 廠區 */
  const factoryArea = factoryAreaData[Seq];

  const res: IAPIResponse<IResFactoryAreaDTO> = {
    result: 1,
    msg: 'success',
    content: factoryArea
  };

  return [200, res];
});

/** 修改 廠區 */
mock.onPut(/v1\/FactoryArea\/?.*/).reply((ctx) => {
  console.log(`[Axios Mock] Call API : [Put] ${ctx.url}`);
  /* 取 Id 從 Url */
  const IdFromUrl = ctx.url?.split("v1/FactoryArea/")[1] as string;
  /* find factoryAreaData Seq */
  const Seq = factoryAreaData.findIndex((x) => x.id == parseInt(IdFromUrl)) as number;
  /* 取 資料 從 Body */
  const dataFromBody = JSON.parse(ctx.data) as IReqFactoryAreaDTO;

  /* 檢查 */
  const factoryArea = factoryAreaData.filter((x) => x.factoryAreaName == dataFromBody.factoryAreaName && x.id != parseInt(IdFromUrl))[0];
  if (factoryArea != null) {
    const res: IBaseAPIResponse = {
      result: 0,
      msg: '重複廠區'
    };
    return [200, res];
  }

  /* 依 序列 找到 廠區 並 修改 */
  factoryAreaData[Seq] = {
    id: parseInt(IdFromUrl),
    factoryAreaName: dataFromBody.factoryAreaName,
    factorys: dataFromBody.factorys,
    remark: dataFromBody.remark
  } as IResFactoryAreaDTO;

  const res: IBaseAPIResponse = {
    result: 1,
    msg: 'success',
  };

  return [200, res];
});

/** 刪除 廠區 */
mock.onDelete(/v1\/FactoryArea\/?.*/).reply((ctx) => {
  console.log(`[Axios Mock] Call API : [Delete] ${ctx.url}`);
  /* 取 Id 從 Url */
  const IdFromUrl = ctx.url?.split("v1/FactoryArea/")[1] as string;
  /* find factoryAreaData Seq */
  const Seq = factoryAreaData.findIndex((x) => x.id == parseInt(IdFromUrl)) as number;

  /* splice 去除掉 */
  factoryAreaData.splice(Seq, 1);

  const res: IBaseAPIResponse = {
    result: 1,
    msg: 'success'
  };
  return [200, res];
});
//#endregion

//#region DDLController : 改善事項相關
/** 取得所有改善事項 */
mock.onPost("v1/KaizenList").reply((ctx) => {
  console.log(`[Axios Mock] Call API : [Post] ${ctx.url}`);
  const dataFromPost = JSON.parse(ctx.data) as any;
  let content = KaizenListData;

  console.log('dataFromPost', dataFromPost);
  if (dataFromPost.codePJ != '') {
    content = KaizenListData.filter((x) => x.codePJ.includes(dataFromPost.codePJ));
  }

  if (dataFromPost.team != '') {
    content = KaizenListData.filter((x) => x.team.includes(dataFromPost.team));
  }

  if (dataFromPost.kaizenPN != '') {
    content = KaizenListData.filter((x) => x.kaizenPN.includes(dataFromPost.kaizenPN));
  }

  if (dataFromPost.startDate != '') {
    content = KaizenListData.filter((x) => x.createDate >= dataFromPost.startDate);
  }

  if (dataFromPost.endDate != '') {
    content = KaizenListData.filter((x) => x.createDate <= dataFromPost.endDate);
  }

  if (dataFromPost.manufactureMethodIds.length > 0) {
    content = KaizenListData.filter((x) => dataFromPost.manufactureMethodIds.includes(x.manufactureMethodId));
  }

  if (dataFromPost.applicant != '') {
    content = KaizenListData.filter((x) => x.applicant.includes(dataFromPost.applicant));
  }

  //ServerSide 處理
  const filterdListLength = content.length;
  const dataStart = (dataFromPost.currentPage - 1) * dataFromPost.pageSize;
  const dataEnd = dataStart + dataFromPost.pageSize;
  content = content.slice(dataStart, dataEnd);

  const res: IAPIServerSideResponse<IResKaizenDTO[]> = {
    result: 1,
    msg: 'success',
    //content: content,
    content: undefined,
    currentPage: dataFromPost.currentPage,
    pageSize: dataFromPost.pageSize,
    filterdListLength: filterdListLength,
  };

  return [200, res];
});
//#endregion


// #region 新增改善事項
// DTO
import IKaizenItem from "@/models/IKaizenItem";
// 假json資料
import KaizenNewData from "@/assets/mockData/KaizenNew.json";

const kaizenListData = KaizenListData as IKaizenItem[];

// 新增改善事項
mock.onPost("v1/KaizenNew").reply((ctx) => {
  console.log(`[Axios Mock] Call API : [Post] ${ctx.url}`);
  const dataFromPost = JSON.parse(ctx.data);
  const Id = Math.max(...kaizenListData.map(x => x.id?? 0)) as number;

  const res: IAPIResponse<IKaizenItem> = {
    result: 1,
    msg: 'success',
    content: kaizenListData[Id]
  };

  kaizenListData.push({
    id: Id + 1,
    creator: dataFromPost.creator,
    // creator: "Emily",
    codePJ: dataFromPost.codePJ,
    // factoryArea: dataFromPost.factoryArea,
    site: dataFromPost.site,
    factory: dataFromPost.factory,
    team: dataFromPost.team,
    kaizenPN: dataFromPost.kaizenPN,
    bu: dataFromPost.bu,
    endCustomer: dataFromPost.endCustomer,
    jobNumber: dataFromPost.jobNumber,
    kaizenBeforeWorkHours: dataFromPost.kaizenBeforeWorkHours,
    jobDescription: dataFromPost.jobDescription,
    originPPH: dataFromPost.originPPH,
    manufactureMethodId: dataFromPost.manufactureMethodId,
    KaizenAfterWorkHours: dataFromPost.KaizenAfterWorkHours,
    kaizenPlan: dataFromPost.kaizenPlan,
    kaizenContent: dataFromPost.kaizenContent,
    kaizen: dataFromPost.kaizen,
    afterTheAreaIsImproved: dataFromPost.afterTheAreaIsImproved,
    areaCost: dataFromPost.areaCost,
    areaBeforeImprovement: dataFromPost.areaBeforeImprovement,
    supplierAfterImprovement: dataFromPost.supplierAfterImprovement,
    originalProcess: dataFromPost.originalProcess,
    originalSystemStandard: dataFromPost.originalSystemStandard,
    improveFormerSuppliers: dataFromPost.improveFormerSuppliers,
    hyperLink: dataFromPost.hyperLink,
    hoursAdjustmentDate: dataFromPost.hoursAdjustmentDate,
    applicant: dataFromPost.creator,
  });

  return [200, res];
});
// #endregion

export default mock;
