<template>
  <!-- Ipt、Btn Group -->
  <div class="d-flex mb-2 pl-2">
    <el-input
      class="me-auto"
      style="width: 240px"
      :placeholder="t('NameFilter')"
      clearable
      v-model="searchText"
      :prefix-icon="Filter"
      @input="onFilterInputed"
    />
    <el-button type="primary" @click="onCreateRoleClicked">{{ t("create") }}</el-button>
  </div>

  <!-- table -->
  <el-row>
    <el-col :span="24">
      <el-table name="userInfoTable" style="width: 100%" height="400" :data="userInfo">
        <el-table-column :label="t('username')"  prop="username"/>
        <el-table-column :label="t('displayName')" prop="displayName" />
        <el-table-column :label="t('Email')" prop="email"/>
        <el-table-column :label="t('Phone')" prop="phone"/>
        <el-table-column width="150px" align="center" prop="operate" class="operateBtnGroup d-flex" :label="t('operation')" v-if="false">
          <template #default="{ row }: { row: any }">
            <el-button type="primary" @click="onEdit(row)"><el-icon><EditPen /></el-icon></el-button>
            <el-button type="danger" @click="onDelet(row)" :disabled="(row.userId === 51)">
              <el-icon><Delete />
            </el-icon></el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-col>
  </el-row>
  <!-- /table -->

  <!-- pagination -->
  <el-row justify="end" class="mt-3" v-if="false">
    <el-col>
      <div class="demo-pagination-block mt-3 d-flex justify-content-end">
        <el-pagination
          v-model:current-page="currentPage4"
          v-model:page-size="pageSize4"
          :page-sizes="[100, 200, 300, 400]"
          :size="size"
          layout="total, sizes, prev, pager, next, jumper"
          :total="400"
          @size-change="handleSizeChange"
          @current-change="handleCurrentChange"
          justify="end"
        />
      </div>
    </el-col>
  </el-row>
  <!-- /pagination -->
  
  <!-- 新增彈窗 -->
  <el-dialog class="dialog" top="3vh" v-model="isShowAddRoleDialog" :title="t('create')">
    <el-form label-width="100px"  ref="createaddRoleForm" :rules="rules" :model="createFormData">
      <el-form-item :label="t('username')" prop="username"  >
        <el-input style="width:90%" v-model="createFormData.username"/>
      </el-form-item>
      <el-form-item :label="t('displayName')" prop="displayName" >
        <el-input  style="width:90%" v-model="createFormData.displayName"/>
      </el-form-item>
      <el-form-item :label="t('Email')" prop="email" >
        <el-input  style="width:90%" v-model="createFormData.email"/>
      </el-form-item>
      <el-form-item :label="t('Phone')" prop="phone"  >
        <el-input  style="width:90%" v-model="createFormData.phone"/>
      </el-form-item>
      <el-form-item :label="t('password')" prop="password"  >
        <el-input  show-password style="width:90%" v-model="createFormData.password"/>
      </el-form-item>
      <el-form-item :label="t('Role')" prop="roleid" >
        <el-select v-model="createFormData.roleid" placeholder="請選擇一個角色" style="width:90%">
          <el-option label="管理者" :value="1" />
          <el-option label="老師" :value="2" />
          <el-option label="學生" :value="3" />
          <el-option label="值班人員" :value="4" />
        </el-select>
      </el-form-item>
      <el-form-item :label="t('Access')" prop="groupIds" v-if="false">
        <el-checkbox-group v-model="createFormData.groupIds">
          <el-checkbox label="大門" :value="1"/>
          <el-checkbox label="Car教室" :value="2"/>
          <el-checkbox label="Sunny教室" :value="3" />
          <el-checkbox label="儲藏室" :value="4" />
        </el-checkbox-group>
      </el-form-item>
    </el-form>
    <template #footer>
      <span class="dialog-footer">
        <el-button @click="isShowAddRoleDialog = false">{{ t("Cancel") }}</el-button>
        <el-button type="primary"  @click="submitForm()">{{ t("Confirm") }}</el-button>
      </span>
    </template>
  </el-dialog>
  <!-- /新增彈窗 -->

  <!-- 編輯彈窗 -->
  <el-dialog class="dialog" top="3vh" v-model="isShowEditRoleDialog" :title="t('edit')">
    <el-form label-width="100px"  ref="updateRoleForm" :rules="rules" :model="updateFormData">
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
      <el-form-item :label="t('Role')" prop="role" >
        <el-select v-model="updateFormData.roleid" placeholder="請選擇一個角色" style="width:90%">
          <el-option label="管理者" :value="1" />
          <el-option label="老師" :value="2" />
          <el-option label="學生" :value="3" />
          <el-option label="值班人員" :value="4" />
        </el-select>
      </el-form-item>
      <el-form-item :label="t('Access')" prop="Access" v-if="false">
        <el-checkbox-group v-model="updateFormData.groupIds">
          <el-checkbox label="大門" :value="1"/>
          <el-checkbox label="Car教室" :value="2"/>
          <el-checkbox label="Sunny教室" :value="3" />
          <el-checkbox label="儲藏室" :value="4" />
        </el-checkbox-group>
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

