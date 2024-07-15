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
    <!-- <el-button @click="onRefreshManufMethodClicked">{{t('refresh')}}</el-button> -->
    <el-button v-if="userInfoStore.permissions.some((x) => x === 420)" @click="onAddManufMethodClicked">{{ t("create") }}</el-button>
  </div>

  <!-- table -->
  <div class="d-flex flex-column">
    <div class="align-self-end">
      <el-popover placement="bottom" @hide="onColDisplayCtrlPanelHidded" trigger="click" :Title="t('column')">
        <template #reference>
          <el-button link>
            <el-icon :size="20"><Operation /></el-icon>
          </el-button>
        </template>
        <div>
          <el-checkbox v-model="colDisplayCtrl[0]">{{ t("manufacture_method") }}</el-checkbox>
          <el-checkbox v-model="colDisplayCtrl[1]">{{ t("creator") }}</el-checkbox>
          <el-checkbox v-model="colDisplayCtrl[2]">{{ t("createDate") }}</el-checkbox>
          <el-checkbox v-model="colDisplayCtrl[3]">{{ t("remark") }}</el-checkbox>
        </div>
      </el-popover>
    </div>

    <el-table
      ref="manufMethodListTableRef"
      class="w-100"
      v-loading.fullscreen.lock="isManufMethodListTableLoading"
      :data="displayManufMethodList"
      @sort-change="onSortChanged"
    >
      <el-table-column min-width="120" v-if="colDisplayCtrl[0]" sortable="custom" :label="t('manufacture_method')" prop="methodName" />
      <el-table-column min-width="120" v-if="colDisplayCtrl[1]" sortable="custom" :label="t('creator')" prop="creator" />
      <el-table-column min-width="160" v-if="colDisplayCtrl[2]" sortable="custom" :label="t('createDate')" prop="createDate">
        <template #default="scope">{{ dateTimeFormat(scope.row.createDate) }}</template>
      </el-table-column>
      <el-table-column min-width="200" v-if="colDisplayCtrl[3]" sortable="custom" :label="t('remark')" prop="remark" />
      <el-table-column width="80" fixed="right" :label="t('operation')" prop="operate" class="operateBtnGroup d-flex">
        <template #default="scope">
          <el-button
            link
            :disabled="!userInfoStore.permissions.some((x) => x === 420)"
            :type="userInfoStore.permissions.some((x) => x === 420) ? 'success' : 'info'"
            @click="onModifyManufMethodClicked(scope.row.id)"
            ><el-icon><EditPen /></el-icon
          ></el-button>
          <el-button
            link
            :disabled="!userInfoStore.permissions.some((x) => x === 420)"
            :type="userInfoStore.permissions.some((x) => x === 420) ? 'danger' : 'info'"
            @click="onDeleteManufMethodClicked(scope.row.id)"
            ><el-icon><Delete /></el-icon
          ></el-button>
        </template>
      </el-table-column>
    </el-table>
  </div>
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

  <!-- 新增彈窗 -->
  <el-dialog class="dialog" v-model="isShowAddManufMethodDialog" :title="t('create')">
    <el-form
      @submit.prevent
      ref="addManufMethodFormRef"
      v-loading.fullscreen.lock="isAddDialogLoading"
      :model="addManufMethodFormRefData"
      :rules="addManufMethodFormRules"
    >
      <el-form-item :label="t('form.label.manufacture_method')" prop="methodName" :label-width="formLabelWidth">
        <el-input v-model.trim="addManufMethodFormRefData.methodName" :autocomplete="'off'" name="methodName" />
      </el-form-item>
      <el-form-item :label="t('form.label.remark')" :label-width="formLabelWidth">
        <textarea class="el-textarea__inner" v-model.trim="addManufMethodFormRefData.remark" :rows="4" :placeholder="t('placeholder.input')" />
      </el-form-item>
    </el-form>
    <template #footer>
      <span class="dialog-footer">
        <el-button @click="isShowAddManufMethodDialog = false">{{ t("Cancel") }}</el-button>
        <el-button type="primary" @click="onSubmitNewManufMethodClicked()">{{ t("Confirm") }}</el-button>
      </span>
    </template>
  </el-dialog>
  <!-- /新增彈窗 -->

  <!-- 編輯彈窗 -->
  <el-dialog class="dialog" v-model="isShowModifyManufMethodDialog" :title="t('modify')">
    <el-form
      @submit.prevent
      ref="modifyManufMethodFormRef"
      v-loading.fullscreen.lock="isModifyDialogLoading"
      :model="modifyManufMethodFormRefData"
      :rules="modifyManufMethodFormRules"
    >
      <el-form-item :label="t('form.label.manufacture_method')" prop="methodName" :label-width="formLabelWidth">
        <el-input v-model.trim="modifyManufMethodFormRefData.methodName" :autocomplete="'off'" name="methodName" />
      </el-form-item>
      <el-form-item :label="t('form.label.remark')" :label-width="formLabelWidth">
        <el-input v-model.trim="modifyManufMethodFormRefData.remark" type="textarea" :placeholder="t('placeholder.input')" />
      </el-form-item>
    </el-form>
    <template #footer>
      <span class="dialog-footer">
        <el-button @click="isShowModifyManufMethodDialog = false">{{ t("Cancel") }}</el-button>
        <el-button type="primary" @click="onSaveModifyClicked()">{{ t("Confirm") }}</el-button>
      </span>
    </template>
  </el-dialog>
  <!-- /編輯彈窗 -->
