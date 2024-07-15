import IReqProjectInfoDTO from '@/models/dto/IReqProjectInfoDTO';
import IFactory from "./IFactory";
import ISite from "./ISite";
import ICreator from './ICreator';
import IResPeriodInfoDTO from './dto/IResPeriodInfoDTO';

export default class ProjectInfo {
  id:number;
  creator?: ICreator;
  pjcode?: string;
  period?: IResPeriodInfoDTO;
  site?: ISite;
  factory?: IFactory;
  teamNum?: number;
  workRate?: number;
  remark?: string;
  

  constructor() {
    this.id = -1;
  }

  ToIReqProjectInfoDTO(): IReqProjectInfoDTO {
    if(!this.creator) throw new Error("creator is required");
    if(!this.site) throw new Error("site is required");
    if(!this.factory) throw new Error("factory is required");
    if(!this.period) throw new Error("period is required");
    if(!this.teamNum) throw new Error("teamNum is required");

    const dto: IReqProjectInfoDTO = {
      creatorName: this.creator.username,
      periodId: this.period.id,
      siteId: this.site.id,
      factoryId: this.factory.id,
      teamNum: this.teamNum,
      workRate: this.workRate,
      remark: this.remark?? "",
    }
    return dto;
  }
}
