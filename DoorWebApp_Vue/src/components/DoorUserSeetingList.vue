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
      <el-table name="userInfoMTITable" style="width: 100%" height="400" :data="userInfoMTI?.pageItems" >
        <el-table-column type="expand">
          <template #default="props">
            <div class="m-2">
              <el-table :data="props.row.studentPermissions"  border="true" :header-cell-style="{ backgroundColor: '#F2F2F2' }">
                <el-table-column label="開始日期" prop="datefrom"  align="center"/>
                <el-table-column label="結束日期" prop="dateto"  align="center"/>
                <el-table-column label="時間" align="center">
                  <template #default="scope">
                    {{ scope.row.timefrom }} ~ {{ scope.row.timeto }}
                  </template>
                </el-table-column>
                <el-table-column label="週期" align="center">
                  <template #default="scope">
                    {{ formatDays(scope.row.days) }}
                  </template>
                </el-table-column>
                <el-table-column label="門組" align="center">
                  <template #default="scope">
                    {{ formatDoors(scope.row.groupIds) }}
                  </template>
                </el-table-column>
                <el-table-column width="160px" align="center" prop="operate" class="operateBtnGroup d-flex" :label="t('operation')">
                  <template v-slot="{ row }: { row: any }">
                    <el-button type="primary" size="small" @click="onEdit(row, props.row.userId)"><el-icon><EditPen /></el-icon>編輯</el-button>
                    <!-- <el-button type="danger" size="small">
                      <el-icon><Delete /></el-icon> 刪除
                    </el-button> -->
                  </template>
                </el-table-column>
              </el-table>
            </div>
          </template>
        </el-table-column>
        <el-table-column sortable :label="t('username')"  prop="username" />
        <el-table-column sortable :label="t('displayName')" prop="displayName" />
        <el-table-column sortable label="設定筆數">
          <template #default="scope">
            {{ scope.row.studentPermissions.length }}
          </template>
        </el-table-column>
      </el-table>
    </el-col>
  </el-row>
  <!-- /table 多時段-->

  <!-- table 多時段 樣式2-->
  <!-- <el-row v-if="false">
    <el-col :span="24">
      <el-table name="userInfoMTITable" style="width: 100%" height="400" :data="flattenedData">
        <el-table-column sortable :label="t('username')" prop="username" />
        <el-table-column sortable :label="t('displayName')" prop="displayName" />
        <el-table-column label="開始日期" prop="datefrom" />
        <el-table-column label="結束日期" prop="dateto" />
        <el-table-column label="時間">
          <template #default="scope">
            {{ scope.row.timefrom }} ~ {{ scope.row.timeto }}
          </template>
        </el-table-column>
        <el-table-column label="週期">
          <template #default="scope">
            {{ formatDays(scope.row.days) }}
          </template>
        </el-table-column>
        <el-table-column label="門組">
          <template #default="scope">
            {{ formatDoors(scope.row.groupIds) }}
          </template>
        </el-table-column>
        <el-table-column width="150px" align="center" prop="operate" class="operateBtnGroup d-flex" :label="t('operation')">
          <template v-slot="{ row }: { row: any }">
            <el-button type="primary" @click="onEdit(row)"><el-icon><EditPen /></el-icon></el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-col>
  </el-row> -->
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
import type { ComponentSize, FormInstance, FormRules, ElMessage } from 'element-plus';
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
  userId: 0,
  datepicker: [], // 修改为 Date 对象数组
  timepicker: [],
  days: [],
  groupIds: [],
  datefrom: '',
  dateto: '',
  timefrom:'',
  timeto: '',

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

const onEdit = (item, userId) => {
  isShowEditRoleDialog.value = true;
  console.log(item)
  updateRoleForm.value?.resetFields();

    // 格式化时间
  const timeFrom = parseTime(item.timefrom);
  const timeTo = parseTime(item.timeto);

  updateFormData.userId = userId;
  updateFormData.groupIds = item.groupIds;
  updateFormData.datepicker = [item.datefrom, item.dateto];
  updateFormData.timepicker = [timeFrom, timeTo];

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


const submitUpdateForm = async () => {
  console.log('Form submitted', updateFormData);

  // 格式化日期为 YYYY-MM-DD
  updateFormData.datefrom = formatDate(new Date(updateFormData.datepicker[0]));
  updateFormData.dateto = formatDate(new Date(updateFormData.datepicker[1]));

  // 格式化时间为 HH:MM
  updateFormData.timefrom = formatTime(new Date(updateFormData.timepicker[0]));
  updateFormData.timeto = formatTime(new Date(updateFormData.timepicker[1]));
  
  console.log(updateFormData);

  
  try {
    const settingResponse = await API.patchStudentPermission(updateFormData);
    
    // 根據需要處理 settingResponse
    console.log('Permission set successfully', settingResponse);
    console.log('Permission set successfully', settingResponse.data);

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


// 表格樣式2 使用
// const flattenedData = computed(() => {
//   return userInfoMTI.value?.pageItems.flatMap(user => 
//     user.studentPermissions.map(permission => ({
//       username: user.username,
//       displayName: user.displayName,
//       ...permission
//     }))
//   );
// });

</script>

<style scoped></style>
