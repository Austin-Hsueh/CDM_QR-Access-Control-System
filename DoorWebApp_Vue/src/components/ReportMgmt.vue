<template>
  <!-- Report Controls -->
  <!-- Daily Report -->
  <h5 style="margin-bottom: 15px; text-align: left;">營業日總表</h5>
  <el-row :gutter="20" style="margin-bottom: 20px;">
    <el-col :span="6">
      <div style="margin-bottom: 10px;">
        <el-date-picker
          v-model="selectedDate"
          type="date"
          placeholder="選擇日期"
          format="YYYY-MM-DD"
          value-format="YYYY-MM-DD"
          style="width: 100%"
        />
      </div>
      <el-button type="primary" @click="downloadDailyReport" style="width: 100%;">
        下載營業日總表
      </el-button>
    </el-col>
  </el-row>

  <el-divider style="margin: 30px 0;" />

  <!-- Salary Report -->
  <h5 style="margin-bottom: 15px; text-align: left;">拆帳明細表</h5>
  <el-row :gutter="20" style="margin-bottom: 20px;">
    <el-col :span="9">
      <div style="margin-bottom: 10px;">
        <el-date-picker
          v-model="salaryDateRange"
          type="daterange"
          range-separator="至"
          start-placeholder="開始日期"
          end-placeholder="結束日期"
          format="YYYY-MM-DD"
          value-format="YYYY-MM-DD"
          style="width: 100%"
        />
      </div>
      <div style="display: flex; gap: 10px;">
        <el-select v-model="salaryTeacherId" placeholder="選擇老師" style="flex: 1;">
          <el-option
            v-for="teacher in teacherList"
            :key="teacher.teacherId"
            :label="teacher.teacherName"
            :value="teacher.teacherId"
          />
        </el-select>
        <el-checkbox v-model="salaryIncludePaid">包含未繳費</el-checkbox>
        <el-button type="success" @click="downloadSalaryReport">
          下載拆帳明細表
        </el-button>
      </div>
    </el-col>
  </el-row>

  <el-divider style="margin: 30px 0;" />

  <!-- Profit Report -->
  <h5 style="margin-bottom: 15px; text-align: left;">獲利總表</h5>
  <el-row :gutter="20" style="margin-bottom: 20px;">
    <el-col :span="9">
      <div style="margin-bottom: 10px;">
        <el-date-picker
          v-model="profitDateRange"
          type="daterange"
          range-separator="至"
          start-placeholder="開始日期"
          end-placeholder="結束日期"
          format="YYYY-MM-DD"
          value-format="YYYY-MM-DD"
          style="width: 100%"
        />
      </div>
      <div style="display: flex; gap: 10px;">
        <el-select v-model="profitTeacherId" placeholder="選擇老師" style="flex: 1;">
          <el-option
            v-for="teacher in teacherList"
            :key="teacher.teacherId"
            :label="teacher.teacherName"
            :value="teacher.teacherId"
          />
        </el-select>
        <el-checkbox v-model="profitIncludePaid">包含未繳費</el-checkbox>
        <el-button type="warning" @click="downloadProfitReport">
          下載獲利彙總表
        </el-button>
      </div>
    </el-col>
  </el-row>

  <el-divider style="margin: 30px 0;" />
</template>

<script setup lang="ts">
import { ref, onMounted } from "vue";
import API from '@/apis/TPSAPI';
import { M_ICloseAccountRecord } from "@/models/M_ICloseAccount";
import { M_ITeachersOptions } from "@/models/M_ITeachersOptions";
import { ElMessage } from 'element-plus';

// PDF Report controls
const selectedDate = ref<string>('');
const salaryDateRange = ref<string[]>([]);
const salaryTeacherId = ref<number | null>(null);
const salaryIncludePaid = ref<boolean>(true);
const profitDateRange = ref<string[]>([]);
const profitTeacherId = ref<number | null>(null);
const profitIncludePaid = ref<boolean>(true);
const teacherList = ref<M_ITeachersOptions[]>([]);

// Close account records
const dateRange = ref<string[]>([]);
const closeAccountRecords = ref<M_ICloseAccountRecord[]>([]);

const formatDate = (dateStr: string) => {
  if (!dateStr) return '';
  const date = new Date(dateStr);
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
};

const handleDateRangeChange = async (dates: string[] | null) => {
  if (!dates || dates.length !== 2) {
    closeAccountRecords.value = [];
    return;
  }

  const [startDate, endDate] = dates;

  try {
    const response = await API.getCloseAccounts(startDate, endDate);

    if (response.data.result !== 1) {
      throw Error(response.data.msg);
    }

    closeAccountRecords.value = response.data.content;

    ElMessage.success(`查詢成功，找到 ${closeAccountRecords.value.length} 筆關帳記錄`);

    console.log('關帳記錄:', closeAccountRecords.value);

  } catch (error) {
    closeAccountRecords.value = [];
    ElMessage.error((error as Error).message || '查詢關帳記錄失敗');
  }
};

// Download PDF helper function
const downloadPDF = (blob: Blob, filename: string) => {
  const url = window.URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = filename;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  window.URL.revokeObjectURL(url);
};

// 下載營業日總表
const downloadDailyReport = async () => {
  if (!selectedDate.value) {
    ElMessage.warning('請選擇日期');
    return;
  }

  try {
    const response = await API.getDailyReportPDF(selectedDate.value);
    downloadPDF(response.data, `營業日總表_${selectedDate.value}.pdf`);
    ElMessage.success('PDF 下載成功');
  } catch (error) {
    ElMessage.error((error as Error).message || '下載失敗');
  }
};

// 下載拆帳明細表
const downloadSalaryReport = async () => {
  if (!salaryDateRange.value || salaryDateRange.value.length !== 2) {
    ElMessage.warning('請選擇日期範圍');
    return;
  }
  if (!salaryTeacherId.value) {
    ElMessage.warning('請選擇老師');
    return;
  }

  try {
    const [startDate, endDate] = salaryDateRange.value;
    const response = await API.getSalaryReportPDF(
      startDate,
      endDate,
      salaryTeacherId.value,
      salaryIncludePaid.value
    );
    downloadPDF(response.data, `拆帳明細表_${startDate}_${endDate}.pdf`);
    ElMessage.success('PDF 下載成功');
  } catch (error) {
    ElMessage.error((error as Error).message || '下載失敗');
  }
};

// 下載獲利彙總表
const downloadProfitReport = async () => {
  if (!profitDateRange.value || profitDateRange.value.length !== 2) {
    ElMessage.warning('請選擇日期範圍');
    return;
  }
  if (!profitTeacherId.value) {
    ElMessage.warning('請選擇老師');
    return;
  }

  try {
    const [startDate, endDate] = profitDateRange.value;
    const response = await API.getCompanyProfitSummaryPDF(
      startDate,
      endDate,
      profitTeacherId.value,
      profitIncludePaid.value
    );
    downloadPDF(response.data, `獲利彙總表_${startDate}_${endDate}.pdf`);
    ElMessage.success('PDF 下載成功');
  } catch (error) {
    ElMessage.error((error as Error).message || '下載失敗');
  }
};

// Load teacher list on mount
onMounted(async () => {
  try {
    const response = await API.getTeachersOptions();
    if (response.data.result === 1) {
      teacherList.value = response.data.content;
    }
  } catch (error) {
    console.error('載入教師列表失敗:', error);
  }
});

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
