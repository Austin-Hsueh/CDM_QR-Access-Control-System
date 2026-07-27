<template>
  <div class="content-body mt-3 d-flex flex-column justify-content-start align-content-start px-3">
    <!-- Title -->
    <div class="text-start mb-3">
      <span class="fs-4 fw-bold content-title">{{ t("QRcode") }}</span>
    </div>

    <el-tabs type="border-card">
        <el-tab-pane :label="t('Access QR Code')">
        <el-card class="qr-card">
          <span v-if="ifCodeError">QRcode將於上課前10分鐘顯示</span>
          <div v-if="ifCodeLoad">
            <span>可通行時間</span><br>
            <span>{{ pass.scheduleDate }} {{ dayOfWeek }}</span><br>
            <span>{{ pass.timefrom }}~{{ pass.timeto }}</span>
          </div>
          <el-divider style="margin: 15px 0;"/> 
          <el-image :src="imageSrc" alt="Base64 Image"  @error="handleImageError" @load="handleImageLoad"/>
          <el-divider style="margin: 15px 0;"/>
          <span>※ 無法使用時重新整理網頁</span>
        </el-card>
      </el-tab-pane>

      <el-tab-pane :label="t('Attendance Sheet')">
        <!-- 課程清單獨立於 QRcode，沒有當下課程時仍可查詢 -->
        <el-empty v-if="!isCourseLoading && !courseOptions.length" description="查無課程，無法顯示簽到表" />
        <template v-else>
          <div class="attendance-header">
            <el-select
              v-model="selectedPermissionId"
              :loading="isCourseLoading"
              placeholder="選擇課程"
              class="course-select"
              @change="onCourseChange">
              <el-option
                v-for="course in courseOptions"
                :key="course.studentPermissionId"
                :label="courseLabel(course)"
                :value="course.studentPermissionId" />
            </el-select>
            <el-tag effect="dark">剩餘可上課堂數：{{ remainingClasses }} 堂</el-tag>
          </div>

          <!-- 選到的正是當下這堂課時，順帶顯示上課時間 -->
          <div v-if="isCurrentSchedule && pass.scheduleDate" class="attendance-course">
            本堂：{{ pass.scheduleDate }} {{ dayOfWeek }} {{ pass.timefrom }}~{{ pass.timeto }}
          </div>

          <!-- 手機直式：卡片式呈現，簽到記錄用可換行的 tag，避免橫向捲動 -->
          <div v-if="isMobile" v-loading="isAttendanceLoading" class="attendance-cards">
            <el-empty v-if="!sortedAttendanceSummary.length" description="暫無簽到記錄" />
            <div v-for="row in sortedAttendanceSummary" :key="row.serialNo" class="attendance-card">
              <div class="card-top">
                <span class="course">{{ row.courseName }}</span>
                <el-tag size="small" type="info">第 {{ row.serialNo }} 期</el-tag>
              </div>
              <div class="card-meta">實際繳款日：{{ row.payDate || '未繳費' }}</div>
              <div class="card-checks">
                <el-tag
                  v-for="i in maxHours"
                  :key="i"
                  size="small"
                  :type="attendanceTagType(row.attendances[i - 1])"
                  :effect="row.attendances[i - 1] ? 'dark' : 'plain'">
                  {{ i }}. {{ row.attendances[i - 1] || '未簽到' }}
                </el-tag>
              </div>
            </div>
          </div>

          <!-- 桌機：維持表格 -->
          <el-table
            v-else
            v-loading="isAttendanceLoading"
            :data="sortedAttendanceSummary"
            border
            style="width: 100%"
            :header-cell-style="{ backgroundColor: '#F2F2F2' }"
            empty-text="暫無簽到記錄">
            <el-table-column prop="serialNo" label="序號" width="70" align="center" />
            <el-table-column prop="courseName" label="課程名稱" min-width="120" />
            <el-table-column prop="payDate" label="實際繳款日" min-width="110" />

            <!-- 動態簽到欄位 (欄數依後端回傳的 maxHours) -->
            <el-table-column
              v-for="i in maxHours"
              :key="i"
              :label="`簽到 ${i}`"
              min-width="130">
              <template #default="{ row }: { row: M_IStudentAttendanceSummary }">
                <el-tag
                  v-if="row.attendances[i - 1]"
                  size="small"
                  :type="attendanceTagType(row.attendances[i - 1])"
                  effect="dark">
                  {{ row.attendances[i - 1] }}
                </el-tag>
                <span v-else>-</span>
              </template>
            </el-table-column>
          </el-table>
        </template>
      </el-tab-pane>
    </el-tabs>
  </div>