</template>

<script lang="ts">
import { defineComponent, reactive, toRefs, ref, onMounted, watch } from "vue";
import { useI18n } from "vue-i18n";
import API from "@/apis/TPSAPI";
import { APIResultCode } from "@/models/enums/APIResultCode";
import _ from "lodash";
import { ElMessage, ElMessageBox, ElNotification, FormItemContext } from "element-plus";
import { Search, EditPen, Delete, Filter } from "@element-plus/icons-vue";
import IResManufMethodDTO from "@/models/dto/IResManufMethodDTO";
import IReqManufMethodDTO from "@/models/dto/IReqManufMethodDTO";
import { useUserInfoStore } from "@/stores/UserInfoStore";
import { usePaginatorSetup } from "@/stores/PaginatorStore";
import ElTable from "element-plus/lib/components/table";
import { compare, dateTimeFormat, delay } from "@/plugins/utility";
import { DataTableType } from "@/models/enums/DataTableType";

export default defineComponent({
  name: "manufMethodMgmt",
  setup() {
    const { t } = useI18n();
    const paginatorSetup = usePaginatorSetup();
    const userInfoStore = useUserInfoStore();
    const tableType = DataTableType.manufMethodTable;

    const state = reactive({
      allManufMethodList: [] as IResManufMethodDTO[],
      displayManufMethodList: [] as IResManufMethodDTO[],
      searchText: "",
      currentPage: 1,
      pageSize: 20,
      filterdListLength: 0,
      isManufMethodListTableLoading: false,
      formLabelWidth: "100px",

      isShowAddManufMethodDialog: false,
      addManufMethodFormRefData: {} as IReqManufMethodDTO,
      isAddDialogLoading: false,

      isShowModifyManufMethodDialog: false,
      modifyManufMethodFormRefData: {} as IResManufMethodDTO,
      isModifyDialogLoading: false,

      colDisplayCtrl: new Array(50).fill(true) as boolean[],
    });

    //#region 建立表單ref與Validator
    const manufMethodListTableRef = ref<InstanceType<typeof ElTable>>();
    const addManufMethodFormRef = ref<FormItemContext>();
    const addManufMethodFormRules = ref({
      methodName: [
        {
          required: true,
          message: () => t("form.validation_msg.manufMethodName_is_required"),
          trigger: "blur",
        },
      ],
    });

    const modifyManufMethodFormRef = ref<FormItemContext>();
    const modifyManufMethodFormRules = ref({
      methodName: [
        {
          required: true,
          message: () => t("form.validation_msg.manufMethodName_is_required"),
          trigger: "blur",
        },
      ],
    });

    //#endregion

    //#region Hook functions
    onMounted(async () => {
      await updateUserPreference();
      await refreshManufMethodList();
    });

    //#endregion

    //#region UI Events
    /** 按下新增按鈕 */
    const onAddManufMethodClicked = () => {
      addManufMethodFormRef.value?.clearValidate();

      //初始化
      state.addManufMethodFormRefData = {
        methodName: "",
        creator: userInfoStore.$state.username,
        // createDate: new Date().toDateString(),
        remark: "",
      } as IReqManufMethodDTO;

      state.isShowAddManufMethodDialog = true;
    };

    /** 按下編輯按鈕 */
    const onModifyManufMethodClicked = (targetId: number) => {
      let target = state.allManufMethodList.find((x) => x.id === targetId);
      if (!target) return;

      modifyManufMethodFormRef.value?.clearValidate();

      state.modifyManufMethodFormRefData = {
        id: target.id,
        methodName: target.methodName,
        creator: target.createDate,
        createDate: target.createDate,
        remark: target.remark,
      };

      state.isShowModifyManufMethodDialog = true;
    };

    /** 按下刪除按鈕 */
    const onDeleteManufMethodClicked = async (targetId: number) => {
      try {
        await ElMessageBox.confirm(t("Delete_Confirm_Msg"), t("Warning"), {
          confirmButtonText: t("Confirm"),
          cancelButtonText: t("Cancel"),
          type: "warning",
        });

        const deleteResponse = await API.deleteManufMethod(targetId);
        if (deleteResponse.data.result == APIResultCode.item_is_being_used) {
          const kaizenItemCodeList = deleteResponse.data.content;
          ElNotification({
            title: t("error"),
            message:  t("delete_fail")+": "+ t("manuf_method_is_being_used"),
            type: "error",
          });
          return;
        }

        if (deleteResponse.data.result !== APIResultCode.success) {
          throw new Error(deleteResponse.data.msg);
        }

        // 刪除 下拉選單
        await deleteManufMethodById(targetId);

        await refreshManufMethodList();
      } catch (error) {
        console.error(error);
      }
    };

    /** 按下新增送出按鈕 */
    const onSubmitNewManufMethodClicked = async () => {
      try {
        await ElMessageBox.confirm(t("Create_Confirm_Msg"), t("Warning"), {
          confirmButtonText: t("Confirm"),
          cancelButtonText: t("Cancel"),
          type: "warning",
        });
      } catch (error) {
        console.error(error);
        return;
      }
      try {
        state.isAddDialogLoading = true;
        await createManufMethod();

        state.isShowAddManufMethodDialog = false;
        // ElMessage({
        //   showClose: true,
        //   message: "新增成功",
        //   type: "success",
        // });
        state.isManufMethodListTableLoading = true;
        await refreshManufMethodList();
      } catch (error) {
        console.log(error);
        ElMessage({
          type: "error",
          message: "處理失敗，請查看詳細訊息",
        });
      } finally {
        state.isManufMethodListTableLoading = false;
        state.isAddDialogLoading = false;
        state.isShowAddManufMethodDialog = false;
      }
    };

    /** 按下修改存檔按鈕 */
    const onSaveModifyClicked = async () => {
      try {
        await ElMessageBox.confirm(t("Modify_Confirm_Msg"), t("Warning"), {
          confirmButtonText: t("Confirm"),
          cancelButtonText: t("Cancel"),
          type: "warning",
        });

        state.isModifyDialogLoading = true;
        await modifyManufMethod();

        state.isManufMethodListTableLoading = true;
        await refreshManufMethodList();
      } catch (error) {
        console.error(error);
      } finally {
        state.isManufMethodListTableLoading = false;
        state.isModifyDialogLoading = false;
        state.isShowModifyManufMethodDialog = false;
      }
    };

    /** 過濾條件輸入(debounce) */
    const onFilterInputed = _.debounce(function () {
      updateDisplayList();
    }, 500);

    /** 改變排序 */
    const onSortChanged = (sort: any) => {
      //console.log(`onSortChanged: ${sort.column}, ${sort.prop}, ${sort.order}`);
      try {
        state.isManufMethodListTableLoading = true;

        state.allManufMethodList.sort(compare(sort.prop, sort.order));
        updateDisplayList();
      } catch (error) {
        console.log(error);
      } finally {
        state.isManufMethodListTableLoading = false;
      }
    };

    const onPagesizeChanged = () => {
      updateDisplayList();
    };

    const onCurrentPageChanged = () => {
      updateDisplayList();
    };

    /** 當PageDDLMgmt中，選擇到此tab時將觸發此函式 */
    const onTabSelected = async () => {
      await refreshManufMethodList();
    };

    /** 關閉欄位顯示設定Panel時 */
    const onColDisplayCtrlPanelHidded = async () => {
      await API.patchTableDisplayPreference(tableType, { displayMap: state.colDisplayCtrl });
    };

    //#endregion

    //#region Private Functions
    /** 更新使用者設置偏好 */
    const updateUserPreference = async () => {
      const getPrefResponse = await API.getTableDisplayPreference(tableType);
      if (getPrefResponse.data.result !== 1) {
        console.log(`updateUserPreference : ${getPrefResponse.data.msg}`);
      }
      state.colDisplayCtrl = getPrefResponse.data.content;
    };

    /** 更新Table資料 */
    async function refreshManufMethodList() {
      try {
        state.isManufMethodListTableLoading = true;
        if (process.env.VUE_APP_RUN_ENV === "DEV") await delay(1000);
        await getAllManufMethods();
        await updateDisplayList();
      } catch (error) {
        console.error(error);
      } finally {
        state.isManufMethodListTableLoading = false;
      }
    }

    /** 呼叫API, 取得所有工藝清單 */
    async function getAllManufMethods() {
      try {
        const getResult = await API.getManufMethods();

        if (getResult.data.result !== APIResultCode.success) {
          throw new Error(getResult.data.msg);
        }

        state.allManufMethodList = getResult.data.content;
      } catch (error: any) {
        console.error(error);
        ElMessage({
          type: "error",
          message: "處理失敗，請查看詳細訊息",
        });
      }
    }

    /** 更新工藝清單顯示結果 */
    function updateDisplayList() {
      const filterdData = state.allManufMethodList.filter((x) => {
        let FilterResult = false;
        if (x.methodName) FilterResult ||= x.methodName.toUpperCase().includes(state.searchText.toUpperCase());
        if (x.creator) FilterResult ||= x.creator.toUpperCase().includes(state.searchText.toUpperCase());
        if (x.remark) FilterResult ||= x.remark.toUpperCase().includes(state.searchText.toUpperCase());

        return FilterResult;
      });

      state.filterdListLength = filterdData.length;
      state.displayManufMethodList = filterdData.slice((state.currentPage - 1) * state.pageSize, state.currentPage * state.pageSize);
    }

    /** 呼叫API, 建立一筆資料 */
    async function createManufMethod() {
      try {
        const newItem: IReqManufMethodDTO = {
          methodName: state.addManufMethodFormRefData.methodName,
          creator: userInfoStore.$state.username,
          // createDate: new Date().toDateString(),
          remark: state.addManufMethodFormRefData.remark,
        } as IReqManufMethodDTO;

        const modifyResult = await API.addManufMethod(newItem);

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
    async function modifyManufMethod() {
      try {
        const targetId = state.modifyManufMethodFormRefData.id;
        const modifedItem: IReqManufMethodDTO = {
          methodName: state.modifyManufMethodFormRefData.methodName,
          creator: userInfoStore.$state.username,
          // createDate: new Date().toDateString(),
          remark: state.modifyManufMethodFormRefData.remark,
        } as IReqManufMethodDTO;
        const modifyResult = await API.modifyManufMethod(targetId, modifedItem);

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
    async function deleteManufMethodById(targetId: number) {
      try {
        const deleteResult = await API.deleteManufMethod(targetId);
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
      addManufMethodFormRef,
      addManufMethodFormRules,
      modifyManufMethodFormRef,
      modifyManufMethodFormRules,
      manufMethodListTableRef,

      // icons
      Search,
      EditPen,
      Delete,
      Filter,

      // function
      onAddManufMethodClicked,
      onModifyManufMethodClicked,
      onDeleteManufMethodClicked,
      onSaveModifyClicked,
      onSubmitNewManufMethodClicked,
      onFilterInputed,
      onPagesizeChanged,
      onCurrentPageChanged,
      onSortChanged,
      onTabSelected,
      onColDisplayCtrlPanelHidded,
      dateTimeFormat,
    };
  },
});
</script>

<style scoped>
textarea:hover,
textarea:focus {
  outline: 0;
  box-shadow: 0 0 0 1px #409eff inset;
}
</style>
