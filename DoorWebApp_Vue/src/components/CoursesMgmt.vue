<template>
  <!-- table -->
  <el-row>
    <el-col :span="12" style="padding: 10px;">
      <el-button type="primary" @click="onCreateCourseTypeClicked">新增分類</el-button>
      <el-table name="userInfoTable" style="width: 100%"  :data="courseTypeList">
        <el-table-column sortable :label="t('Course Type')"  prop="courseTypeName"/>
        <el-table-column width="170px" align="center" prop="operate" class="operateBtnGroup d-flex" :label="t('operation')">
          <template #default="{ row }: { row: any }">
            <el-button type="primary" size="small" @click="onEditCourseType(row)"><el-icon><EditPen /></el-icon>{{ t('edit') }}</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-col>
    <el-col :span="12" style="padding: 10px;">
      <el-button type="primary" @click="onCreateCourseClicked">新增課程</el-button>
      <el-table name="userInfoTable" style="width: 100%"  :data="courseList">
        <el-table-column sortable :label="t('Course Type')"  prop="courseTypeName"/>
        <el-table-column sortable :label="t('Course Name')"  prop="courseName"/>
        <el-table-column width="170px" align="center" prop="operate" class="operateBtnGroup d-flex" :label="t('operation')">
          <template #default="{ row }: { row: any }">
            <el-button type="primary" size="small" @click="onEdit(row)"><el-icon><EditPen /></el-icon>{{ t('edit') }}</el-button>
            <el-button type="danger" size="small" @click="onDelet(row)" v-if="(row.userId != 51)">
              <el-icon><Delete /></el-icon>{{ t('delete') }}
            </el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-col>
  </el-row>
  <!-- /table -->
  
  <!-- 新增課程彈窗 -->
  <el-dialog class="dialog"  v-model="isShowAddRoleDialog" :title="t('create')">
    <el-form label-width="100px"  ref="createaddRoleForm" :rules="rules" :model="createFormData">
      <el-form-item :label="t('Course Name')" prop="courseName"  >
        <el-input style="width:90%" v-model="createFormData.courseName"/>
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
      <el-form-item :label="t('Course Name')" prop="courseName"  >
        <el-input style="width:90%" v-model="updateFormData.courseName"/>
      </el-form-item>
      <el-form-item label="課程分類" prop="courseTypeName">
        <el-select style="width:90%" v-model="updateFormData.courseTypeId">
          <el-option
            label="=未選擇分類="
            :value="0"
          />
          <el-option
            v-for="item in courseTypeList"
            :key="item.courseTypeId"
            :label="item.courseTypeName"
            :value="item.courseTypeId"
          />
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

  <!-- 新增分類彈窗 -->
  <el-dialog class="dialog"  v-model="isShowAddCourseTypeDialog" :title="t('create')">
    <el-form label-width="100px"  ref="createaddCourseTypeForm" :rules="rules" :model="createCourseTypeFormData">
      <el-form-item :label="t('Course Type')" prop="courseTypeName"  >
        <el-input style="width:90%" v-model="createCourseTypeFormData.courseTypeName"/>
      </el-form-item>
    </el-form>
    <template #footer>
      <span class="dialog-footer">
        <el-button @click="isShowAddCourseTypeDialog = false">{{ t("Cancel") }}</el-button>
        <el-button type="primary"  @click="submitCourseTypeForm()">{{ t("Confirm") }}</el-button>
      </span>
    </template>
  </el-dialog>
  <!-- /新增彈窗 -->

  <!-- 編輯分類彈窗 -->
  <el-dialog class="dialog"  v-model="isShowEditCourseTypeDialog" :title="t('edit')">
    <el-form label-width="100px"  ref="updateCourseTypeForm" :rules="editRules" :model="updateCourseTypeFormData">
      <el-form-item :label="t('Course Name')" prop="courseTypeName"  >
        <el-input style="width:90%" v-model="updateCourseTypeFormData.courseTypeName"/>
      </el-form-item>
    </el-form>
    <template #footer>
      <span class="dialog-footer">
        <el-button @click="isShowEditCourseTypeDialog = false">{{ t("Cancel") }}</el-button>
        <el-button type="primary"  @click="submitCourseTypeUpdateForm()">{{ t("Confirm") }}</el-button>
      </span>
    </template>
  </el-dialog>
  <!-- /編輯彈窗 -->

</template>

<script setup lang="ts">

