<template>
<!-- 日期與教室選擇器 -->
<div style="margin-bottom: 15px; display: flex; align-items: center; gap: 20px;">
  <!-- 日期選擇器 - 只在日視圖顯示 -->
  <div v-if="currentViewType === 'resourceTimeGridDay'" style="display: flex; align-items: center; gap: 10px;">
    <span style="font-weight: bold;">跳到指定日期：</span>
    <el-date-picker
      v-model="selectedDate"
      type="date"
      placeholder="選擇日期"
      format="YYYY-MM-DD"
      value-format="YYYY-MM-DD"
      style="width: 200px"
      @change="handleDateChange"
    />
  </div>
  <!-- 教室篩選器 - 只在月視圖顯示 -->
  <div v-if="currentViewType === 'dayGridMonth'" style="display: flex; align-items: center; gap: 10px;">
    <span style="font-weight: bold;">篩選教室：</span>
    <el-select
      v-model="selectedClassroomId"
      placeholder="全部教室"
      clearable
      style="width: 200px"
      @change="handleClassroomFilterChange"
    >
      <el-option label="全部教室" :value="null"></el-option>
      <el-option
        v-for="item in classRoomList"
        :key="item.classroomId"
        :label="item.classroomName"
        :value="item.classroomId"
      />
    </el-select>
  </div>
</div>

<FullCalendar
  ref="fullCalendar"
  :options="calendarOptions"
  class="custom-calendar"
/>

<!-- 選擇更新模式 Dialog -->
<el-dialog
  v-model="isShowUpdateModeDialog"
  title="選擇更新模式"
  width="400px"
>
  <el-form label-position="top">
    <el-form-item label="請選擇更新範圍：">
      <el-select v-model="selectedUpdateMode" placeholder="請選擇" style="width: 100%">
        <el-option label="僅此次課程" :value="1"></el-option>
        <el-option label="此日期後的所有課程" :value="2"></el-option>
        <el-option label="全部課程" :value="3"></el-option>
      </el-select>
    </el-form-item>
    <el-form-item v-if="selectedUpdateMode === 2" label="起始日期：">
      <el-date-picker
        v-model="selectedFromDate"
        type="date"
        placeholder="請選擇日期"
        format="YYYY-MM-DD"
        value-format="YYYY-MM-DD"
        style="width: 100%"
      />
    </el-form-item>
  </el-form>
  <template #footer>
    <span class="dialog-footer">
      <el-button @click="cancelUpdateMode">取消</el-button>
      <el-button type="primary" @click="confirmUpdateMode">確定</el-button>
    </span>
  </template>
</el-dialog>

<!-- 新增課程 Dialog -->
<el-dialog
  v-model="isShowAddCourseDialog"
  title="新增課程"
  width="800px"
>
  <el-form
    ref="addCourseFormRef"
    :model="addCourseFormData"
    :rules="courseRules"
    label-width="100px"
    label-position="top"
  >
    <el-row :gutter="20">
      <el-col :span="8">
        <el-form-item label="教室" prop="classroomId">
          <el-select filterable placeholder="請選擇" v-model="addCourseFormData.classroomId" style="width: 100%" disabled>
            <el-option
              v-for="item in classRoomList"
              :key="item.classroomId"
              :label="item.classroomName"
              :value="item.classroomId?.toString()"
            />
          </el-select>
        </el-form-item>
      </el-col>
      <el-col :span="8">
        <el-form-item label="學生姓名" prop="userId">
          <el-select filterable placeholder="請選擇" v-model="addCourseFormData.userId" style="width: 100%">
            <el-option
              v-for="item in usersOptions"
              :key="item.userId"
              :label="item.displayName"
              :value="item.userId"
            />
          </el-select>
        </el-form-item>
      </el-col>
      <el-col :span="8">
        <el-form-item label="門禁種類" prop="type">
          <el-select filterable placeholder="請選擇" v-model="addCourseFormData.type" style="width: 100%">
            <el-option label="上課" value="1"></el-option>
            <el-option label="租借教室" value="2"></el-option>
          </el-select>
        </el-form-item>
      </el-col>
    </el-row>    
    
    <el-row :gutter="20" v-show="addCourseFormData.type === '1'">
      <el-col :span="8">
        <el-form-item label="課程類別" prop="courseId">
          <el-select filterable placeholder="請選擇" v-model="addCourseFormData.courseId" style="width: 100%">
            <el-option
              v-for="item in courseList"
              :key="item.courseId"
              :label="item.courseName"
              :value="item.courseId">
            </el-option>
          </el-select>
        </el-form-item>
      </el-col>
      <el-col :span="8">
        <el-form-item label="課程模式" prop="courseMode">
          <el-select filterable placeholder="請選擇" v-model="addCourseFormData.courseMode" style="width: 100%">
            <el-option label="現場" value="1"></el-option>
            <el-option label="視訊" value="2"></el-option>
          </el-select>
        </el-form-item>
      </el-col>
      <el-col :span="8">
        <el-form-item label="老師姓名" prop="teacherId">
          <el-select filterable placeholder="請選擇" v-model="addCourseFormData.teacherId" style="width: 100%">
            <el-option
              v-for="item in teachersOptions"
              :key="item.teacherId"
              :label="item.teacherName"
              :value="item.teacherId"
            />
          </el-select>
        </el-form-item>
      </el-col>
    </el-row>

    <el-form-item label="門禁權限" prop="groupIds">
      <el-checkbox-group v-model="addCourseFormData.groupIds">
        <el-checkbox v-for="item in doors" :key="item" :label="item" :value="item">
          <template v-if="item === 1">大門</template>
          <template v-else-if="item === 2">car教室</template>
          <template v-else-if="item === 3">Sunny教室</template>
          <template v-else-if="item === 4">儲藏室</template>
        </el-checkbox>
      </el-checkbox-group>
    </el-form-item>

    <el-form-item label="週期模式" prop="scheduleMode">
      <el-select filterable placeholder="請選擇" v-model="addCourseFormData.scheduleMode" style="width: 100%">
        <el-option label="每週" value="1"></el-option>
        <el-option label="每兩週" value="2"></el-option>
        <el-option label="單次" value="3"></el-option>
      </el-select>
    </el-form-item>

    <el-form-item label="上課(門禁)日期" prop="datepicker">
      <el-date-picker
        type="daterange"
        range-separator="至"
        start-placeholder="開始日期"
        end-placeholder="結束日期"
        v-model="addCourseFormData.datepicker"
        style="width: 100%"
      />
    </el-form-item>

    <el-form-item label="上課(門禁)時間" prop="timepicker">
      <el-time-picker
        is-range
        range-separator="至"
        start-placeholder="開始時間"
        end-placeholder="結束時間"
        v-model="addCourseFormData.timepicker"
        style="width: 100%"
        disabled
      />
    </el-form-item>

    <el-form-item label="週期" prop="days">
      <el-checkbox-group v-model="addCourseFormData.days">
        <el-checkbox v-for="item in daysOfWeek" :key="item" :label="item" :value="item">
          <template v-if="item === 1">週一</template>
          <template v-else-if="item === 2">週二</template>
          <template v-else-if="item === 3">週三</template>
          <template v-else-if="item === 4">週四</template>
          <template v-else-if="item === 5">週五</template>
          <template v-else-if="item === 6">週六</template>
          <template v-else-if="item === 7">週日</template>
        </el-checkbox>
      </el-checkbox-group>
    </el-form-item>

    <el-form-item label="備註" prop="remark">
      <el-input
        v-model="addCourseFormData.remark"
        type="textarea"
        :rows="3"
        placeholder="請輸入備註"
      />
    </el-form-item>
  </el-form>
  <template #footer>
    <span class="dialog-footer">
      <el-button @click="cancelAddCourse">取消</el-button>
      <el-button @click="clearAddCourseForm">清除</el-button>
      <el-button type="primary" @click="submitAddCourse">確定</el-button>
    </span>
  </template>
