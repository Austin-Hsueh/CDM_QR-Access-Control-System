/** 專案基本資料(DataTable顯示用) */
export default interface IResProjectInfoDTO {
  /** 專案Id(流水號) */
  id: number;

  /** 專案代碼 */
  pjCode: string;

  /** 期數 */
  periodNum: number;

  /** 廠區代碼 */
  siteName: string;

  /** 工廠代碼 */
  factoryName: string;

  /** 小組名稱 */
  teamNum: number;

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

  /** 工資率, 0.00~1.00 */
  workRate: number;

  /** 備註 */
  remark: string;
}
