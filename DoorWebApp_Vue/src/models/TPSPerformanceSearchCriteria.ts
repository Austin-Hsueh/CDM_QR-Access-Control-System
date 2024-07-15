import IReqReportPerformanceSearchCriteriaDTO from "@/models/dto/IReqReportPerformanceSearchCriteriaDTO";
import IFactory from "./IFactory";
import ISite from "./ISite";

export default class TPSPerformanceSearchCriteria {
  page: number;
  pageSize: number;
  sortColName?: string;
  sortOrder?: string;
  
  lastPeriodCnt: number;
  site?: ISite;
  factory?: IFactory;
  projectCodeKeyword?: string;
  pnKeyword? :string;

  constructor(page: number, pageSize:number) {
    this.page = page;
    this.pageSize = pageSize;
    this.lastPeriodCnt = 1;
  }
  
  clear() {
    this.projectCodeKeyword = undefined;
    this.lastPeriodCnt = 1;
    this.site = undefined;
    this.factory = undefined;
    this.pnKeyword = undefined;
  }

  ToReqDTO(): IReqReportPerformanceSearchCriteriaDTO {
    const dto: IReqReportPerformanceSearchCriteriaDTO = {
      page: this.page,
      pageSize: this.pageSize,
      sortOrder: this.sortOrder,
      sortColName: this.sortColName,
      projectCodeKeyword: this.projectCodeKeyword,
      lastPeriodCnt: this.lastPeriodCnt,
      siteId: this.site?.id,
      factoryId: this.factory?.id,
      pnKeyword: this.pnKeyword,
    }
    return dto;
  }
}