</el-dialog>

<!-- 課程詳情 Dialog -->
<el-dialog
  v-model="isShowCourseDetailDialog"
  title="課程詳情"
  width="800px"
>
  <el-row :gutter="20">
    <!-- 左側：課程資訊 (2/3) -->
    <el-col :span="16">
      <el-descriptions :column="1" border>
        <el-descriptions-item label="學生" label-width="80px">{{ courseDetail.studentName }}</el-descriptions-item>
        <el-descriptions-item label="課程名稱" label-width="80px">{{ courseDetail.courseName }}</el-descriptions-item>
        <el-descriptions-item label="老師" label-width="80px">{{ courseDetail.teacherName }}</el-descriptions-item>
        <el-descriptions-item label="教室" label-width="80px">{{ courseDetail.classroomName }}</el-descriptions-item>
        <el-descriptions-item label="課程時間" label-width="80px">{{ courseDetail.scheduleDate }} {{ courseDetail.startTime }} ~ {{ courseDetail.endTime }}</el-descriptions-item>
      </el-descriptions>
    </el-col>

    <!-- 右側：操作按鈕 (1/3) -->
    <el-col :span="8">
      <div style="display: flex; flex-direction: column; gap: 15px; height: 100%; justify-content: center;">
        <!-- 第一列：刪除、繳費紀錄 -->
        <div style="display: flex; gap: 10px;">
          <el-button type="danger" size="default" @click="handleDeleteCourse" style="margin-left: 0; flex: 1;">
            <el-icon><Delete /></el-icon>刪除課程
          </el-button>
          <el-button type="info" size="default" @click="handleCheckIn(2)" style="margin-left: 0; flex: 1;">
            <el-icon><CircleCheck /></el-icon>請假
          </el-button>
        </div>
        <!-- 第二列：缺席、出席 -->
        <div style="display: flex; gap: 10px;">
          <el-button type="warning" size="default" @click="handleCheckIn(0)" style="margin-left: 0; flex: 1;">
            <el-icon><CircleCheck /></el-icon>曠課
          </el-button>
          <el-button type="success" size="default" @click="handleCheckIn(1)" style="margin-left: 0; flex: 1;">
            <el-icon><CircleCheck /></el-icon>簽到
          </el-button>
        </div>
        <!-- 第三列：繳費紀錄 -->
        <el-button type="primary" size="default" @click="handlePaymentRecord" style="margin-left: 0;">
          <el-icon><Money /></el-icon>繳費紀錄
        </el-button>
      </div>
    </el-col>
  </el-row>
</el-dialog>

<!-- 繳費紀錄 Dialog -->
<el-dialog
  v-model="isShowPaymentRecordDialog"
  title="繳費紀錄"
  width="90%"
>
  <div style="margin-bottom: 15px;">
    <el-button type="primary" size="small" @click="handleCreatePayment"><el-icon><EditPen /></el-icon>{{ '新增一期繳費' }}</el-button>
  </div>
  <el-table :data="paymentRecordData" border style="width: 100%">
    <el-table-column prop="serialNo" label="序號" />
    <el-table-column prop="courseName" label="課程名稱"/>
    <!-- <el-table-column prop="paymentDate" label="應繳款日"/> -->
    <el-table-column prop="payDate" label="實際繳款日" />
    <el-table-column prop="receivableAmount" label="應收金額" />
    <el-table-column prop="receivedAmount" label="已收金額" />
    <el-table-column prop="outstandingAmount" label="欠款金額" />
    <el-table-column prop="receiptNumber" label="結帳單號" />

    <!-- 動態簽到欄位 -->
    <el-table-column
      v-for="i in maxHours"
      :key="i"
      :label="`簽到 ${i}`"
    >
      <template #default="{ row }">
        {{ row.attendances[i - 1] ?? '-' }}
      </template>
    </el-table-column>
    <el-table-column align="center" class="operateBtnGroup d-flex" label="操作">
      <template #default="{ row }: { row: any }">
        <el-button type="primary" size="small" @click="handlePayment(row)" v-if="(row.receivedAmount === 0)"><el-icon><EditPen /></el-icon>{{ '繳費' }}</el-button>
        <el-button type="primary" size="small" @click="handlePayment(row)" v-if="(row.receivedAmount !== 0)"><el-icon><EditPen /></el-icon>{{ '編輯繳款資訊' }}</el-button>
      </template>
    </el-table-column>
  </el-table>
</el-dialog>

<!-- 繳費 Dialog -->
<el-dialog
  v-model="isShowPaymentDialog"
  title="繳費"
  width="500px"
>
  <el-form :model="paymentFormData" label-width="120px">
    <el-form-item label="課程名稱">
      <el-input v-model="paymentFormData.courseName" disabled />
    </el-form-item>
    <el-form-item label="應收金額">
      <el-input-number v-model="paymentFormData.receivableAmount" :min="0" :controls="false" disabled style="width: 100%" />
    </el-form-item>
    <el-form-item label="繳費金額">
      <el-input-number v-model="paymentFormData.pay" :min="0" :controls="false" style="width: 100%" />
    </el-form-item>
    <el-form-item label="折扣金額">
      <el-input-number v-model="paymentFormData.discountAmount" :min="0" :controls="false" style="width: 100%" />
    </el-form-item>
    <el-form-item label="繳費日期">
      <el-date-picker
        v-model="paymentFormData.payDate"
        type="date"
        placeholder="選擇繳費日期（空值=系統自動填入今日）"
        format="YYYY/MM/DD"
        value-format="YYYY/MM/DD"
        style="width: 100%"
      />
    </el-form-item>
    <el-form-item label="備註">
      <el-input v-model="paymentFormData.remark" type="textarea" :rows="3" />
    </el-form-item>
  </el-form>
  <template #footer>
    <span class="dialog-footer">
      <el-button @click="isShowPaymentDialog = false">取消</el-button>
      <el-button v-if="isEditMode" type="danger" @click="deletePayment">刪除此筆繳費</el-button>
      <el-button type="primary" @click="submitPayment">確定</el-button>
    </span>
  </template>
</el-dialog>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue';
import { CalendarOptions } from '@fullcalendar/core';
import resourceTimeGridPlugin from '@fullcalendar/resource-timegrid';
import FullCalendar from '@fullcalendar/vue3'
import dayGridPlugin from '@fullcalendar/daygrid'
import timeGridPlugin from '@fullcalendar/timegrid'
import interactionPlugin from '@fullcalendar/interaction'
import { FormInstance, FormRules, ElMessage, ElMessageBox } from 'element-plus';
import { Delete, Edit, CircleCheck, Money, EditPen } from '@element-plus/icons-vue';
import { useUserInfoStore } from '@/stores/UserInfoStore';

import API from '@/apis/TPSAPI';

import { M_IClassRoomOptions } from '@/models/M_IClassRoomOptions';
import { M_IUsersOptions } from '@/models/M_IUsersOptions';
import { M_ICourseOptions } from '@/models/M_ICourseOptions';
import { M_ITeachersOptions } from '@/models/M_ITeachersOptions';
import { M_IStudentAttendanceSummary } from '@/models/M_ICloseAccount';
import { el } from 'element-plus/es/locale';