import { ref, onMounted, reactive } from "vue";
import { useI18n } from "vue-i18n";
import API from '@/apis/TPSAPI';
import { EditPen, Delete } from '@element-plus/icons-vue';
import { M_IUsers } from "@/models/M_IUser";
import { M_ICreateRuleForm, M_IUpdateRuleForm, M_IDeleteRuleForm } from '@/models/M_IRuleForm';
import { FormInstance, FormRules, ElNotification, NotificationParams, ElMessageBox  } from 'element-plus';
import {M_ICourseOptions} from "@/models/M_ICourseOptions";
import {M_ICourseTypeOptions} from "@/models/M_ICourseTypeOptions"; 
import { de } from "element-plus/es/locale";

const isShowAddRoleDialog = ref(false);
const isShowEditRoleDialog = ref(false);
const isShowAddCourseTypeDialog =ref(false);
const isShowEditCourseTypeDialog = ref(false);
const { t } = useI18n();
const courseList = ref<M_ICourseOptions[]>([]);
const courseTypeList = ref<M_ICourseTypeOptions[]>([]);



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
const onEdit = (item: M_ICourseOptions) => {
  console.log(item.courseName);
  updateRoleForm.value?.resetFields();
  updateFormData.courseId = item.courseId;
  updateFormData.courseName = item.courseName;
  updateFormData.courseTypeId = item.courseTypeId;
  updateFormData.IsDelete = false;
  isShowEditRoleDialog.value = true;
}

const onDelet = async(item: M_ICourseOptions) => {
 
  try {
    await ElMessageBox.confirm("確定刪除?", "提示", {
      confirmButtonText: "確定",
      cancelButtonText: "取消",
      type: "warning",
    });
  } catch (error) {
    return;
  }

  deleteFormData.courseId = item.courseId;
  deleteFormData.courseName = item.courseName;
  deleteFormData.IsDelete = true;

  let notifyParam: NotificationParams = {};

  try {
    const deleteResponse = await API.updateCourse(deleteFormData);
    if (deleteResponse.data.result != 1) throw new Error(deleteResponse.data.msg);
    notifyParam = {
      title: "成功",
      type: "success",
      message: `已刪除 ${deleteFormData.courseName}`,
      duration: 1000,
    };
    getCourseOptions();
  } catch (error) {
    notifyParam = {
      title: "錯誤",
      type: "error",
      message: (error as Error).message,
      duration: 3000,
    };
  } finally {
    ElNotification(notifyParam);
  }
}

const onEditCourseType = async (item: M_ICourseTypeOptions) => {
  console.log(item.courseTypeName);
  updateCourseTypeForm.value?.resetFields();
  updateCourseTypeFormData.courseTypeId = item.courseTypeId;
  updateCourseTypeFormData.courseTypeName = item.courseTypeName;
  updateCourseTypeFormData.IsDelete = false;
  isShowEditCourseTypeDialog.value = true;
}

const onCreateCourseClicked = () => {
  createaddRoleForm.value?.resetFields();
  isShowAddRoleDialog.value = true;
}

const onCreateCourseTypeClicked = () => {
  createaddCourseTypeForm.value?.resetFields();
  isShowAddCourseTypeDialog.value = true;
}

const submitForm = async () => {
  let notifyParam: NotificationParams = {};

  createaddRoleForm.value?.validate(async(valid) => {
    if (valid) {
      const addUser = await API.addCourse(createFormData);
      if (addUser.data.result != 1) {
        notifyParam = {
          title: "失敗",
          type: "error",
          message: `課程：${createFormData.courseName} 新增失敗<br>原因：${addUser.data.msg}`,
          duration: 2000,
          dangerouslyUseHTMLString: true
        };
      }else{
        notifyParam = {
          title: "成功",
          type: "success",
          message: `課程：${createFormData.courseName} 已成功新增`,
          duration: 3000,
          dangerouslyUseHTMLString: true // 啟用 HTML 字符串，message 中使用<br>。
        };
      }

      ElNotification(notifyParam);
      isShowAddRoleDialog.value = false;
      getCourseOptions();

    } else {
      console.log('error submit!')
    }
  })
};

