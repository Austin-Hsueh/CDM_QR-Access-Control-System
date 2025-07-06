<template>
  <!-- table -->
  <el-row>
    <el-col :span="24">
      <el-table name="userInfoTable" style="width: 100%" height="400" :data="userInfo">
        <el-table-column sortable :label="t('username')"  prop="username"/>
        <el-table-column sortable :label="t('displayName')" prop="displayName" />
        <el-table-column sortable :label="t('Email')" prop="email"/>
        <el-table-column sortable :label="t('Phone')" prop="phone"/>
        <el-table-column sortable :label="t('Role')">
          <template #default="scope">
            {{ roleMap[scope.row.roleId as keyof typeof roleMap] }}
          </template>
        </el-table-column>
        <el-table-column width="160px" align="center" prop="operate" class="operateBtnGroup d-flex" :label="t('operation')">
          <template #default="{ row }: { row: any }">
            <el-button type="primary" size="small" @click="onEdit(row)"><el-icon><EditPen /></el-icon> 編輯</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-col>
  </el-row>
  <!-- /table -->
  
  <!-- 編輯彈窗 -->
  <el-dialog class="dialog"  v-model="isShowEditRoleDialog" :title="t('edit')">
    <el-form label-width="100px"  ref="updateRoleForm" :rules="editRules" :model="updateFormData">
      <el-form-item :label="t('username')" prop="username"  >
        <el-input style="width:90%" v-model="updateFormData.username"/>
      </el-form-item>
      <el-form-item :label="t('displayName')" prop="displayName" >
        <el-input  style="width:90%" v-model="updateFormData.displayName"/>
      </el-form-item>
      <el-form-item :label="t('Email')" prop="email" >
        <el-input  style="width:90%" v-model="updateFormData.email"/>
      </el-form-item>
      <el-form-item :label="t('Phone')" prop="phone"  >
        <el-input  style="width:90%" v-model="updateFormData.phone"/>
      </el-form-item>
      <el-form-item :label="t('password')" prop="password"  >
        <el-input  style="width:90%" v-model="updateFormData.password"  placeholder="若不變更密碼，留空即可。" />
      </el-form-item>
      <el-form-item :label="t('Role')" prop="role" >
        <el-select v-model="updateFormData.roleid" placeholder="請選擇一個角色" style="width:90%" disabled>
          <el-option label="管理者" :value="1" />
          <el-option label="老師" :value="2" />
          <el-option label="學生" :value="3" />
          <el-option label="值班人員" :value="4" />
        </el-select>
      </el-form-item>
    </el-form>
    <template #footer>
      <span class="dialog-footer">
        <el-button @click="isShowEditRoleDialog = false">{{ t("Cancel") }}</el-button>
        <el-button type="primary"  @click="submitUpdateForm()">{{ t("Confirm") }}</el-button>
      </span>
    </template>
  </el-dialog>
  <!-- /編輯彈窗 -->

</template>

<script setup lang="ts">

import { ref, onMounted, onActivated, reactive, computed } from "vue";
import { useI18n } from "vue-i18n";
import API from '@/apis/TPSAPI';
import { EditPen } from '@element-plus/icons-vue';
import { M_IUsers } from "@/models/M_IUser";
import {  M_IUpdateRuleForm} from '@/models/M_IRuleForm';
import { FormInstance, FormRules, ElNotification, NotificationParams  } from 'element-plus';
import { useUserInfoStore } from "@/stores/UserInfoStore";

const isShowEditRoleDialog = ref(false);
const { t } = useI18n();
const userInfo = ref<M_IUsers[]>([]);
const userInfoStore = useUserInfoStore();

const roleMap = computed(() => ({
  1: '管理者',
  2: '老師',
  3: '學生',
  4: '值班人員'
}));

const Error = (error: string) => {
  let notifyParam: NotificationParams = {};
  notifyParam = {
      title: "錯誤",
      type: "error",
      message: error,
      duration: 1000,
  };
}

//#region UI Events
const onEdit = (item: M_IUsers) => {
  console.log(item.userId);
  updateRoleForm.value?.resetFields();
  updateFormData.userId = item.userId;
  updateFormData.username = item.username;
  updateFormData.displayName =item.displayName;
  updateFormData.email = item.email;
  updateFormData.phone = item.phone ?? '';
  updateFormData.roleid = item.roleId;
  isShowEditRoleDialog.value = true;
}

const submitUpdateForm = async () => {
  let notifyParam: NotificationParams = {};

  updateRoleForm.value?.validate(async (valid: boolean) => {
    if (valid) {
      console.log(updateFormData);
      const updateResponse = await API.updateUsers(updateFormData);
      console.log(updateResponse.data.msg);

      if (updateResponse.data.result != 1) {
        notifyParam = {
          title: "失敗",
          type: "error",
          message: `帳號：${updateFormData.username} 新增失敗`,
          duration: 2000,
        };
      } else {
        notifyParam = {
          title: "成功",
          type: "success",
          message: `帳號：${updateFormData.username} 已成功更新`,
          duration: 2000,
        };
      }

      ElNotification(notifyParam);
      isShowEditRoleDialog.value = false;
      getUsers();
    } else {
      console.log('error submit!');
    }
  });
};


//#endregion

//#region 建立表單ref與Validator

// 編輯使用者表單
const updateRoleForm = ref<FormInstance>()
const updateFormData = reactive<M_IUpdateRuleForm>({
  userId:0,
  username: '',
  displayName: '',
  email: '',
  phone: '',
  password: '',
  roleid: 0
})

// 編輯表單規則
const editRules  = reactive<FormRules>({
  username: [{ required: true, message: () => t("validation_msg.username_is_required"), trigger: "blur" }],
  displayName: [{ required: true, message: () => t("validation_msg.displayname_is_required"), trigger: "blur" }],
  email: [{ required: true, message: () => t("validation_msg.email_is_required"), trigger: "blur" }],
  phone: [{ required: true, message: () => t("validation_msg.phone_is_required"), trigger: "blur" }],
  
});


//#endregion

//#region Hook functions

onMounted(() => {
  getUsers();
});

//#endregion

//#region Private Functions
async function getUsers() {
  try {
    const getUsersResult = await API.getOnerUser(userInfoStore.userId);

    if (getUsersResult.data.result != 1) throw new Error(getUsersResult.data.msg);
    userInfo.value = [getUsersResult.data.content];
    console.log(getUsersResult.data.content)
    console.log(userInfo.value)

  } catch (error) {
    console.error(error);
  }
}
//#endregion

</script>

<style scoped></style>