</template>
<script setup lang="ts">
import { onMounted, onUnmounted, ref, reactive, computed} from "vue";
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import { useUserInfoStore } from "@/stores/UserInfoStore";
import API from "@/apis/TPSAPI";
import type { M_IStudentAttendanceSummary, M_IMyCourse } from "@/models/M_ICloseAccount";

const { t, locale } = useI18n();
const router = useRouter();
const userInfoStore = useUserInfoStore();

const qrcode = ref('');
const imageSrc = ref('');
const ifCodeError = ref(false);
const ifCodeLoad = ref(false);
const pass = ref({
      scheduleDate: '',
      timefrom: '',
      timeto: ''
});

// 手機直式判斷 (斷點與 MainLayout.vue 的 .sidebar RWD 一致)
const MOBILE_QUERY = '(max-width: 767px)';
const isMobile = ref(false);
let mql: MediaQueryList | null = null;
const onMqlChange = (e: MediaQueryListEvent | MediaQueryList) => { isMobile.value = e.matches; };

// 簽到表
// courseOptions 來自 MyCourses (不依賴 QRcode)，所以沒有當下課程時仍可查詢；
// 有當下課程 (schedules[0]) 時預設自動選它，使用者手動切換後就不再自動跟隨。
const courseOptions = ref<M_IMyCourse[]>([]);
const selectedPermissionId = ref<number | null>(null);
const schedulePermissionId = ref<number | null>(null);
const hasManualSelection = ref(false);
const isCourseLoading = ref(false);

const attendanceSummary = ref<M_IStudentAttendanceSummary[]>([]);
const maxHours = ref(0);
const isAttendanceLoading = ref(false);

// 選到的是否為當下這堂課
const isCurrentSchedule = computed(() =>
  schedulePermissionId.value !== null && schedulePermissionId.value === selectedPermissionId.value
);

// 母帳號會同時看到多個孩子的課，此時才需要在選項前標示學生姓名
const showStudentName = computed(() =>
  new Set(courseOptions.value.map(c => c.studentId)).size > 1
);

// 簽到格顏色：後端 FormatAttendance 回傳 "YYYY-MM-DD 出席/缺席/請假"
// 缺席用 warning(橘)，與課程詳情的「曠課」按鈕同色
const attendanceTagType = (value?: string | null): string => {
  if (!value) return 'info';                      // 尚未簽到
  if (value.includes('缺席')) return 'warning';
  if (value.includes('請假')) return 'info';
  return 'success';                               // 出席
};

const courseLabel = (course: M_IMyCourse): string => {
  const parts = [course.courseName || '未命名課程'];
  if (course.teacherName) parts.push(course.teacherName);
  const label = parts.join(' - ');
  return showStudentName.value && course.studentName ? `${course.studentName}｜${label}` : label;
};

// 顯示用：以時間降冪 (最新的期數排最前面)
// 後端 SerialNo 依費用 Id 遞增指派，簽到記錄亦依日期順序填入各期，故 SerialNo 即為時間順序；
// 不用 payDate 排序是因為未繳費的期數 payDate 為 null，會被排到最後面。
const sortedAttendanceSummary = computed(() =>
  [...attendanceSummary.value].sort((a, b) => b.serialNo - a.serialNo)
);

// 剩餘可上課堂數：僅統計已繳費 (receivedAmount > 0) 的期數中尚未簽到的格數
// (與 CourseScheduling.vue 繳費紀錄彈窗的算法一致)
const remainingClasses = computed(() =>
  attendanceSummary.value
    .filter(row => row.receivedAmount > 0)
    .reduce((sum, row) => sum + (row.attendances ?? []).filter(a => !a).length, 0)
);


let executionCount = 0; // 計數器

const handleImageError = () => {
  ifCodeError.value = true;
  ifCodeLoad.value = false;
}

const handleImageLoad = () => {
  ifCodeError.value = false;
  ifCodeLoad.value = true;
};

onMounted(() => {
  mql = window.matchMedia(MOBILE_QUERY);
  onMqlChange(mql);
  mql.addEventListener('change', onMqlChange);

  getMyCourses();

  // getUserSettingPermission()
  scheduleGetUserSettingPermission();
});

onUnmounted(() => {
  mql?.removeEventListener('change', onMqlChange);
});