const submitCourseTypeForm = async () => {
  let notifyParam: NotificationParams = {};

  createaddCourseTypeForm.value?.validate(async(valid) => {
    if (valid) {
      const addUser = await API.addCourseType(createCourseTypeFormData);
      if (addUser.data.result != 1) {
        notifyParam = {
          title: "失敗",
          type: "error",
          message: `課程：${createCourseTypeFormData.courseTypeName} 新增失敗`,
          duration: 2000,
          dangerouslyUseHTMLString: true
        };
      }else{
        notifyParam = {
          title: "成功",
          type: "success",
          message: `課程：${createCourseTypeFormData.courseTypeName} 已成功新增`,
          duration: 3000,
          dangerouslyUseHTMLString: true // 啟用 HTML 字符串，message 中使用<br>。
        };
      }

      ElNotification(notifyParam);
      isShowAddCourseTypeDialog.value = false;
      getCourseOptions();
      getCourseTypeOptions();

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
      const updateResponse = await API.updateCourse(updateFormData);
      console.log(updateResponse.data.msg);

      if (updateResponse.data.result != 1) {
        notifyParam = {
          title: "失敗",
          type: "error",
          message: `課程：${updateFormData.courseName} 更新失敗`,
          duration: 2000,
        };
      } else {
        notifyParam = {
          title: "成功",
          type: "success",
          message: `帳號：${updateFormData.courseName} 已成功更新`,
          duration: 2000,
        };
        getCourseOptions();
      }

      ElNotification(notifyParam);
      isShowEditRoleDialog.value = false;
    } else {
      console.log('error submit!');
    }
  });
};

const submitCourseTypeUpdateForm = async () => {
  let notifyParam: NotificationParams = {};

  updateCourseTypeForm.value?.validate(async (valid: boolean) => {
    if (valid) {
      console.log(updateCourseTypeFormData);
      const updateResponse = await API.updateCourseType(updateCourseTypeFormData);
      console.log(updateResponse.data.msg);

      if (updateResponse.data.result != 1) {
        notifyParam = {
          title: "失敗",
          type: "error",
          message: `課程：${updateCourseTypeFormData.courseTypeName} 更新失敗`,
          duration: 2000,
        };
      } else {
        notifyParam = {
          title: "成功",
          type: "success",
          message: `帳號：${updateCourseTypeFormData.courseTypeName} 已成功更新`,
          duration: 2000,
        };
          getCourseOptions();
          getCourseTypeOptions();
      }

      ElNotification(notifyParam);
      isShowEditCourseTypeDialog.value = false;
    } else {
      console.log('error submit!');
    }
  });
};

//#endregion

//#region 建立表單ref與Validator
// 新增課程表單
const createaddRoleForm = ref<FormInstance>()
const createFormData = reactive<M_ICourseOptions>({
  courseName: '',
})

// 編輯課程表單
const updateRoleForm = ref<FormInstance>()
const updateFormData = reactive<M_ICourseOptions>({
 courseName: '',
 courseTypeId: 0,
 IsDelete: false
})

const deleteFormData = reactive<M_ICourseOptions>({
  courseName: '',
  IsDelete: true
})

// 新增分類表單
const createaddCourseTypeForm = ref<FormInstance>()
const createCourseTypeFormData = reactive<M_ICourseTypeOptions>({
  courseTypeName: ''
})

// 編輯分類表單
const updateCourseTypeForm = ref<FormInstance>()
const updateCourseTypeFormData = reactive<M_ICourseTypeOptions>({
 courseTypeName: '',
 IsDelete: false
})

// 新增表單, 編輯表單共用規則
const rules  = reactive<FormRules>({
  courseName: [{ required: true, message: () => t("validation_msg.coursename_is_required"), trigger: "blur" }],
  courseTypeName: [{ required: true, message: () => t("validation_msg.coursetype_is_required"), trigger: "blur" }],
});

const editRules  = reactive<FormRules>({
  courseName: [{ required: true, message: () => t("validation_msg.coursename_is_required"), trigger: "blur" }],
});


//#endregion

//#region Hook functions
// onActivated(() => {
  
// });
onMounted(() => {
  getCourseOptions();
  getCourseTypeOptions();
});

//#endregion

//#region Private Functions
async function getCourseOptions () {
  try {
      const response = await API.getCourse();
      courseList.value = response.data.content
      console.log(courseList.value)
    } catch (error) {
      console.error('載入課程資料失敗:', error);
    }
}

async function getCourseTypeOptions () {
  try {
      const response = await API.getCourseType();
      courseTypeList.value = response.data.content
      console.log(courseTypeList.value)
    } catch (error) {
      console.error('載入課程資料失敗:', error);
    }
}
//#endregion
</script>

<style scoped>
.el-row{
  justify-content: end;
}
</style>
