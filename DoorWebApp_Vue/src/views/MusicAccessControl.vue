<template>
  <div class="content-body mt-3 d-flex flex-column justify-content-start align-content-start px-3">
    <!-- Title -->
    <div class="text-start mb-3">
      <span class="fs-4 fw-bold content-title">{{ t("Access Control") }}</span>
    </div>
    <el-tabs type="border-card" @tab-click="handleTabClick">
      <el-tab-pane :label="t('Open Door Button')">
        <el-button  @click="callApi()" >開啟大門</el-button>
        <el-button  @click="callApiCar()" >開啟Car教室</el-button>
        <el-button  @click="callApiSunny()" >開啟Sunny教室</el-button>
        <el-button  @click="callApiStore()" >開啟儲藏室</el-button>
        <!-- <p v-if="!trueURL" style="margin-top: 10px;">※  {{ fullURL  }} 不在內網，無法使用 ※</p>
        <p v-if="trueURL" style="margin-top: 10px;">※  {{ fullURL  }} 可使用 ※</p> -->
      </el-tab-pane>
      <el-tab-pane :label="t('Access Control Settings')">
        <div class="d-flex flex-column">
          <div class="col-12 text-start d-flex">
            <el-form class="col-4" :inline="true" label-width="100px"  ref="settingAccessTimeForm" :rules="rules" :model="settingAccessTimeFormData" label-position="top" style="margin-top: 15px; width:100%;">
              <el-form-item label="姓名" prop="userId">
                <el-select filterable placeholder="請選擇" v-model="settingAccessTimeFormData.userId">
                  <el-option
                    v-for="item in usersOptions"
                    :key="item.userId"
                    :label="item.displayName"
                    :value="item.userId"
                  />
                </el-select>
              </el-form-item>
              <el-form-item label="課程類別" prop="courseId">
                <el-select filterable placeholder="請選擇" v-model="settingAccessTimeFormData.courseId">
                  <el-option label="鋼琴" :value="1"></el-option>
                  <el-option label="歌唱" :value="2"></el-option>
                  <el-option label="吉他" :value="3"></el-option>
                  <el-option label="貝斯" :value="4"></el-option>
                  <el-option label="烏克麗麗" :value="5"></el-option>
                  <el-option label="創作" :value="6"></el-option>
                  <el-option label="管樂" :value="7"></el-option>
                  <el-option label="爵士鼓" :value="8"></el-option>
                </el-select>
              </el-form-item>
              <el-form-item label="老師姓名" prop="teacherId">
                <el-select filterable placeholder="請選擇" v-model="settingAccessTimeFormData.teacherId">
                  <el-option
                    v-for="item in teachersOptions"
                    :key="item.userId"
                    :label="item.displayName"
                    :value="item.userId"
                  />
                </el-select>
              </el-form-item>
              <el-form-item label="門禁權限" prop="groupIds">
                <el-checkbox-group v-model="settingAccessTimeFormData.groupIds">
                  <el-checkbox v-for="item in doors" :key="item" :label="item" :value="item">
                    <template v-if="item === 1">大門</template>
                    <template v-else-if="item === 2">car教室</template>
                    <template v-else-if="item === 3">Sunny教室</template>
                    <template v-else-if="item === 4">儲藏室</template>
                  </el-checkbox>
                </el-checkbox-group>
              </el-form-item>
              <el-form-item label="通行日期" prop="datepicker"  >
                <el-date-picker
                  type="daterange"
                  range-separator="至"
                  start-placeholder="開始日期"
                  end-placeholder="結束日期"
                  v-model="settingAccessTimeFormData.datepicker"
                />
              </el-form-item>
              <el-form-item label="通行時間" prop="timepicker" >
                <el-time-picker
                  is-range
                  range-separator="至"
                  start-placeholder="開始時間"
                  end-placeholder="結束時間"
                  v-model="settingAccessTimeFormData.timepicker"
                />
              </el-form-item>
              <el-form-item label="週期" prop="days" >
                <el-checkbox-group v-model="settingAccessTimeFormData.days">
                  <el-checkbox v-for="item in days" :key="item" :label="item" :value="item">
                    <template v-if="item === 1">週一</template>
                    <template v-else-if="item === 2">週二</template>
                    <template v-else-if="item === 3">週三</template>
                    <template v-else-if="item === 4">週四</template>
                    <template v-else-if="item === 5">週五</template>
                    <template v-else-if="item === 6">週六</template>
                    <template v-else-if="item === 7">週日</template>
                  </el-checkbox>
                </el-checkbox-group>
              </el-form-item>
              <el-form-item style="margin-top: auto;">
                <el-button type="primary" @click="settingForm()">{{ t("Settings") }}</el-button>
                <el-button type="primary" @click="clearForm()">清除</el-button>
                <el-button type="primary" @click="onUploadClicked()">批次匯入</el-button>
              </el-form-item>
            </el-form>
          </div>
        </div>
      </el-tab-pane>
      <el-tab-pane :label="t('User Access Data')" name="doorUser">
        <DoorUserSettingList ref="doorUserSettingListRef" />
      </el-tab-pane>
      <el-tab-pane label="大門" v-if="false">
        <DoorUserList :doorId="1"/>
      </el-tab-pane>
      <el-tab-pane label="Car教室" v-if="false">
        <DoorUserList :doorId="2"/>
      </el-tab-pane>
      <el-tab-pane label="Sunny教室" v-if="false">
        <DoorUserList :doorId="3"/>
      </el-tab-pane>
      <el-tab-pane label="儲藏室" v-if="false">
        <DoorUserList :doorId="4"/>
      </el-tab-pane>
    </el-tabs>

    <!-- Dialog : 批次匯入-->
      <el-dialog width="500px" title="批次匯入" v-model="isShowUploadDialog">
        <el-upload
          class="upload-demo"
          drag
          :before-upload="beforeUpload"
          action="https://run.mocky.io/v3/9d059bf9-4660-45f2-925d-ce80ad6c4d15"
          :on-change="handleChange"
          :file-list="fileList"
          multiple>
          <el-icon><upload-filled /></el-icon>
          <div class="el-upload__text">拖曳文件至此 或 <em>點擊上傳</em></div>
          <template #tip>
            <div class="el-upload__tip">上傳檔案格式: *.csv</div>
          </template>
        </el-upload>
        <el-button type="primary" @click="onFileUploadClicked">處理檔案</el-button>
      </el-dialog>
    <!-- #endregion -->
  </div>

