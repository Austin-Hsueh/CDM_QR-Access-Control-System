<template>
  <!-- Ipt、Btn Group -->
  <div class="d-flex mb-2 pl-2">
    <el-input
      class="me-auto"
      style="width: 240px"
      :placeholder="t('NameFilter')"
      clearable
      v-model="searchText"
      :prefix-icon="Filter"
      @input="onFilterInputed"
    />
  </div>

  <!-- table 多時段 樣式1-->
  <el-row>
    <el-col :span="24">
      <el-table name="userInfoMTITable" style="width: 100%" height="800" :data="userInfoMTI?.pageItems" >
        <el-table-column type="expand">
          <template #default="props">
            <div class="m-3" style="margin-bottom: 3rem !important;">
              <h4 style="font-weight: bold;text-decoration: underline;text-align: center;">通行資料</h4><br>
              <el-table :data="props.row.studentPermissions"  border="true" :header-cell-style="{ backgroundColor: '#F2F2F2' }">
                <el-table-column :label="t('Start Date')" prop="datefrom"  align="center"/>
                <el-table-column :label="t('End Date')" prop="dateto"  align="center"/>
                <el-table-column :label="t('Time')" align="center">
                  <template #default="scope">
                    {{ scope.row.timefrom }} ~ {{ scope.row.timeto }}
                  </template>
                </el-table-column>
                <el-table-column :label="t('Period')" align="center">
                  <template #default="scope">
                    {{ formatDays(scope.row.days) }}
                  </template>
                </el-table-column>
                <el-table-column :label="t('Door Group')" align="center">
                  <template #default="scope">
                    {{ formatDoors(scope.row.groupIds) }}
                  </template>
                </el-table-column>
                <el-table-column label="課程類別" align="center">
                  <template  #default="scope">
                    {{ formatCourse(scope.row.courseId) }}
                  </template>
                </el-table-column>
                <el-table-column label="老師" align="center">
                  <template #default="scope">
                    {{ teacherMap[scope.row.teacherId] || '未知' }}
                  </template>
                </el-table-column>
                <el-table-column width="170px" align="center" prop="operate" class="operateBtnGroup d-flex" :label="t('operation')">
                  <template v-slot="{ row }: { row: any }">
                    <el-button type="primary" size="small" @click="onEdit(row, props.row.userId, props.row.displayName)"><el-icon><EditPen /></el-icon>{{ t('edit') }}</el-button>
                    <el-button type="danger" size="small" @click="onDelet(row, props.row.userId, props.row.displayName)">
                      <el-icon><Delete /></el-icon> {{ t('delete') }}
                    </el-button>
                  </template>
                </el-table-column>
              </el-table>
            </div>
            <div class="m-3" style="margin-bottom: 3rem !important;">
              <h4 style="font-weight: bold;text-decoration: underline;text-align: center;">學生簽到表</h4><br>
              <el-table name="userInfoTable" style="width: 100%" :data="Info" border="true" :header-cell-style="{ backgroundColor: '#F2F2F2' }">
                <el-table-column :label="t('Term')" prop="term" />
                <el-table-column :label="t('PaymentDate')" prop="paymentDate" />
                <el-table-column :label="t('PaymentStamp')" prop="paymentStamp" />
                <el-table-column :label="t('AttendanceFirst')" prop="attendanceFirst" />
                <el-table-column :label="t('AttendanceSecond')" prop="attendanceSecond" />
                <el-table-column :label="t('AttendanceThird')" prop="attendanceThird" />
                <el-table-column :label="t('AttendanceFourth')" prop="attendanceFourth" />
                <el-table-column :label="t('AbsenceRecord')" prop="absenceRecord" />
                <el-table-column :label="t('CourseDeadline')" prop="courseDeadline" />
                <el-table-column width="170px" align="center" prop="operate" class="operateBtnGroup d-flex" :label="t('operation')">
                  <template #default="{ row }: { row: any }">
                    <el-button type="primary" size="small" @click="onEdit(row)"><el-icon><EditPen /></el-icon>{{ t('edit') }}</el-button>
                  </template>
                </el-table-column>
              </el-table>
            </div>
          </template>
        </el-table-column>
        <el-table-column sortable :label="t('username')"  prop="username" />
        <el-table-column sortable :label="t('displayName')" prop="displayName" />
        <el-table-column sortable :label="t('Number of Settings')">
          <template #default="scope">
            {{ scope.row.studentPermissions.length }}
          </template>
        </el-table-column>
      </el-table>
    </el-col>
  </el-row>
  <!-- /table 多時段-->

  <!-- pagination -->
   <!-- 20240731 上線測試, 暫時排除未完分頁功能 by Austin -->
  <el-row justify="end" class="mt-3">
    <el-col>
      <div class="demo-pagination-block mt-3 d-flex justify-content-end">
        <el-pagination
          v-model:current-page="searchPagination.Page"
          v-model:page-size="searchPagination.SearchPage"
          :page-sizes="[10, 50, 100]"
          :size="size"
          layout="total, sizes, prev, pager, next, jumper"
          :total= "userInfoMTI?.totalItems"
          @size-change="handleSizeChange"
          @current-change="handleCurrentChange"
          justify="end"
        />
      </div>
    </el-col>
  </el-row>
  <!-- /pagination -->

  <!-- 編輯彈窗 -->
  <el-dialog class="dialog" top="3vh" v-model="isShowEditRoleDialog" :title="t('edit')">
    <el-form label-width="100px"  ref="updateRoleForm" :rules="rules" :model="updateFormData" label-position="top">
      <el-form-item label="門禁權限" prop="groupIds">
        <el-checkbox-group v-model="updateFormData.groupIds">
          <el-checkbox v-for="item in doors" :key="item" :label="item" :value="item">
            <template v-if="item === 1">大門</template>
            <template v-else-if="item === 2">car教室</template>
            <template v-else-if="item === 3">Sunny教室</template>
            <template v-else-if="item === 4">儲藏室</template>
          </el-checkbox>
        </el-checkbox-group>
      </el-form-item>
      <el-form-item label="通行日期" prop="datepicker"  >
        <el-date-picker
          type="daterange"
          range-separator="至"
          start-placeholder="開始日期"
          end-placeholder="結束日期"
          v-model="updateFormData.datepicker"
        />
      </el-form-item>
      <el-form-item label="通行時間" prop="timepicker" >
        <el-time-picker
          is-range
          range-separator="至"
          start-placeholder="開始時間"
          end-placeholder="結束時間"
          v-model="updateFormData.timepicker"
        />
      </el-form-item>
      <el-form-item label="週期" prop="days" >
        <el-checkbox-group v-model="updateFormData.days">
          <el-checkbox v-for="item in days" :key="item" :label="item" :value="item">
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
    </el-form>
    <template #footer>
      <span class="dialog-footer">
        <el-button @click="isShowEditRoleDialog = false">{{ t("Cancel") }}</el-button>
        <el-button type="primary" @click="submitUpdateForm">{{ t("Confirm") }}</el-button>
      </span>
    </template>
  </el-dialog>
  <!-- /編輯彈窗 -->
