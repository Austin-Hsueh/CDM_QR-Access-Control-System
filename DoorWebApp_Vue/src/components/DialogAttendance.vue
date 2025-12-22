<template>
  <el-dialog class="dialog" :model-value="visible" @update:model-value="handleDialogChange" style="max-width: 1344px;">
    <div class="course-attendance-container">
          <!-- 期數配置管理 -->
      <div class="period-config-section">
        <div class="section-header">
          <h3>期數日期管理</h3>
          <el-button type="primary" size="small" @click="addNewPeriod">
            <el-icon><Plus /></el-icon>
            新增期數
          </el-button>
        </div>
        
        <div class="config-cards">
          <div 
            v-for="config in periodConfigs" 
            :key="config.periodNumber"
            class="config-card">
            <div class="config-header">
              <h4>第{{ config.periodNumber }}期配置</h4>
              <el-button 
                type="danger" 
                size="small" 
                @click="removePeriod(config.periodNumber)"
                :disabled="config.periodNumber === 1">
                <el-icon><Delete /></el-icon>
              </el-button>
            </div>
            <div class="config-form">
              <div class="form-row">
                <span class="form-label">開始日期：</span>
                <el-date-picker
                  v-model="config.startDate"
                  type="date"
                  size="small"
                  format="YYYY-MM-DD"
                  value-format="YYYY-MM-DD"
                  @change="updatePeriodConfig"
                />
              </div>
              <div class="form-row">
                <span class="form-label">課程天數：</span>
                <el-input-number
                  v-model="config.totalDays"
                  :min="1"
                  :max="365"
                  size="small"
                  @change="updatePeriodConfig"
                />
              </div>
              <div class="form-row">
                <span class="form-label">簽到上限：</span>
                <el-input-number
                  v-model="config.maxAttendanceCount"
                  :min="1"
                  :max="10"
                  size="small"
                  @change="updatePeriodConfig"
                />
              </div>
            </div>
          </div>
        </div>
      </div>
    <div class="periods-overview">
      <h3>課程期數總覽</h3>
      <div class="periods-grid">
        <div 
          v-for="period in periods" 
          :key="period.periodNumber"
          class="period-card"
          :class="getPeriodCardClass(period)">
          <div class="period-header">
            <h4>第{{ period.periodNumber }}期</h4>
            <el-tag :type="getPeriodStatusType(period)" size="small">
              {{ getPeriodStatusText(period) }}
            </el-tag>
          </div>
          <div class="period-info">
            <div class="period-dates">
              <span>{{ formatDate(period.startDate) }} ~ {{ formatDate(period.endDate) }}</span>
              <span v-if="period.hasLeaveExtension" class="extension-note">
                (因請假延期 +7天)
              </span>
            </div>
            <div class="period-stats">
              <span>出席及曠課: {{ period.attendanceCount }}</span>
              <span>請假: {{ period.leaveCount }}</span>
              <span>剩餘: {{ period.remainingCount }}</span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- 出席記錄表格 -->
    <el-table 
      style="width: 100%" 
      :data="attendanceRecords" 
      border="true" 
      :header-cell-style="{ backgroundColor: '#F2F2F2' }"
      empty-text="暫無出席記錄"
      :row-class-name="getRowClassName">
      
      <!-- 期數欄位 -->
      <el-table-column label="期數" width="80" align="center">
        <template #default="{ row }: { row: AttendanceRecord }">
          <el-tag 
            :type="getPeriodTagType(row.periodNumber)" 
            size="small">
            {{ row.periodNumber }}期
          </el-tag>
        </template>
      </el-table-column>

      <!-- 簽到欄位 -->
      <el-table-column label="簽到">
        <template #default="{ row }: { row: AttendanceRecord }">
          <div class="attendance-cell">
            <el-tag :type="getAttendanceTagType(row.attendanceType)" class="attendance-tag">
              {{ row.attendanceDate }} （{{ getAttendanceTypeText(row.attendanceType) }}）
            </el-tag>
            <el-tag 
              size="small" 
              :type="row.isTrigger ? 'info' : 'warning'"
              class="trigger-tag">
              {{ row.isTrigger ? '自動' : '人工' }}
            </el-tag>
          </div>
        </template>
      </el-table-column>

      <!-- 期內進度欄位 -->
      <el-table-column label="期內進度" width="120" align="center">
        <template #default="{ row }: { row: AttendanceRecord }">
          <div v-if="row.attendanceType !== 2" class="progress-cell">
            <span class="progress-text">{{ row.periodProgress }}</span>
            <el-progress 
              :percentage="(row.periodAttendanceIndex / 4) * 100" 
              :stroke-width="6"
              :show-text="false"
              :color="getProgressColor(row.periodAttendanceIndex)"
              style="margin-top: 4px;"
            />
          </div>
          <span v-else style="color: #909399;">請假不計入</span>
        </template>
      </el-table-column>

      <!-- 期數狀態欄位 -->
      <el-table-column label="期數狀態" width="100" align="center">
        <template #default="{ row }: { row: AttendanceRecord }">
          <el-tag 
            :type="getPeriodRecordStatusType(row)" 
            size="small">
            {{ getPeriodRecordStatusText(row) }}
          </el-tag>
        </template>
      </el-table-column>

      <!-- 操作欄位 -->
      <el-table-column 
        width="170px" 
        align="center" 
        prop="operate" 
        class="operateBtnGroup d-flex" 
        label="操作">
        <template #default="{ row }: { row: AttendanceRecord }">
          <el-button 
            type="primary" 
            size="small" 
            @click="onEdit(row)">
            <el-icon><EditPen /></el-icon>
            編輯
          </el-button>
          <el-button 
            type="danger" 
            size="small" 
            @click="onDelete(row)"
            style="margin-left: 5px;">
            <el-icon><Delete /></el-icon>
            刪除
          </el-button>
        </template>
      </el-table-column>
    </el-table>
  </div>
  </el-dialog>

  <!-- 編輯對話框 -->
  <el-dialog 
    v-model="isShowEditDialog" 
    title="編輯出勤記錄" 
    width="400px">
    <el-form :model="editForm" label-width="100px">
      <el-form-item label="出勤日期">
        <el-date-picker
          v-model="editForm.attendanceDate"
          type="date"
          placeholder="選擇日期"
          format="YYYY-MM-DD"
          value-format="YYYY-MM-DD"
          style="width: 100%"
        />
      </el-form-item>
      
      <el-form-item label="出勤狀態">
        <el-select 
          v-model="editForm.attendanceType" 
          placeholder="請選擇出勤狀態"
          style="width: 100%">
          <el-option
            v-for="option in attendanceTypeOptions"
            :key="option.value"
            :label="option.label"
            :value="option.value"
          />
        </el-select>
      </el-form-item>
    </el-form>
    
    <template #footer>
      <div class="dialog-footer">
        <el-button @click="cancelEdit">取消</el-button>
        <el-button type="primary" @click="saveEdit">保存</el-button>
      </div>
    </template>
  </el-dialog>
  
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue';
import { ElTable, ElTableColumn, ElTag, ElButton, ElIcon, ElAlert, ElProgress, ElDatePicker, ElInputNumber, ElMessage, ElMessageBox } from 'element-plus';
import { EditPen, Delete, Plus } from '@element-plus/icons-vue';
import API from '@/apis/TPSAPI';

