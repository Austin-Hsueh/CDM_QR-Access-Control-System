<template>
  <div class="content-body mt-3 d-flex flex-column justify-content-start align-content-start px-3">
    <!-- Title -->
    <div class="text-start mb-3">
      <span class="fs-4 fw-bold content-title">{{ t("QRcode") }}</span>
    </div>

    <el-tabs type="border-card">
      <!-- <el-tab-pane :label="t('Access QR Code')" style="display: flex;flex-direction: row;gap: 10px;" ></el-tab-pane> -->
        <el-tab-pane :label="t('Access QR Code')">
        <el-card style="max-width: 300px">
          <span v-if="ifCodeError">QRcode將於上課前2分鐘顯示</span>
          <div v-if="ifCodeLoad">
            <span>可通行時間</span><br>
            <span>{{dayNames}}</span><br>
            <span>{{ pass.timefrom }}~{{ pass.timeto }}</span>
          </div>
          <el-divider style="margin: 15px 0;"/> 
          <el-image :src="imageSrc" alt="Base64 Image"  @error="handleImageError" @load="handleImageLoad"/>
          <el-divider style="margin: 15px 0;"/>
          <span>※ 無法使用時重新整理網頁</span>
        </el-card>
        <!-- <el-card style="width: 350px;">
          <el-form class="col-4" :inline="true" label-width="100px"  ref="settingAccessTimeForm" label-position="top" style="width:100%;">
              <el-form-item label="課程區間" prop="datepicker"  style="width: 100%;">
                <el-date-picker
                  type="daterange"
                  range-separator="至"
                  start-placeholder="開始日期"
                  end-placeholder="結束日期"
                />
              </el-form-item>
              <el-form-item style="margin-top: auto;">
                <el-button type="primary" @click="settingForm()">搜尋</el-button>
              </el-form-item>
            </el-form>
            <h4 style="font-weight: bold;padding-bottom: 8px;text-decoration: underline;">簽到表</h4><br>
            <el-table name="userInfoTable" style="width: 100%" :data="transposeTable(Info)">
              <el-table-column :label="t('Term')" prop="term" />
              <el-table-column :label="t('PaymentDate')" prop="paymentDate" />
              <el-table-column :label="t('PaymentStamp')" prop="paymentStamp" />
              <el-table-column :label="t('AttendanceFirst')" prop="attendanceFirst" />
              <el-table-column :label="t('AttendanceSecond')" prop="attendanceSecond" />
              <el-table-column :label="t('AttendanceThird')" prop="attendanceThird" />
              <el-table-column :label="t('AttendanceFourth')" prop="attendanceFourth" />
              <el-table-column :label="t('AbsenceRecord')" prop="absenceRecord" />
              <el-table-column :label="t('CourseDeadline')" prop="courseDeadline" />
              <el-table-column prop="property" label="" width="50px;"></el-table-column>
              <el-table-column prop="value0" label="第一期"></el-table-column>
              <el-table-column prop="value1" label="第二期"></el-table-column>
              <el-table-column prop="value2" label="第三期"></el-table-column>
            </el-table>
        </el-card> -->
      </el-tab-pane>
    </el-tabs>
  </div>
</template>
<script setup lang="ts">
import { onMounted, ref, reactive, computed} from "vue";
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import { useUserInfoStore } from "@/stores/UserInfoStore";
import API from "@/apis/TPSAPI";
import { APIResultCode } from "@/models/enums/APIResultCode";
import { pa } from "element-plus/es/locale";

const { t, locale } = useI18n();
const router = useRouter();
const userInfoStore = useUserInfoStore();

const qrcode = ref('');
const imageSrc = ref('');
const ifCodeError = ref(false);
const ifCodeLoad = ref(false);
const pass = ref({
      datefrom: '',
      timefrom: '',
      timeto: '',
      days:[]
});


let executionCount = 0; // 計數器

const handleImageError = () => {
  ifCodeError.value = true;
  ifCodeLoad.value = false;
}

const handleImageLoad = () => {
  ifCodeError.value = false;
  ifCodeLoad.value = true;
};

onMounted(() => {
  // getUserSettingPermission()
  scheduleGetUserSettingPermission();
});

//#region Private Functions
async function getUserSettingPermission() {
  try {
    const getUserSettingPermission = await API.getUserSettingPermission(userInfoStore.userId);
    if (getUserSettingPermission.data.result != 1) throw new Error(getUserSettingPermission.data.msg);
    qrcode.value = getUserSettingPermission.data.content.qrcode || '';
    imageSrc.value = `data:image/png;base64,${qrcode.value}`;

    console.log(getUserSettingPermission.data.content)
    const QRcodeResult = getUserSettingPermission.data.content;
    if (QRcodeResult.studentpermissions && QRcodeResult.studentpermissions.length > 0) {
        pass.value.datefrom = QRcodeResult.studentpermissions[0].datefrom;
        pass.value.timefrom = QRcodeResult.studentpermissions[0].timefrom;
        pass.value.timeto = QRcodeResult.studentpermissions[0].timeto;
        pass.value.days = QRcodeResult.studentpermissions[0].days;
    } else {
        console.log("studentpermissions array is empty or undefined");
    }
    console.log(pass)

  } catch (error) {
    console.error(error);
  }
}

