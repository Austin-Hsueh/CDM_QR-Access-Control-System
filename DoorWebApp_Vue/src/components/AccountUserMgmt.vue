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
    <!-- <el-button @click="onAddManufMethodClicked">{{ t("create") }}</el-button> -->
  </div>

  <!-- table -->
  <el-row>
    <el-col :span="24">
      <el-table name="userInfoTable" v-loading.fullscreen.lock="isUserInfoListTableLoading" :data="displayUserInfoList" style="width: 100%" height="300">
        <el-table-column :label="t('username')" prop="username" />
        <el-table-column :label="t('displayName')" prop="displayName" />
        <el-table-column :label="t('last_login')" prop="lastLoginTime" />
        <el-table-column :label="t('role')" prop="roleNames">
          <template #default="scope">
            {{ scope.row.roles.map((x: IRole) => x.name).toString() }}
          </template>
        </el-table-column>
        <el-table-column width="60px" align="center" prop="operate" class="operateBtnGroup d-flex" :label="t('operation')">
          <template #default="scope">
            <el-button
              link
              :disabled="!userInfoStore.permissions.some((x) => x === 420)"
              :type="userInfoStore.permissions.some((x) => x === 420) ? 'success' : 'danger'"
              @click="onModifyUserInfoClicked(scope.row)"
              ><el-icon><EditPen /></el-icon
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
          :layout="paginatorSetup.layout"
          :small="paginatorSetup.small"
          :page-sizes="[20, 50, 100]"
          :total="filterdListLength"
          @size-change="onPagesizeChanged"
          @current-change="onCurrentPageChanged"
          justify="end"
        />
      </div>
    </el-col>
  </el-row>
  <!-- /pagination -->

  <!-- 編輯彈窗 -->
  <el-dialog class="dialog" top="3vh" v-model="isShowModifyUserInfoDialog" :title="t('modify')">
    <el-form
      @submit.prevent
      v-loading.fullscreen.lock="isModifyDialogLoading"
      ref="modifyManufMethodForm"
      :model="selectedUserInfo"
      :rules="modifyManufMethodFormRules"
    >
      <el-form-item :label-width="formLabelWidth" :label="t('form.label.username')" >
        <label>{{ selectedUserInfo.username }}</label>
      </el-form-item>
      <el-form-item :label-width="formLabelWidth" :label="t('form.label.display_name')" >
        <label>{{ selectedUserInfo.displayName }}</label>
      </el-form-item>
      <el-form-item :label-width="formLabelWidth" :label="t('form.label.role')">
        <el-checkbox-group class="d-flex flex-column" v-model="selectedUserInfo.roleNames">
          <el-checkbox v-for="role in roleList" :key="role.roleId" :label="role.name">{{ role.name }}</el-checkbox>
        </el-checkbox-group>
      </el-form-item>
    </el-form>
    <template #footer>
      <span class="dialog-footer">
        <el-button @click="isShowModifyUserInfoDialog = false">{{ t("Cancel") }}</el-button>
        <el-button type="primary" @click="onSaveModifyClicked()">{{ t("Confirm") }}</el-button>
      </span>
    </template>
  </el-dialog>
  <!-- /編輯彈窗 -->
</template>

<script lang="ts">
import { defineComponent, reactive, toRefs, ref, onMounted, onActivated } from "vue";
import { useI18n } from "vue-i18n";
import API from "@/apis/TPSAPI";
import { APIResultCode } from "@/models/enums/APIResultCode";
import _ from "lodash";
import { ElMessage, ElMessageBox, FormItemContext } from "element-plus";
import { Search, EditPen, Delete, Filter } from "@element-plus/icons-vue";
import { useUserInfoStore } from "@/stores/UserInfoStore";
import { usePaginatorSetup } from "@/stores/PaginatorStore";
import { delay } from "@/plugins/utility";
import IResUserInfoDTO from "@/models/dto/IResUserInfoDTO";
import IResRoleInfoDTO from "@/models/dto/IResRoleInfoDTO";
import IRole from "@/models/IRole";
import IReqUserRoleDTO from "@/models/dto/IReqUserRoleDTO";
import IUserRole from "@/models/IUserRole";