// 定義介面
interface AttendanceRecord {
  id: number;
  attendanceDate: string;
  attendanceType: number; // 0: 曠課, 1: 出席, 2: 請假
  isTrigger: boolean;
  periodNumber: number; // 期數
  periodProgress: string; // 期內進度 "1/4", "2/4" 等
  periodAttendanceIndex: number; // 期內出席/曠課索引
}

interface Period {
  periodNumber: number;
  startDate: string;
  endDate: string;
  attendanceCount: number; // 出席+曠課次數
  leaveCount: number;
  remainingCount: number;
  hasLeaveExtension: boolean; // 是否因請假延期
  status: 'active' | 'completed' | 'upcoming';
}

// Props 定義
interface Props {
  visible: boolean;
  studentpermissionId?: number | null;
}

const props = defineProps<Props>();

// Emits 定義 - 使用函數簽章語法
const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void;
  (e: 'edit', row: AttendanceRecord): void;
  (e: 'delete', row: AttendanceRecord): void;
}>();

// 更新值的方法
const updateValue = (value: boolean) => {
  emit('update:modelValue', value);
};

// 處理對話框變化
const handleDialogChange = (value: boolean) => {
  if (!value) {
    emit('update:modelValue', false);
  }
};

// 關閉對話框
const closeDialog = () => {
  emit('update:modelValue', false);
};

