<template>
<FullCalendar
  ref="fullCalendar"
  :options="calendarOptions"
/>

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
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue';
import { CalendarOptions } from '@fullcalendar/core';
import resourceTimeGridPlugin from '@fullcalendar/resource-timegrid';
import FullCalendar from '@fullcalendar/vue3'
import dayGridPlugin from '@fullcalendar/daygrid'
import timeGridPlugin from '@fullcalendar/timegrid'
import interactionPlugin from '@fullcalendar/interaction'
import { FormInstance, FormRules, ElMessage } from 'element-plus';

import API from '@/apis/TPSAPI';

import { M_IClassRoomOptions } from '@/models/M_IClassRoomOptions';
import { M_IUsersOptions } from '@/models/M_IUsersOptions';
import { M_ICourseOptions } from '@/models/M_ICourseOptions';
import { M_ITeachersOptions } from '@/models/M_ITeachersOptions';


const classRoomList = ref<M_IClassRoomOptions[]>([]);
const fullCalendar = ref<any>(null);

// 教室資源 (從 API 動態載入)
const resources = ref<any[]>([]);

// Dialog 控制
const isShowAddCourseDialog = ref(false);
const addCourseFormRef = ref<FormInstance>();

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
          courseId: addCourseFormData.courseId ? Number(addCourseFormData.courseId) : null,
          teacherId: addCourseFormData.teacherId ? Number(addCourseFormData.teacherId) : null,
          datefrom: formatDate(addCourseFormData.datepicker[0]),
          dateto: formatDate(addCourseFormData.datepicker[1]),
          timefrom: formatTime(addCourseFormData.timepicker[0]),
          timeto: formatTime(addCourseFormData.timepicker[1]),
          type: Number(addCourseFormData.type),
          days: addCourseFormData.days,
          groupIds: addCourseFormData.groupIds,
          classroomId: Number(addCourseFormData.classroomId),
          courseMode: addCourseFormData.courseMode ? Number(addCourseFormData.courseMode) : null,
          scheduleMode: Number(addCourseFormData.scheduleMode),
          remark: addCourseFormData.remark || ''
        };

        // 呼叫 API 儲存課程資料
        const response = await API.createCourseSchedule(courseData);

        if (response.data.result === 1) {
          // 成功後重新載入課程
          await loadTodayCourses();

          // 清除選擇並關閉 Dialog
          const calendarApi = addCourseFormData.selectInfo?.view.calendar;
          if (calendarApi) {
            calendarApi.unselect();
          }
          isShowAddCourseDialog.value = false;
          addCourseFormRef.value?.resetFields();

          // 顯示成功訊息
          console.log('課程新增成功');
        } else {
          console.error('課程新增失敗:', response.data.msg);
          alert(`課程新增失敗: ${response.data.msg}`);
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
  const studentName = event.extendedProps.studentName || '';
  const courseName = event.extendedProps.courseName || '';
  const teacherName = event.extendedProps.teacherName || '';
  const startTime = event.start;
  const endTime = event.end;
  const resourceTitle = event.getResources()[0]?.title || '';

  // 格式化時間顯示
  const formatDateTime = (date: Date) => {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    return `${year}-${month}-${day} ${hours}:${minutes}`;
  };

  // 顯示完整課程資訊
  const message = `
學生: ${studentName}
課程名稱: ${courseName}
老師: ${teacherName}
教室: ${resourceTitle}
時間: ${formatDateTime(startTime)} ~ ${formatDateTime(endTime)}

確定要刪除此課程嗎？
  `;

  if (confirm(message)) {
    try {
      // 驗證 scheduleId
      if (!scheduleId) {
        ElMessage.error('找不到課程 ID，無法刪除');
        return;
      }

      // 準備 API 請求資料 (patch 方法直接傳遞資料物件)
      const cmd = {
        scheduleId: scheduleId,
        isDelete: true
      };

      console.log('刪除課程排程:', cmd);

      // 呼叫 API 刪除 (使用 patch 方法)
      const response = await API.deleteCourseSchedule(cmd);

      if (response.data.result === 1) {
        // 從日曆中移除事件
        event.remove();
        ElMessage.success('課程已刪除');
      } else {
        ElMessage.error(response.data.msg || '刪除課程失敗');
      }
    } catch (error) {
      console.error('刪除課程排程失敗:', error);
      ElMessage.error('刪除課程失敗，請稍後再試');
    }
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
      searchPage: 250, // 增加數量以涵蓋週視圖
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

      // 將每個課程加入 Calendar
      response.data.content.pageItems.forEach((schedule: any) => {
        // 組合日期和時間
        const scheduleDate = schedule.scheduleDate.replace(/\//g, '-'); // "2025/10/08" -> "2025-10-08"
        const startDateTime = `${scheduleDate}T${schedule.startTime}:00`;
        const endDateTime = `${scheduleDate}T${schedule.endTime}:00`;

        // 組合標題
        const titleParts = [];
        if (schedule.studentName) titleParts.push(`學生：${schedule.studentName}`);
        if (schedule.courseName) titleParts.push(`課程：${schedule.courseName}`);
        if (schedule.teacherName) titleParts.push(`老師：${schedule.teacherName}`);
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
            teacherName: schedule.teacherName
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

    const scheduleDate = startDate.toISOString().split('T')[0]; // "2024-02-15"
    const startTime = `${String(startDate.getHours()).padStart(2, '0')}:${String(startDate.getMinutes()).padStart(2, '0')}`; // "15:00"
    const endTime = `${String(endDate.getHours()).padStart(2, '0')}:${String(endDate.getMinutes()).padStart(2, '0')}`; // "17:00"

    // 準備 API 請求資料
    const cmd = {
      scheduleId: scheduleId,
      classroomId: Number(newResource.id),
      scheduleDate: scheduleDate,
      startTime: startTime,
      endTime: endTime,
      courseMode: event.extendedProps.courseMode || 1,
      status: event.extendedProps.status || 1,
      remark: event.extendedProps.remark || '',
      isDelete: false
    };

    console.log('更新課程排程:', cmd);

    // 呼叫 API 更新
    const response = await API.updateCourseSchedule(cmd);

    if (response.data.result === 1) {
      ElMessage.success(`課程已移動至 ${newResource.title} ${scheduleDate} ${startTime}-${endTime}`);
    } else {
      ElMessage.error(response.data.msg || '更新課程失敗');
      dropInfo.revert(); // 還原拖曳
    }
  } catch (error) {
    console.error('更新課程排程失敗:', error);
    ElMessage.error('更新課程失敗，請稍後再試');
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

    const scheduleDate = startDate.toISOString().split('T')[0]; // "2024-02-15"
    const startTime = `${String(startDate.getHours()).padStart(2, '0')}:${String(startDate.getMinutes()).padStart(2, '0')}`; // "15:00"
    const endTime = `${String(endDate.getHours()).padStart(2, '0')}:${String(endDate.getMinutes()).padStart(2, '0')}`; // "17:00"

    // 準備 API 請求資料
    const cmd = {
      scheduleId: scheduleId,
      classroomId: Number(resource.id),
      scheduleDate: scheduleDate,
      startTime: startTime,
      endTime: endTime,
      courseMode: event.extendedProps.courseMode || 1,
      status: event.extendedProps.status || 1,
      remark: event.extendedProps.remark || '',
      isDelete: false
    };

    console.log('更新課程時間:', cmd);

    // 呼叫 API 更新
    const response = await API.updateCourseSchedule(cmd);

    if (response.data.result === 1) {
      ElMessage.success(`課程時間已調整為 ${startTime}-${endTime}`);
    } else {
      ElMessage.error(response.data.msg || '更新課程失敗');
      resizeInfo.revert(); // 還原調整
    }
  } catch (error) {
    console.error('更新課程時間失敗:', error);
    ElMessage.error('更新課程失敗，請稍後再試');
    resizeInfo.revert(); // 還原調整
  }
};

// 自訂事件顯示內容
const eventContent = (arg: any) => {
  const studentName = arg.event.extendedProps.studentName || '';
  const courseName = arg.event.extendedProps.courseName || '';
  const teacherName = arg.event.extendedProps.teacherName || '';

  // 建立容器（使用 flexbox 兩欄佈局）
  const container = document.createElement('div');
  container.style.display = 'flex';
  container.style.height = '100%';
  container.style.overflow = 'hidden';

  // 取得事件的原始背景色
  const eventBgColor = arg.backgroundColor || arg.event.backgroundColor || '#3788d8';

  // 計算對比色（互補色）
  const getComplementaryColor = (hex: string) => {
    const r = parseInt(hex.slice(1, 3), 16);
    const g = parseInt(hex.slice(3, 5), 16);
    const b = parseInt(hex.slice(5, 7), 16);

    // 計算互補色（255 - RGB 值）
    const compR = (255 - r).toString(16).padStart(2, '0');
    const compG = (255 - g).toString(16).padStart(2, '0');
    const compB = (255 - b).toString(16).padStart(2, '0');

    return `#${compR}${compG}${compB}`;
  };

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
    courseLine.textContent = `課程：${courseName}`;
    column1.appendChild(courseLine);
  }

  container.appendChild(column1);

  // 第二欄：課程名稱和老師名稱（使用對比色）
  const column2 = document.createElement('div');
  column2.style.fontWeight = 'bold';
  column2.style.flex = '1';
  column2.style.padding = '4px';
  column2.style.backgroundColor = getComplementaryColor(eventBgColor);
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
    end: 'resourceTimeGridDay,resourceTimeGridWeek'
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
  slotMaxTime: '23:00:00',
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
  // loadTodayCourses(); // 已改用 handleDatesSet 自動載入
  getUsersOptions();
  getCourseOptions();
});

//#endregion

//#region Private Functions
async function getClassRoomsOptions() {
  try {
    const response = await API.getClassrooms();
    classRoomList.value = response.data.content;

    // 轉換為 FullCalendar resources 格式
    resources.value = response.data.content.map((room: M_IClassRoomOptions) => ({
      id: room.classroomId?.toString() || '',
      title: room.classroomName || '',
      eventColor: room.description || '#409eff'
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

// 載入當日所有課程
async function loadTodayCourses() {
  try {
    // 取得當前日期 (格式: "2025-10-08")
    const today = new Date().toISOString().split('T')[0];

    // 準備 API 請求參數
    const cmd = {
      page: 1,
      searchPage: 20,
      dateFrom: today,
      dateTo: today,
      status: 1
    };

    const response = await API.getCoursesByDate(cmd);

    if (response.data.content?.pageItems && Array.isArray(response.data.content.pageItems)) {
      const calendarApi = fullCalendar.value?.getApi();
      if (!calendarApi) return;

      // 清除現有事件（可選）
      // calendarApi.removeAllEvents();

      // 將每個課程加入 Calendar
      response.data.content.pageItems.forEach((schedule: any) => {
        // 組合日期和時間
        const scheduleDate = schedule.scheduleDate.replace(/\//g, '-'); // "2025/10/08" -> "2025-10-08"
        const startDateTime = `${scheduleDate}T${schedule.startTime}:00`;
        const endDateTime = `${scheduleDate}T${schedule.endTime}:00`;

        // 組合標題
        const titleParts = [];
        if (schedule.studentName) titleParts.push(`學生：${schedule.studentName}`);
        if (schedule.courseName) titleParts.push(`課程：${schedule.courseName}`);
        if (schedule.teacherName) titleParts.push(`老師：${schedule.teacherName}`);
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
            teacherName: schedule.teacherName
          }
        });
      });

      console.log('載入的課程數量:', response.data.content.pageItems.length);
    }
  } catch (error) {
    console.error('載入課程資料失敗:', error);
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

</style>
