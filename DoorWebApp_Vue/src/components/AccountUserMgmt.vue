<template>
  <!-- Ipt、Btn Group -->
  <div class="d-flex mb-2 pl-2">
    <!-- 搜尋列 -->
    <el-form class="d-flex" @submit.prevent>
      <el-form-item style="width: 240px; margin-right: 5px;">
        <el-input v-model="searchText" clearable :placeholder="t('NameFilter')" />
      </el-form-item>
      <el-form-item style="width: 200px; margin-right: 5px;">
        <el-select v-model="searchType" aria-label="選擇模式" :placeholder="t('Type')" >
          <el-option label="全部" :value="0" />
          <el-option label="在學" :value="1" />
          <el-option label="停課" :value="2" />
          <el-option label="約課" :value="3" />
        </el-select>
      </el-form-item>
      <el-form-item>
        <el-button type="primary" @click="onFilterInputed">搜尋</el-button>
      </el-form-item>
    </el-form>
  </div>

  <!-- table -->
  <el-row>
    <el-switch 
      v-model="showId" 
      :active-text="t('Show Account ID Field')" 
      :inactive-text="t('Hide Account ID Field')" 
      style="--el-switch-on-color: #526E60; flex:1;"
      width="100"/>
    <el-button type="primary" @click="onCreateRoleClicked">{{ t("create") }}</el-button>
    <el-col :span="24">
      <!-- <el-table name="userInfoTable" style="width: 100%" height="400" :data="userInfo"> -->
      <el-table name="userInfoTable" style="width: 100%" height="400" :data="userInfo?.pageItems">
        <el-table-column sortable :label="t('userID')"  prop="userId" v-if="showId"/>
        <el-table-column sortable :label="t('username')"  prop="username"/>
        <el-table-column sortable :label="t('displayName')" prop="displayName" />
        <el-table-column sortable :label="t('Phone')" prop="phone"/>
        <el-table-column sortable label="緊急聯絡人" prop="contactPerson" />
        <el-table-column sortable label="聯絡人電話" prop="contactPhone" />
        <el-table-column sortable :label="t('Role')">
          <template #default="scope">
            {{ roleMap[scope.row.roleId as keyof typeof roleMap] }}
          </template>
        </el-table-column>
        <el-table-column sortable :label="t('Type')">
          <template #default="scope">
            {{ typeMap[scope.row.type as keyof typeof typeMap] }}
          </template>
        </el-table-column>
        <el-table-column sortable label="母帳號(id)">
          <template #default="scope">
            {{ scope.row.parentUsername ? `${scope.row.parentUsername} (${scope.row.parentId})` : '' }}
          </template>
        </el-table-column>
        <el-table-column width="300px" align="center" prop="operate" class="operateBtnGroup d-flex" :label="t('operation')">
          <template #default="{ row }: { row: any }">
            <el-button type="primary" size="small" @click="onEdit(row)" v-if="(row.roleId === 2)"><el-icon><EditPen /></el-icon>{{ '教師資料' }}</el-button>
            <el-button type="primary" size="small" @click="onEdit(row)" v-if="(row.roleId === 3)"><el-icon><EditPen /></el-icon>{{ '學生資料' }}</el-button>
            <el-button type="danger" size="small" @click="onDelet(row)" v-if="(row.userId != 51)">
              <el-icon><Delete /></el-icon>{{ t('delete') }}
            </el-button>
            <el-button type="primary" size="small" @click="onAddChild(row)" v-if="(row.roleId === 3)"><el-icon><EditPen /></el-icon>子帳號</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-col>
  </el-row>
  <!-- /table -->

  <!-- pagination -->
  <el-row justify="end" class="mt-3">
    <el-col>
      <div class="demo-pagination-block mt-3 d-flex justify-content-end">
        <el-pagination
          v-model:current-page="searchPagination.Page"
          v-model:page-size="searchPagination.SearchPage"
          :page-sizes="[10, 50, 100]"
          :size="size"
          layout="total, sizes, prev, pager, next, jumper"
          :total= "userInfo?.totalItems"
          @size-change="handleSizeChange"
          @current-change="handleCurrentChange"
          justify="end"
        />
      </div>
    </el-col>
  </el-row>
  <!-- /pagination -->
  
  <!-- 新增彈窗 -->
  <el-dialog class="dialog"  v-model="isShowAddRoleDialog" :title="t('create')">
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
      <el-form-item label="緊急聯絡人" prop="contactPerson"  >
        <el-input  style="width:90%" v-model="createFormData.contactPerson"/>
      </el-form-item>
      <el-form-item label="聯絡人電話" prop="contactPhone"  >
        <el-input  style="width:90%" v-model="createFormData.contactPhone"/>
      </el-form-item>
      <el-form-item label="關係稱謂" prop="relationshipTitle"  >
        <el-input  style="width:90%" v-model="createFormData.relationshipTitle"/>
      </el-form-item>
      <el-form-item :label="t('password')" prop="password"  >
        <el-input  show-password style="width:90%" v-model="createFormData.password"/>
      </el-form-item>
      <el-form-item :label="t('Role')" prop="roleid" >
        <el-select v-model="createFormData.roleid" placeholder="請選擇一個角色" style="width:90%" disabled>
          <el-option label="學生" :value="3" />
        </el-select>
      </el-form-item>
      <el-form-item :label="t('Type')" prop="type" >
        <el-select v-model="createFormData.type" placeholder="請選擇一個角色" style="width:90%">
          <el-option label="預設" :value="0" />
          <el-option label="在學" :value="1" />
          <el-option label="停課" :value="2" />
          <el-option label="約課" :value="3" />
        </el-select>
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
          <el-option label="學生" :value="3" />
        </el-select>
      </el-form-item>
      <el-form-item :label="t('Type')" prop="type" v-if="updateFormData.roleid === 3">
        <el-select v-model="updateFormData.type" placeholder="請選擇一個角色" style="width:90%">
          <el-option label="預設" :value="0" />
          <el-option label="在學" :value="1" />
          <el-option label="停課" :value="2" />
          <el-option label="約課" :value="3" />
        </el-select>
      </el-form-item>
      <el-form-item label="緊急聯絡人" prop="contactPerson" v-if="updateFormData.roleid === 3">
        <el-input  style="width:90%" v-model="updateFormData.contactPerson"/>
      </el-form-item>
      <el-form-item label="聯絡人電話" prop="contactPhone" v-if="updateFormData.roleid === 3">
        <el-input  style="width:90%" v-model="updateFormData.contactPhone"/>
      </el-form-item>
      <el-form-item label="關係稱謂" prop="relationshipTitle" v-if="updateFormData.roleid === 3">
        <el-input  style="width:90%" v-model="updateFormData.relationshipTitle"/>
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

  <!-- 子帳號彈窗 -->
  <el-dialog class="dialog"  v-model="isShowAddChildDialog" :title="`設定子帳號: ${addChildFormData.username}(${addChildFormData.parentId})`">
    <el-form label-width="100px"  ref="addChildForm" :rules="editRules" :model="addChildFormData">
      <el-form-item label="子帳號" >
        <el-select style="width:90%" filterable placeholder="請選擇要綁定的子帳號" v-model="addChildFormData.childId">
          <el-option
            v-for="item in usersOptions"
            :key="item.userId"
            :label="item.displayName"
            :value="item.userId"
          />
        </el-select>
      </el-form-item>
    </el-form>
    <template #footer>
      <span class="dialog-footer">
        <el-button @click="isShowAddChildDialog = false">{{ t("Cancel") }}</el-button>
        <el-button type="primary"  @click="submitaddChildForm()">{{ t("Confirm") }}</el-button>
      </span>
    </template>
  </el-dialog>
  <!-- /子帳號彈窗 -->