</template>
<script setup lang="ts">
import { onMounted, ref, reactive, computed, watch } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import { useUserInfoStore } from "@/stores/UserInfoStore";
import API from '@/apis/TPSAPI';
import { FormInstance, FormRules, UploadProps, UploadUserFile, ElMessage, NotificationParams, ElNotification  } from 'element-plus';
import {formatDate, formatTime} from "@/plugins/dateUtils";

import DoorUserList from "@/components/DoorUserList.vue";
import DoorUserSettingList from "@/components/DoorUserSeetingList.vue";
import {M_IUsersOptions} from "@/models/M_IUsersOptions";
import {M_IsettingAccessRuleForm} from "@/models/M_IsettingAccessRuleForm";
import CreateStudentPermission from "@/models/M_CreateStudentPermission";



const { t, locale } = useI18n();
const router = useRouter();
const userInfoStore = useUserInfoStore();
const usersOptions = ref<M_IUsersOptions[]>([]);
const teachersOptions = ref<M_IUsersOptions[]>([]);
const doors = [1,2,3,4];
const days = [1,2,3,4,5,6,7];

const doorUserSettingListRef = ref(null);
const isShowUploadDialog = ref(false);


//#region UI Events
const settingForm = async () => {
  settingAccessTimeForm.value?.validate(async (valid) => {
    if (valid) {

      // 格式化日期为 YYYY-MM-DD
      ssetStudentPermission.datefrom = formatDate(new Date(settingAccessTimeFormData.datepicker[0]));
      ssetStudentPermission.dateto = formatDate(new Date(settingAccessTimeFormData.datepicker[1]));

      // 格式化时间为 HH:MM
      ssetStudentPermission.timefrom = formatTime(new Date(settingAccessTimeFormData.timepicker[0]));
      ssetStudentPermission.timeto = formatTime(new Date(settingAccessTimeFormData.timepicker[1]));

      ssetStudentPermission.userId = settingAccessTimeFormData.userId;
      ssetStudentPermission.days = settingAccessTimeFormData.days;
      ssetStudentPermission.groupIds = settingAccessTimeFormData.groupIds;
      ssetStudentPermission.courseId = settingAccessTimeFormData.courseId;
      ssetStudentPermission.teacherId = settingAccessTimeFormData.teacherId;

      console.log(ssetStudentPermission);

      try {
        // const settingResponse = await API.setPermission(settingAccessTimeFormData);

        // 更改為多時段
        const settingResponse = await API.setStudentPermission(ssetStudentPermission);
        
        // 根據需要處理 settingResponse
        console.log('Permission set successfully', settingResponse);
        console.log('Permission set successfully', settingResponse.data);

        let notifyParam: NotificationParams = {};
        if (settingResponse.data.result == 1) {
            notifyParam = {
            title: "成功",
            type: "success",
            message:`門禁設定成功`,
            duration: 1000,
          };
          clearForm();
          doorUserSettingListRef.value?.onFilterInputed();
        }
        if (settingResponse.data.result != 1) {
          clearForm();
          return;
        }
        ElNotification(notifyParam);

      } catch (error) {
        console.error('Failed to set permission', error);
      }

    } else {
      console.log('error submit!');
    }
  });
};

