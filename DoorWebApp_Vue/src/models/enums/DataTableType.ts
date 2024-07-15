export enum DataTableType {
  /** 角色清單 */
  roleListTable = 'RoleListTable',

  /** 使用者清單 */
  userInfoTable = 'UserInfoTable',

  /** 改善事項清單 */
  kaizenItemTable = 'KaizenItemTable',

  /** 期數清單 () */
  periodTable = 'PeriodTable',

  /** 專案清單*/
  projectTable = 'ProjectTable',

  /** 改善事項複製查詢頁 */
  kaizenItemSearchTable = 'KaizenItemSearchTable',

  /** 專案查詢頁 (PageSearch.vue) */
  projectSearchTable = 'ProjectSearchTable',

  /** 工藝類別清單 */
  manufMethodTable = 'ManufMethodTable',

  /** 廠區清單 */
  siteTable = 'SiteTable',

  /** 報表: 改善對策查詢 */
  kaizenItemsByManufMethod = 'KaizenItemsByManufMethod',

  /** 報表: 效益追蹤表 */
  tpsPerformance = 'TPSPerformance',

  /** 成品料號維護頁 */
  topPNInfoTable = 'TopPNInfoTable',
}