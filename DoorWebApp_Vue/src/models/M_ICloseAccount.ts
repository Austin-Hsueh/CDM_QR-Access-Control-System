/**
 * 單筆課程簽到狀態
 */
export interface M_IScheduleCheckStatus {
  scheduleId: number;
  studentPermissionId: number;
  studentId: number;
  username: string;
  studentName: string;
  courseName: string;
  classroomName: string;
  scheduleDate: string;  // 格式: "yyyy/MM/dd"
  startTime: string;     // 格式: "HH:mm"
  endTime: string;       // 格式: "HH:mm"
  status: string;        // "已簽到" | "未簽到"
  attendanceId?: number;
  attendanceType?: number; // 簽到類型 (0=缺席, 1=出席, 2=請假)，未簽到為 null
  checkedInTime?: string; // ISO 格式
}

/**
 * 關帳 - 日期課表與簽到狀態回應
 */
export interface M_IResDailyScheduleStatus {
  queryDate: string;
  totalSchedules: number;
  checkedInCount: number;
  notCheckedInCount: number;
  scheduleStatuses: M_IScheduleCheckStatus[];
  checkedInSchedules: M_IScheduleCheckStatus[];
  notCheckedInSchedules: M_IScheduleCheckStatus[];
  canCloseAccount: boolean;
}

/**
 * 批量簽到結果
 */
export interface M_ICheckInAllResult {
  date: string;
  totalSchedules: number;
  checkedInCount: number;
  successfulCheckins: number;
}

/**
 * 關帳明細資料
 */
export interface M_ICloseAccountDetail {
  closeDate: string;           // 關帳日期 (datetime)
  yesterdayPettyIncome: number; // 昨日零用金結餘
  businessIncome: number;       // 營業收入（學生學費）
  closeAccountAmount: number;   // 關帳結算金額
  depositAmount: number;        // 提存金額
  pettyIncome: number;          // 零用金結餘
}

/**
 * 儲存關帳請求
 */
export interface M_ISaveCloseAccountRequest {
  date: string;           // 關帳日期 (格式: yyyy-MM-dd)
  depositAmount: number;  // 提存金額
}

/**
 * 建立簽到請求
 */
export interface M_IReqCreateAttendance {
  studentPermissionId: number;  // 學生權限 ID
  attendanceDate: string;       // 簽到日期 (格式: yyyy-MM-dd)
  attendanceType: number;       // 簽到類型 (0=缺席, 1=出席, 2=請假)
  modifiedUserId: number;       // 操作者 ID
}

/**
 * 更新簽到請求
 */
export interface M_IReqUpdateAttendance {
  id: number;                   // 簽到記錄 ID
  studentPermissionId: number;  // 學生權限 ID
  attendanceDate: string;       // 簽到日期 (格式: yyyy-MM-dd)
  attendanceType: number;       // 簽到類型 (0=缺席, 1=出席, 2=請假)
  modifiedUserId: number;       // 操作者 ID
  isDelete: boolean;            // 是否刪除
}

/**
 * 簽到記錄回應
 */
export interface M_IResAttendance {
  attendanceId: number;
  studentPermissionId: number;
  attendanceDate: string;
  attendanceType: number;       // 0=缺席, 1=出席, 2=請假
  checkInTime?: string;         // HH:mm
}

/**
 * 學生簽到摘要項目
 */
export interface M_IStudentAttendanceSummary {
  serialNo: number;
  studentPermissionId: number;
  studentPermissionFeeId: number;
  courseName: string;
  paymentDate?: string;         // 應繳款日 民國年格式: 114/02/27
  payDate?: string;             // 實際繳款日 民國年格式: 114/02/27
  receivableAmount: number;     // 應收金額
  receivedAmount: number;       // 已收金額
  outstandingAmount: number;    // 欠款金額
  receiptNumber?: string;       // 結帳單號
  attendances: string[];        // 簽到記錄摘要列表
}

/**
 * 學生簽到記錄回應
 */