</template>

<script setup lang="ts">

import { computed, ref, onMounted, onActivated, reactive, defineProps, defineExpose } from "vue";
import { useI18n } from "vue-i18n";
import API from '@/apis/TPSAPI';
import { M_IUsers } from "@/models/M_IUser";
import {M_IsettingAccessRuleForm} from "@/models/M_IsettingAccessRuleForm";
import {M_IUserList_MTI, M_IUsersContent_MTI} from "@/models/M_IUserList_MTI";
import { ComponentSize, FormInstance, FormRules, ElNotification, NotificationParams, ElMessageBox  } from 'element-plus';
import {formatDate, formatTime} from "@/plugins/dateUtils";
import SearchPaginationRequest from "@/models/M_ISearchPaginationRequest";

const isShowEditRoleDialog = ref(false);

const { t } = useI18n();
const userInfo = ref<M_IUsers[]>([]); // Specify the type of the array
const userInfoMTI = ref<M_IUsersContent_MTI | null>(null); // Specify the type of the array
const size = ref<ComponentSize>('default')
const searchText = ref('')
const doors = [1,2,3,4];
const days = [1,2,3,4,5,6,7];

// 定义 props
const props = defineProps<{
  doorId: number
}>();



const handleSizeChange = async(val: number) => {
  console.log(`${val} items per page`)
  searchPagination.value.SearchPage = val;
  await getStudentPermissions();
}