//#region Private Functions
async function getUserSettingPermission() {
  try {
    const getUserSettingPermission = await API.getUserSettingPermission(userInfoStore.userId, Date.now());
    if (getUserSettingPermission.data.result != 1) throw new Error(getUserSettingPermission.data.msg);
    qrcode.value = getUserSettingPermission.data.content.qrcode || '';
    imageSrc.value = `data:image/png;base64,${qrcode.value}`;

    console.log(getUserSettingPermission.data.content)
    const QRcodeResult = getUserSettingPermission.data.content;
    if (QRcodeResult.schedules && QRcodeResult.schedules.length > 0) {
        pass.value.scheduleDate = QRcodeResult.schedules[0].scheduleDate;
        pass.value.timefrom = QRcodeResult.schedules[0].startTime;
        pass.value.timeto = QRcodeResult.schedules[0].endTime;
        schedulePermissionId.value = QRcodeResult.schedules[0].studentPermissionId ?? null;
    } else {
        console.log("studentpermissions array is empty or undefined");
        schedulePermissionId.value = null;
    }
    console.log(pass)

    // 未手動選過課程時，簽到表自動跟隨當下這堂課
    if (!hasManualSelection.value && schedulePermissionId.value) {
      selectedPermissionId.value = schedulePermissionId.value;
    }
    // 每次輪詢都重抓，讓學生剛簽到的記錄能反映在畫面上
    await getAttendanceSheet();

  } catch (error) {
    console.error(error);
  }
}

// 取得本人(含子帳號)的課程清單，供選擇器使用
async function getMyCourses() {
  isCourseLoading.value = true;
  try {
    const response = await API.getMyCourses();
    if (response.data.result != 1) throw new Error(response.data.msg);

    courseOptions.value = response.data.content ?? [];

    // 尚未決定要看哪一門時，預設選第一筆 (清單已依學生、課程名排序)
    if (!selectedPermissionId.value && courseOptions.value.length > 0) {
      selectedPermissionId.value = courseOptions.value[0].studentPermissionId;
      await getAttendanceSheet();
    }
  } catch (error) {
    console.error('載入課程清單失敗:', error);
    courseOptions.value = [];
  } finally {
    isCourseLoading.value = false;
  }
}

// 使用者手動切換課程後就不再自動跟隨當下課程
const onCourseChange = async () => {
  hasManualSelection.value = true;
  await getAttendanceSheet();
};

// 取得該課程的簽到表 (與繳費紀錄同一支 API，此處唯讀顯示)
async function getAttendanceSheet() {
  if (!selectedPermissionId.value) {
    attendanceSummary.value = [];
    maxHours.value = 0;
    return;
  }

  isAttendanceLoading.value = true;
  try {
    const response = await API.getStudentAttendance(selectedPermissionId.value);
    if (response.data.result != 1) throw new Error(response.data.msg);

    attendanceSummary.value = response.data.content.attendances ?? [];
    maxHours.value = response.data.content.maxHours ?? 0;
  } catch (error) {
    console.error('載入簽到表失敗:', error);
    attendanceSummary.value = [];
    maxHours.value = 0;
  } finally {
    isAttendanceLoading.value = false;
  }
}

function scheduleGetUserSettingPermission() {
  const interval = 5 * 60 * 1000; // 每隔 5 分鐘
  // const startHour = 7;
  // const endHour = 23;

  const executeFunction = () => {
    const now = new Date();
    // const currentHour = now.getHours();

    // 7點-23點 每5分鐘問一次資料庫QRcode
    // if (currentHour >= startHour && currentHour < endHour) {
    //   executionCount++;
    //   console.log(`第 ${executionCount} 次執行`);
    //   getUserSettingPermission();
    // }
    
    // 整天都每5分鐘問一次資料庫QRcode
    executionCount++;
    console.log(`第 ${executionCount} 次執行`);
    getUserSettingPermission();
  };

  // 立即執行一次，然後每隔 5 分鐘執行
  executeFunction();
  setInterval(executeFunction, interval);
}

// 使用 computed 將 pass.scheduleDate 轉換成對應的中文星期
const dayOfWeek = computed(() => {
  if (!pass.value.scheduleDate) return '';
  const daysOfWeek = ['星期日', '星期一', '星期二', '星期三', '星期四', '星期五', '星期六'];
  const date = new Date(pass.value.scheduleDate);
  return daysOfWeek[date.getDay()];
});

//#endregion