const clearForm = ()=>{
  settingAccessTimeForm.value?.resetFields();
  settingAccessTimeFormData.datepicker = '';
  settingAccessTimeFormData.timepicker = '';
};

const onUploadClicked = () => {
  isShowUploadDialog.value = true;
};

const handleTabClick = (tab) => {
  if (tab.name === 'doorUser') {
    doorUserSettingListRef.value?.onFilterInputed(); // 调用 DoorUserSettingList 中的方法
  }
}

//#region Hook functions
onMounted(() => {
  console.log('Component is mounted');
  getUsersOptions();
});
//#endregion

//#region Private Functions
async function getUsersOptions() {
  try {
    const getUsersOptionsResult = await API.getUsersOptions();
    if (getUsersOptionsResult.data.result != 1) throw new Error(getUsersOptionsResult.data.msg);
    // usersOptions.value = getUsersOptionsResult.data.content;
    usersOptions.value = getUsersOptionsResult.data.content.filter(user => ![52, 54].includes(user.userId));

    const validIds = [66, 67, 68, 69, 70, 71, 72, 73, 74, 75];
    teachersOptions.value = getUsersOptionsResult.data.content.filter(user => validIds.includes(user.userId));

  } catch (error) {
    console.error(error);
  }
}
//#endregion

//#region 建立表單ref與Validator
// 設定通行時間表單
const settingAccessTimeForm = ref<FormInstance>();
const settingAccessTimeFormData = reactive<M_IsettingAccessRuleForm>({
  userId: '',
  datepicker: '',
  timepicker: '',
  days: [],
  groupIds: [],
  datefrom: '',
  dateto: '',
  timefrom:'',
  timeto: '',
  courseId: '',
  teacherId: '',
})
const ssetStudentPermission = reactive<M_IsettingAccessRuleForm>({
  userId: '',
  days: [],
  groupIds: [],
  datefrom: '',
  dateto: '',
  timefrom:'',
  timeto: '',
})
const rules  = reactive<FormRules>({
  userId: [{ required: true, message: () => t("validation_msg.username_is_required"), trigger: "blur" }],
  datepicker: [{ required: true, message: () => t("validation_msg.displayname_is_required"), trigger: "blur" }],
  timepicker: [{ required: true, message: () => t("validation_msg.displayname_is_required"), trigger: "blur" }],
  days: [{ required: true, message: () => t("validation_msg.displayname_is_required"), trigger: "blur" }],
  groupIds: [{ required: true, message: () => t("validation_msg.displayname_is_required"), trigger: "blur" }],
});