const handleCurrentChange = async(val: number) => {
  console.log(`current page: ${val}`)
  searchPagination.value.Page = val;
  await getStudentPermissions();
}

const searchPagination = ref<SearchPaginationRequest>({
  SearchText: "",
  SearchPage: 10,
  Page: 1
});

const parseTime = (time: string): Date => {
  const [hours, minutes] = time.split(':').map(Number);
  const date = new Date();
  date.setHours(hours, minutes, 0, 0);
  return date;
};

//#region 建立表單ref與Validator
// 編輯門禁設定表單
const updateRoleForm = ref<FormInstance>()
const updateFormData = reactive<M_IsettingAccessRuleForm>({
  Id:0,
  userId: 0,
  datepicker: [], // 修改为 Date 对象数组
  timepicker: [],
  days: [],
  groupIds: [],
  datefrom: '',
  dateto: '',
  timefrom:'',
  timeto: '',
  displayName: ''

})
//#endregion


//#region UI Events
const onFilterInputed = () => {
  console.log("Search Function");
  if(!searchText.value || searchText.value.trim() === ''){
    getStudentPermissions();
  }else{
    setTimeout(()=>{
      console.log(searchText.value)
      searchPagination.value.SearchText = searchText.value
      console.log(searchPagination.value);
      getStudentPermissions();
    }, 500);
  }
}

const onEdit = (item, userId, displayName) => {
  isShowEditRoleDialog.value = true;
  console.log(item)
  updateRoleForm.value?.resetFields();

    // 格式化时间
  const timeFrom = parseTime(item.timefrom);
  const timeTo = parseTime(item.timeto);
  
  updateFormData.Id = item.id;
  updateFormData.userId = userId;
  updateFormData.groupIds = item.groupIds;
  updateFormData.datepicker = [item.datefrom, item.dateto];
  updateFormData.timepicker = [timeFrom, timeTo];
  updateFormData.displayName = displayName;
  updateFormData.courseId = item.courseId;
  updateFormData.teacherId = item.teacherId;


  // 檢查 item.days 是否為字符串
  if (typeof item.days === 'string') {
    updateFormData.days = item.days.split(',').map(Number);
  } else if (Array.isArray(item.days)) {
    // 如果 item.days 已經是數組，直接賦值
    updateFormData.days = item.days.map(Number);
  } else {
    // 處理其他可能的類型或情況
    console.warn('item.days 的類型不符合預期:', typeof item.days);
    updateFormData.days = [];
  }
  console.log(updateFormData)
}

