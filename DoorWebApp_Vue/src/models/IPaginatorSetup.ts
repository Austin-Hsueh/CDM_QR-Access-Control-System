export default interface IPaginatorSetup {
  /** 總頁數 total 和 page-count 設置任意一個就可以達到顯示頁碼的功能；如果要支持 page-sizes 的更改，則需要使用 total 屬性 */
  pagerCount: number,

  /** 組件布局，子組件名使用逗號分隔 */
  layout: string;

  /** 是否使用小型布局 */
  small: boolean;
}