// 保存並關閉
const saveAndClose = () => {
  // 這裡可以執行保存邏輯
  emit('update:modelValue', false);
};

// 出席類型映射
const attendanceTypeMap = {
  0: '曠課',
  1: '出席', 
  2: '請假'
} as const;

// 標籤顏色映射
const attendanceTagTypeMap = {
  0: 'danger',   // 曠課 - 紅色
  1: 'success',  // 出席 - 綠色
  2: 'warning'   // 請假 - 黃色
} as const;

// 模擬10筆記錄資料（調整為更清楚的示例）
const rawAttendanceData = ref([
  // { id: 1, attendanceDate: "2025-07-01", attendanceType: 1, isTrigger: true },  // 第1期-第1次
  // { id: 2, attendanceDate: "2025-07-03", attendanceType: 1, isTrigger: false }, // 第1期-第2次
  // { id: 3, attendanceDate: "2025-07-05", attendanceType: 0, isTrigger: true },  // 第1期-第3次
  // { id: 4, attendanceDate: "2025-07-08", attendanceType: 2, isTrigger: false }, // 第1期-請假(延期7天)
  // { id: 5, attendanceDate: "2025-07-12", attendanceType: 1, isTrigger: false }, // 第1期-第4次(期滿)
  // { id: 6, attendanceDate: "2025-08-05", attendanceType: 1, isTrigger: true },  // 第2期-第1次
  // { id: 7, attendanceDate: "2025-08-08", attendanceType: 0, isTrigger: false }, // 第2期-第2次
  // { id: 8, attendanceDate: "2025-08-10", attendanceType: 1, isTrigger: true },  // 第2期-第3次
  // { id: 9, attendanceDate: "2025-08-15", attendanceType: 1, isTrigger: false }, // 第2期-第4次(期滿)
  // { id: 10, attendanceDate: "2025-09-03", attendanceType: 1, isTrigger: true } // 第3期-第1次
]);

// 定義期數配置介面
interface PeriodConfig {
  periodNumber: number;
  startDate: string;
  totalDays: number;
  maxAttendanceCount: number;
  maxLeaveCount: number;
}

const today = new Date().toISOString().split('T')[0];

// 每期的獨立配置（可以從後端獲取或手動設定）
const periodConfigs = ref<PeriodConfig[]>([
  {
    periodNumber: 1,
    startDate: '2025-07-04',
    totalDays: 28,
    maxAttendanceCount: 4,
    maxLeaveCount: 1
  },
  // {
  //   periodNumber: 2,
  //   startDate: today, // 獨立設定的開始日期
  //   totalDays: 28,
  //   maxAttendanceCount: 4,
  //   maxLeaveCount: 1
  // }
]);

