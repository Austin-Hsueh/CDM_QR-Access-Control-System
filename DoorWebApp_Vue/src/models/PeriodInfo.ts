import IReqPeriodInfoDTO from '@/models/dto/IReqPeriodInfoDTO';
import IResPeriodInfoDTO from '@/models/dto/IResPeriodInfoDTO';
import ICreator from './ICreator';

export default class PeriodInfo {
  id:number;
  periodNum?: number;
  creator?: ICreator;
  duration: Date[] = [];
  remark?: string;

  constructor() {
    this.id = -1;
  }

  ToIReqPeriodInfoDTO(): IReqPeriodInfoDTO {
    if(!this.periodNum) throw new Error("period is required");
    if(this.duration.length != 2) throw new Error("Date range is required");

    const dto: IReqPeriodInfoDTO = {
      periodNum: this.periodNum,
      remark: this.remark?? "",
      startMonth: this.duration[0],
      endMonth:  this.duration[1],
    }
    return dto;
  }
}
