<template>
  <!-- Date Picker -->
  <div class="d-flex mb-2 pl-2">
    <el-date-picker
      v-model="selectedDate"
      type="date"
      placeholder="查詢簽到狀態"
      format="YYYY-MM-DD"
      value-format="YYYY-MM-DD"
      style="width: 220px"
      @change="handleDateChange"
    />

    <div v-if="dailyScheduleStatus" class="ml-3 d-flex align-items-center">
      <!-- <el-tag type="info" class="mr-2">總課程：{{ dailyScheduleStatus.totalSchedules }}</el-tag>
      <el-tag type="success" class="mr-2">已簽到：{{ dailyScheduleStatus.checkedInCount }}</el-tag>
      <el-tag type="warning" class="mr-2">未簽到：{{ dailyScheduleStatus.notCheckedInCount }}</el-tag> -->

      <el-button
        v-if="!dailyScheduleStatus.canCloseAccount"
        type="primary"
        size="small"
        @click="handleCheckInAll"
        :loading="isCheckingInAll"
      >
        <el-icon><CircleCheck /></el-icon>一鍵全部簽到
      </el-button>

      <el-button
        type="primary"
        size="small"
        @click="handleCloseAccount"
        :disabled="!dailyScheduleStatus.canCloseAccount"
      >
        <el-icon><Check /></el-icon>{{ dailyScheduleStatus.canCloseAccount ? '今日關帳' : '無法關帳' }}
      </el-button>
    </div>
  </div>

  <!-- Table -->
  <el-row>
    <el-col :span="24">
      <el-table
        name="scheduleCheckTable"
        style="width: 100%"
        height="500"
        :data="dailyScheduleStatus?.scheduleStatuses"
        empty-text="請選擇日期查詢簽到狀態"
      >
        <el-table-column sortable label="學生編號" prop="username"  />
        <el-table-column sortable label="學生名稱" prop="studentName"  />
        <el-table-column sortable label="課程名稱" prop="courseName"  />
        <el-table-column sortable label="教室" prop="classroomName"  />
        <el-table-column sortable label="課程時間" >
          <template #default="scope">
            {{ scope.row.startTime }} ~ {{ scope.row.endTime }}
          </template>
        </el-table-column>
        <el-table-column sortable label="簽到狀態" prop="status" align="center">
          <template #default="scope">
            <el-tag :type="scope.row.status === '已簽到' ? 'success' : 'error'">
              {{ scope.row.status }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="簽到時間" prop="checkedInTime">
          <template #default="scope">
            {{ scope.row.checkedInTime ? new Date(scope.row.checkedInTime).toLocaleString('zh-TW') : '-' }}
          </template>
        </el-table-column>
      </el-table>
    </el-col>
  </el-row>
  <!-- /Table -->

  <!-- 關帳彈窗 -->
  <el-dialog class="dialog"  v-model="isShowAddRoleDialog" title="關帳">
    <el-form label-width="100px"  ref="" :rules="rules" :model="createFormData">
      <el-form-item label="關帳日期" prop="closeDate"  >
        <el-input style="width:90%" v-model="createFormData.closeDate" disabled/>
      </el-form-item>
      <el-form-item label="昨日現金結餘" prop="yesterdayPettyIncome" >
        <el-input  style="width:90%" v-model="createFormData.yesterdayPettyIncome" disabled/>
      </el-form-item>
      <el-form-item label="營業收入（學生學費）" prop="businessIncome" >
        <el-input  style="width:90%" v-model="createFormData.businessIncome" disabled/>
      </el-form-item>
      <el-form-item label="關帳結算金額" prop="closeAccountAmount"  >
        <el-input  style="width:90%" v-model="createFormData.closeAccountAmount" disabled/>
      </el-form-item>
      <el-form-item label="提存金額" prop="depositAmount"  >
        <el-input-number  style="width:90%" v-model="createFormData.depositAmount" :min="0" :controls="false"/>
      </el-form-item>
      <el-form-item label="零用金結餘" prop="pettyIncome"  >
        <el-input  style="width:90%" :value="computedPettyIncome" disabled/>
      </el-form-item>
    </el-form>
    <template #footer>
      <span class="dialog-footer">
        <el-button @click="isShowAddRoleDialog = false">{{ t("Cancel") }}</el-button>
        <el-button type="primary"  @click="submitForm()">{{ t("Confirm") }}</el-button>
      </span>
    </template>
  </el-dialog>
  <!-- /關帳彈窗 -->
</template>

<script setup lang="ts">
import { ref, reactive, computed } from "vue";
import { useI18n } from "vue-i18n";
import API from '@/apis/TPSAPI';
import { M_IResDailyScheduleStatus, M_ICloseAccountDetail } from "@/models/M_ICloseAccount";
import { ElNotification, ElMessageBox, NotificationParams, FormInstance, FormRules } from 'element-plus';
import { Check, CircleCheck } from '@element-plus/icons-vue';

const { t } = useI18n();
const selectedDate = ref<string>('');
const dailyScheduleStatus = ref<M_IResDailyScheduleStatus | null>(null);
const isCheckingInAll = ref<boolean>(false);
const isShowAddRoleDialog = ref<boolean>(false);

const createFormData = reactive<M_ICloseAccountDetail>({
  closeDate: '',
  yesterdayPettyIncome: 0,
  businessIncome: 0,
  closeAccountAmount: 0,
  depositAmount: 0,
  pettyIncome: 0
});

const rules = reactive<FormRules>({
  depositAmount: [{ required: true, message: '請輸入提存金額', trigger: 'blur' }]
});

// 自動計算零用金結餘
const computedPettyIncome = computed(() => {
  return createFormData.closeAccountAmount - createFormData.depositAmount;
});

const handleDateChange = async (date: string) => {
  if (!date) {
    dailyScheduleStatus.value = null;
    return;
  }

  let notifyParam: NotificationParams = {};

  try {
    const response = await API.getCloseAccountDailyStatus(date);

    if (response.data.result !== 1) {
      throw Error(response.data.msg);
    }

    dailyScheduleStatus.value = response.data.content;

    notifyParam = {
      title: "查詢成功",
      type: "success",
      message: `日期：${date}<br>總課程數：${dailyScheduleStatus.value.totalSchedules}<br>已簽到：${dailyScheduleStatus.value.checkedInCount}<br>未簽到：${dailyScheduleStatus.value.notCheckedInCount}`,
      duration: 3000,
      dangerouslyUseHTMLString: true
    };

    console.log('簽到狀態:', dailyScheduleStatus.value);

  } catch (error) {
    dailyScheduleStatus.value = null;
    notifyParam = {
      title: "查詢失敗",
      type: "error",
      message: (error as Error).message,
      duration: 3000,
    };
  } finally {
    ElNotification(notifyParam);
  }
};

const handleCheckInAll = async () => {
  if (!selectedDate.value || !dailyScheduleStatus.value) {
    return;
  }

  try {
    await ElMessageBox.confirm(
      `確定要為 ${selectedDate.value} 的所有未簽到課程一鍵簽到嗎？`,
      '確認操作',
      {
        confirmButtonText: '確定',
        cancelButtonText: '取消',
        type: 'warning',
      }
    );
  } catch {
    return;
  }

  isCheckingInAll.value = true;
  let notifyParam: NotificationParams = {};

  try {
    const response = await API.checkInAllForDate(selectedDate.value);

    if (response.data.result !== 1) {
      throw Error(response.data.msg);
    }

    const result = response.data.content;

    notifyParam = {
      title: "簽到成功",
      type: "success",
      message: `批量簽到完成<br>總課程數：${result.totalSchedules}<br>成功簽到：${result.successfulCheckins} 筆`,
      duration: 3000,
      dangerouslyUseHTMLString: true
    };

    // 重新查詢簽到狀態
    await handleDateChange(selectedDate.value);

  } catch (error) {
    notifyParam = {
      title: "簽到失敗",
      type: "error",
      message: (error as Error).message,
      duration: 3000,
    };
  } finally {
    isCheckingInAll.value = false;
    ElNotification(notifyParam);
  }
};

const handleCloseAccount = async () => {
  if (!dailyScheduleStatus.value || !dailyScheduleStatus.value.canCloseAccount || !selectedDate.value) {
    return;
  }

  let notifyParam: NotificationParams = {};

  try {
    // 查詢關帳明細資料
    const response = await API.getCloseAccountDetail(selectedDate.value);

    if (response.data.result !== 1) {
      throw Error(response.data.msg);
    }

    const detail = response.data.content;

    // 填充表單資料
    createFormData.closeDate = detail.closeDate;
    createFormData.yesterdayPettyIncome = detail.yesterdayPettyIncome;
    createFormData.businessIncome = detail.businessIncome;
    createFormData.closeAccountAmount = detail.closeAccountAmount;
    createFormData.depositAmount = detail.depositAmount;
    createFormData.pettyIncome = detail.pettyIncome;

    // 顯示彈窗
    isShowAddRoleDialog.value = true;

  } catch (error) {
    notifyParam = {
      title: "查詢失敗",
      type: "error",
      message: (error as Error).message,
      duration: 3000,
    };
    ElNotification(notifyParam);
  }
};

const submitForm = async () => {
  if (!createFormData.closeDate) return;

  let notifyParam: NotificationParams = {};

  try {
    const response = await API.saveCloseAccount({
      date: createFormData.closeDate,
      depositAmount: createFormData.depositAmount
    });

    if (response.data.result !== 1) {
      throw Error(response.data.msg);
    }

    notifyParam = {
      title: "關帳成功",
      type: "success",
      message: `已完成 ${createFormData.closeDate} 的關帳作業`,
      duration: 3000,
    };

    isShowAddRoleDialog.value = false;

  } catch (error) {
    notifyParam = {
      title: "關帳失敗",
      type: "error",
      message: (error as Error).message,
      duration: 3000,
    };
  } finally {
    ElNotification(notifyParam);
  }
};
</script>

<style scoped>
.ml-3 {
  margin-left: 1rem;
}

.mr-2 {
  margin-right: 0.5rem;
}

.align-items-center {
  align-items: center;
}
</style>