const onDelet = async(item, userId, displayName) => {
 
 try {
    await ElMessageBox.confirm("確定刪除?", "提示", {
      confirmButtonText: "確定",
      cancelButtonText: "取消",
      type: "warning",
    });
  } catch (error) {
    return;
  }

 const deleteFormData = reactive<M_IsettingAccessRuleForm>({
    Id:0,
    userId: 0,
    datefrom: '',
    dateto: '',
    timefrom:'',
    timeto: '',
    days: [],
    groupIds: [],
    IsDelete: false
  })

  deleteFormData.Id = item.id;
  deleteFormData.userId = userId;
  deleteFormData.groupIds = item.groupIds;
  deleteFormData.datefrom = item.datefrom;
  deleteFormData.dateto = item.dateto;

  // 格式化时间
  const timeFrom = parseTime(item.timefrom);
  const timeTo = parseTime(item.timeto);

  deleteFormData.timefrom = item.timefrom;
  deleteFormData.timeto = item.timeto;
  deleteFormData.days = item.days;
  deleteFormData.groupIds = item.groupIds;
  deleteFormData.IsDelete = true;

  console.log(deleteFormData)

 try {
    let notifyParam: NotificationParams = {};
    const settingResponse = await API.patchStudentPermission(deleteFormData);
    
    // 根據需要處理 settingResponse
    console.log('Permission set successfully', settingResponse);
    console.log('Permission set successfully', settingResponse.data);

    if (settingResponse.data.result != 1) {
        notifyParam = {
          title: "失敗",
          type: "error",
          message: `帳號：${displayName} 刪除失敗`,
          duration: 2000,
        };
      } else {
        notifyParam = {
          title: "成功",
          type: "success",
          message: `帳號：${displayName} 已成功刪除`,
          duration: 2000,
        };
      }

      ElNotification(notifyParam);
      isShowEditRoleDialog.value = false;
      onFilterInputed();

  } catch (error) {
    console.error('Failed to set permission', error);
  }
}


const submitUpdateForm = async () => {
  console.log('Form submitted', updateFormData);

  // 格式化日期为 YYYY-MM-DD
  updateFormData.datefrom = formatDate(new Date(updateFormData.datepicker[0]));
  updateFormData.dateto = formatDate(new Date(updateFormData.datepicker[1]));

  // 格式化时间为 HH:MM
  updateFormData.timefrom = formatTime(new Date(updateFormData.timepicker[0]));
  updateFormData.timeto = formatTime(new Date(updateFormData.timepicker[1]));
  
  console.log('setTime&Date',updateFormData);

  const sentFormData = reactive<M_IsettingAccessRuleForm>({
    Id:0,
    userId: 0,
    datefrom: '',
    dateto: '',
    timefrom:'',
    timeto: '',
    days: [],
    groupIds: [],
    IsDelete: false
  })

  sentFormData.Id = updateFormData.Id;
  sentFormData.userId = updateFormData.userId;
  sentFormData.datefrom = updateFormData.datefrom;
  sentFormData.dateto = updateFormData.dateto;
  sentFormData.timefrom = updateFormData.timefrom;
  sentFormData.timeto = updateFormData.timeto;
  sentFormData.days = updateFormData.days;
  sentFormData.groupIds = updateFormData.groupIds;
  sentFormData.courseId = updateFormData.courseId;
  sentFormData.teacherId = updateFormData.teacherId;

  console.log('change Data From updateFormData to sentFormData',sentFormData);

  
  try {
    let notifyParam: NotificationParams = {};
    const settingResponse = await API.patchStudentPermission(sentFormData);
    
    // 根據需要處理 settingResponse
    console.log('Permission set successfully', settingResponse);
    console.log('Permission set successfully', settingResponse.data);

    if (settingResponse.data.result != 1) {
        notifyParam = {
          title: "失敗",
          type: "error",
          message: `帳號：${updateFormData.displayName} 更新失敗`,
          duration: 2000,
        };
      } else {
        notifyParam = {
          title: "成功",
          type: "success",
          message: `帳號：${updateFormData.displayName} 已成功更新`,
          duration: 2000,
        };
      }

      ElNotification(notifyParam);
      isShowEditRoleDialog.value = false;
      onFilterInputed();

  } catch (error) {
    console.error('Failed to set permission', error);
  }
};

//#endregion

// 將函式傳到父組件
defineExpose({
  onFilterInputed,
});


//#region Hook functions
// onActivated(() => {
  
// });
onMounted(() => {

  console.log(`Received door ID: ${props.doorId}`);
  getStudentPermissions();
});