export default defineComponent({
  name: "accountUserMgmt",
  setup() {
    const { t } = useI18n();
    const paginatorSetup = usePaginatorSetup();
    const userInfoStore = useUserInfoStore();

    const state = reactive({
      allUserInfoList: [] as IResUserInfoDTO[],
      displayUserInfoList: [] as IResUserInfoDTO[],
      searchText: "",
      currentPage: 1,
      pageSize: 20,
      filterdListLength: 0,
      isUserInfoListTableLoading: false,
      formLabelWidth: "100px",
      isShowModifyUserInfoDialog: false,
      isModifyDialogLoading: false,
      selectedUserInfo: {} as IUserRole,
      modifyUserInfoFormData: {} as IResUserInfoDTO,
      roleList: [] as IResRoleInfoDTO[],
    });

    //#region 建立表單ref與Validator
    const modifyUserInfoForm = ref<FormItemContext>();
    const modifyUserInfoFormRules = ref({
      methodName: [{ required: true, message: () => t("form.validation_msg.manufMethodName_is_required"), trigger: "blur" }],
    });

    //#endregion

    //#region Hook functions
    onActivated(async () => {
      await refreshUserList();
    });
    //#endregion

    //#region UI Events
    /** 按下重新整理按鈕 */
    // const onRefreshManufMethodClicked = async () => {
    //   await refreshManufMethodList();
    // };

    /** 按下編輯按鈕 */
    const onModifyUserInfoClicked = (targetUser: IResUserInfoDTO) => {
      state.selectedUserInfo = {
        userId: targetUser.userId,
        username: targetUser.username,
        displayName: targetUser.displayName,
        email: targetUser.email,
        roleNames: targetUser.roles.map((x) => x.name),
      };

      //state.selectedUserInfo.roleNames.push(state.roleList[0].name);

      modifyUserInfoForm.value?.clearValidate();

      state.modifyUserInfoFormData = {
        userId: targetUser.userId,
        username: targetUser.username,
        displayName: targetUser.displayName,
        email: targetUser.email,
        lastLoginTime: targetUser.lastLoginTime,
        roles: targetUser.roles,
      };

      state.isShowModifyUserInfoDialog = true;
    };

    /** 按下修改存檔按鈕 */
    const onSaveModifyClicked = async () => {
      try {
        await ElMessageBox.confirm(t("Modify_Confirm_Msg"), t("Warning"), {
          confirmButtonText: t("Confirm"),
          cancelButtonText: t("Cancel"),
          type: "warning",
        });

        await modifyUserInfo();

        await refreshUserList();

        state.isShowModifyUserInfoDialog = false;
      } catch (error) {
        console.error(error);
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
    async function refreshUserList() {
      try {
        state.isUserInfoListTableLoading = true;
        if (process.env.VUE_APP_RUN_ENV === "DEV") await delay(1000);

        await getAllRoles();

        await getAllUsers();

        await updateDisplayList();
      } catch (error) {
        console.error(error);
      } finally {
        state.isUserInfoListTableLoading = false;
      }
    }

    /** 呼叫API, 取得所有角色清單 */
    async function getAllRoles() {
      try {
        const getRoleResult = await API.getAllRolesWithPermissions();

        if (getRoleResult.data.result !== APIResultCode.success) {
          throw new Error(getRoleResult.data.msg);
        }

        state.roleList = getRoleResult.data.content;
      } catch (error: any) {
        console.error(error);
        ElMessage({
          type: "error",
          message: "處理失敗，請查看詳細訊息",
        });
      }
    }

    async function getAllUsers() {
      try {
        const getUserResult = await API.getAllUsersWithRoles();

        if (getUserResult.data.result !== APIResultCode.success) {
          throw new Error(getUserResult.data.msg);
        }

        state.allUserInfoList = getUserResult.data.content;
      } catch (error: any) {
        console.error(error);
        ElMessage({
          type: "error",
          message: "處理失敗，請查看詳細訊息",
        });
      }
    }

    /** 更新使用者清單顯示結果 */
    function updateDisplayList() {
      const filterdData = state.allUserInfoList.filter((x) => {
        let FilterResult = false;
        if (x.username) FilterResult ||= x.username.toUpperCase().includes(state.searchText.toUpperCase());
        if (x.displayName) FilterResult ||= x.displayName.toUpperCase().includes(state.searchText.toUpperCase());
        return FilterResult;
      });

      state.filterdListLength = filterdData.length;
      state.displayUserInfoList = filterdData.slice((state.currentPage - 1) * state.pageSize, state.currentPage * state.pageSize);
    }

    /** 呼叫API, 更新一筆資料 */
    async function modifyUserInfo() {
      try {
        state.isModifyDialogLoading = true;

        const selectedRoleIds = state.selectedUserInfo.roleNames.map((x) => {
          const role = state.roleList.find((y) => y.name === x);
          return role ? role.roleId : -1;
        });
        const modifedItem: IReqUserRoleDTO = {
          userId: state.selectedUserInfo.userId,
          roleIds: selectedRoleIds,
        };

        const modifyResult = await API.updateUserRoles(state.selectedUserInfo.userId, modifedItem);

        if (modifyResult.data.result !== APIResultCode.success) {
          throw new Error(modifyResult.data.msg);
        }
      } catch (error) {
        console.error(error);
        ElMessage({
          type: "error",
          message: "處理失敗，請查看詳細訊息",
        });
      } finally {
        state.isModifyDialogLoading = false;
      }
    }

    //#endregion

    return {
      ...toRefs(state),
      t,
      userInfoStore,
      paginatorSetup,
      modifyManufMethodForm: modifyUserInfoForm,
      modifyManufMethodFormRules: modifyUserInfoFormRules,

      // icons
      Search,
      EditPen,
      Delete,
      Filter,

      // function
      onModifyUserInfoClicked,
      onSaveModifyClicked,
      onFilterInputed,
      onPagesizeChanged,
      onCurrentPageChanged,

      refreshUserList,
    };
  },
});
</script>

<style scoped></style>