export interface M_IResStudentAttendance {
  nowStudentPermissionId: number;  // 最新學生權限 ID
  maxHours: number;                // 最大 Hours
  attendances: M_IStudentAttendanceSummary[];
}

/**
 * 建立繳費記錄請求
 */
export interface M_IReqCreatePayment {
  studentPermissionFeeId: number;  // 學生權限費用 ID
  pay: number;                     // 繳費金額
  discountAmount?: number;         // 折扣金額 (預設0)
  receiptNumber?: string;          // 結帳單號 (可選)
  remark?: string;                 // 備註
  modifiedUserId: number;          // 操作者 ID
  payDate?: string;                // 繳費日期 (可選，格式：yyyy/MM/dd，空值時系統預設)
  isDelete?: boolean;              // 是否執行軟刪除 (true=軟刪除，false/空值=維持或新增)
}

/**
 * 更新繳費記錄請求
 */
export interface M_IReqUpdatePayment {
  studentPermissionFeeId: number;  // 學生權限費用 ID
  pay: number;                     // 繳費金額
  discountAmount?: number;         // 折扣金額 (預設0)
  receiptNumber?: string;          // 結帳單號 (可選)
  remark?: string;                 // 備註
  modifiedUserId: number;          // 操作者 ID
  payDate?: string;                // 繳費日期 (可選，格式：yyyy/MM/dd，空值時沿用原值或系統預設)
  isDelete?: boolean;              // 是否執行軟刪除 (true=軟刪除，false/空值=維持或新增)
}

/**
 * 換綁繳費記錄請求
 */
export interface M_IReqRebindPayment {
  receiptNumber: string;           // 結帳單號（用於定位要換綁的 Payment）
  newStudentPermissionFeeId: number; // 新的學生權限費用 ID（轉入目標）
}

/**
 * 建立學生權限費用記錄請求
 */
export interface M_IReqCreateStudentPermissionFee {
  studentPermissionId: number;     // 學生權限 ID
}

/**
 * 建立學生權限費用記錄回應
 */
export interface M_IResCreateStudentPermissionFee {
  feeId: number;                   // 新建立的費用記錄 ID
  paymentDate: string;             // 繳款日 (yyyy/MM/dd 格式)
}

/**
 * 關帳記錄項目（TblCloseAccount）
 */
export interface M_ICloseAccountRecord {
  closeDate: string;               // 關帳日期 (datetime)
  yesterdayPettyIncome: number;    // 昨日零用金結餘
  businessIncome: number;          // 營業收入
  closeAccountAmount: number;      // 關帳結算金額
  depositAmount: number;           // 提存金額
  pettyIncome: number;             // 零用金結餘
}

/**
 * 查詢關帳記錄請求參數
 */
export interface M_IReqGetCloseAccounts {
  startDate?: string;              // 開始日期 (格式: yyyy-MM-dd)
  endDate?: string;                // 結束日期 (格式: yyyy-MM-dd)
}

/**
 * 學生繳費記錄項目
 */
export interface M_IStudentPaymentRecord {
  paymentId: number;
  studentPermissionFeeId: number;
  studentPermissionId: number;
  paymentDate: string;             // ISO 格式時間
  payDate: string;                 // 繳費日期 (格式: yyyy/MM/dd)
  pay: number;                     // 繳費金額
  discountAmount: number;          // 折扣金額
  receiptNumber: string | null;    // 結帳單號
  remark: string | null;           // 備註
  isDelete: boolean;               // 是否已刪除
  courseId: number;
  courseName: string;
  teacherId: number;
  teacherName: string;
  scheduleDate: string;            // 課程日期 (格式: yyyy/MM/dd~yyyy/MM/dd)
  startTime: string;               // 開始時間 (格式: HH:mm)
  endTime: string;                 // 結束時間 (格式: HH:mm)
  classroomName: string;
}

/**
 * 查詢學生所有繳費記錄回應
 */
export interface M_IResStudentPaymentByStudent {
  content: M_IStudentPaymentRecord[];
}
