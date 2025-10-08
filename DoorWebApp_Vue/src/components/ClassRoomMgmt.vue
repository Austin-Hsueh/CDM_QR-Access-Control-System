<template>
  <!-- table -->
  <el-row>
    <el-col :span="12" style="padding: 10px;">
      <el-button type="primary" @click="onCreateClassRoomClicked">新增教室</el-button>
      <el-table name="userInfoTable" style="width: 100%"  :data="classRoomList">
        <el-table-column sortable label="教室名稱"  prop="classroomName"/>
        <el-table-column sortable label="顯示顏色"  prop="description"/>
        <el-table-column width="170px" align="center" prop="operate" class="operateBtnGroup d-flex" :label="t('operation')">
          <template #default="{ row }: { row: any }">
            <el-button type="primary" size="small" @click="onEditClassRoom(row)"><el-icon><EditPen /></el-icon>{{ t('edit') }}</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-col>
  </el-row>
  <!-- /table -->

  <!-- 新增教室彈窗 -->
  <el-dialog class="dialog"  v-model="isShowAddClassRoomDialog" :title="t('create')">
    <el-form label-width="100px"  ref="createaddClassRoomForm" :rules="rules" :model="createClassRoomFormData" @keyup.enter="submitClassRoomForm()">
      <el-form-item label="教室" prop="classroomName"  >
        <el-input style="width:90%" v-model="createClassRoomFormData.classroomName"/>
      </el-form-item>
      <el-form-item label="選擇事件顏色" prop="description"  >
        <el-color-picker v-model="createClassRoomFormData.description" />
      </el-form-item>
    </el-form>
    <template #footer>
      <span class="dialog-footer">
        <el-button @click="isShowAddClassRoomDialog = false">{{ t("Cancel") }}</el-button>
        <el-button type="primary"  @click="submitClassRoomForm()">{{ t("Confirm") }}</el-button>
      </span>
    </template>
  </el-dialog>
  <!-- /新增彈窗 -->

  <!-- 編輯教室彈窗 -->
  <el-dialog class="dialog"  v-model="isShowEditClassRoomDialog" :title="t('edit')">
    <el-form label-width="100px"  ref="updateClassRoomForm" :rules="editRules" :model="updateClassRoomFormData" @keyup.enter="submitClassRoomUpdateForm()">
      <el-form-item label="教室" prop="classroomName"  >
        <el-input style="width:90%" v-model="updateClassRoomFormData.classroomName"/>
      </el-form-item>
      <el-form-item label="選擇事件顏色" prop="description"  >
        <el-color-picker v-model="updateClassRoomFormData.description" />
      </el-form-item>
    </el-form>
    <template #footer>
      <span class="dialog-footer">
        <el-button @click="isShowEditClassRoomDialog = false">{{ t("Cancel") }}</el-button>
        <el-button type="primary"  @click="submitClassRoomUpdateForm()">{{ t("Confirm") }}</el-button>
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
import {M_IClassRoomOptions} from "@/models/M_IClassRoomOptions"; 
import { de } from "element-plus/es/locale";

const isShowAddRoleDialog = ref(false);
const isShowEditRoleDialog = ref(false);
const isShowAddClassRoomDialog =ref(false);
const isShowEditClassRoomDialog = ref(false);
const { t } = useI18n();
const courseList = ref<M_ICourseOptions[]>([]);
const classRoomList = ref<M_IClassRoomOptions[]>([]);



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

const onEditClassRoom = async (item: M_IClassRoomOptions) => {
  console.log(item.classroomName);
  updateClassRoomForm.value?.resetFields();
  updateClassRoomFormData.classroomId = item.classroomId;
  updateClassRoomFormData.classroomName = item.classroomName;
  updateClassRoomFormData.description = item.description;
  updateClassRoomFormData.IsDelete = false;
  isShowEditClassRoomDialog.value = true;
}

const onCreateClassRoomClicked = () => {
  createaddClassRoomForm.value?.resetFields();
  isShowAddClassRoomDialog.value = true;
}

const submitClassRoomForm = async () => {
  let notifyParam: NotificationParams = {};

  createaddClassRoomForm.value?.validate(async(valid) => {
    if (valid) {
      const addClassRoom = await API.addClassroom(createClassRoomFormData);
      if (addClassRoom.data.result != 1) {
        notifyParam = {
          title: "新增失敗",
          type: "error",
          message: `${addClassRoom.data.msg}`,
          duration: 2000,
          dangerouslyUseHTMLString: true
        };
      }else{
        notifyParam = {
          title: "新增成功",
          type: "success",
          message: `教室：${createClassRoomFormData.classroomName} 已成功新增`,
          duration: 3000,
          dangerouslyUseHTMLString: true // 啟用 HTML 字符串，message 中使用<br>。
        };
      }

      ElNotification(notifyParam);
      isShowAddClassRoomDialog.value = false;
      getClassRoomsOptions();

    } else {
      console.log('error submit!')
    }
  })
};

const submitClassRoomUpdateForm = async () => {
  let notifyParam: NotificationParams = {};

  updateClassRoomForm.value?.validate(async (valid: boolean) => {
    if (valid) {
      console.log(updateClassRoomFormData);
      const updateResponse = await API.updateClassroom(updateClassRoomFormData);
      console.log(updateResponse.data.msg);

      if (updateResponse.data.result != 1) {
        notifyParam = {
          title: "編輯失敗",
          type: "error",
          message: `${updateResponse.data.msg} `,
          duration: 3000,
        };
      } else {
        notifyParam = {
          title: "編輯成功",
          type: "success",
          message: `帳號：${updateClassRoomFormData.classroomName} 已成功更新`,
          duration: 2000,
        };
          getClassRoomsOptions();
      }

      ElNotification(notifyParam);
      isShowEditClassRoomDialog.value = false;
    } else {
      console.log('error submit!');
    }
  });
};

//#endregion

//#region 建立表單ref與Validator

// 新增教室表單
const createaddClassRoomForm = ref<FormInstance>()
const createClassRoomFormData = reactive<M_IClassRoomOptions>({
  classroomName: '',
  description: ''
})

// 編輯教室表單
const updateClassRoomForm = ref<FormInstance>()
const updateClassRoomFormData = reactive<M_IClassRoomOptions>({
  classroomName: '',
  description: '',
  IsDelete: false
})

// 新增表單, 編輯表單共用規則
const rules  = reactive<FormRules>({
  classroomName: [{ required: true, message: () => t("validation_msg.classroomName_is_required"), trigger: "blur" }],
});

const editRules  = reactive<FormRules>({
  classroomName: [{ required: true, message: () => t("validation_msg.classroomName_is_required"), trigger: "blur" }],
});


//#endregion

//#region Hook functions
// onActivated(() => {
  
// });
onMounted(() => {
  getClassRoomsOptions();
  // getCourseTypeOptions();
});

//#endregion

//#region Private Functions
async function getClassRoomsOptions () {
  try {
      const response = await API.getClassrooms();
      classRoomList.value = response.data.content
      console.log(classRoomList.value)
    } catch (error) {
      console.error('載入教室資料失敗:', error);
    }
}

// async function getCourseTypeOptions () {
//   try {
//       const response = await API.getCourseType();
//       classRoomList.value = response.data.content
//       console.log(classRoomList.value)
//     } catch (error) {
//       console.error('載入課程資料失敗:', error);
//     }
// }
//#endregion
</script>

<style scoped>

</style>
