<template>
  <div class="content-body mt-3 d-flex flex-column justify-content-start align-content-start px-3">
    <!-- Title -->
    <div class="text-start mb-3">
      <span class="fs-4 fw-bold content-title">{{ t("Access Control") }}</span>
    </div>
    <!-- 門禁設定、使用者門禁資料已移除：課程門禁一律由課表操作連動 -->
    <el-tabs type="border-card">
      <el-tab-pane :label="t('Open Door Button')">
        <el-button  @click="callApi()" >開啟大門</el-button>
        <el-button  @click="callApiCar()" >開啟Car教室</el-button>
        <el-button  @click="callApiSunny()" >開啟Sunny教室</el-button>
        <el-button  @click="callApiStore()" >開啟儲藏室</el-button>
        <!-- <p v-if="!trueURL" style="margin-top: 10px;">※  {{ fullURL  }} 不在內網，無法使用 ※</p>
        <p v-if="trueURL" style="margin-top: 10px;">※  {{ fullURL  }} 可使用 ※</p> -->
      </el-tab-pane>
    </el-tabs>
  </div>

</template>
<script setup lang="ts">
import { useI18n } from "vue-i18n";
import axios from "axios";

const { t } = useI18n();

const callApi = () => {
  const requestOptions = {
      method: "POST",
      url: "http://127.0.0.1:1029/api/v1/Unlock/1",
      redirect: "follow"
  };

  axios(requestOptions)
  .then((response) => {
      console.log(response.data);
  })
  .catch((error) => {
      console.error(error);
  });
}

const callApiCar = () => {
  const requestOptions2 = {
      method: "POST",
      url: "http://127.0.0.1:1029/api/v1/Unlock/2",
      redirect: "follow"
  };

  axios(requestOptions2)
  .then((response) => {
      console.log(response.data);
  })
  .catch((error) => {
      console.error(error);
  });
}

const callApiSunny = () => {
  const requestOptions3 = {
      method: "POST",
      url: "http://127.0.0.1:1029/api/v1/Unlock/3",
      redirect: "follow"
  };

  axios(requestOptions3)
  .then((response) => {
      console.log(response.data);
  })
  .catch((error) => {
      console.error(error);
  });
}

const callApiStore = () => {
  const requestOptions4 = {
      method: "POST",
      url: "http://127.0.0.1:1029/api/v1/Unlock/4",
      redirect: "follow"
  };

  axios(requestOptions4)
  .then((response) => {
      console.log(response.data);
  })
  .catch((error) => {
      console.error(error);
  });
}
</script>