// 計算分期資料
const periods = computed((): Period[] => {
  const periodList: Period[] = [];
  
  // 為每個配置的期數計算實際資料
  periodConfigs.value.forEach(config => {
    const periodStartDate = new Date(config.startDate);
    let periodEndDate = new Date(periodStartDate.getTime() + config.totalDays * 24 * 60 * 60 * 1000);
    
    // 找到屬於此期的記錄
    const periodRecords = rawAttendanceData.value.filter(record => {
      const recordDate = new Date(record.attendanceDate);
      return recordDate >= periodStartDate && recordDate <= periodEndDate;
    });
    
    let attendanceCount = 0; // 出席+曠課的總數
    let leaveCount = 0;
    let hasLeaveExtension = false;
    
    // 按日期排序處理記錄
    periodRecords.sort((a, b) => new Date(a.attendanceDate).getTime() - new Date(b.attendanceDate).getTime());
    
    periodRecords.forEach(record => {
      if (record.attendanceType === 2) { 
        // 請假：不計入4次限制，但延期7天
        if (leaveCount < config.maxLeaveCount) {
          leaveCount++;
          if (!hasLeaveExtension) {
            hasLeaveExtension = true;
            // 延期7天
            periodEndDate = new Date(periodEndDate.getTime() + 7 * 24 * 60 * 60 * 1000);
          }
        }
      } else {
        // 出席或曠課：計入4次限制
        if (attendanceCount < config.maxAttendanceCount) {
          attendanceCount++;
        }
      }
    });
    
    // 如果有記錄屬於此期，或者是第一期，就加入期數列表
    if (periodRecords.length > 0 || config.periodNumber === 1) {
      periodList.push({
        periodNumber: config.periodNumber,
        startDate: config.startDate,
        endDate: periodEndDate.toISOString().split('T')[0],
        attendanceCount,
        leaveCount,
        remainingCount: Math.max(0, config.maxAttendanceCount - attendanceCount),
        hasLeaveExtension,
        status: attendanceCount >= config.maxAttendanceCount ? 'completed' : 
                periodRecords.length > 0 ? 'active' : 'upcoming'
      });
    }
  });
  
  return periodList.sort((a, b) => a.periodNumber - b.periodNumber);
});

// 計算帶有期數資訊的出席記錄
const attendanceRecords = computed((): AttendanceRecord[] => {
  const records: AttendanceRecord[] = [];
  
  // 為每筆記錄分配到對應的期數
  rawAttendanceData.value.forEach(record => {
    const recordDate = new Date(record.attendanceDate);
    
    // 找到記錄所屬的期數
    for (const period of periods.value) {
      const periodStartDate = new Date(period.startDate);
      const periodEndDate = new Date(period.endDate);
      
      if (recordDate >= periodStartDate && recordDate <= periodEndDate) {
        // 計算期內進度
        const periodRecords = rawAttendanceData.value.filter(r => {
          const rDate = new Date(r.attendanceDate);
          return rDate >= periodStartDate && 
                 rDate <= recordDate && 
                 r.attendanceType !== 2; // 不包含請假
        }).sort((a, b) => new Date(a.attendanceDate).getTime() - new Date(b.attendanceDate).getTime());
        
        const periodAttendanceIndex = record.attendanceType === 2 ? 0 : 
          periodRecords.findIndex(r => r.id === record.id) + 1;
        
        records.push({
          ...record,
          periodNumber: period.periodNumber,
          periodProgress: record.attendanceType === 2 ? '請假' : `${periodAttendanceIndex}/4`,
          periodAttendanceIndex: periodAttendanceIndex
        });
        break; // 找到對應期數就跳出
      }
    }
  });
  
  return records.sort((a, b) => new Date(a.attendanceDate).getTime() - new Date(b.attendanceDate).getTime());
});

// 當前期數警告
const currentPeriodWarning = computed(() => {
  const activePeriod = periods.value.find(p => p.status === 'active');
  if (!activePeriod) return null;
  
  if (activePeriod.remainingCount === 0) {
    return {
      title: '期數已滿',
      type: 'error' as const,
      description: `第${activePeriod.periodNumber}期已達到4次簽到限制，將進入下一期！`
    };
  }
  
  if (activePeriod.remainingCount === 1) {
    return {
      title: '繳費提醒',
      type: 'warning' as const,
      description: `第${activePeriod.periodNumber}期僅剩1次簽到機會，請盡快續費！`
    };
  }
  
  return null;
});

// 方法
const getAttendanceTypeText = (type: number): string => {
  return attendanceTypeMap[type as keyof typeof attendanceTypeMap] || '未知';
};

const getAttendanceTagType = (type: number): string => {
  return attendanceTagTypeMap[type as keyof typeof attendanceTagTypeMap] || 'info';
};

const formatDate = (dateString: string): string => {
  const date = new Date(dateString);
  return `${date.getMonth() + 1}/${date.getDate()}`;
};