</template>

<script setup lang="ts">

import { ref, onMounted, onActivated, reactive, computed } from "vue";
import { useI18n } from "vue-i18n";
import API from '@/apis/TPSAPI';
import { EditPen, Delete } from '@element-plus/icons-vue';
import { M_IUsers, M_IUsersContent } from "@/models/M_IUser";
import { M_ICreateRuleForm, M_IUpdateRuleForm, M_IDeleteRuleForm, M_IAddChildRuleForm } from '@/models/M_IRuleForm';
import { ComponentSize, FormInstance, FormRules, ElNotification, NotificationParams, ElMessageBox  } from 'element-plus';
import SearchPaginationRequest from "@/models/M_ISearchPaginationRequest";
import { useUserInfoStore } from "@/stores/UserInfoStore";
import { M_IUsersOptions } from "@/models/M_IUsersOptions";
import { s } from "@fullcalendar/core/internal-common";

const isShowAddRoleDialog = ref(false);
const isShowEditRoleDialog = ref(false);
const isShowAddChildDialog = ref(false);
const { t } = useI18n();
const userInfo = ref<M_IUsersContent | null>(null);  // Specify the type of the array
// const userInfo = ref<M_IUsers[]>([]);
const currentPage4 = ref(4)
const size = ref<ComponentSize>('default')
const searchText = ref('')
const searchType = ref(0)
const userInfoStore = useUserInfoStore();
const showId = ref(false);
const usersOptions = ref<M_IUsersOptions[]>([]);

