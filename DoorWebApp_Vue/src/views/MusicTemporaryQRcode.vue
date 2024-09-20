<template>
  <div class="content-body mt-3 d-flex flex-column justify-content-start align-content-start px-3">
    <!-- Title -->
    <div class="text-start mb-3">
      <span class="fs-4 fw-bold content-title"></span>
    </div>
    <el-tabs type="border-card">
      <el-tab-pane :label="t('Temporary Access Control Settings - Main Door')" class="d-flex">
        <div class="d-flex flex-column col-4 p-3">
          <div class="d-flex col-12 text-start mb-2">
            <el-time-picker
                  is-range
                  range-separator="至"
                  start-placeholder="開始時間"
                  end-placeholder="結束時間"
                  v-model="timePicker1.timepicker"
                />
            <el-button type="primary" @click="setTemp1()">{{ t("Settings") }} (臨時門禁1)</el-button>
          </div>
          <el-card style="max-width: 250px">
            <el-image :src="imageSrc" alt="Base64 Image" />
            <el-divider style="margin: 15px 0;"/>
            <span>可通行時間</span><br>
            <span>{{ pass.datefrom }} {{ pass.timefrom }}~{{ pass.timeto }}</span>
          </el-card>
        </div>
        <div class="d-flex flex-column col-4 p-3">
          <div class="d-flex col-12 text-start mb-2">
            <el-time-picker
                  is-range
                  range-separator="至"
                  start-placeholder="開始時間"
                  end-placeholder="結束時間"
                  v-model="timePicker2.timepicker"
                />
            <el-button type="primary" @click="setTemp2()">{{ t("Settings") }} (臨時門禁2)</el-button>
          </div>
          <el-card style="max-width: 250px">
            <el-image :src="imageSrc2" alt="Base64 Image" />
            <el-divider style="margin: 15px 0;"/>
            <span>可通行時間</span><br>
            <span>{{ pass2.datefrom }} {{ pass2.timefrom }}~{{ pass2.timeto }}</span>
          </el-card>
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
import {formatTime} from "@/plugins/dateUtils";

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
    const imageSrc2 = ref('');
    const pass = reactive({
      datefrom: '',
      timefrom: '',
      timeto: ''
    });

    const pass2 = reactive({
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


    const timePicker1 = reactive({
      timepicker: ''
    })

    const settingTempFormData1 = {
      datefrom: currentDate,
      dateto: currentDate,
      timefrom: '',
      timeto: '',
      days: [formattedDayOfWeek],
      groupIds: [1]
    };

    const setTemp1 = async() =>{
      let notifyParam: NotificationParams = {};

      // 格式化时间为 HH:MM
      settingTempFormData1.timefrom = formatTime(new Date(timePicker1.timepicker[0]));
      settingTempFormData1.timeto = formatTime(new Date(timePicker1.timepicker[1]));

      try{
        const setTemp = await API.setTempDoorCode(settingTempFormData1);
        if (setTemp.data.result == 1) {
            notifyParam = {
            title: "成功",
            type: "success",
            message:`臨時QRcode_1 設定成功`,
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

    //#region 臨時門禁2

    const timePicker2 = reactive({
      timepicker: ''
    })

    const settingTempFormData2 = {
      datefrom: currentDate,
      dateto: currentDate,
      timefrom: '',
      timeto: '',
      days: [formattedDayOfWeek],
      groupIds: [1]
    };

    const setTemp2 = async() =>{
      let notifyParam: NotificationParams = {};

      // 格式化时间为 HH:MM
      settingTempFormData2.timefrom = formatTime(new Date(timePicker2.timepicker[0]));
      settingTempFormData2.timeto = formatTime(new Date(timePicker2.timepicker[1]));

      try{
        const setTemp = await API.setTempDoorCode54(settingTempFormData2);
        if (setTemp.data.result == 1) {
            notifyParam = {
            title: "成功",
            type: "success",
            message:`臨時QRcode_2 設定成功`,
            duration: 1000,
          };
        }
        if (setTemp.data.result != 1) {
          console.log(setTemp.data.msg)
          return;
        }
        ElNotification(notifyParam);
        getTempQRcode2()
      }catch (error) {
        console.log(error);
        state.isShowErrorMsg = true;
      }
    }
    // Fetch the QR code and set image source
    const getTempQRcode2 = async () => {
      try {
        const tempQRcodeResponse = await API.getTempDoorCode54();
        const tempQRcodeResult = tempQRcodeResponse.data.content;
        qrcode.value = tempQRcodeResult.qrcode || '';
        imageSrc2.value = `data:image/png;base64,${qrcode.value}`;
        console.log(tempQRcodeResult);
        console.log(qrcode.value);
        pass2.datefrom = tempQRcodeResult.datefrom;
        pass2.timefrom = tempQRcodeResult.timefrom;
        pass2.timeto = tempQRcodeResult.timeto;
      } catch (error) {
        console.log(error);
        state.isShowErrorMsg = true;
      }
    };

    //#endregion

    // Hook functions
    onMounted(() => {
      getTempQRcode();
      getTempQRcode2();
    });

    return {
      t,
      onCardClicked,
      imageSrc,
      imageSrc2,
      state,
      setTemp1,
      setTemp2,
      pass,
      pass2,
      timePicker1,
      timePicker2
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