const getPeriodCardClass = (period: Period): string => {
  return `period-${period.status}`;
};

const getPeriodStatusType = (period: Period): string => {
  switch (period.status) {
    case 'active': return 'primary';
    case 'completed': return 'success';
    case 'upcoming': return 'info';
    default: return 'info';
  }
};

const getPeriodStatusText = (period: Period): string => {
  switch (period.status) {
    case 'active': return '進行中';
    case 'completed': return '已完成';
    case 'upcoming': return '未開始';
    default: return '未知';
  }
};

const getPeriodTagType = (periodNumber: number): string => {
  const period = periods.value.find(p => p.periodNumber === periodNumber);
  return getPeriodStatusType(period!);
};

const getProgressColor = (index: number): string => {
  if (index >= 4) return '#f56c6c';
  if (index >= 3) return '#e6a23c';
  return '#67c23a';
};

const getPeriodRecordStatusType = (row: AttendanceRecord): string => {
  if (row.periodAttendanceIndex > 4) return 'danger';
  if (row.periodAttendanceIndex === 4) return 'warning';
  return 'success';
};

const getPeriodRecordStatusText = (row: AttendanceRecord): string => {
  if (row.attendanceType === 2) return '請假';
  if (row.periodAttendanceIndex > 4) return '超限';
  if (row.periodAttendanceIndex === 4) return '期滿';
  return '正常';
};

const getRowClassName = ({ row }: { row: AttendanceRecord }): string => {
  const period = periods.value.find(p => p.periodNumber === row.periodNumber);
  return `period-${period?.status || 'unknown'}-row`;
};

// 期數配置管理方法
const addNewPeriod = () => {
  const maxPeriodNumber = Math.max(...periodConfigs.value.map(p => p.periodNumber));
  const lastConfig = periodConfigs.value[periodConfigs.value.length - 1];
  const newStartDate = new Date(lastConfig.startDate);
  newStartDate.setDate(newStartDate.getDate() + lastConfig.totalDays + 1);
  
  periodConfigs.value.push({
    periodNumber: maxPeriodNumber + 1,
    startDate: newStartDate.toISOString().split('T')[0],
    totalDays: 28,
    maxAttendanceCount: 4,
    maxLeaveCount: 1
  });
};

const removePeriod = (periodNumber: number) => {
  if (periodNumber === 1) return; // 不能刪除第一期
  
  const index = periodConfigs.value.findIndex(p => p.periodNumber === periodNumber);
  if (index > -1) {
    periodConfigs.value.splice(index, 1);
  }
};

const updatePeriodConfig = () => {
  // 當配置更新時觸發重新計算
  // 這裡可以加入保存到後端的邏輯
  console.log('期數配置已更新:', periodConfigs.value);
};

watch(
  [() => props.visible, () => props.studentpermissionId], 
  ([visible, id]) => {
    if (visible && id !== null && id !== undefined) {
      getAttends();
    }
  }, 
  { immediate: true }
);

async function getAttends() {
  if (!props.studentpermissionId) return;
  
  try {
    const getAttendsResult = await API.getAttends(props.studentpermissionId)
    if (getAttendsResult.data.result != 1) throw new Error(getAttendsResult.data.msg);
    rawAttendanceData.value = getAttendsResult.data.content
    console.log(rawAttendanceData.value)
  } catch (error) {
    console.error(error);
  }
}

const isShowEditDialog = ref(false);

const editForm = reactive({
  id: 0,
  studentPermissionId: 0,
  attendanceDate: '',
  attendanceType: 0,
  modifiedUserId: 51,
  isDelete: false
});

// 出勤類型選項
const attendanceTypeOptions = [
  { label: '缺席', value: 0 },
  { label: '出席', value: 1 },
  { label: '請假', value: 2 }
];

const onEdit = (row: AttendanceRecord) => {
  // 直接賦值而不是整個對象替換
  editForm.id = row.id;
  editForm.studentPermissionId = props.studentpermissionId || 0;
  editForm.attendanceDate = row.attendanceDate;
  editForm.attendanceType = row.attendanceType;
  editForm.modifiedUserId = 51;
  editForm.isDelete = false;
  
  isShowEditDialog.value = true;
};