const handleSizeChange = async(val: number) => {
  console.log(`${val} items per page`)
  searchPagination.value.SearchPage = val;
  await getUsers();
}
const handleCurrentChange = async(val: number) => {
  console.log(`current page: ${val}`)
  searchPagination.value.Page = val;
  await getUsers();
}

const searchPagination = ref<SearchPaginationRequest>({
  SearchText: "",
  SearchPage: 10,
  Page: 1,
  type: 0
});

const roleMap = computed(() => ({
  1: '管理者',
  2: '老師',
  3: '學生',
  4: '值班人員'
}));

const typeMap = computed(() => ({
  0: '預設',
  1: '在學',
  2: '停課',
  3: '約課'
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
  console.log(item)
  updateRoleForm.value?.resetFields();
  updateFormData.userId = item.userId;
  updateFormData.username = item.username;
  updateFormData.displayName =item.displayName;
  updateFormData.email = item.email;
  updateFormData.phone = item.phone ?? '';
  updateFormData.roleid = item.roleId;
  updateFormData.type = item.type;
  updateFormData.address = item.address;
  updateFormData.idcard = item.idcard;
  updateFormData.contactPerson = item.contactPerson ?? '';
  updateFormData.contactPhone = item.contactPhone ?? '';
  updateFormData.relationshipTitle = item.relationshipTitle ?? '';
  isShowEditRoleDialog.value = true;
}

const onDelet = async(item: M_IUsers) => {
 
  try {
    await ElMessageBox.confirm("確定刪除?", "提示", {
      confirmButtonText: "確定",
      cancelButtonText: "取消",
      type: "warning",
    });
  } catch (error) {
    return;
  }

  deleteFormData.userId = item.userId;
  deleteFormData.username = item.username;
  deleteFormData.displayName =item.displayName;
  deleteFormData.email = item.email;
  deleteFormData.phone = item.phone ?? '';
  deleteFormData.roleid = item.roleId;

  let notifyParam: NotificationParams = {};

  try {
    const deleteResponse = await API.updateUsers(deleteFormData);
    if (deleteResponse.data.result != 1) throw new Error(deleteResponse.data.msg);
    notifyParam = {
      title: "成功",
      type: "success",
      message: `已刪除 ${deleteFormData.username}`,
      duration: 1000,
    };
  } catch (error) {
    notifyParam = {
      title: "錯誤",
      type: "error",
      message: (error as Error).message,
      duration: 3000,
    };
  } finally {
    ElNotification(notifyParam);
    onFilterInputed();
  }
}

const onAddChild = (item: M_IUsers) => {
  console.log(item.userId);
  console.log(item)
  addChildFormData.parentId = item.userId;
  addChildFormData.username = item.username;
  isShowAddChildDialog.value = true;
}

const onCreateRoleClicked = () => {
  createaddRoleForm.value?.resetFields();
  isShowAddRoleDialog.value = true;
}

const submitForm = async () => {
  console.log(createFormData)
  let notifyParam: NotificationParams = {};

  createaddRoleForm.value?.validate(async(valid) => {
    if (valid) {
      const addUser = await API.addUser(createFormData);
      if (addUser.data.result != 1) {
        notifyParam = {
          title: "失敗",
          type: "error",
          message: `帳號：${createFormData.username} 新增失敗<br>原因：${addUser.data.msg}`,
          duration: 2000,
          dangerouslyUseHTMLString: true
        };
      }else{
        notifyParam = {
          title: "成功",
          type: "success",
          message: `帳號：${createFormData.username} 已成功更新<br>請至門禁管理為${createFormData.username}設定通行權限。`,
          duration: 3000,
          dangerouslyUseHTMLString: true // 啟用 HTML 字符串，message 中使用<br>。
        };
      }

      ElNotification(notifyParam);
      isShowAddRoleDialog.value = false;
      onFilterInputed();

    } else {
      console.log('error submit!')
    }
  })
};

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
          message: `帳號：${updateFormData.username} 更新失敗`,
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
      onFilterInputed();
    } else {
      console.log('error submit!');
    }
  });
};

