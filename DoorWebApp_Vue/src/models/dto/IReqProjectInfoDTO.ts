import IFactory from "../IFactory";
import ISite from "../ISite";

export default interface IReqProjectInfoDTO {
  siteId: number;
  factoryId: number;
  periodId: number;
  teamNum: number;
  workRate?: number;
  creatorName: string;
  remark: string;
}
