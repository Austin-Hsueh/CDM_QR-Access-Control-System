/** 期數基本資料(DataTable顯示用) */
export default interface IResPeriodInfoDTO {
  /** 期數Id(流水號) */
  id: number;

  /** 期數 */
  periodNum: number;

  /** 建立者UserId */
  creatorUserId: number;

  /** 建立者DisplayName */
  creatorDisplayName: string;

  /** 建立日期,  格式:yyyy-MM-dd */
  createTime: string;

  /** 開始月份, 格式:yyyy-MM */
  startMonth: string;

  /** 結束月份, 格式:yyyy-MM */
  endMonth: string;

  /** 備註 */
  remark: string;
}
