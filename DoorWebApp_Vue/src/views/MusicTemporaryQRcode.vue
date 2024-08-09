<template>
  <div class="content-body mt-3 d-flex flex-column justify-content-start align-content-start px-3">
    <!-- Title -->
    <div class="text-start mb-3">
      <span class="fs-4 fw-bold content-title"></span>
    </div>

    <el-tabs type="border-card">
      <el-tab-pane label="臨時門禁設定-大門">
        <div class="d-flex flex-column">
          <div class="col-12 text-start">
            <el-button type="primary" @click="setTemp()">{{ t("Settings") }}(通行時間1小時)</el-button>
          </div>
        </div>
        <el-divider />
        <div class="d-flex flex-column">
          <div class="col-md-4  col-xs-12 col-sm-12">
            <span>可通行時間</span><br>
            <span>{{ pass.datefrom }} {{ pass.timefrom }}~{{ pass.timeto }}</span>
            <el-image :src="imageSrc" alt="Base64 Image" />
          </div>
        </div>
      </el-tab-pane>
    </el-tabs>
  </div>
</template>
<script lang="ts">
import { defineComponent, onMounted, ref, reactive } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import { useUserInfoStore } from "@/stores/UserInfoStore";
import API from "@/apis/TPSAPI";
import { APIResultCode } from "@/models/enums/APIResultCode";
import { FormInstance, FormRules, UploadProps, UploadUserFile, ElMessage, NotificationParams, ElNotification  } from 'element-plus';

export default defineComponent({
  setup() {
    const { t, locale } = useI18n();
    const router = useRouter();
    const userInfoStore = useUserInfoStore();

    const state = reactive({
      isShowErrorMsg: false,
    });

    const onCardClicked = async () => {
      console.log("click");
      const url = "https://academy.sinbon.com/eHRD/eHRDOrg";
      window.open(url, "_blank");
    };

    const qrcode = ref('');
    const imageSrc = ref('');
    const pass = reactive({
      datefrom: '',
      timefrom: '',
      timeto: ''
    });

    // 获取当前日期
    const today = new Date();
    const yyyy = today.getFullYear();
    const mm = String(today.getMonth() + 1).padStart(2, '0'); // 月份从 0 开始，所以需要加 1
    const dd = String(today.getDate()).padStart(2, '0');

    // 格式化日期为 "YYYY-MM-DD"
    const currentDate = `${yyyy}-${mm}-${dd}`;

    // 获取当前时间
    const now = new Date();
    const hh = String(now.getHours()).padStart(2, '0');
    const mi = String(now.getMinutes()).padStart(2, '0');

    // 格式化时间为 "HH:MM"
    const currentTime = `${hh}:${mi}`;

    // 获取一小时后的时间
    const oneHourLater = new Date(now.getTime() + 60 * 60 * 1000);
    const laterHH = String(oneHourLater.getHours()).padStart(2, '0');
    const laterMI = String(oneHourLater.getMinutes()).padStart(2, '0');

    // 格式化一小时后的时间为 "HH:MM"
    const oneHourLaterTime = `${laterHH}:${laterMI}`;


    // 获取当天星期几，星期一为 1，星期日为 7
    const dayOfWeek = today.getDay(); // getDay() 返回 0（星期日）到 6（星期六）
    const formattedDayOfWeek = dayOfWeek === 0 ? 7 : dayOfWeek; // 将星期日从 0 转换为 7

    // 生成 settingTempFormData
    const settingTempFormData = {
      datefrom: currentDate,
      dateto: currentDate,
      timefrom: currentTime,
      timeto: oneHourLaterTime,
      days: [formattedDayOfWeek],
      groupIds: [1]
    };

    const setTemp = async() =>{
      let notifyParam: NotificationParams = {};
      try{
        const setTemp = await API.setTempDoorCode(settingTempFormData);
        if (setTemp.data.result == 1) {
            notifyParam = {
            title: "成功",
            type: "success",
            message:`臨時QRcode設定成功`,
            duration: 1000,
          };
        }
        if (setTemp.data.result != 1) {
          console.log(setTemp.data.msg)
          return;
        }
        ElNotification(notifyParam);
        getTempQRcode()
      }catch (error) {
        console.log(error);
        state.isShowErrorMsg = true;
      }
    }
    // Fetch the QR code and set image source
    const getTempQRcode = async () => {
      try {
        const tempQRcodeResponse = await API.getTempDoorCode();
        const tempQRcodeResult = tempQRcodeResponse.data.content;
        qrcode.value = tempQRcodeResult.qrcode || '';
        imageSrc.value = `data:image/png;base64,${qrcode.value}`;
        console.log(tempQRcodeResult);
        console.log(qrcode.value);
        pass.datefrom = tempQRcodeResult.datefrom;
        pass.timefrom = tempQRcodeResult.timefrom;
        pass.timeto = tempQRcodeResult.timeto;
      } catch (error) {
        console.log(error);
        state.isShowErrorMsg = true;
      }
    };

    // Hook functions
    onMounted(() => {
      getTempQRcode();
    });

    return {
      t,
      onCardClicked,
      imageSrc, // Make sure imageSrc is returned here
      state,
      setTemp,
      pass
    };
  },
});
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