const classRoomList = ref<M_IClassRoomOptions[]>([]);
const fullCalendar = ref<any>(null);
const userInfoStore = useUserInfoStore();

// 教室資源 (從 API 動態載入)
const resources = ref<any[]>([]);

// 日期選擇器
const selectedDate = ref('');

// 教室篩選
const selectedClassroomId = ref<number | null>(null);

// 當前視圖類型
const currentViewType = ref('resourceTimeGridDay');

// Dialog 控制
const isShowAddCourseDialog = ref(false);
const addCourseFormRef = ref<FormInstance>();

// 課程詳情 Dialog 控制
const isShowCourseDetailDialog = ref(false);
const courseDetail = reactive({
  scheduleId: 0,
  studentName: '',
  courseName: '',
  teacherName: '',
  classroomName: '',
  scheduleDate: '',
  startTime: '',
  endTime: '',
  studentPermissionId: 0
});

// 繳費紀錄 Dialog 控制
const isShowPaymentRecordDialog = ref(false);
const paymentRecordData = ref<M_IStudentAttendanceSummary[]>([]);
const maxHours = ref(0);

// 繳費 Dialog 控制
const isShowPaymentDialog = ref(false);
const isEditMode = ref(false); // 是否為編輯模式
const paymentFormData = reactive({
  studentPermissionFeeId: 0,
  courseName: '',
  receivableAmount: 0,
  receivedAmount: 0, // 已收金額，用於判斷是否為編輯模式
  pay: 0,
  discountAmount: 0,
  remark: '',
  payDate: '' // 繳費日期
});

// 更新模式 Dialog 控制
const isShowUpdateModeDialog = ref(false);
const selectedUpdateMode = ref(1);
const selectedFromDate = ref('');
let updateModeResolve: ((value: { updateMode: number; fromDate: string } | null) => void) | null = null;

// 選項資料
const usersOptions = ref<M_IUsersOptions[]>([]);
const teachersOptions = ref<M_ITeachersOptions[]>([]);
const courseList = ref<M_ICourseOptions[]>([]);
const doors = [1, 2, 3, 4];
const daysOfWeek = [1, 2, 3, 4, 5, 6, 7];

// 表單資料
const addCourseFormData = reactive({
  // 從 calendar click 自動填入
  classroomId: '',
  classroomName: '',
  startTime: '',
  endTime: '',
  selectInfo: null as any,
  // 使用者輸入欄位
  userId: '',
  type: '',          // 門禁種類 (1:上課, 2:租借教室)
  courseMode: '',    // 課程模式 (1:現場, 2:視訊)
  scheduleMode: '',  // 週期模式 (1:每週, 2:每兩週, 3:單次)
  courseId: '',
  teacherId: '',
  groupIds: [] as number[],
  datepicker: null as any,
  timepicker: null as any,
  days: [] as number[],
  remark: ''         // 備註
});

// 表單驗證規則
const courseRules: FormRules = {
  userId: [
    { required: true, message: '請選擇學生姓名', trigger: 'change' }
  ],
  type: [
    { required: true, message: '請選擇門禁種類', trigger: 'change' }
  ],
  courseMode: [
    { required: false, message: '請選擇課程模式', trigger: 'change' }
  ],
  scheduleMode: [
    { required: true, message: '請選擇週期模式', trigger: 'change' }
  ],
  courseId: [
    { required: false, message: '請選擇課程類別', trigger: 'change' }
  ],
  teacherId: [
    { required: false, message: '請選擇老師', trigger: 'change' }
  ],
  groupIds: [
    { required: true, message: '請選擇門禁權限', trigger: 'change' }
  ],
  datepicker: [
    { required: true, message: '請選擇通行日期', trigger: 'change' }
  ],
  timepicker: [
    { required: true, message: '請選擇通行時間', trigger: 'change' }
  ],
  days: [
    { required: true, message: '請選擇週期', trigger: 'change' }
  ]
};

// 事件處理函數
const handleWeekendsToggle = () => {
  calendarOptions.weekends = !calendarOptions.weekends;
};

// 跳到指定日期
const handleDateChange = (date: string) => {
  if (!date) return;

  const calendarApi = fullCalendar.value?.getApi();
  if (calendarApi) {
    calendarApi.gotoDate(date);
    console.log('跳轉到日期:', date);
  }
};