import { ref, onMounted, onActivated, reactive } from "vue";
import { useI18n } from "vue-i18n";
import API from '@/apis/TPSAPI';
import { EditPen, Delete } from '@element-plus/icons-vue';
import { M_IUsers } from "@/models/M_IUser";
import { M_ICreateRuleForm } from '@/models/M_IRuleForm'
import type { ComponentSize, FormInstance, FormRules, ElNotification, NotificationParams  } from 'element-plus';

const isShowAddRoleDialog = ref(false);
const isShowEditRoleDialog = ref(false);
const { t } = useI18n();
const userInfo = ref<M_IUsers[]>([]); // Specify the type of the array
const currentPage4 = ref(4)
const pageSize4 = ref(100)
const size = ref<ComponentSize>('default')
const searchText = ref('')


const handleSizeChange = (val: number) => {
  console.log(`${val} items per page`)
}
const handleCurrentChange = (val: number) => {
  console.log(`current page: ${val}`)
}

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
  updateFormData.username = item.username 
  console.log(item.username)
  console.log(updateFormData.username)
  updateFormData.displayName =item.displayName
  updateFormData.email = item.email
  updateFormData.phone = item.phone
  updateFormData.roleid = item.roleId
  // updateFormData.Access = item.permissionNames
  isShowEditRoleDialog.value = true;
}

const onCreateRoleClicked = () => {
  createaddRoleForm.value?.resetFields();
  isShowAddRoleDialog.value = true;
}

const submitForm = async () => {
  createaddRoleForm.value?.validate(async(valid) => {
    if (valid) {
      console.log(createFormData)
      const addUser = await API.addUser(createFormData);
      console.log(addUser.data.result);
      if(addUser.data.result !== 1){
        console.log('error submit!');
        console.log(addUser.data.msg);
      }
      console.log('submit!')
      isShowAddRoleDialog.value = false;
      getUsers();
    } else {
      console.log('error submit!')
    }
  })
}

const submitUpdateForm = async () => {
  updateRoleForm.value?.validate((valid) => {
    if (valid) {
      console.log(createFormData)
    } else {
      console.log('error submit!')
    }
  })
}

const onFilterInputed = () => {
  console.log("Search Function");
  if(!searchText.value || searchText.value.trim() === ''){
    getUsers();
  }else{
    setTimeout(()=>{
      console.log(searchText.value)
      const filteredData = userInfo.value.filter(item => {
        const matchesIp = item.displayName.includes(searchText.value);
        return matchesIp;
      });
      console.log(filteredData)
      userInfo.value = filteredData;
    }, 500);
  }
}
//#endregion

//#region 建立表單ref與Validator
// 新增使用者表單
const createaddRoleForm = ref<FormInstance>()
const createFormData = reactive<M_ICreateRuleForm>({
  username: '',
  displayName: '',
  email: '',
  phone: '',
  password: '',
  roleid: '',
  groupIds:[]
})
const rules  = reactive<FormRules>({
  username: [{ required: true, message: () => t("validation_msg.username_is_required"), trigger: "blur" }],
  displayName: [{ required: true, message: () => t("validation_msg.displayname_is_required"), trigger: "blur" }],
  email: [{ required: true, message: () => t("validation_msg.email_is_required"), trigger: "blur" }],
  password: [{ required: true, message: () => t("validation_msg.password_is_required"), trigger: "blur" }],
  role: [{ required: true, message: "請至少選擇一個角色", trigger: "blur" }],
});

// 編輯使用者表單
const updateRoleForm = ref<FormInstance>()
const updateFormData = reactive<M_ICreateRuleForm>({
  username: '',
  displayName: '',
  email: '',
  phone: '',
  password: '',
  roleid: '',
  groupIds:[]

})

//#endregion

//#region Hook functions
// onActivated(() => {
  
// });
onMounted(() => {
  getUsers();
});

//#endregion

//#region Private Functions
async function getUsers() {
  try {
    const getUsersResult = await API.getAllUsers();
    if (getUsersResult.data.result != 1) throw new Error(getUsersResult.data.msg);
    userInfo.value = getUsersResult.data.content;
    console.log(getUsersResult.data.content)

  } catch (error) {
    console.error(error);
  }
}
//#endregion

