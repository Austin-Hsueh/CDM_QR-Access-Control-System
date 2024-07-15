import IReqProjectSearchCriteriaDTO from "@/models/dto/IReqProjectSearchCriteriaDTO";
import IFactory from "./IFactory";
import ISite from "./ISite";

export default class ProjectSearchCriteria {
  pjCode?: string;
  site?: ISite;
  factory?: IFactory;
  creatorKeyword? :string;
  startDate?: Date;
  endDate?:Date

  clear() {
    this.pjCode = undefined;
    this.site = undefined;
    this.factory = undefined;
    this.creatorKeyword = undefined;
    this.startDate = undefined;
    this.endDate = undefined;
  }

  ToReqDTO(): IReqProjectSearchCriteriaDTO {
    const dto: IReqProjectSearchCriteriaDTO = {
      pjcode: this.pjCode,
      siteId: this.site?.id,
      factoryId: this.factory?.id,
      creator: this.creatorKeyword,
      startDate: this.startDate,
      endDate: this.endDate
    }
    return dto;
  }
}