// 教室篩選變更
const handleClassroomFilterChange = async () => {
  const calendarApi = fullCalendar.value?.getApi();
  if (!calendarApi) return;

  const currentView = calendarApi.view;

  // 如果清除篩選（值為 null），顯示全部教室
  if (selectedClassroomId.value === null) {
    console.log('清除教室篩選，顯示全部教室');
  } else {
    console.log('教室篩選:', selectedClassroomId.value);
  }

  // 直接重新載入課程資料（不通過 handleDatesSet，避免觸發視圖切換邏輯）
  try {
    const startDate = new Date(currentView.activeStart);
    const endDate = new Date(currentView.activeEnd);

    const formatDate = (date: Date) => {
      const year = date.getFullYear();
      const month = String(date.getMonth() + 1).padStart(2, '0');
      const day = String(date.getDate()).padStart(2, '0');
      return `${year}-${month}-${day}`;
    };

    const cmd = {
      page: 1,
      searchPage: 2000,
      dateFrom: formatDate(startDate),
      dateTo: formatDate(endDate),
      status: 1
    };

    const response = await API.getCoursesByDate(cmd);

    if (response.data.content?.pageItems && Array.isArray(response.data.content.pageItems)) {
      calendarApi.removeAllEvents();

      // 後端已過濾課程，前端只需處理教室篩選
      let filteredSchedules = response.data.content.pageItems;

      // 如果有選擇教室篩選，只顯示該教室的課程
      if (selectedClassroomId.value !== null) {
        filteredSchedules = filteredSchedules.filter((schedule: any) => {
          return schedule.classroomId === selectedClassroomId.value;
        });
      }

      // 將課程加入 Calendar
      filteredSchedules.forEach((schedule: any) => {
        const scheduleDate = schedule.scheduleDate.replace(/\//g, '-');
        const startDateTime = `${scheduleDate}T${schedule.startTime}:00`;
        const endDateTime = `${scheduleDate}T${schedule.endTime}:00`;

        const titleParts = [];
        if (schedule.type === 1) {
          if (schedule.studentName) titleParts.push(`學生：${schedule.studentName}`);
          if (schedule.courseName) titleParts.push(`課程：${schedule.courseName}`);
          if (schedule.teacherName) titleParts.push(`老師：${schedule.teacherName}`);
        } else if (schedule.type === 2) {
          if (schedule.studentName) titleParts.push(`學生：${schedule.studentName}`);
          titleParts.push(`租借教室`);
          if (schedule.remark) titleParts.push(`備註：${schedule.remark}`);
        }
        const title = titleParts.length > 0 ? titleParts.join(' ') : '課程';

        calendarApi.addEvent({
          id: schedule.scheduleId?.toString(),
          title: title,
          start: startDateTime,
          end: endDateTime,
          resourceId: schedule.classroomId.toString(),
          extendedProps: {
            scheduleId: schedule.scheduleId,
            studentPermissionId: schedule.studentPermissionId,
            courseName: schedule.courseName,
            studentName: schedule.studentName,
            classroomName: schedule.classroomName,
            courseMode: schedule.courseMode,
            courseModeName: schedule.courseModeName,
            scheduleMode: schedule.scheduleMode,
            scheduleModeName: schedule.scheduleModeName,
            status: schedule.status,
            statusName: schedule.statusName,
            remark: schedule.remark,
            teacherName: schedule.teacherName,
            type: schedule.type
          }
        });
      });

      console.log('載入的課程數量:', filteredSchedules.length);
    }
  } catch (error) {
    console.error('載入課程資料失敗:', error);
    ElMessage.error('載入課程失敗');
  }
};

const handleDateSelect = (selectInfo: any) => {
  // 儲存選擇資訊
  addCourseFormData.selectInfo = selectInfo;
  addCourseFormData.classroomId = selectInfo.resource?.id || '';
  addCourseFormData.classroomName = selectInfo.resource?.title || '';
  addCourseFormData.startTime = selectInfo.startStr;
  addCourseFormData.endTime = selectInfo.endStr;

  // 從 calendar 選擇的時間轉換為 timepicker 格式
  const startDate = new Date(selectInfo.start);
  const endDate = new Date(selectInfo.end);
  addCourseFormData.timepicker = [startDate, endDate];

  // 取得選擇的星期幾 (0=週日, 1=週一, ..., 6=週六)
  const dayOfWeek = startDate.getDay();
  // 轉換為 1=週一, ..., 7=週日
  const selectedDay = dayOfWeek === 0 ? 7 : dayOfWeek;

  // 清空使用者輸入欄位
  addCourseFormData.userId = '';
  addCourseFormData.type = '';
  addCourseFormData.courseMode = '';
  addCourseFormData.scheduleMode = '';
  addCourseFormData.courseId = '';
  addCourseFormData.teacherId = '';
  addCourseFormData.groupIds = [1]; // 預設帶大門
  addCourseFormData.datepicker = null;
  addCourseFormData.days = [selectedDay]; // 帶入選擇的星期幾
  addCourseFormData.remark = '';

  // 顯示 Dialog
  isShowAddCourseDialog.value = true;
};

// 取消新增課程
const cancelAddCourse = () => {
  isShowAddCourseDialog.value = false;
  if (addCourseFormData.selectInfo) {
    addCourseFormData.selectInfo.view.calendar.unselect();
  }
  addCourseFormRef.value?.resetFields();
};

// 顯示更新模式 Dialog
const showUpdateModeDialog = (defaultDate: string): Promise<{ updateMode: number; fromDate: string } | null> => {
  return new Promise((resolve) => {
    selectedUpdateMode.value = 1;
    selectedFromDate.value = defaultDate;
    updateModeResolve = resolve;
    isShowUpdateModeDialog.value = true;
  });
};

// 確認更新模式
const confirmUpdateMode = () => {
  const result = {
    updateMode: selectedUpdateMode.value,
    fromDate: selectedFromDate.value
  };
  isShowUpdateModeDialog.value = false;
  if (updateModeResolve) {
    updateModeResolve(result);
    updateModeResolve = null;
  }
};

// 取消更新模式
const cancelUpdateMode = () => {
  isShowUpdateModeDialog.value = false;
  if (updateModeResolve) {
    updateModeResolve(null);
    updateModeResolve = null;
  }
};

// 清除表單
const clearAddCourseForm = () => {
  addCourseFormData.userId = '';
  addCourseFormData.type = '';
  addCourseFormData.courseMode = '';
  addCourseFormData.scheduleMode = '';
  addCourseFormData.courseId = '';
  addCourseFormData.teacherId = '';
  addCourseFormData.groupIds = [];
  addCourseFormData.datepicker = null;
  addCourseFormData.timepicker = null;
  addCourseFormData.days = [];
  addCourseFormData.remark = '';
};

// 提交新增課程
const submitAddCourse = async () => {
  if (!addCourseFormRef.value) return;

  await addCourseFormRef.value.validate(async (valid) => {
    if (valid) {
      try {
        // 格式化日期和時間
        const formatDate = (date: Date) => {
          const year = date.getFullYear();
          const month = String(date.getMonth() + 1).padStart(2, '0');
          const day = String(date.getDate()).padStart(2, '0');
          return `${year}-${month}-${day}`;
        };

        const formatTime = (date: Date) => {
          const hours = String(date.getHours()).padStart(2, '0');
          const minutes = String(date.getMinutes()).padStart(2, '0');
          return `${hours}:${minutes}`;
        };

        // 準備 API 資料
        const courseData = {
          userId: Number(addCourseFormData.userId),
          courseId: addCourseFormData.courseId ? Number(addCourseFormData.courseId) : 0,
          teacherId: addCourseFormData.teacherId ? Number(addCourseFormData.teacherId) : 0,
          datefrom: formatDate(addCourseFormData.datepicker[0]),
          dateto: formatDate(addCourseFormData.datepicker[1]),
          timefrom: formatTime(addCourseFormData.timepicker[0]),
          timeto: formatTime(addCourseFormData.timepicker[1]),
          type: Number(addCourseFormData.type),
          days: addCourseFormData.days,
          groupIds: addCourseFormData.groupIds,
          classroomId: Number(addCourseFormData.classroomId),
          courseMode: addCourseFormData.courseMode ? Number(addCourseFormData.courseMode) : 1,
          scheduleMode: Number(addCourseFormData.scheduleMode),
          remark: addCourseFormData.remark || ''
        };

        // 呼叫 API 儲存課程資料
        const response = await API.createCourseSchedule(courseData);

        if (response.data.result === 1) {
          // 清除選擇並關閉 Dialog
          const calendarApi = addCourseFormData.selectInfo?.view.calendar;
          if (calendarApi) {
            calendarApi.unselect();
          }
          isShowAddCourseDialog.value = false;
          addCourseFormRef.value?.resetFields();

          // 重新載入當前視圖的課程（觸發 handleDatesSet）
          const fullCalendarApi = fullCalendar.value?.getApi();
          if (fullCalendarApi) {
            const currentView = fullCalendarApi.view;
            await handleDatesSet({
              start: currentView.activeStart,
              end: currentView.activeEnd,
              startStr: currentView.activeStart.toISOString(),
              endStr: currentView.activeEnd.toISOString(),
              view: currentView
            });
          }

          // 顯示成功訊息
          ElMessage.success('課程新增成功');
        } else {
          console.error('課程新增失敗:', response.data.msg);
          ElMessage.error(`課程新增失敗: ${response.data.msg}`);
        }
      } catch (error) {
        console.error('提交課程時發生錯誤:', error);
        alert('提交課程時發生錯誤，請稍後再試');
      }
    }
  });
};

const handleEventClick = async (clickInfo: any) => {
  const event = clickInfo.event;
  const scheduleId = event.extendedProps.scheduleId;
  const studentPermissionId = event.extendedProps.studentPermissionId || 0;
  const studentName = event.extendedProps.studentName || '';
  const courseName = event.extendedProps.courseName || '';
  const teacherName = event.extendedProps.teacherName || '';
  const startTime = event.start;
  const endTime = event.end;
  const resourceTitle = event.getResources()[0]?.title || '';

  // 格式化日期和時間
  const formatDate = (date: Date) => {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  };

  const formatTime = (date: Date) => {
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    return `${hours}:${minutes}`;
  };

  // 填充課程詳情資料
  courseDetail.scheduleId = scheduleId;
  courseDetail.studentPermissionId = studentPermissionId;
  courseDetail.studentName = studentName;
  courseDetail.courseName = courseName;
  courseDetail.teacherName = teacherName;
  courseDetail.classroomName = resourceTitle;
  courseDetail.scheduleDate = formatDate(startTime);
  courseDetail.startTime = formatTime(startTime);
  courseDetail.endTime = formatTime(endTime);

  // 顯示課程詳情彈窗
  isShowCourseDetailDialog.value = true;
};

// 刪除課程
const handleDeleteCourse = async () => {
  const scheduleId = courseDetail.scheduleId;

  try {
    // 先確認是否要刪除
    await ElMessageBox.confirm(
      `確定要刪除 ${courseDetail.studentName} 的 ${courseDetail.courseName} 課程嗎？`,
      '確認刪除',
      {
        confirmButtonText: '確定',
        cancelButtonText: '取消',
        type: 'warning',
      }
    );
  } catch {
    return; // 使用者取消
  }

  try {
    // 驗證 scheduleId
    if (!scheduleId) {
      ElMessage.error('找不到課程 ID，無法刪除');
      return;
    }

    // 詢問刪除模式
    const result = await showUpdateModeDialog(courseDetail.scheduleDate);

    if (!result) {
      // 使用者取消操作
      return;
    }

    // 準備 API 請求資料
    const cmd: any = {
      scheduleId: scheduleId,
      isDelete: true,
      updateMode: result.updateMode
    };

    // 根據 updateMode 決定帶入的日期欄位
    if (result.updateMode === 1) {
      // 單次刪除：需要 scheduleDate
      cmd.scheduleDate = courseDetail.scheduleDate;
    } else if (result.updateMode === 2) {
      // 某日後全部刪除：需要 fromDate
      cmd.fromDate = result.fromDate;
    }
    // updateMode === 3 (全部刪除)：不需要日期欄位

    console.log('刪除課程排程:', cmd);

    // 呼叫 API 刪除 (使用 patch 方法)
    const response = await API.deleteCourseSchedule(cmd);

    if (response.data.result === 1) {
      const modeText = result.updateMode === 1 ? '此次課程' : result.updateMode === 2 ? `${result.fromDate} 之後的課程` : '全部課程';
      ElMessage.success(`已刪除${modeText}`);

      // 重新載入課程資料
      const fullCalendarApi = fullCalendar.value?.getApi();
      if (fullCalendarApi) {
        const currentView = fullCalendarApi.view;
        await handleDatesSet({
          start: currentView.activeStart,
          end: currentView.activeEnd,
          startStr: currentView.activeStart.toISOString(),
          endStr: currentView.activeEnd.toISOString(),
          view: currentView
        });
      }
    } else {
      ElMessage.error(response.data.msg || '刪除課程失敗');
    }
  } catch (error) {
    console.error('刪除課程排程失敗:', error);
    ElMessage.error('刪除課程失敗，請稍後再試');
  }

  // 關閉彈窗
  isShowCourseDetailDialog.value = false;
};

// 更改課程
const handleUpdateCourse = () => {
  ElMessage.info('更改課程功能開發中...');
  // TODO: 實作更改課程功能
  console.log('更改課程:', courseDetail);
};

// 簽到
const handleCheckIn = async (attendanceType: number) => {
  // attendanceType: 0=曠課, 1=簽到, 2=請假
  const typeText = ['曠課', '簽到', '請假'][attendanceType];

  try {
    await ElMessageBox.confirm(
      `確定要將 ${courseDetail.studentName} 標記為「${typeText}」嗎？`,
      '確認簽到',
      {
        confirmButtonText: '確定',
        cancelButtonText: '取消',
        type: 'warning',
      }
    );
  } catch {
    return; // 使用者取消
  }

  try {
    const response = await API.createAttendance({
      studentPermissionId: courseDetail.studentPermissionId,
      attendanceDate: courseDetail.scheduleDate,
      attendanceType: attendanceType,
      modifiedUserId: userInfoStore.userId
    });

    if (response.data.result !== 1) {
      throw Error(response.data.msg);
    }

    ElMessage.success(`已成功標記為「${typeText}」`);

    // 關閉彈窗
    isShowCourseDetailDialog.value = false;

    // 重新載入行事曆資料
    const fullCalendarApi = fullCalendar.value?.getApi();
    if (fullCalendarApi) {
      const currentView = fullCalendarApi.view;
      await handleDatesSet({
        start: currentView.activeStart,
        end: currentView.activeEnd,
        startStr: currentView.activeStart.toISOString(),
        endStr: currentView.activeEnd.toISOString(),
        view: currentView
      });
    }

  } catch (error) {
    ElMessage.error((error as Error).message || '簽到失敗，請稍後再試');
  }
};

// 繳費紀錄
const handlePaymentRecord = async () => {
  try {
    const response = await API.getStudentAttendance(courseDetail.studentPermissionId);

    if (response.data.result !== 1) {
      throw Error(response.data.msg);
    }

    const data = response.data.content;
    paymentRecordData.value = data.attendances;
    maxHours.value = data.maxHours;

    // 顯示繳費紀錄彈窗
    isShowPaymentRecordDialog.value = true;

    console.log('繳費紀錄:', data);
  } catch (error) {
    ElMessage.error((error as Error).message || '查詢繳費紀錄失敗');
  }
};

// 開啟繳費對話框
const handlePayment = (row: M_IStudentAttendanceSummary) => {
  paymentFormData.studentPermissionFeeId = row.studentPermissionFeeId;
  paymentFormData.courseName = row.courseName;
  paymentFormData.receivableAmount = row.receivableAmount;
  paymentFormData.receivedAmount = row.receivedAmount;

  // 判斷是否為編輯模式（已收金額不為0表示已繳費）
  isEditMode.value = row.receivedAmount !== 0;

  if (isEditMode.value) {
    // 編輯模式：載入已存在的繳費資料
    paymentFormData.pay = row.receivedAmount;
    paymentFormData.discountAmount = 0; // TODO: 從後端取得實際的折扣金額
    paymentFormData.remark = ''; // TODO: 從後端取得實際的備註
    paymentFormData.payDate = row.payDate || '';
  } else {
    // 新增模式：使用預設值
    paymentFormData.pay = row.receivableAmount; // 預設代入應收金額
    paymentFormData.discountAmount = 0;
    paymentFormData.remark = '';
    paymentFormData.payDate = '';
  }

  isShowPaymentDialog.value = true;
};

// 提交繳費
const submitPayment = async () => {
  try {
    let response;

    if (isEditMode.value) {
      // 編輯模式：使用 updatePayment API
      response = await API.updatePayment({
        studentPermissionFeeId: paymentFormData.studentPermissionFeeId,
        pay: paymentFormData.pay,
        discountAmount: paymentFormData.discountAmount,
        remark: paymentFormData.remark,
        modifiedUserId: 51,
        payDate: paymentFormData.payDate || undefined,
        isDelete: false
      });
    } else {
      // 新增模式：使用 createPayment API
      response = await API.createPayment({
        studentPermissionFeeId: paymentFormData.studentPermissionFeeId,
        pay: paymentFormData.pay,
        discountAmount: paymentFormData.discountAmount,
        remark: paymentFormData.remark,
        modifiedUserId: 51,
        payDate: paymentFormData.payDate || undefined,
        isDelete: false
      });
    }

    if (response.data.result !== 1) {
      throw Error(response.data.msg);
    }

    ElMessage.success(isEditMode.value ? '更新繳費成功' : '繳費成功');
    isShowPaymentDialog.value = false;

    // 重新載入繳費紀錄
    await handlePaymentRecord();
  } catch (error) {
    ElMessage.error((error as Error).message || (isEditMode.value ? '更新繳費失敗' : '繳費失敗'));
  }
};

// 刪除繳費
const deletePayment = async () => {
  try {
    await ElMessageBox.confirm(
      `確定要刪除此筆繳費記錄嗎？`,
      '確認刪除',
      {
        confirmButtonText: '確定',
        cancelButtonText: '取消',
        type: 'warning',
      }
    );
  } catch {
    return; // 使用者取消
  }

  try {
    const response = await API.updatePayment({
      studentPermissionFeeId: paymentFormData.studentPermissionFeeId,
      pay: paymentFormData.pay,
      discountAmount: paymentFormData.discountAmount,
      remark: paymentFormData.remark,
      modifiedUserId: 51,
      payDate: paymentFormData.payDate || undefined,
      isDelete: true
    });

    if (response.data.result !== 1) {
      throw Error(response.data.msg);
    }

    ElMessage.success('刪除繳費成功');
    isShowPaymentDialog.value = false;

    // 重新載入繳費紀錄
    await handlePaymentRecord();
  } catch (error) {
    ElMessage.error((error as Error).message || '刪除繳費失敗');
  }
};

// 新增一期繳費
const handleCreatePayment = async () => {
  try {
    await ElMessageBox.confirm(
      `確定要為學生新增一期繳費記錄嗎？`,
      '確認新增',
      {
        confirmButtonText: '確定',
        cancelButtonText: '取消',
        type: 'warning',
      }
    );
  } catch {
    return; // 使用者取消
  }

  try {
    const response = await API.createStudentPermissionFee({
      studentPermissionId: courseDetail.studentPermissionId
    });

    if (response.data.result !== 1) {
      throw Error(response.data.msg);
    }

    const data = response.data.content;
    ElMessage.success(`新增成功！費用記錄 ID: ${data.feeId}，繳款日: ${data.paymentDate}`);

    // 重新載入繳費紀錄
    await handlePaymentRecord();
  } catch (error) {
    ElMessage.error((error as Error).message || '新增費用記錄失敗');
  }
};

const handleEvents = (events: any) => {
  console.log('Events updated:', events);
};

// 處理日期範圍變更（當點擊 prev/next/today 或切換視圖時觸發）
const handleDatesSet = async (dateInfo: any) => {
  console.log('日期範圍變更:', {
    start: dateInfo.startStr,
    end: dateInfo.endStr,
    view: dateInfo.view.type
  });

  // 更新當前視圖類型
  const previousViewType = currentViewType.value;
  currentViewType.value = dateInfo.view.type;

  // 切換到日視圖或週視圖時，重置教室篩選為全教室
  if ((dateInfo.view.type === 'resourceTimeGridDay' || dateInfo.view.type === 'resourceTimeGridWeek')
      && previousViewType === 'dayGridMonth') {
    selectedClassroomId.value = null;
    console.log('切換到日/週視圖，重置教室篩選為全教室');
  }

  // 切換到月視圖時，預設載入 Car 教室 (classroomId: 2)
  if (dateInfo.view.type === 'dayGridMonth' && previousViewType !== 'dayGridMonth') {
    selectedClassroomId.value = 2;
    console.log('切換到月視圖，預設載入 Car 教室');
  }

  try {
    // 取得當前視圖的日期範圍
    const startDate = new Date(dateInfo.start);
    const endDate = new Date(dateInfo.end);

    // 格式化日期為 "2025-10-08"
    const formatDate = (date: Date) => {
      const year = date.getFullYear();
      const month = String(date.getMonth() + 1).padStart(2, '0');
      const day = String(date.getDate()).padStart(2, '0');
      return `${year}-${month}-${day}`;
    };

    // 準備 API 請求參數
    const cmd = {
      page: 1,
      searchPage: 2000, // 增加數量以涵蓋週視圖
      dateFrom: formatDate(startDate),
      dateTo: formatDate(endDate),
      status: 1
    };

    console.log('載入課程範圍:', cmd);

    const response = await API.getCoursesByDate(cmd);

    if (response.data.content?.pageItems && Array.isArray(response.data.content.pageItems)) {
      const calendarApi = fullCalendar.value?.getApi();
      if (!calendarApi) return;

      // 清除現有事件
      calendarApi.removeAllEvents();

      // 後端已過濾「上課類型但沒有老師」的課程，前端只需處理教室篩選
      let filteredSchedules = response.data.content.pageItems;

      // 如果有選擇教室篩選，只顯示該教室的課程
      if (selectedClassroomId.value !== null) {
        filteredSchedules = filteredSchedules.filter((schedule: any) => {
          return schedule.classroomId === selectedClassroomId.value;
        });
      }

      // 將每個課程加入 Calendar
      filteredSchedules.forEach((schedule: any) => {
        // 組合日期和時間
        const scheduleDate = schedule.scheduleDate.replace(/\//g, '-'); // "2025/10/08" -> "2025-10-08"
        const startDateTime = `${scheduleDate}T${schedule.startTime}:00`;
        const endDateTime = `${scheduleDate}T${schedule.endTime}:00`;

        // 組合標題（依據類型顯示不同資訊）
        const titleParts = [];
        if (schedule.type === 1) {
          // 上課：顯示學生、課程、老師
          if (schedule.studentName) titleParts.push(`${schedule.studentName}`);
          if (schedule.courseName) titleParts.push(`${schedule.courseName}`);
          if (schedule.teacherName) titleParts.push(`${schedule.teacherName}`);
        } else if (schedule.type === 2) {
          // 租借教室：顯示學生、租借教室、備註
          if (schedule.studentName) titleParts.push(`學生：${schedule.studentName}`);
          titleParts.push(`租借教室`);
          if (schedule.remark) titleParts.push(`備註：${schedule.remark}`);
        }
        const title = titleParts.length > 0 ? titleParts.join(' ') : '課程';

        calendarApi.addEvent({
          id: schedule.scheduleId?.toString(),
          title: title,
          start: startDateTime,
          end: endDateTime,
          resourceId: schedule.classroomId.toString(),
          extendedProps: {
            scheduleId: schedule.scheduleId,
            studentPermissionId: schedule.studentPermissionId,
            courseName: schedule.courseName,
            studentName: schedule.studentName,
            classroomName: schedule.classroomName,
            courseMode: schedule.courseMode,
            courseModeName: schedule.courseModeName,
            scheduleMode: schedule.scheduleMode,
            scheduleModeName: schedule.scheduleModeName,
            status: schedule.status,
            statusName: schedule.statusName,
            remark: schedule.remark,
            teacherName: schedule.teacherName,
            type: schedule.type
          }
        });
      });

      console.log('載入的課程數量:', response.data.content.pageItems.length);
    }
  } catch (error) {
    console.error('載入課程資料失敗:', error);
    ElMessage.error('載入課程失敗');
  }
};

// 處理事件拖曳
const handleEventDrop = async (dropInfo: any) => {
  const event = dropInfo.event;
  const newResource = event.getResources()[0];

  try {
    // 從事件中取得 scheduleId 和其他必要資訊
    const scheduleId = event.extendedProps.scheduleId;
    if (!scheduleId) {
      ElMessage.error('找不到課程 ID，無法更新');
      dropInfo.revert(); // 還原拖曳
      return;
    }

    // 格式化日期和時間
    const startDate = new Date(event.start);
    const endDate = new Date(event.end);

    // 使用本地時間格式化日期，避免時區問題
    const formatLocalDate = (date: Date) => {
      const year = date.getFullYear();
      const month = String(date.getMonth() + 1).padStart(2, '0');
      const day = String(date.getDate()).padStart(2, '0');
      return `${year}-${month}-${day}`;
    };

    const scheduleDate = startDate.toISOString().split('T')[0]; // "2024-02-15"
    const startTime = `${String(startDate.getHours()).padStart(2, '0')}:${String(startDate.getMinutes()).padStart(2, '0')}`; // "15:00"
    const endTime = `${String(endDate.getHours()).padStart(2, '0')}:${String(endDate.getMinutes()).padStart(2, '0')}`; // "17:00"

    // 詢問使用者更新模式
    const result = await showUpdateModeDialog(scheduleDate);

    if (!result) {
      // 使用者取消操作
      dropInfo.revert();
      return;
    }

    const updateMode = result.updateMode;
    const fromDate = result.fromDate;

    // 準備 API 請求資料
    const cmd: any = {
      scheduleId: scheduleId,
      classroomId: Number(newResource.id),
      startTime: startTime,
      endTime: endTime,
      courseMode: event.extendedProps.courseMode || 1,
      status: event.extendedProps.status || 1,
      remark: event.extendedProps.remark || '',
      updateMode: updateMode
    };

    // 根據 updateMode 決定帶入的日期欄位
    if (updateMode === 1) {
      // 單次修改：需要 scheduleDate
      cmd.scheduleDate = scheduleDate;
    } else if (updateMode === 2) {
      // 某日後全部修改：需要 fromDate
      cmd.fromDate = fromDate;
    } else if (updateMode === 3) {
      // 全部修改：需要 fromDate
      cmd.fromDate = fromDate;
    }

    console.log('更新課程排程:', cmd);

    // 呼叫 API 更新
    const response = await API.updateCourseSchedule(cmd);

    if (response.data.result === 1) {
      const modeText = updateMode === 1 ? '此次課程' : updateMode === 2 ? `${fromDate} 之後的課程` : '全部課程';
      ElMessage.success(`已更新${modeText}至 ${newResource.title} ${startTime}-${endTime}`);
      // 重新載入課程資料
      const fullCalendarApi = fullCalendar.value?.getApi();
      if (fullCalendarApi) {
        const currentView = fullCalendarApi.view;
        await handleDatesSet({
          start: currentView.activeStart,
          end: currentView.activeEnd,
          startStr: currentView.activeStart.toISOString(),
          endStr: currentView.activeEnd.toISOString(),
          view: currentView
        });
      }
    } else {
      ElMessage.error(response.data.msg || '更新課程失敗');
      dropInfo.revert(); // 還原拖曳
    }
  } catch (error: any) {
    console.error('更新課程排程失敗:', error);
    console.error('錯誤詳情:', error.response?.data);
    ElMessage.error(`更新課程失敗: ${error.response?.data?.msg || error.message || '請稍後再試'}`);
    dropInfo.revert(); // 還原拖曳
  }
};

// 處理事件調整大小
const handleEventResize = async (resizeInfo: any) => {
  const event = resizeInfo.event;

  try {
    // 從事件中取得 scheduleId 和其他必要資訊
    const scheduleId = event.extendedProps.scheduleId;
    if (!scheduleId) {
      ElMessage.error('找不到課程 ID，無法更新');
      resizeInfo.revert(); // 還原調整
      return;
    }

    // 格式化日期和時間
    const startDate = new Date(event.start);
    const endDate = new Date(event.end);
    const resource = event.getResources()[0];

    // 使用本地時間格式化日期，避免時區問題
    const formatLocalDate = (date: Date) => {
      const year = date.getFullYear();
      const month = String(date.getMonth() + 1).padStart(2, '0');
      const day = String(date.getDate()).padStart(2, '0');
      return `${year}-${month}-${day}`;
    };

    const scheduleDate = startDate.toISOString().split('T')[0]; // "2024-02-15"
    const startTime = `${String(startDate.getHours()).padStart(2, '0')}:${String(startDate.getMinutes()).padStart(2, '0')}`; // "15:00"
    const endTime = `${String(endDate.getHours()).padStart(2, '0')}:${String(endDate.getMinutes()).padStart(2, '0')}`; // "17:00"

    // 詢問使用者更新模式
    const result = await showUpdateModeDialog(scheduleDate);

    if (!result) {
      // 使用者取消操作
      resizeInfo.revert();
      return;
    }

    const updateMode = result.updateMode;
    const fromDate = result.fromDate;

    // 準備 API 請求資料
    const cmd: any = {
      scheduleId: scheduleId,
      classroomId: Number(resource.id),
      startTime: startTime,
      endTime: endTime,
      courseMode: event.extendedProps.courseMode || 1,
      status: event.extendedProps.status || 1,
      remark: event.extendedProps.remark || '',
      updateMode: updateMode
    };

    // 根據 updateMode 決定帶入的日期欄位
    if (updateMode === 1) {
      // 單次修改：需要 scheduleDate
      cmd.scheduleDate = scheduleDate;
    } else if (updateMode === 2) {
      // 某日後全部修改：需要 fromDate
      cmd.fromDate = fromDate;
    } else if (updateMode === 3) {
      // 某日後全部修改：需要 fromDate
      cmd.fromDate = fromDate;
    }
    

    console.log('更新課程時間:', cmd);

    // 呼叫 API 更新
    const response = await API.updateCourseSchedule(cmd);

    if (response.data.result === 1) {
      const modeText = updateMode === 1 ? '此次課程' : updateMode === 2 ? `${fromDate} 之後的課程` : '全部課程';
      ElMessage.success(`已更新${modeText}的時間為 ${startTime}-${endTime}`);
      // 重新載入課程資料
      const fullCalendarApi = fullCalendar.value?.getApi();
      if (fullCalendarApi) {
        const currentView = fullCalendarApi.view;
        await handleDatesSet({
          start: currentView.activeStart,
          end: currentView.activeEnd,
          startStr: currentView.activeStart.toISOString(),
          endStr: currentView.activeEnd.toISOString(),
          view: currentView
        });
      }
    } else {
      ElMessage.error(response.data.msg || '更新課程失敗');
      resizeInfo.revert(); // 還原調整
    }
  } catch (error: any) {
    console.error('更新課程時間失敗:', error);
    console.error('錯誤詳情:', error.response?.data);
    ElMessage.error(`更新課程失敗: ${error.response?.data?.msg || error.message || '請稍後再試'}`);
    resizeInfo.revert(); // 還原調整
  }
};

// 自訂事件顯示內容
const eventContent = (arg: any) => {
  const studentName = arg.event.extendedProps.studentName || '';
  const courseName = arg.event.extendedProps.courseName || '';
  const teacherName = arg.event.extendedProps.teacherName || '';
  const type = arg.event.extendedProps.type || 1;
  const scheduleMode = arg.event.extendedProps.scheduleMode || 1;
  const studentPermissionId = arg.event.extendedProps.studentPermissionId || null;

  // 建立容器（使用 flexbox 兩欄佈局）
  const container = document.createElement('div');
  container.style.display = 'flex';
  container.style.height = '100%';
  container.style.overflow = 'hidden';

  // 根據 type 和 scheduleMode 決定背景色
  const getEventColor = (type: number, scheduleMode: number) => {
    if (type === 1 && scheduleMode === 1) return '#b1c5af'; // 上課 & 每週固定
    if (type === 1 && scheduleMode === 2) return '#d4bfe2'; // 上課 & 每兩週固定
    if (type === 1 && scheduleMode === 3) return '#e8bbbb'; // 上課 & 單次課程
    if (type === 2 && scheduleMode === 3) return '#ccb9aa'; // 租借教室 & 單次課程
    return '#3788d8'; // 預設顏色
  };

  const eventBgColor = getEventColor(type, scheduleMode);

  // 第一欄：學生名稱和課程名稱（使用原始色碼）
  const column1 = document.createElement('div');
  column1.style.fontWeight = 'bold';
  column1.style.flex = '1';
  column1.style.padding = '4px';
  column1.style.backgroundColor = eventBgColor;
  column1.style.display = 'flex';
  column1.style.flexDirection = 'column';
  column1.style.alignItems = 'center';
  column1.style.justifyContent = 'center';
  column1.style.fontSize = '14px';
  column1.style.overflow = 'hidden';
  column1.style.lineHeight = '1.3';
  
  if (studentName) {
    const studentLine = document.createElement('div');
    studentLine.style.overflow = 'hidden';
    studentLine.style.textOverflow = 'ellipsis';
    studentLine.style.whiteSpace = 'nowrap';
    studentLine.style.width = '100%';
    studentLine.style.textAlign = 'center';
    studentLine.textContent = `${studentName}`;
    column1.appendChild(studentLine);
  }

  if (courseName) {
    const courseLine = document.createElement('div');
    courseLine.style.overflow = 'hidden';
    courseLine.style.textOverflow = 'ellipsis';
    courseLine.style.whiteSpace = 'nowrap';
    courseLine.style.width = '100%';
    courseLine.style.textAlign = 'center';
    courseLine.textContent = `${courseName}`;
    column1.appendChild(courseLine);
  }

  container.appendChild(column1);

  // 第二欄：課程名稱和老師名稱（使用 #2c3e50）
  const column2 = document.createElement('div');
  column2.style.fontWeight = 'bold';
  column2.style.flex = '1';
  column2.style.padding = '4px';
  column2.style.backgroundColor = '#2c3e50';
  column2.style.color = '#ffffff';
  column2.style.display = 'flex';
  column2.style.alignItems = 'center';
  column2.style.justifyContent = 'center';
  column2.style.fontSize = '14px';
  column2.style.overflow = 'hidden';
  column2.style.textOverflow = 'ellipsis';
  column2.style.whiteSpace = 'nowrap';

  const column2Parts = [];
  // if (courseName) column2Parts.push(courseName);
  if (teacherName) column2Parts.push(teacherName);

  if (type === 2) column2Parts.push('租借');

  if (column2Parts.length > 0) {
    column2.textContent = column2Parts.join(' ');
  }
  container.appendChild(column2);

  return { domNodes: [container] };
};

// Calendar 設定
const calendarOptions: CalendarOptions = reactive({
  plugins: [
    resourceTimeGridPlugin,
    dayGridPlugin,
    timeGridPlugin,
    interactionPlugin
  ],
  headerToolbar: {
    left: 'prev,next today',
    center: 'title',
    end: 'resourceTimeGridDay,dayGridMonth'
  },
  titleFormat: (date) => {
    const year = date.date.year;
    const month = String(date.date.month + 1).padStart(2, '0'); // month 是 0-based，要加 1
    const day = String(date.date.day).padStart(2, '0');
    const weekdays = ['星期日', '星期一', '星期二', '星期三', '星期四', '星期五', '星期六'];
    // 直接用 marker 建立 Date 物件來取得正確的星期
    const weekday = weekdays[new Date(date.date.marker).getDay()];
    return `${year}/${month}/${day} ${weekday}`;
  },
  initialView: 'resourceTimeGridDay',
  resources: (_fetchInfo, successCallback) => {
    successCallback(resources.value);
  },
  editable: true,
  selectable: true,
  selectMirror: true,
  droppable:true,
  dayMaxEvents: true,
  weekends: true,
  locale: 'zh-tw',
  height: 'auto',
  resourceAreaHeaderContent: '教室',
  resourceAreaWidth: '30%',
  slotMinTime: '07:00:00',
  slotMaxTime: '24:00:00',
  slotDuration: '00:30:00',
  slotLabelInterval: '00:30:00',
  select: handleDateSelect,
  eventClick: handleEventClick,
  eventsSet: handleEvents,
  eventContent: eventContent, //自訂事件顯示內容
  eventDrop: handleEventDrop, //拖曳事件處理
  eventResize: handleEventResize, //調整事件大小處理
  datesSet: handleDatesSet, //日期範圍變更處理（prev/next/today）

});

//#region Hook functions
onMounted(() => {
  getClassRoomsOptions();
  getUsersOptions();
  getCourseOptions();
  // 課程會由 handleDatesSet 自動載入（當 calendar 初始化時觸發）
});

//#endregion

//#region Private Functions
// 載入教室選項
async function getClassRoomsOptions() {
  try {
    const response = await API.getClassrooms();
    classRoomList.value = response.data.content;

    // 轉換為 FullCalendar resources 格式
    resources.value = response.data.content.map((room: M_IClassRoomOptions) => ({
      id: room.classroomId?.toString() || '',
      title: room.classroomName || ''
      // 移除 eventColor，讓事件使用自訂顏色而非教室顏色
    }));

    console.log('載入的教室資源:', resources.value);

    // 強制刷新 Calendar 的 resources
    const calendarApi = fullCalendar.value?.getApi();
    if (calendarApi) {
      calendarApi.refetchResources();
    }
  } catch (error) {
    console.error('載入教室資料失敗:', error);
  }
}

// 載入使用者選項
async function getUsersOptions() {
  try {
    const getUsersOptionsResult = await API.getUsersOptions();
    if (getUsersOptionsResult.data.result != 1) throw new Error(getUsersOptionsResult.data.msg);
    usersOptions.value = getUsersOptionsResult.data.content.filter(user => ![52, 54].includes(user.userId));

    const getTeachersOptionsResult = await API.getTeachersOptions();
    if (getTeachersOptionsResult.data.result != 1) throw new Error(getTeachersOptionsResult.data.msg);
    teachersOptions.value = getTeachersOptionsResult.data.content;
  } catch (error) {
    console.error('載入使用者資料失敗:', error);
  }
}

// 載入課程選項
async function getCourseOptions() {
  try {
    const response = await API.getCourse();
    courseList.value = response.data.content;
    console.log('課程列表:', courseList.value);
  } catch (error) {
    console.error('載入課程資料失敗:', error);
  }
}
//#endregion
</script>

<style scoped>
/* 月視圖事件樣式調整 */
.custom-calendar :deep(.fc-daygrid-event) {
  min-height: 50px !important;
  font-size: 13px !important;
}

.custom-calendar :deep(.fc-daygrid-event div) {
  width: 100%;
}


.custom-calendar :deep(.fc-daygrid-day-events) {
  min-height: 60px !important;
}

.custom-calendar :deep(.fc-daygrid-day-frame) {
  min-height: 120px !important;
}

/* 增加月視圖每格的高度 */
/* .custom-calendar :deep(.fc-daygrid-day) {
  min-height: 120px !important;
} */

/* 事件文字樣式 */
.custom-calendar :deep(.fc-event-title) {
  font-size: 13px !important;
  line-height: 1.4 !important;
  padding: 2px 4px !important;
  white-space: normal !important;
  overflow: visible !important;
}

/* 事件容器 */
.custom-calendar :deep(.fc-event-main) {
  padding: 1px !important;
}
</style>