function scheduleGetUserSettingPermission() {
  const interval = 5 * 60 * 1000; // 每隔 5 分鐘
  const startHour = 7;
  const endHour = 23;

  const executeFunction = () => {
    const now = new Date();
    const currentHour = now.getHours();

    // 7點-23點 每5分鐘問一次資料庫QRcode
    // if (currentHour >= startHour && currentHour < endHour) {
    //   executionCount++;
    //   console.log(`第 ${executionCount} 次執行`);
    //   getUserSettingPermission();
    // }
    
    // 整天都每5分鐘問一次資料庫QRcode
    executionCount++;
    console.log(`第 ${executionCount} 次執行`);
    getUserSettingPermission();
  };

  // 立即執行一次，然後每隔 5 分鐘執行
  executeFunction();
  setInterval(executeFunction, interval);
}

const dayNamesArray = ["", "星期一", "星期二", "星期三", "星期四", "星期五", "星期六","星期日"];
// 使用 computed 將 pass.days 轉換成對應的中文星期
const dayNames = computed(() => {
  return pass.value.days.map(day => dayNamesArray[day]).join(', ');
});

//#endregion

// const Info = ref([
//     // {
//     //   id: 1,
//     //   term: '第一期',
//     //   paymentDate: '2025-01-15',
//     //   paymentStamp: '已繳費',
//     //   attendanceFirst: '出席',
//     //   attendanceSecond: '出席',
//     //   attendanceThird: '請假',
//     //   attendanceFourth: '出席',
//     //   absenceRecord: '第三堂請假',
//     //   courseDeadline: '2025-06-30'
//     // },
//     // {
//     //   id: 2,
//     //   term: '第一期',
//     //   paymentDate: '2025-01-16',
//     //   paymentStamp: '已繳費',
//     //   attendanceFirst: '出席',
//     //   attendanceSecond: '缺席',
//     //   attendanceThird: '出席',
//     //   attendanceFourth: '出席',
//     //   absenceRecord: '第二堂缺席',
//     //   courseDeadline: '2025-06-30'
//     // },
//     // {
//     //   id: 3,
//     //   term: '第二期',
//     //   paymentDate: '2025-04-10',
//     //   paymentStamp: '未繳費',
//     //   attendanceFirst: '出席',
//     //   attendanceSecond: '出席',
//     //   attendanceThird: '出席',
//     //   attendanceFourth: '缺席',
//     //   absenceRecord: '第四堂缺席',
//     //   courseDeadline: '2025-09-15'
//     // },
//     // {
//     //   id: 4,
//     //   term: '第二期',
//     //   paymentDate: '2025-04-12',
//     //   paymentStamp: '已繳費',
//     //   attendanceFirst: '缺席',
//     //   attendanceSecond: '出席',
//     //   attendanceThird: '出席',
//     //   attendanceFourth: '出席',
//     //   absenceRecord: '第一堂缺席',
//     //   courseDeadline: '2025-09-15'
//     // },
//     // {
//     //   id: 5,
//     //   term: '第三期',
//     //   paymentDate: '2025-07-20',
//     //   paymentStamp: '已繳費',
//     //   attendanceFirst: '出席',
//     //   attendanceSecond: '出席',
//     //   attendanceThird: '出席',
//     //   attendanceFourth: '出席',
//     //   absenceRecord: '無',
//     //   courseDeadline: '2025-12-20'
//     // },
//     {
//       T1: '04/01',
//       T2: '04/08',
//       T3: '04/15',
//       T4: '04/22'
//     },
//     {
//       T1: '04/01',
//       T2: '04/08',
//       T3: '04/15',
//       T4: '04/22'
//     },
//     {
//       T1: '04/01',
//       T2: '04/08',
//       T3: '04/15',
//       T4: '04/22'
//     },
//     {
//       T1: '04/01',
//       T2: '04/08',
//       T3: '04/15',
//       T4: '04/22'
//     },
//   ]);

// // 可以通過以下函數轉換為直式數據
// const transposeTable = (data) => {
//   const keys = Object.keys(data[0]);
//   const transposed = [];
  
//   keys.forEach(key => {
//     const row = { property: key };
//     data.forEach((item, index) => {
//       row[`value${index}`] = item[key];
//     });
//     transposed.push(row);
//   });
  
//   return transposed;
// }
</script>


<style scoped>
.card-wrap {
  display: grid;
  grid-template-rows: 2fr 1fr;
  width:100%;
  height: 100%;
}

/* .image {
  width: 100%;
  display: block;
} */
.card-wrap:hover {
  cursor: pointer;
}

.card-wrap:hover .image {
  transition: transform 0.2s; /* Animation */
  transform: scale(var(--card-hover-scale));
}

.card-wrap .image {
  grid-row: 1 / 2;
  grid-column: 1 / 1;
  display: block;
}

.card-wrap .content {
  grid-row: 2 / 3;
  grid-column: 1 / 1;
}
</style>