// 刪除按鈕點擊事件
const onDelete = async (row: AttendanceRecord) => {
  try {
    const confirmResult = await ElMessageBox.confirm(
      '確定要刪除這筆出勤記錄嗎？',
      '刪除確認',
      {
        confirmButtonText: '確定',
        cancelButtonText: '取消',
        type: 'warning',
      }
    );

    if (confirmResult === 'confirm') {
      const deleteData = {
        id: row.id,
        studentPermissionId: props.studentpermissionId || 0,
        attendanceDate: row.attendanceDate,
        attendanceType: row.attendanceType,
        modifiedUserId: 51,
        isDelete: true // 標記為刪除
      };
      console.log(deleteData)
      const result = await API.updateAttendance(deleteData); // 假設你的API方法名稱
      if (result.data.result === 1) {
        ElMessage.success('刪除成功');
        getAttends(); // 重新載入數據
      } else {
        throw new Error(result.data.msg);
      }
    }
  } catch (error) {
    console.error('刪除失敗:', error);
    ElMessage.error('刪除失敗');
  }
};

// 保存編輯
const saveEdit = async () => {
  try {
    console.log(editForm)
    const result = await API.updateAttendance(editForm);
    if (result.data.result === 1) {
      ElMessage.success('編輯成功');
      isShowEditDialog.value = false;
      getAttends(); // 重新載入數據
    } else {
      throw new Error(result.data.msg);
    }
  } catch (error) {
    console.error('編輯失敗:', error);
    ElMessage.error('編輯失敗');
  }
};

// 取消編輯
const cancelEdit = () => {
  isShowEditDialog.value = false;
};
</script>

<style scoped>
.period-config-section {
  margin-bottom: 24px;
  padding: 16px;
  background-color: #f8f9fa;
  border-radius: 8px;
  border-left: 4px solid #409eff;
}

.section-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
}

.section-header h3 {
  margin: 0;
  color: #303133;
}

.config-cards {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
  gap: 16px;
}

.config-card {
  background: white;
  border: 1px solid #e4e7ed;
  border-radius: 6px;
  padding: 16px;
}

.config-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
}

.config-header h4 {
  margin: 0;
  color: #303133;
  font-size: 14px;
}

.config-form {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.form-row {
  display: flex;
  align-items: center;
  gap: 8px;
}

.form-label {
  min-width: 80px;
  font-size: 13px;
  color: #606266;
  font-weight: 500;
}

.course-attendance-container {
  padding: 0;
}

.periods-overview {
  margin-bottom: 24px;
}

.periods-overview h3 {
  margin: 0 0 16px 0;
  color: #303133;
}

.periods-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
  gap: 16px;
  margin-bottom: 20px;
}

.period-card {
  border: 2px solid #e4e7ed;
  border-radius: 8px;
  padding: 16px;
  background: #fff;
  transition: all 0.3s;
}

.period-card:hover {
  box-shadow: 0 4px 12px rgba(0,0,0,0.1);
}

.period-active {
  border-color: #409eff;
  background: #f0f9ff;
}

.period-completed {
  border-color: #67c23a;
  background: #f0f9f0;
}

.period-upcoming {
  border-color: #909399;
  background: #f8f9fa;
}

.period-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
}

.period-header h4 {
  margin: 0;
  color: #303133;
}

.period-info {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.period-dates {
  font-size: 14px;
  color: #606266;
}

.extension-note {
  color: #e6a23c;
  font-weight: 500;
}

.period-stats {
  display: flex;
  gap: 12px;
  font-size: 13px;
  color: #909399;
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

.overall-stats {
  margin-top: 24px;
  padding: 16px;
  background-color: #f8f9fa;
  border-radius: 8px;
}

.overall-stats h4 {
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

/* 表格行樣式 */
:deep(.period-active-row) {
  background-color: #f0f9ff !important;
}

:deep(.period-completed-row) {
  background-color: #f0f9f0 !important;
}

.dialog-footer {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
}
</style>