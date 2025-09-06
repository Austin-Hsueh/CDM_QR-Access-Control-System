<template>
  <!-- Ipt、Btn Group -->
  <div class="d-flex mb-2 pl-2 gap-3">
    <el-input
      style="width: 240px"
      :placeholder="t('NameFilter')"
      clearable
      v-model="searchText"
      :prefix-icon="Filter"
      @input="onFilterInputed"
    />
    <el-select
      v-model="selectedRole"
      placeholder="選擇角色"
      style="width: 120px"
      @change="onRoleChange"
    >
      <el-option label="全部" :value="0" />
      <el-option label="管理者" :value="1" />
      <el-option label="老師" :value="2" />
      <el-option label="學生" :value="3" />
      <el-option label="值班人員" :value="4" />
    </el-select>
    <el-select
      v-model="selectedType"
      placeholder="選擇門禁種類"
      style="width: 120px"
      @change="onTypeChange"
    >
      <el-option label="全部" :value="0" />
      <el-option label="上課" :value="1" />
      <el-option label="租借教室" :value="2" />
    </el-select>
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
          </template>
        </el-table-column>
        <el-table-column width="200" sortable :label="t('username')"  prop="username" />
        <el-table-column width="200" sortable :label="t('displayName')" prop="displayName" />
        <el-table-column align="left" label="簽到表" :header-align="'left'">
          <template #default="scope">
            <div v-if="selectedRole === 3" style="display: flex; flex-wrap: wrap; gap: 4px; justify-content: left;">
              <el-button v-for="permission in scope.row.studentPermissions" :key="permission.id" type="warning" size="small" @click="onView(permission, permission.id)"><el-icon><Document /></el-icon>{{ formatCourse(permission.courseId) }}</el-button>
            </div>
            <div v-else style="display: flex; align-items: center; justify-content: left; height: 100%;">
              <p style="color: #999999; margin: 0; text-align: center;">僅學生有簽到表</p>
            </div>
          </template>
        </el-table-column>
        <el-table-column width="120" sortable :label="t('Number of Settings')">
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
  <el-dialog class="dialog"  v-model="isShowEditRoleDialog" :title="t('edit')">
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

  <!-- 簽到彈窗 -->
  <DialogAttendance :key="StudentpermissionId" :visible="isShowAttendanceDialog" :studentpermission-id="StudentpermissionId" @close="handleCloseDialog" />
  <!-- /簽到彈窗 -->
</template>

<script setup lang="ts">

import { ref, onMounted, reactive, defineProps, defineExpose, computed } from "vue";
import { useI18n } from "vue-i18n";
import API from '@/apis/TPSAPI';
import { M_IUsers } from "@/models/M_IUser";
import {M_IsettingAccessRuleForm} from "@/models/M_IsettingAccessRuleForm";
import {M_IUsersContent_MTI} from "@/models/M_IUserList_MTI";
import { ComponentSize, FormInstance, FormRules, ElNotification, NotificationParams, ElMessageBox  } from 'element-plus';
import {formatDate, formatTime} from "@/plugins/dateUtils";
import SearchPaginationRequest from "@/models/M_ISearchPaginationRequest";
import DialogAttendance from "@/components/DialogAttendance.vue";

const isShowEditRoleDialog = ref(false);
const isShowAttendanceDialog = ref(false);

const handleCloseDialog = () => {
  isShowAttendanceDialog.value = false;
};

const { t } = useI18n();
const userInfo = ref<M_IUsers[]>([]); // Specify the type of the array
const userInfoMTI = ref<M_IUsersContent_MTI | null>(null); // Specify the type of the array
const size = ref<ComponentSize>('default')
const searchText = ref('')
const selectedRole = ref(3) // 預設選擇學生
const selectedType = ref(0) // 預設選擇全部
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
  Page: 1,
  Role: 3,
  type:0
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

const onRoleChange = () => {
  console.log("Role Changed:", selectedRole.value);
  searchPagination.value.Role = selectedRole.value;
  getStudentPermissions();
}

const onTypeChange = () => {
  console.log("Type Changed:", selectedType.value);
  searchPagination.value.type = selectedType.value;
  getStudentPermissions();
}

const onEdit = (item: any, userId: number, displayName: string) => {
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

const onDelet = async(item: any, userId: number, displayName: string) => {
 
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

const StudentpermissionId = ref<number | null>(null);

const onView = async (item: any, id: number) => {
  StudentpermissionId.value = id;
  isShowAttendanceDialog.value = true;
}

//#endregion

// 將函式傳到父組件
defineExpose({
  onFilterInputed,
});


//#region Hook functions
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
    console.log("拿userInfoMTI")
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
  64: '小朝',
  65: 'Car',
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

const attendanceTypeMap = {
  0: '曠課',
  1: '出席',
  2: '請假'
} as const;

// 定義標籤顏色映射
const attendanceTagTypeMap = {
  0: 'danger',   // 缺席 - 紅色
  1: 'success',  // 出席 - 綠色
  2: 'warning'   // 請假 - 黃色
} as const;

// 獲取出席類型文字
const getAttendanceTypeText = (type: number): string => {
  return attendanceTypeMap[type as keyof typeof attendanceTypeMap] || '未知';
};

// 獲取標籤類型
const getAttendanceTagType = (type: number): string => {
  return attendanceTagTypeMap[type as keyof typeof attendanceTagTypeMap] || 'info';
};

const Info = ref([]);

</script>

<style scoped>
.course-attendance-container {
  padding: 20px;
}

.course-summary {
  margin-bottom: 20px;
  padding: 16px;
  background-color: #f8f9fa;
  border-radius: 8px;
  border-left: 4px solid #409eff;
}

.summary-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 16px;
}

.summary-item {
  display: flex;
  align-items: center;
  gap: 8px;
}

.label {
  font-weight: 600;
  color: #606266;
  min-width: 80px;
}

.attendance-cell {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.attendance-tag {
  font-size: 13px;
}

.trigger-tag {
  align-self: flex-start;
  font-size: 11px;
}

.progress-cell {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4px;
}

.progress-text {
  font-size: 12px;
  font-weight: 600;
}

.course-stats {
  margin-top: 20px;
  padding: 16px;
  background-color: #f8f9fa;
  border-radius: 8px;
}

.course-stats h4 {
  margin: 0 0 12px 0;
  color: #303133;
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
  gap: 16px;
}

.stat-item {
  display: flex;
  align-items: center;
  gap: 8px;
}

.stat-label {
  font-weight: 500;
  color: #606266;
}

.stat-value {
  font-weight: 600;
  font-size: 16px;
}

.stat-value.success {
  color: #67c23a;
}

.stat-value.warning {
  color: #e6a23c;
}

.stat-value.danger {
  color: #f56c6c;
}

.operateBtnGroup {
  display: flex;
  gap: 8px;
}
</style>
