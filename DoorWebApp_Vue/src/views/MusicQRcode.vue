<template>
  <div class="content-body mt-3 d-flex flex-column justify-content-start align-content-start px-3">
    <!-- Title -->
    <div class="text-start mb-3">
      <span class="fs-4 fw-bold content-title">{{ t("QRcode") }}</span>
    </div>

    <el-tabs type="border-card">
      <el-tab-pane :label="t('Access QR Code')" >
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
