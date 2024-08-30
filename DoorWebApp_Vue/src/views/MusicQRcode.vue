<template>
  <div class="content-body mt-3 d-flex flex-column justify-content-start align-content-start px-3">
    <!-- Title -->
    <div class="text-start mb-3">
      <span class="fs-4 fw-bold content-title">{{ t("QRcode") }}</span>
    </div>

    <el-tabs type="border-card">
      <el-tab-pane :label="t('Access QR Code')" >
        <!-- <div class="d-flex flex-column">
          <div class="col-md-4 col-xs-12 col-sm-12">
            <el-image :src="imageSrc" alt="Base64 Image" />
          </div>
        </div> -->
        <el-card style="max-width: 250px">
          <el-image :src="imageSrc" alt="Base64 Image" />
          <el-divider style="margin: 15px 0;"/>
          <!-- <span>可通行時間</span><br> -->
          <span>※ 無法使用時重新整理網頁</span>
        </el-card>
      </el-tab-pane>
    </el-tabs>
  </div>
</template>
<script setup lang="ts">
import { onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import { useUserInfoStore } from "@/stores/UserInfoStore";
import API from "@/apis/TPSAPI";
import { APIResultCode } from "@/models/enums/APIResultCode";

const { t, locale } = useI18n();
const router = useRouter();
const userInfoStore = useUserInfoStore();

const qrcode = ref('');
const imageSrc = ref('');
let executionCount = 0; // 計數器

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

  } catch (error) {
    console.error(error);
  }
}

function scheduleGetUserSettingPermission() {
  const interval = 5 * 60 * 1000; // 每隔 30 分鐘
  const startHour = 7;
  const endHour = 23;

  const executeFunction = () => {
    const now = new Date();
    const currentHour = now.getHours();

    if (currentHour >= startHour && currentHour < endHour) {
      executionCount++;
      console.log(`第 ${executionCount} 次執行`);
      getUserSettingPermission();
    }
  };

  // 立即執行一次，然後每隔 30 分鐘執行
  executeFunction();
  setInterval(executeFunction, interval);
}
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