//#region MockData
const MockData =[
  {
    accessDays: "周一,周二,周三,周四,周五,周六,周日",
    accessTime: "2024/07/2100:00~2124/07/2124:00",
    displayName: "Administrator",
    email: "",
    password: "1qaz2wsx",
    phone: "0",
    roleId: 1,
    roleName: "Admin",
    userId: 51,
    username: "admin"
  },
  {
    accessDays: "周一,周二,周三,周四,周五,周六,周日",
    accessTime: "2024/07/2100:00~2124/07/2124:00",
    displayName: "Administrator",
    email: "",
    password: "1qaz2wsx",
    phone: "0",
    roleId: 1,
    roleName: "Admin",
    userId: 51,
    username: "admin"
  },
  {
    accessDays: "周一,周二,周三,周四,周五,周六,周日",
    accessTime: "2024/07/2100:00~2124/07/2124:00",
    displayName: "Administrator",
    email: "",
    password: "1qaz2wsx",
    phone: "0",
    roleId: 1,
    roleName: "Admin",
    userId: 51,
    username: "admin"
  },
  {
    accessDays: "周一,周二,周三,周四,周五,周六,周日",
    accessTime: "2024/07/2100:00~2124/07/2124:00",
    displayName: "Administrator",
    email: "",
    password: "1qaz2wsx",
    phone: "0",
    roleId: 1,
    roleName: "Admin",
    userId: 51,
    username: "admin"
  },
  {
    accessDays: "周一,周二,周三,周四,周五,周六,周日",
    accessTime: "2024/07/2100:00~2124/07/2124:00",
    displayName: "Administrator",
    email: "",
    password: "1qaz2wsx",
    phone: "0",
    roleId: 1,
    roleName: "Admin",
    userId: 51,
    username: "admin"
  },
  {
    accessDays: "周一,周二,周三,周四,周五,周六,周日",
    accessTime: "2024/07/2100:00~2124/07/2124:00",
    displayName: "Administrator",
    email: "",
    password: "1qaz2wsx",
    phone: "0",
    roleId: 1,
    roleName: "Admin",
    userId: 51,
    username: "admin"
  },
  {
    accessDays: "周一,周二,周三,周四,周五,周六,周日",
    accessTime: "2024/07/2100:00~2124/07/2124:00",
    displayName: "Administrator",
    email: "",
    password: "1qaz2wsx",
    phone: "0",
    roleId: 1,
    roleName: "Admin",
    userId: 51,
    username: "admin"
  },
  {
    accessDays: "周一,周二,周三,周四,周五,周六,周日",
    accessTime: "2024/07/2100:00~2124/07/2124:00",
    displayName: "Administrator",
    email: "",
    password: "1qaz2wsx",
    phone: "0",
    roleId: 1,
    roleName: "Admin",
    userId: 51,
    username: "admin"
  },
  {
    accessDays: "周一,周二,周三,周四,周五,周六,周日",
    accessTime: "2024/07/2100:00~2124/07/2124:00",
    displayName: "Administrator",
    email: "",
    password: "1qaz2wsx",
    phone: "0",
    roleId: 1,
    roleName: "Admin",
    userId: 51,
    username: "admin"
  },
  {
    accessDays: "周一,周二,周三,周四,周五,周六,周日",
    accessTime: "2024/07/2100:00~2124/07/2124:00",
    displayName: "Administrator",
    email: "",
    password: "1qaz2wsx",
    phone: "0",
    roleId: 1,
    roleName: "Admin",
    userId: 51,
    username: "admin"
  },
  {
    accessDays: "周一,周二,周三,周四,周五,周六,周日",
    accessTime: "2024/07/2100:00~2124/07/2124:00",
    displayName: "Administrator",
    email: "",
    password: "1qaz2wsx",
    phone: "0",
    roleId: 1,
    roleName: "Admin",
    userId: 51,
    username: "admin"
  },
  {
    accessDays: "周一,周二,周三,周四,周五,周六,周日",
    accessTime: "2024/07/2100:00~2124/07/2124:00",
    displayName: "Administrator",
    email: "",
    password: "1qaz2wsx",
    phone: "0",
    roleId: 1,
    roleName: "Admin",
    userId: 51,
    username: "admin"
  },
  {
    accessDays: "周一,周二,周三,周四,周五,周六,周日",
    accessTime: "2024/07/2100:00~2124/07/2124:00",
    displayName: "Administrator",
    email: "",
    password: "1qaz2wsx",
    phone: "0",
    roleId: 1,
    roleName: "Admin",
    userId: 51,
    username: "admin"
  },
]
//#endregion

</script>

<style scoped></style>
