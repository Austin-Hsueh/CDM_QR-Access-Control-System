<template>
  <!-- Date Range Picker -->
  <el-row>
    <el-col :span="6">
      <el-date-picker
        v-model="dateRange"
        type="daterange"
        range-separator="至"
        start-placeholder="開始日期"
        end-placeholder="結束日期"
        format="YYYY-MM-DD"
        value-format="YYYY-MM-DD"
        style="width: 100%"
        @change="handleDateRangeChange"
      />
    </el-col>
  </el-row>

  <!-- Table -->
  <el-row>
    <el-col :span="24">
      <el-table
        name="closeAccountTable"
        style="width: 100%"
        height="500"
        :data="closeAccountRecords"
        empty-text="請選擇日期範圍查詢關帳記錄"
      >
        <el-table-column sortable label="關帳日期" prop="closeDate">
          <template #default="{ row }">
            {{ formatDate(row.closeDate) }}
          </template>
        </el-table-column>
        <el-table-column sortable label="昨日零用金結餘" prop="yesterdayPettyIncome"  />
        <el-table-column sortable label="營業收入" prop="businessIncome"  />
        <el-table-column sortable label="關帳結算金額" prop="closeAccountAmount"  />
        <el-table-column sortable label="提存金額" prop="depositAmount"  />
        <el-table-column sortable label="當日零用金結餘" prop="pettyIncome"  />
      </el-table>
    </el-col>
  </el-row>
  <!-- /Table -->

</template>

<script setup lang="ts">
import { ref } from "vue";
import API from '@/apis/TPSAPI';
import { M_ICloseAccountRecord } from "@/models/M_ICloseAccount";
import { ElMessage } from 'element-plus';
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