const submitaddChildForm = async () => {
  let notifyParam: NotificationParams = {};

  addChildForm.value?.validate(async (valid: boolean) => {
    if (valid) {
      console.log(addChildFormData);
      const addChildResponse = await API.addChild(addChildFormData);
      console.log(addChildResponse.data.msg);

      if (addChildResponse.data.result != 1) {
        notifyParam = {
          title: "失敗",
          type: "error",
          message: `帳號：${addChildFormData.username} 新增子帳號`,
          duration: 2000,
        };
      } else {
        notifyParam = {
          title: "成功",
          type: "success",
          message: `帳號：${addChildFormData.username} 已成功新增子帳號`,
          duration: 2000,
        };
      }

      ElNotification(notifyParam);
      isShowAddChildDialog.value = false;
      onFilterInputed();
    } else {
      console.log('error submit!');
    }
  });
};

const onFilterInputed = () => {
  console.log("Search Function");
  if(!searchText.value || searchText.value.trim() === '' || searchType.value ){
    searchPagination.value.SearchText = searchText.value
    searchPagination.value.type = searchType.value
    getUsers();
  }else{
    setTimeout(()=>{
      console.log(searchText.value)
      searchPagination.value.SearchText = searchText.value
      searchPagination.value.type = searchType.value
      console.log(searchPagination.value);
      getUsers();
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
  roleid: 3,
  type:1,
  address:'', //UI不顯示，預設回寫空白
  idcard:'', //UI不顯示，預設回寫空白
  contactPerson:'',
  contactPhone:'',
  relationshipTitle:''
})

// 編輯使用者表單
const updateRoleForm = ref<FormInstance>()
const updateFormData = reactive<M_IUpdateRuleForm>({
  userId:0,
  username: '',
  displayName: '',
  email: '',
  phone: '',
  password: '',
  roleid: 0,
  type:0,
  address:'', //UI不顯示，預設回寫空白
  idcard:'', //UI不顯示，預設回寫空白
  contactPerson:'',
  contactPhone:'',
  relationshipTitle:''
})

const deleteFormData = reactive<M_IDeleteRuleForm>({
  userId:0,
  username: '',
  displayName: '',
  email: '',
  phone: '',
  password: '',
  roleid: 0,
  IsDelete: true
})

const addChildForm = ref<FormInstance>()
const addChildFormData = reactive<M_IAddChildRuleForm>({
  parentId:0,
  childId:null,
  username:''
})

// 新增表單, 編輯表單共用規則
const rules  = reactive<FormRules>({
  username: [{ required: true, message: () => t("validation_msg.username_is_required"), trigger: "blur" }],
  displayName: [{ required: true, message: () => t("validation_msg.displayname_is_required"), trigger: "blur" }],
  email: [{ required: true, message: () => t("validation_msg.email_is_required"), trigger: "blur" }],
  password: [{ required: true, message: () => t("validation_msg.password_is_required"), trigger: "blur" }],
  phone: [{ required: true, message: () => t("validation_msg.phone_is_required"), trigger: "blur" }],
  idcard: [{ required: true, message: () => t("validation_msg.idcard_is_required"), trigger: "blur" }],
  address: [{ required: true, message: () => t("validation_msg.adress_is_required"), trigger: "blur" }],
});

const editRules  = reactive<FormRules>({
  username: [{ required: true, message: () => t("validation_msg.username_is_required"), trigger: "blur" }],
  displayName: [{ required: true, message: () => t("validation_msg.displayname_is_required"), trigger: "blur" }],
  email: [{ required: true, message: () => t("validation_msg.email_is_required"), trigger: "blur" }],
  phone: [{ required: true, message: () => t("validation_msg.phone_is_required"), trigger: "blur" }],
});


//#endregion

//#region Hook functions
// onActivated(() => {
  
// });
onMounted(() => {
  getUsers();
  getUsersOptions();
});

//#endregion

//#region Private Functions
async function getUsers() {
  try {
    // const getUsersResult = await API.getAllUsers();

    /** 取得使用者清單-後端分頁 */
    const getUsersResult = await API.getAllStudentsV2(searchPagination.value);

    if (getUsersResult.data.result != 1) throw new Error(getUsersResult.data.msg);
    userInfo.value = getUsersResult.data.content;
    console.log(getUsersResult.data.content)
    console.log(userInfo.value)

  } catch (error) {
    console.error(error);
  }
}

async function getUsersOptions() {
  try {
    const getUsersOptionsResult = await API.getUsersOptions();
    if (getUsersOptionsResult.data.result != 1) throw new Error(getUsersOptionsResult.data.msg);
    // usersOptions.value = getUsersOptionsResult.data.content;
    usersOptions.value = getUsersOptionsResult.data.content.filter(user => ![52, 54].includes(user.userId));

  } catch (error) {
    console.error(error);
  }
}
//#endregion

</script>

<style scoped></style>
