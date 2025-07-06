<template>
  <!-- Ipt、Btn Group -->
  <div class="d-flex mb-2 pl-2">
    <el-input
      class="me-auto"
      style="width: 240px"
      v-model="searchText"
      :placeholder="t('Filter')"
      :prefix-icon="Filter"
      @input="onFilterInputed"
      clearable
    />
    <el-button v-if="userInfoStore.permissions.some((x) => x === 420)" @click="onCreateRoleClicked">{{ t("create") }}</el-button>
  </div>

  <!-- table -->
  <el-row>
    <el-col :span="24">
      <el-table name="roleListTable" v-loading.fullscreen.lock="isRoleListTableLoading" :data="displayRoleList" style="width: 100%" height="300">
        <el-table-column :label="t('role')" prop="name" />
        <el-table-column :label="t('creator')" prop="creatorDisplayName" />
        <el-table-column :label="t('createDate')" prop="createTime" />
        <el-table-column :label="t('description')" prop="description" />
        <el-table-column :label="t('status')" prop="isEnable">
          <template #default="scope">
            <el-tag v-if="scope.row.isEnable" type="success">{{ t("enable") }}</el-tag>
            <el-tag v-else type="danger">{{ t("disable") }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column :label="t('operation')" align="center" prop="operate" class="operateBtnGroup d-flex" width="100px">
          <template #default="scope">
            <el-button
              link
              :disabled="!userInfoStore.permissions.some((x) => x === 420)"
              :type="userInfoStore.permissions.some((x) => x === 420) ? 'success' : 'danger'"
              @click="onModifyRoleClicked(scope.row)"
              ><el-icon><EditPen /></el-icon
            ></el-button>
            <el-tooltip
              v-if="scope.row.roleId == 1 || scope.row.roleId == 2"
              effect="light"
              :content="t('can_not_delete_default_role')"
              show-after="500"
              placement="top-start"
            >
              <el-button link :disabled="true" :type="'info'"
                ><el-icon><Delete /></el-icon
              ></el-button>
            </el-tooltip>
            <el-button
              v-else
              link
              :disabled="!userInfoStore.permissions.some((x) => x === 420)"
              :type="userInfoStore.permissions.some((x) => x === 420) ? 'danger' : 'info'"
              @click="onDeleteRoleClicked(scope.row)"
              ><el-icon><Delete /></el-icon
            ></el-button>
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
          v-model:currentPage="currentPage"
          v-model:page-size="pageSize"
          :pager-count="paginatorSetup.pagerCount"
          :page-sizes="[20, 50, 100]"
          :layout="paginatorSetup.layout"
          :small="paginatorSetup.small"
          :total="filterdListLength"
          @size-change="onPagesizeChanged"
          @current-change="onCurrentPageChanged"
          justify="end"
        />
      </div>
    </el-col>
  </el-row>
  <!-- /pagination -->

  <!-- 新增彈窗 -->
  <el-dialog class="dialog"  v-model="isShowAddRoleDialog" :title="t('create')">
    <el-form @submit.prevent v-loading.fullscreen.lock="isAddDialogLoading" ref="addRoleForm" :model="addRoleFormData" :rules="addRoleFormRules">
      <el-form-item :label="t('form.label.role')" prop="name" :label-width="formLabelWidth">
        <el-input v-model.trim="addRoleFormData.name" :autocomplete="'off'" />
      </el-form-item>
      <el-form-item :label="t('form.label.description')" prop="description" :label-width="formLabelWidth">
        <el-input v-model.trim="addRoleFormData.description" :rows="2" type="textarea" :placeholder="t('placeholder.input')" />
      </el-form-item>
      <el-form-item :label="t('form.label.status')" prop="isEnable" :label-width="formLabelWidth">
        <el-switch v-model="addRoleFormData.isEnable" />
      </el-form-item>
      <el-form-item :label="t('form.label.premission')" prop="permission" :label-width="formLabelWidth">
        <el-tree ref="addPermissionTreeRef" :data="permissionTree" :default-expand-all="true" show-checkbox node-key="id">
          <template #default="{ node }">
            <span>{{ t(node.label) }}</span>
          </template>
        </el-tree>
      </el-form-item>
    </el-form>
    <template #footer>
      <span class="dialog-footer">
        <el-button @click="isShowAddRoleDialog = false">{{ t("Cancel") }}</el-button>
        <el-button type="primary" @click="onSubmitNewRoleClicked()">{{ t("Confirm") }}</el-button>
      </span>
    </template>
  </el-dialog>
  <!-- /新增彈窗 -->

  <!-- 編輯彈窗 -->
  <el-dialog class="dialog"  v-model="isShowModifyRoleDialog" :title="t('modify')">
    <el-form @submit.prevent v-loading.fullscreen.lock="isModifyDialogLoading" ref="modifyRoleForm" :model="modifyRoleFormData" :rules="modifyRoleFormRules">
      <el-form-item :label="t('form.label.role')" prop="name" :label-width="formLabelWidth">
        <el-input v-model.trim="modifyRoleFormData.name" :autocomplete="'off'" />
      </el-form-item>
      <el-form-item :label="t('form.label.description')" prop="description" :label-width="formLabelWidth">
        <el-input v-model.trim="modifyRoleFormData.description" :rows="2" type="textarea" :placeholder="t('placeholder.input')" />
      </el-form-item>
      <el-form-item :label="t('form.label.status')" prop="isEnable" :label-width="formLabelWidth">
        <el-switch v-model="modifyRoleFormData.isEnable" />
      </el-form-item>
      <el-form-item :label="t('form.label.premission')" prop="permission" :label-width="formLabelWidth">
        <el-tree ref="modifyPermissionTreeRef" :data="permissionTree" :default-expand-all="true" show-checkbox node-key="id">
          <template #default="{ node }">
            <span>{{ t(node.label) }}</span>
          </template>
        </el-tree>
      </el-form-item>
    </el-form>
    <template #footer>
      <span class="dialog-footer">
        <el-button @click="isShowModifyRoleDialog = false">{{ t("Cancel") }}</el-button>
        <el-button type="primary" @click="onSaveModifyResultClicked()">{{ t("Confirm") }}</el-button>
      </span>
    </template>
  </el-dialog>
  <!-- /編輯彈窗 -->
</template>

<script lang="ts">
import { defineComponent, nextTick, reactive, toRefs, ref, onMounted, onActivated } from "vue";
import { useI18n } from "vue-i18n";
import API from "@/apis/TPSAPI";
import { APIResultCode } from "@/models/enums/APIResultCode";
import _ from "lodash";
import { ElMessage, ElMessageBox, FormItemContext } from "element-plus";
import { Search, EditPen, Delete, Filter } from "@element-plus/icons-vue";
import IResRoleInfoDTO from "@/models/dto/IResRoleInfoDTO";
import IReqRoleInfoDTO from "@/models/dto/IReqRoleInfoDTO";
import { useUserInfoStore } from "@/stores/UserInfoStore";
import { usePaginatorSetup } from "@/stores/PaginatorStore";
import { delay } from "@/plugins/utility";
import PermissionTree, { Tree } from "@/components/PermissionTree";
import { ElTree } from "element-plus/lib/components/tree";

export default defineComponent({
  name: "accountRoleMgmt",
  setup() {
    const { t } = useI18n();
    const userInfoStore = useUserInfoStore();
    const paginatorSetup = usePaginatorSetup();

    const state = reactive({
      allRoleList: [] as IResRoleInfoDTO[],
      displayRoleList: [] as IResRoleInfoDTO[],
      searchText: "",
      currentPage: 1,
      pageSize: 20,
      filterdListLength: 0,
      isRoleListTableLoading: false,
      formLabelWidth: "100px",

      isShowAddRoleDialog: false,
      isAddDialogLoading: false,
      addRoleFormData: {} as IReqRoleInfoDTO,

      isShowModifyRoleDialog: false,
      isModifyDialogLoading: false,
      modifyRoleFormData: {} as IReqRoleInfoDTO,
      selectedRole: {} as IResRoleInfoDTO,

      permissionTree: PermissionTree as Tree[],
    });

    //#region 建立表單ref與Validator
    const addRoleForm = ref<FormItemContext>();
    const addRoleFormRules = ref({
      methodName: [{ required: true, message: () => t("form.validation_msg.role_name_is_required"), trigger: "blur" }],
    });

    const modifyRoleForm = ref<FormItemContext>();
    const modifyRoleFormRules = ref({
      methodName: [{ required: true, message: () => t("form.validation_msg.role_name_is_required"), trigger: "blur" }],
    });

    const modifyPermissionTreeRef = ref<InstanceType<typeof ElTree>>();
    const addPermissionTreeRef = ref<InstanceType<typeof ElTree>>();
    //#endregion

    //#region Hook functions
    onActivated(async () => {
      await refreshRoleList();
    });
    //#endregion

    //#region UI Events
    /** 按下新增按鈕 */
    const onCreateRoleClicked = async () => {
      addRoleForm.value?.clearValidate();

      //初始化
      state.addRoleFormData = {
        name: "",
        description: "",
        isEnable: true,
        permissionIds: [],
      };

      state.isShowAddRoleDialog = true;

      await nextTick(); //等待dialog渲染完畢

      //設定已勾選的權限項目 -> 清空
      addPermissionTreeRef.value?.setCheckedKeys([], false);
    };

    /** 按下編輯按鈕 */
    const onModifyRoleClicked = async (target: IResRoleInfoDTO) => {
      modifyRoleForm.value?.clearValidate();

      state.selectedRole = target;
      state.modifyRoleFormData = {
        name: state.selectedRole.name,
        description: state.selectedRole.description,
        isEnable: state.selectedRole.isEnable,
        permissionIds: state.selectedRole.permissionIds,
      };

      state.isShowModifyRoleDialog = true;

      await nextTick(); //等待dialog渲染完畢

      //設定已勾選的權限項目 -> 套用權限清單
      modifyPermissionTreeRef.value?.setCheckedKeys(target.permissionIds, false);
    };

    /** 按下刪除按鈕 */
    const onDeleteRoleClicked = async (target: IResRoleInfoDTO) => {
      try {
        await ElMessageBox.confirm(t("Delete_Confirm_Msg"), t("Warning"), {
          confirmButtonText: t("Confirm"),
          cancelButtonText: t("Cancel"),
          type: "warning",
        });

        // 刪除 下拉選單
        await deleteRoleById(target.roleId);

        await refreshRoleList();
      } catch (error) {
        console.error(error);
      }
    };

    /** 按下新增送出按鈕 */
    const onSubmitNewRoleClicked = async () => {
      try {
        state.isAddDialogLoading = true;
        await createRole();
        await refreshRoleList();
        state.isShowAddRoleDialog = false;
      } catch (error) {
        console.error(error);
      } finally {
        state.isAddDialogLoading = false;
      }
    };

    /** 按下修改存檔按鈕 */
    const onSaveModifyResultClicked = async () => {
      try {
        await ElMessageBox.confirm(t("Modify_Confirm_Msg"), t("Warning"), {
          confirmButtonText: t("Confirm"),
          cancelButtonText: t("Cancel"),
          type: "warning",
        });

        state.isModifyDialogLoading = true;
        await modifyRole();
        await refreshRoleList();

        state.isShowModifyRoleDialog = false;
      } catch (error) {
        console.error(error);
      } finally {
        state.isModifyDialogLoading = false;
      }
    };

    /** 過濾條件輸入(debounce) */
    const onFilterInputed = _.debounce(function () {
      updateDisplayList();
    }, 500);

    const onPagesizeChanged = () => {
      updateDisplayList();
    };

    const onCurrentPageChanged = () => {
      updateDisplayList();
    };
    //#endregion

    //#region Private Functions
    /** 更新Table資料 */
    async function refreshRoleList() {
      try {
        state.isRoleListTableLoading = true;
        if (process.env.VUE_APP_RUN_ENV === "DEV") await delay(1000);

        await getAllRoles();

        updateDisplayList();
      } catch (error) {
        console.error(error);
      } finally {
        state.isRoleListTableLoading = false;
      }
    }

    /** 呼叫API, 取得所有角色清單 */
    async function getAllRoles() {
      try {
        const getRoleResult = await API.getAllRolesWithPermissions();

        if (getRoleResult.data.result !== APIResultCode.success) {
          throw new Error(getRoleResult.data.msg);
        }

        state.allRoleList = getRoleResult.data.content;
      } catch (error: any) {
        console.error(error);
        ElMessage({
          type: "error",
          message: "處理失敗，請查看詳細訊息",
        });
      }
    }

    /** 更新角色清單顯示結果 */
    function updateDisplayList() {
      const filterdData = state.allRoleList.filter((x) => {
        let FilterResult = false;
        if (x.name) FilterResult ||= x.name.toUpperCase().includes(state.searchText.toUpperCase());
        if (x.description) FilterResult ||= x.description.toUpperCase().includes(state.searchText.toUpperCase());

        return FilterResult;
      });

      state.filterdListLength = filterdData.length;
      state.displayRoleList = filterdData.slice((state.currentPage - 1) * state.pageSize, state.currentPage * state.pageSize);
    }

    /** 呼叫API, 建立一筆資料 */
    async function createRole() {
      try {
        const newItem: IReqRoleInfoDTO = {
          name: state.addRoleFormData.name,
          description: state.addRoleFormData.description,
          isEnable: state.addRoleFormData.isEnable,
          permissionIds: addPermissionTreeRef.value?.getCheckedKeys(false) as number[],
        };

        const modifyResult = await API.createRole(newItem);

        if (modifyResult.data.result !== APIResultCode.success) {
          throw new Error(modifyResult.data.msg);
        }
      } catch (error) {
        console.error(error);
        ElMessage({
          type: "error",
          message: "處理失敗，請查看詳細訊息",
        });
      }
    }

    /** 呼叫API, 更新一筆資料 */
    async function modifyRole() {
      try {
        const modifedItem: IReqRoleInfoDTO = {
          name: state.modifyRoleFormData.name,
          description: state.modifyRoleFormData.description,
          isEnable: state.modifyRoleFormData.isEnable,
          permissionIds: modifyPermissionTreeRef.value?.getCheckedKeys(false) as number[],
        };
        const modifyResult = await API.updateRole(state.selectedRole.roleId, modifedItem);

        if (modifyResult.data.result !== APIResultCode.success) {
          throw new Error(modifyResult.data.msg);
        }
      } catch (error) {
        console.error(error);
        ElMessage({
          type: "error",
          message: "處理失敗，請查看詳細訊息",
        });
      }
    }

    /** 呼叫API, 刪除一筆資料 */
    async function deleteRoleById(targetId: number) {
      try {
        const deleteResult = await API.deleteRole(targetId);
        if (deleteResult.data.result !== APIResultCode.success) {
          throw new Error(deleteResult.data.msg);
        }
      } catch (error) {
        console.error(error);
        ElMessage({
          type: "error",
          message: "處理失敗，請查看詳細訊息",
        });
      }
    }
    //#endregion

    return {
      ...toRefs(state),
      t,
      userInfoStore,
      paginatorSetup,
      addRoleForm,
      addRoleFormRules,
      addPermissionTreeRef,
      modifyRoleForm,
      modifyRoleFormRules,
      modifyPermissionTreeRef,

      // icons
      Search,
      EditPen,
      Delete,
      Filter,

      // function
      onCreateRoleClicked,
      onModifyRoleClicked,
      onDeleteRoleClicked,
      onSaveModifyResultClicked,
      onSubmitNewRoleClicked,
      onFilterInputed,
      onPagesizeChanged,
      onCurrentPageChanged,

      refreshRoleList,
    };
  },
});
</script>

<style scoped></style>