//#endregion

import axios from "axios";
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

// #region 批次匯入功能
const fileList = ref<UploadUserFile[]>([])

const csvContent = ref<Array<CreateStudentPermission>>([]);

const beforeUpload = (file: File) => {
  const isCSV = file.type === 'text/csv';
  if (!isCSV) {
    ElMessage.error('上傳檔案格式不正確');
  }
  return isCSV;
};


const handleChange: UploadProps['onChange'] = (uploadFile, uploadFiles) => {
  // Add the new file entry to the fileList
  fileList.value.push({
    name: uploadFile.name,
    url: '' // Assuming there's no URL for this local file
  })
  
  // Keep only the last 3 entries in the fileList
  fileList.value = fileList.value.slice(-3)

  const reader = new FileReader();
  console.log(123)
  reader.onload = (e: ProgressEvent<FileReader>) => {
    if (e.target && e.target.result) {
      const content = e.target.result.toString();
      csvContent.value = csvToArray(content);
      console.log(JSON.stringify(csvContent.value, null, 2));
    }
  };
  if (uploadFile.raw) {
    reader.readAsText(uploadFile.raw);
  }
}

// 將 CSV 字串轉換為物件陣列
const csvToArray = (str: string, delimiter = ",") => {
  const headers = str.slice(0, str.indexOf("\n")).split(delimiter).map(header => header.trim());
  const rows = str.slice(str.indexOf("\n") + 1).split("\n").filter(row => row.trim() !== '');
  return rows.map(row => {
    const values = row.split(delimiter).map(value => value.trim());
    const obj = headers.reduce((object, header, index) => {
      object[header] = values[index];
      return object;
    }, {} as Record<string, any>);
    
    const parseArray = (str: string) => {
      return str ? str.split(',').map(value => {
        const num = Number(value.trim());
        return isNaN(num) ? null : num;
      }).filter(num => num !== null) : [];
    };

    // 使用更严格的解析方法
    const request: M_IsettingAccessRuleForm = {
      userId: parseInt(obj.userId),
      datefrom: obj.datefrom,
      dateto: obj.dateto,
      timefrom: obj.timefrom,
      timeto: obj.timeto,
      days: parseArray(obj.days),
      groupIds: parseArray(obj.groupIds)
      // datepicker: obj.datefrom,  // 假设 datepicker 为 datefrom 的值
      // timepicker: `${obj.timefrom} - ${obj.timeto}`  // 假设 timepicker 为 timefrom 和 timeto 的组合
    };
    
    return request;
  });
};

const onFileUploadClicked = async () => {
  for (const item of csvContent.value) {
    try {
      const response = await API.setStudentPermission(item);
      console.log(response.data);

      let notifyParam: NotificationParams = {};

      if (response.data.result != 1) {
          notifyParam = {
          title: "失敗",
          type: "error",
          message: `新增失敗：${item.userId} : ${response.data.msg}`,
          duration: 1000,
        };
      }

      if (response.data.result == 1) {
          notifyParam = {
          title: "成功",
          type: "success",
          message:`已新增使用者：${item.userId}`,
          duration: 1000,
        };
      }

      ElNotification(notifyParam);

    } catch (error) {
      console.error(error);
    }
  }

  // 清空 fileList
  fileList.value = [];

  isShowUploadDialog.value = false;
};
//#endregion

// #region 偵測IP
// import { useRoute } from 'vue-router';

// const route = useRoute();
// const trueURL = ref<boolean>(false);

// const fullURL = computed(() => {
//   const { protocol, host } = window.location;
//   const path = route.fullPath;
//   return `${protocol}//${host}${path}`;
// });

// // 使用 watch 侦听 fullURL 的变化
// watch(fullURL, (newURL) => {
//   if (newURL === "http://127.0.0.1/accesscontrol") {
//     trueURL.value = true;
//     console.log(newURL)
//   } else {
//     trueURL.value = false;
//     console.log(newURL)
//   }
// });

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
