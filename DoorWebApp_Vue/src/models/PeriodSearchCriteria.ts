import IReqPeriodSearchCriteriaDTO from "@/models/dto/IReqPeriodSearchCriteriaDTO";

export default class PeriodSearchCriteria {
  periodNum?: number; 
  creatorKeyword? :string;
  startDate?: Date;
  endDate?:Date

  clear() {
    this.periodNum = undefined;
    this.creatorKeyword = undefined;
    this.startDate = undefined;
    this.endDate = undefined;
  }

  ToReqDTO(): IReqPeriodSearchCriteriaDTO {
    const dto: IReqPeriodSearchCriteriaDTO = {
      periodNum: this.periodNum ? this.periodNum : undefined,
      creatorKeyword: this.creatorKeyword,
      startDate: this.startDate,
      endDate: this.endDate
    }
    return dto;
  }
}
