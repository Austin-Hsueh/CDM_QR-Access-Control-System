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

  <!-- table -->
  <el-row>
    <el-col :span="24">
      <el-table name="userInfoTable" style="width: 100%" height="400" :data="userInfo">
        <el-table-column sortable :label="t('username')"  prop="username" />
        <el-table-column sortable :label="t('displayName')" prop="displayName" />
        <el-table-column sortable :label="t('Permissions')" prop="groupNames"/>
        <el-table-column sortable :label="t('releaseTime')">
          <template v-slot="scope">
            {{ scope.row.accessDays }}
            <br>
            {{ scope.row.accessTime }}
          </template>
        </el-table-column>
        <el-table-column width="150px" align="center" prop="operate" class="operateBtnGroup d-flex" :label="t('operation')">
          <template v-slot="{ row }: { row: any }">
            <el-button type="primary" @click="onEdit(row)"><el-icon><EditPen /></el-icon></el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-col>
  </el-row>
  <!-- /table -->

  <!-- pagination -->
   <!-- 20240731 上線測試, 暫時排除未完分頁功能 by Austin -->
  <el-row justify="end" class="mt-3" v-if="false">
    <el-col>
      <div class="demo-pagination-block mt-3 d-flex justify-content-end">
        <el-pagination
          v-model:current-page="currentPage4"
          v-model:page-size="pageSize4"
          :page-sizes="[100, 200, 300, 400]"
          :size="size"
          layout="total, sizes, prev, pager, next, jumper"
          :total="400"
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

import { ref, onMounted, onActivated, reactive, defineProps, defineExpose } from "vue";
import { useI18n } from "vue-i18n";
import API from '@/apis/TPSAPI';
import { M_IUsers } from "@/models/M_IUser";
import {M_IsettingAccessRuleForm} from "@/models/M_IsettingAccessRuleForm";
import type { ComponentSize, FormInstance, FormRules, ElMessage } from 'element-plus';
import {formatDate, formatTime} from "@/plugins/dateUtils";

const isShowEditRoleDialog = ref(false);

const { t } = useI18n();
const userInfo = ref<M_IUsers[]>([]); // Specify the type of the array
const currentPage4 = ref(4)
const pageSize4 = ref(100)
const size = ref<ComponentSize>('default')
const searchText = ref('')
const doors = [1,2,3,4];
const days = [1,2,3,4,5,6,7];

// 定义 props
const props = defineProps<{
  doorId: number
}>();



const handleSizeChange = (val: number) => {
  console.log(`${val} items per page`)
}
const handleCurrentChange = (val: number) => {
  console.log(`current page: ${val}`)
}

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
    getUsers();
  }else{
    setTimeout(()=>{
      console.log(searchText.value)
      const filteredData = userInfo.value.filter(item => {
        const matchesIp = item.displayName.includes(searchText.value);
        return matchesIp;
      });
      console.log(filteredData)
      userInfo.value = filteredData;
    }, 500);
  }
}

const onEdit = (item: M_IUsers) => {
  updateRoleForm.value?.resetFields();

    // 格式化时间
  const timeFrom = parseTime(item.timefrom);
  const timeTo = parseTime(item.timeto);

  updateFormData.userId = item.userId;
  updateFormData.groupIds = item.groupIds;
  updateFormData.datepicker = [item.datefrom, item.dateto];
  updateFormData.timepicker = [timeFrom, timeTo];
  updateFormData.days = item.days.split(',').map(Number);
  isShowEditRoleDialog.value = true;
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
    const settingResponse = await API.setPermission(updateFormData);
    
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
  onFilterInputed
});


//#region Hook functions
// onActivated(() => {
  
// });
onMounted(() => {
  getUsers();
  console.log(`Received door ID: ${props.doorId}`);
});

//#endregion

//#region Private Functions
async function getUsers() {
  try {
    const getUsersResult = await API.getAllUsers();
    if (getUsersResult.data.result != 1) throw new Error(getUsersResult.data.msg);
    userInfo.value = getUsersResult.data.content;

  } catch (error) {
    console.error(error);
  }
}
//#endregion


</script>

<style scoped></style>