//#endregion

//#region Private Functions
// 多時段API
async function getStudentPermissions() {
  try {
    // const getStudentPermissionsResult = await API.getStudentPermissions();

    /** 取得門禁使用者清單-多時段-後端分頁 */
    const getStudentPermissionsResult = await API.getStudentPermissions(searchPagination.value);
    
    if (getStudentPermissionsResult.data.result != 1) throw new Error(getStudentPermissionsResult.data.msg);
    userInfoMTI.value = getStudentPermissionsResult.data.content;
    console.log(userInfoMTI.value)

  } catch (error) {
    console.error(error);
  }
}
//#endregion

const dayMap = {
  1: '星期一',
  2: '星期二',
  3: '星期三',
  4: '星期四',
  5: '星期五',
  6: '星期六',
  7: '星期日',
};

const formatDays = (days: number[]) => {
  return days.map(day => dayMap[day]).join(', ');
};

const doorMap = {
  1: '大門',
  2: 'Car教室',
  3: 'Sunny教室',
  4: '儲藏室'
};

const formatDoors = (groupIds: number[]) => {
  return groupIds.map(door => doorMap[door]).join(', ');
};

const courseMap = {
  1: '鋼琴',
  2: '歌唱',
  3: '吉他',
  4: '貝斯',
  5: '烏克麗麗',
  6: '創作',
  7: '管樂',
  8: '爵士鼓'
};

const formatCourse = (courseId: number): string => {
  // 這裡告訴 TypeScript，courseId 將會是 courseMap 的一個鍵。
  // (keyof typeof courseMap) 會推斷出 '1' | '2' | ... '8' 的聯合字串字面量型別
  return courseMap[courseId as keyof typeof courseMap] || '未知課程';
};

const teacherMap: { [key: number]: string } = {
  66: '丁喬',
  67: '鄭元',
  68: '學宜',
  69: '周廷',
  70: '思儂',
  71: '琪伶',
  72: '羽安',
  73: '玉璇',
  74: '建軍',
  75: '映伶',
};

const Info = ref([
    {
      id: 1,
      term: '第一期',
      paymentDate: '2025-01-15',
      paymentStamp: '已繳費',
      attendanceFirst: '出席',
      attendanceSecond: '出席',
      attendanceThird: '請假',
      attendanceFourth: '出席',
      absenceRecord: '第三堂請假',
      courseDeadline: '2025-06-30'
    },
    {
      id: 2,
      term: '第一期',
      paymentDate: '2025-01-16',
      paymentStamp: '已繳費',
      attendanceFirst: '出席',
      attendanceSecond: '缺席',
      attendanceThird: '出席',
      attendanceFourth: '出席',
      absenceRecord: '第二堂缺席',
      courseDeadline: '2025-06-30'
    },
    {
      id: 3,
      term: '第二期',
      paymentDate: '2025-04-10',
      paymentStamp: '未繳費',
      attendanceFirst: '出席',
      attendanceSecond: '出席',
      attendanceThird: '出席',
      attendanceFourth: '缺席',
      absenceRecord: '第四堂缺席',
      courseDeadline: '2025-09-15'
    },
    {
      id: 4,
      term: '第二期',
      paymentDate: '2025-04-12',
      paymentStamp: '已繳費',
      attendanceFirst: '缺席',
      attendanceSecond: '出席',
      attendanceThird: '出席',
      attendanceFourth: '出席',
      absenceRecord: '第一堂缺席',
      courseDeadline: '2025-09-15'
    },
    {
      id: 5,
      term: '第三期',
      paymentDate: '2025-07-20',
      paymentStamp: '已繳費',
      attendanceFirst: '出席',
      attendanceSecond: '出席',
      attendanceThird: '出席',
      attendanceFourth: '出席',
      absenceRecord: '無',
      courseDeadline: '2025-12-20'
    }
  ]);
</script>

<style scoped></style>