// const Info = ref([
//     // {
//     //   id: 1,
//     //   term: '第一期',
//     //   paymentDate: '2025-01-15',
//     //   paymentStamp: '已繳費',
//     //   attendanceFirst: '出席',
//     //   attendanceSecond: '出席',
//     //   attendanceThird: '請假',
//     //   attendanceFourth: '出席',
//     //   absenceRecord: '第三堂請假',
//     //   courseDeadline: '2025-06-30'
//     // },
//     // {
//     //   id: 2,
//     //   term: '第一期',
//     //   paymentDate: '2025-01-16',
//     //   paymentStamp: '已繳費',
//     //   attendanceFirst: '出席',
//     //   attendanceSecond: '缺席',
//     //   attendanceThird: '出席',
//     //   attendanceFourth: '出席',
//     //   absenceRecord: '第二堂缺席',
//     //   courseDeadline: '2025-06-30'
//     // },
//     // {
//     //   id: 3,
//     //   term: '第二期',
//     //   paymentDate: '2025-04-10',
//     //   paymentStamp: '未繳費',
//     //   attendanceFirst: '出席',
//     //   attendanceSecond: '出席',
//     //   attendanceThird: '出席',
//     //   attendanceFourth: '缺席',
//     //   absenceRecord: '第四堂缺席',
//     //   courseDeadline: '2025-09-15'
//     // },
//     // {
//     //   id: 4,
//     //   term: '第二期',
//     //   paymentDate: '2025-04-12',
//     //   paymentStamp: '已繳費',
//     //   attendanceFirst: '缺席',
//     //   attendanceSecond: '出席',
//     //   attendanceThird: '出席',
//     //   attendanceFourth: '出席',
//     //   absenceRecord: '第一堂缺席',
//     //   courseDeadline: '2025-09-15'
//     // },
//     // {
//     //   id: 5,
//     //   term: '第三期',
//     //   paymentDate: '2025-07-20',
//     //   paymentStamp: '已繳費',
//     //   attendanceFirst: '出席',
//     //   attendanceSecond: '出席',
//     //   attendanceThird: '出席',
//     //   attendanceFourth: '出席',
//     //   absenceRecord: '無',
//     //   courseDeadline: '2025-12-20'
//     // },
//     {
//       T1: '04/01',
//       T2: '04/08',
//       T3: '04/15',
//       T4: '04/22'
//     },
//     {
//       T1: '04/01',
//       T2: '04/08',
//       T3: '04/15',
//       T4: '04/22'
//     },
//     {
//       T1: '04/01',
//       T2: '04/08',
//       T3: '04/15',
//       T4: '04/22'
//     },
//     {
//       T1: '04/01',
//       T2: '04/08',
//       T3: '04/15',
//       T4: '04/22'
//     },
//   ]);

// // 可以通過以下函數轉換為直式數據
// const transposeTable = (data) => {
//   const keys = Object.keys(data[0]);
//   const transposed = [];
  
//   keys.forEach(key => {
//     const row = { property: key };
//     data.forEach((item, index) => {
//       row[`value${index}`] = item[key];
//     });
//     transposed.push(row);
//   });
  
//   return transposed;
// }
</script>


<style scoped>
.attendance-header {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 15px;
  margin-bottom: 15px;
}

.attendance-course {
  font-weight: 600;
  color: #303133;
  margin-bottom: 12px;
}

.course-select {
  min-width: 240px;
}

.qr-card {
  max-width: 300px;
}

/* 簽到表 - 手機卡片式 */
.attendance-cards {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.attendance-card {
  border: 1px solid #e4e7ed;
  border-radius: 8px;
  padding: 12px;
  background: #fff;
}

.card-top {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 8px;
  margin-bottom: 8px;
}

.card-top .course {
  font-weight: 600;
  color: #303133;
}

.card-meta {
  font-size: 13px;
  color: #909399;
  margin-bottom: 10px;
}

.card-checks {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}

/** Mobile - 手機直式瀏覽 (斷點同 MainLayout.vue) */
@media screen and (max-width: 767px) {
  .content-body {
    padding-left: 8px !important;
    padding-right: 8px !important;
  }

  /* border-card 預設左右各 15px 內距，手機上縮小以換取內容寬度 */
  :deep(.el-tabs--border-card > .el-tabs__content) {
    padding: 10px;
  }

  :deep(.el-tabs__item) {
    padding: 0 12px;
    font-size: 14px;
  }

  /* QRcode 置中並放大，方便掃描 */
  .qr-card {
    max-width: 100%;
    margin: 0 auto;
  }

  .qr-card :deep(.el-image) {
    width: 100%;
    max-width: 320px;
  }

  .attendance-header {
    flex-direction: column;
    align-items: flex-start;
    gap: 8px;
  }

  .attendance-course {
    font-size: 14px;
  }

  /* 課程選擇器在手機上撐滿，方便點選 */
  .course-select {
    width: 100%;
    min-width: 0;
  }
}

.card-wrap {
  display: grid;
  grid-template-rows: 2fr 1fr;
  width:100%;
  height: 100%;
}

/* .image {
  width: 100%;
  display: block;
} */
.card-wrap:hover {
  cursor: pointer;
}

.card-wrap:hover .image {
  transition: transform 0.2s; /* Animation */
  transform: scale(var(--card-hover-scale));
}

.card-wrap .image {
  grid-row: 1 / 2;
  grid-column: 1 / 1;
  display: block;
}

.card-wrap .content {
  grid-row: 2 / 3;
  grid-column: 1 / 1;
}
</style>
