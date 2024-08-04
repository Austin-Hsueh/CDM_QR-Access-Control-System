<template>
  <div class="content-body mt-3 d-flex flex-column justify-content-start align-content-start px-3">
    <!-- Title -->
    <div class="text-start mb-3">
      <span class="fs-4 fw-bold content-title">{{ t("Access Control") }}</span>
    </div>
    <el-tabs type="border-card">
      <el-tab-pane label="門禁設定">
        <div class="d-flex flex-column">
          <div class="col-12 text-start d-flex">
            <el-form class="col-4" :inline="true" label-width="100px"  ref="settingAccessTimeForm" :rules="rules" :model="settingAccessTimeFormData" label-position="top" style="margin-top: 15px; width:100%;">
              <el-form-item label="姓名" prop="userId">
                <el-select placeholder="請選擇" v-model="settingAccessTimeFormData.userId">
                  <el-option
                    v-for="item in usersOptions"
                    :key="item.userId"
                    :label="item.username"
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
              </el-form-item>
            </el-form>
          </div>
        </div>
        <el-divider />
        <DoorUserSettingList ref="doorUserSettingListRef" :doorId="1"/>
      </el-tab-pane>
      <el-tab-pane label="大門" name="1">
        <DoorUserList :doorId="1"/>
      </el-tab-pane>
      <el-tab-pane label="Car教室" name="2">
        <DoorUserList :doorId="2"/>
      </el-tab-pane>
      <el-tab-pane label="Sunny教室" name="3">
        <DoorUserList :doorId="3"/>
      </el-tab-pane>
      <el-tab-pane label="儲藏室" name="4">
        <DoorUserList :doorId="4"/>
      </el-tab-pane>
    </el-tabs>
  </div>

</template>
<script setup lang="ts">
import { onMounted, ref, reactive } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import { useUserInfoStore } from "@/stores/UserInfoStore";
import API from '@/apis/TPSAPI';
import type {  FormInstance, FormRules  } from 'element-plus';
import {formatDate, formatTime} from "@/plugins/dateUtils";

import DoorUserList from "@/components/DoorUserList.vue";
import DoorUserSettingList from "@/components/DoorUserSeetingList.vue";
import {M_IUsersOptions} from "@/models/M_IUsersOptions";
import {M_IsettingAccessRuleForm} from "@/models/M_IsettingAccessRuleForm";



const { t, locale } = useI18n();
const router = useRouter();
const userInfoStore = useUserInfoStore();
const usersOptions = ref<M_IUsersOptions[]>([]);
const doors = [1,2,3,4];
const days = [1,2,3,4,5,6,7];

const doorUserSettingListRef = ref(null);


//#region UI Events
const settingForm = async () => {
  settingAccessTimeForm.value?.validate(async (valid) => {
    if (valid) {

      // 格式化日期为 YYYY-MM-DD
      settingAccessTimeFormData.datefrom = formatDate(new Date(settingAccessTimeFormData.datepicker[0]));
      settingAccessTimeFormData.dateto = formatDate(new Date(settingAccessTimeFormData.datepicker[1]));

      // 格式化时间为 HH:MM
      settingAccessTimeFormData.timefrom = formatTime(new Date(settingAccessTimeFormData.timepicker[0]));
      settingAccessTimeFormData.timeto = formatTime(new Date(settingAccessTimeFormData.timepicker[1]));
      
      console.log(settingAccessTimeFormData);

      try {
        const settingResponse = await API.setPermission(settingAccessTimeFormData);
        
        // 根據需要處理 settingResponse
        console.log('Permission set successfully', settingResponse);
        console.log('Permission set successfully', settingResponse.data);

        // 呼叫子組件(doorUserSettingList)的搜尋函式，更新表格
        doorUserSettingListRef.value?.onFilterInputed();
        clearForm();

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
}

//#region Hook functions
onMounted(() => {
  console.log('Component is mounted');
  getUsersOptions()
});
//#endregion

//#region Private Functions
async function getUsersOptions() {
  try {
    const getUsersOptionsResult = await API.getUsersOptions();
    if (getUsersOptionsResult.data.result != 1) throw new Error(getUsersOptionsResult.data.msg);
    usersOptions.value = getUsersOptionsResult.data.content;

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
})
const rules  = reactive<FormRules>({
  userId: [{ required: true, message: () => t("validation_msg.username_is_required"), trigger: "blur" }],
  datepicker: [{ required: true, message: () => t("validation_msg.displayname_is_required"), trigger: "blur" }],
  timepicker: [{ required: true, message: () => t("validation_msg.displayname_is_required"), trigger: "blur" }],
  days: [{ required: true, message: () => t("validation_msg.displayname_is_required"), trigger: "blur" }],
  groupIds: [{ required: true, message: () => t("validation_msg.displayname_is_required"), trigger: "blur" }],
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
