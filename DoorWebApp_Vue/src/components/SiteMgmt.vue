<template>
  <!-- Ipt、Btn Group -->
  <div class="d-flex mb-2 pl-2">
    <!-- Search Ipt -->
    <el-input
      class="me-auto"
      style="width: 240px"
      v-model="searchText"
      :placeholder="t('Filter')"
      :prefix-icon="Filter"
      @input="onFilterInputed"
      clearable
    />
    <!-- Add Site Btn -->
    <el-button v-if="userInfoStore.permissions.some((x) => x === 420)" @click="onAddSiteClicked">{{ t("create") }}</el-button>
  </div>
  <!-- /Ipt、Btn Group -->

  <!-- Table -->
  <div class="d-flex flex-column">
    <div class="align-self-end">
      <el-popover placement="bottom" @hide="onColDisplayCtrlPanelHidded" trigger="click" :Title="t('column')">
        <template #reference>
          <el-button link>
            <el-icon :size="20"><Operation /></el-icon>
          </el-button>
        </template>
        <div>
          <el-checkbox v-model="colDisplayCtrl[0]">{{ t("site") }}</el-checkbox>
          <el-checkbox v-model="colDisplayCtrl[1]">{{ t("factorys") }}</el-checkbox>
          <el-checkbox v-model="colDisplayCtrl[2]">{{ t("remark") }}</el-checkbox>
        </div>
      </el-popover>
    </div>
    <el-table ref="manufMethodListTableRef" class="w-100" v-loading.fullscreen.lock="isSiteListTableLoading" :data="displaySiteList" @sort-change="onSortChanged">
      <el-table-column min-width="80" v-if="colDisplayCtrl[0]" sortable="custom" :label="t('site')" prop="name" />
      <el-table-column min-width="80" v-if="colDisplayCtrl[1]" :label="t('factorys')" prop="factories">
        <template #default="scope">
          {{ scope.row.factories.map((x: IFactory) => x.name).toString() }}
        </template>
      </el-table-column>
      <el-table-column min-width="200" v-if="colDisplayCtrl[2]" sortable="custom" :label="t('remark')" prop="remark" />
      <el-table-column width="80" fixed="right" :label="t('operation')" prop="operate" class="operateBtnGroup d-flex">
        <template #default="scope">
          <el-button 
            link 
            :disabled="!userInfoStore.permissions.some((x) => x === 420)"
            :type="userInfoStore.permissions.some((x) => x === 420) ? 'success' : 'info'"
            @click="onModifySiteClicked(scope.row.id)">
            <el-icon><EditPen /></el-icon>
          </el-button>
          <el-button 
            link 
            :disabled="!userInfoStore.permissions.some((x) => x === 420)"
            :type="userInfoStore.permissions.some((x) => x === 420) ? 'danger' : 'info'"
            @click="onDeleteSiteClicked(scope.row.id)">
            <el-icon><Delete /></el-icon>
          </el-button>
        </template>
      </el-table-column>
    </el-table>
  </div>
  <!-- /Table -->

  <!-- pagination -->
  <el-row justify="end" class="mt-3">
    <el-col>
      <div class="demo-pagination-block mt-3 d-flex justify-content-end">
        <!-- pagination -->
        <el-pagination
          v-model:currentPage="currentPage"
          v-model:page-size="pageSize"
          :pager-count="paginatorSetup.pagerCount"
          :layout="paginatorSetup.layout"
          :small="paginatorSetup.small"
          :page-sizes="[2, 50, 100, 150, 200]"
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
  <el-dialog class="dialog" v-model="isShowAddSiteDialog" :title="t('create')">
    <el-form @submit.prevent ref="addSiteFormRef" v-loading.fullscreen.lock="isAddDialogLoading" :model="newSiteFormData" :rules="addSiteFormRules">
      <el-form-item :label="t('form.label.site')" prop="name" :label-width="formLabelWidth">
        <el-select
          v-model="newSiteFormData.name"
          :loading="isSiteOptionLoading"
          :placeholder="t('placeholder.select')"
          @visible-change="onSiteOptionListShown">
          <el-option v-for="site in siteOptions" :key="site" :label="site" :value="site"/>
        </el-select>
      </el-form-item>
      <el-form-item :label="t('form.label.factories')" prop="factoryNames" :label-width="formLabelWidth">
        <AddTags v-model=" newSiteFormData.factoryNames"/>
      </el-form-item>
      <el-form-item :label="t('form.label.remark')" prop="remark" :label-width="formLabelWidth">
        <el-input v-model="newSiteFormData.remark" :rows="4" type="textarea" :placeholder="t('placeholder.input')" />
      </el-form-item>
    </el-form>
    <template #footer>
      <span class="dialog-footer">
        <el-button @click="isShowAddSiteDialog = false">{{ t("Cancel") }}</el-button>
        <el-button type="primary" @click="onSubmitNewSiteClicked()">{{ t("Confirm") }}</el-button>
      </span>
    </template>
  </el-dialog>
  <!-- /新增彈窗 -->

  <!-- 修改彈窗 -->
  <el-dialog class="dialog" v-model="isShowModifySiteDialog" :title="t('modify')">
    <el-form @submit.prevent ref="modifySiteFormRef" v-loading.fullscreen.lock="isModifyDialogLoading" :model="selectedSiteData" :rules="modifySiteFormRules">
      <el-form-item :label="t('form.label.site')" prop="name" :label-width="formLabelWidth">
        <label>{{selectedSiteData.name}}</label>
      </el-form-item>
      <el-form-item :label="t('form.label.factories')" prop="factoryNames" :label-width="formLabelWidth">
        <AddTags v-model="selectedFactoryNames"/>
      </el-form-item>
      <el-form-item :label="t('form.label.remark')" prop="remark" :label-width="formLabelWidth">
        <el-input v-model="selectedSiteData.remark" :rows="4" type="textarea" :placeholder="t('placeholder.input')" />
      </el-form-item>
    </el-form>
    <template #footer>
      <span class="dialog-footer">
        <el-button @click="isShowModifySiteDialog = false">{{ t("Cancel") }}</el-button>
        <el-button type="primary" @click="onSubmitModifySiteClicked()">{{ t("Confirm") }}</el-button>
      </span>
    </template>
  </el-dialog>
  <!-- /修改彈窗 -->
</template>

<script lang="ts">
/** Vue */
import { defineComponent, reactive, toRefs, ref, onMounted } from "vue";

/** 套件 */
import { useI18n } from "vue-i18n";
import _ from "lodash";

/** Element Plus */
import { ElMessage, ElMessageBox } from "element-plus";
import { Filter } from "@element-plus/icons-vue";

/** API */
import API from "@/apis/TPSAPI";
import { APIResultCode } from "@/models/enums/APIResultCode";

/** Store  */
import { useUserInfoStore } from "@/stores/UserInfoStore";

/** Utils */
import { compare, delay } from "@/plugins/utility";

/** Custom Components */
import AddTags from "@/components/AddTags.vue";

/** Custom DTO */
import IResSiteInfoDTO from "@/models/dto/IResSiteInfoDTO";

/** Custom Enums */
import { usePaginatorSetup } from "@/stores/PaginatorStore";
import IReqSiteInfoDTO from "@/models/dto/IReqSiteInfoDTO";

import IFactory from "@/models/IFactory";
import { DataTableType } from "@/models/enums/DataTableType";
import { Label } from "@ionic/core/dist/types/components/label/label";

export default defineComponent({
  components: {
    AddTags,
  },
  name: "SiteMgmt",
  setup() {
    //#region
    const { t } = useI18n();
    const userInfoStore = useUserInfoStore();
    const paginatorSetup = usePaginatorSetup();
    const tableType = DataTableType.siteTable;

    const state = reactive({
      /* Component Data */
      allSiteList: [] as IResSiteInfoDTO[],
      displaySiteList: [] as IResSiteInfoDTO[],
      filterdListLength: 0,
      isSiteListTableLoading: false,
      formLabelWidth: "100px",

      /* 廠區選擇清單 */
      isSiteOptionLoading: false,
      siteOptions: [] as string[],

      /* 新增廠區資料 */
      newSiteFormData: {} as IReqSiteInfoDTO,
      isShowAddSiteDialog: false,
      isAddDialogLoading: false,

      /* 修改廠區資料 */
      
      selectedFactoryNames:[] as string[],
      isShowModifySiteDialog: false,
      isModifyDialogLoading: false,

      /* 工廠Tags */
      selectedSiteData: {} as IResSiteInfoDTO,
      searchText: "",
      currentPage: 1,
      pageSize: 20,

      colDisplayCtrl: new Array(50).fill(true) as boolean[],
    });

    //#endregion

    //#region 建立表單ref與Validator
    /* 自訂檢查 Site SiteName */
    const checkSiteNameValidator = (rules: any, value: string, callback: any) => {
      if (!value) {
        callback(new Error(t("form.validation_msg.SiteName_is_required")));
      }

      //檢查重複
      const duplicateNameItem = state.allSiteList.find((x) => x.name === value.trim());
      if (duplicateNameItem) {
        //當新增時
        if (state.isShowAddSiteDialog) {
          callback(new Error(t("form.validation_msg.SiteName_is_repeated")));
          return;
        }

        //當修改時 (是否與其他名稱有重複(撇除自身))
        if (duplicateNameItem.id !== state.selectedSiteData.id) {
          callback(new Error(t("form.validation_msg.SiteName_is_repeated")));
          return;
        }
      }
      callback();
    };

    /* 新增廠區 表單ref與Validator */
    const addSiteFormRef = ref();
    const addSiteFormRules = ref({
      name: [{ validator: checkSiteNameValidator, trigger: "blur" }],
    });

    /* 修改廠區 表單ref與Validator */
    const modifySiteFormRef = ref();
    const modifySiteFormRules = ref({
      name: [{ validator: checkSiteNameValidator, trigger: "blur" }],
    });

    //#endregion

    //#region Hook functions
    onMounted(async () => {
      try {
        console.log("onMounted");

        state.isSiteListTableLoading = true;
        await updateUserPreference();
        await refreshSiteList();
      } catch (error) {
        console.log(error);
        ElMessage({
          type: "error",
          message: "處理失敗，請查看詳細訊息",
        });
      } finally {
        state.isSiteListTableLoading = false;
      }
    });

    //#endregion

    //#region UI Events

    /** 按下 新增 新增廠區 按鈕 */
    const onAddSiteClicked = () => {
      /* 重置檢查 */
      clearValidator(addSiteFormRef);

      // 初始化
      state.newSiteFormData.name = "";
      state.newSiteFormData.factoryNames = [];
      state.newSiteFormData.remark = "";

      /* 顯示 新增廠區彈窗 */
      state.isShowAddSiteDialog = true;
    };

    /** 按下 送出 新增廠區 按鈕 */
    const onSubmitNewSiteClicked = async () => {
      try {
        /* 檢查欄位 */
        await addSiteFormRef.value.validate();

        await ElMessageBox.confirm(t("Create_Confirm_Msg"), t("Warning"), {
          confirmButtonText: t("Confirm"),
          cancelButtonText: t("Cancel"),
          type: "warning",
        });
      } catch (error) {
        return;
      }

      try {
        /* Call 新增廠區 */
        state.isAddDialogLoading = true;
        const addSiteResult = await API.addSite(state.newSiteFormData);
        state.isAddDialogLoading = false;

        if (addSiteResult.data.result !== APIResultCode.success) {
          throw new Error(addSiteResult.data.msg);
        }

        state.isShowAddSiteDialog = false;
        ElMessage({
          showClose: true,
          message: t("create_success"),
          type: "success",
        });

        /* 刷新資料 */
        state.isSiteListTableLoading = true;
        await refreshSiteList();
        state.isSiteListTableLoading = false;
      } catch (error) {
        console.error(error);
        ElMessage({
          type: "error",
          message: "處理失敗，請查看詳細訊息",
        });
      } finally {
        state.isAddDialogLoading = false;
        state.isSiteListTableLoading = false;
      }
    };

    /** 按下 修改 修改廠區 按鈕 */
    const onModifySiteClicked = async (siteId: number) => {
      try {
        /* 重置檢查 */
        clearValidator(modifySiteFormRef);

        /* 取得資料 */
        // 撈取資料
        const getSiteResponse = await API.getSiteById(siteId);
        if (getSiteResponse.data.result !== APIResultCode.success) {
          throw new Error(getSiteResponse.data.msg);
        }

        state.selectedSiteData = getSiteResponse.data.content;
        state.selectedFactoryNames = state.selectedSiteData.factories.map(x => x.name);

        /* 顯示 新增廠區彈窗 */
        state.isShowModifySiteDialog = true;
      } catch (error) {
        console.error(error);
        ElMessage({
          type: "error",
          message: "處理失敗，請查看詳細訊息",
        });
      }
    };

    /** 按下 送出 修改廠區 按鈕 */
    const onSubmitModifySiteClicked = async () => {
      try {
        /* 檢查欄位 */
        await modifySiteFormRef.value.validate();

        await ElMessageBox.confirm(t("Modify_Confirm_Msg"), t("Warning"), {
          confirmButtonText: t("Confirm"),
          cancelButtonText: t("Cancel"),
          type: "warning",
        });
      } catch (error) {
        return;
      }

      try {
        /* Call 修改廠區 */
        const siteId = state.selectedSiteData.id;
        const modifiedItem: IReqSiteInfoDTO = {
          id: siteId,
          name: state.selectedSiteData.name,
          factoryNames: state.selectedFactoryNames,
          remark: state.selectedSiteData.remark,
        };

        state.isModifyDialogLoading = true;
        const modifySiteResponse = await API.modifySite(siteId, modifiedItem);
        state.isModifyDialogLoading = false;

        if (modifySiteResponse.data.result !== APIResultCode.success) {
          throw new Error(modifySiteResponse.data.msg);
        }

        state.isShowModifySiteDialog = false;
        ElMessage({
          showClose: true,
          message: t("modify_success"),
          type: "success",
        });

        /* 刷新資料 */
        state.isSiteListTableLoading = true;
        await refreshSiteList();
        state.isSiteListTableLoading = false;
      } catch (error) {
        console.error(error);
        ElMessage({
          type: "error",
          message: "處理失敗，請查看詳細訊息",
        });
      } finally {
        state.isModifyDialogLoading = false;
        state.isSiteListTableLoading = false;
      }
    };

    /** 按下 刪除 刪除廠區 按鈕 */
    const onDeleteSiteClicked = async (siteId: number) => {
      try {
        await ElMessageBox.confirm(t("Delete_Confirm_Msg"), t("Warning"), {
          confirmButtonText: t("Confirm"),
          cancelButtonText: t("Cancel"),
          type: "warning",
        });

        state.isSiteListTableLoading = true;

        // 刪除 下拉選單
        await deleteSiteAsync(siteId);

        ElMessage({
          showClose: true,
          message: t("delete_success"),
          type: "success",
        });

        /* 刷新資料 */
        await refreshSiteList();
      } catch (error) {
        console.error(error);
        ElMessage({
          type: "error",
          message: "處理失敗，請查看詳細訊息",
        });
      } finally {
        state.isSiteListTableLoading = false;
      }
    };

    /** 搜尋 Ipt 過濾條件輸入(debounce) */
    const onFilterInputed = _.debounce(async function () {
      try {
        state.isSiteListTableLoading = true;
        await refreshSiteList();
      } catch (error) {
        console.log(error);
        ElMessage({
          type: "error",
          message: "處理失敗，請查看詳細訊息",
        });
      } finally {
        state.isSiteListTableLoading = false;
      }
    }, 500);

    /** 改變排序 */
    const onSortChanged = (sort: any) => {
      //console.log(`onSortChanged: ${sort.column}, ${sort.prop}, ${sort.order}`);
      try {
        state.isSiteListTableLoading = true;

        state.allSiteList.sort(compare(sort.prop, sort.order));
        updateDisplayList();
      } catch (error) {
        console.log(error);
      } finally {
        state.isSiteListTableLoading = false;
      }
    };

    const onPagesizeChanged = async () => {
      updateDisplayList();
    };

    const onCurrentPageChanged = async () => {
      updateDisplayList();
    };

    /** 當PageDDLMgmt中，選擇到此tab時將觸發此函式 */
    const onTabSelected = async () => {
      try {
        state.isSiteListTableLoading = true;
        await refreshSiteList();
        state.isSiteListTableLoading = false;
      } catch (error) {
        console.log(error);
        ElMessage({
          type: "error",
          message: "處理失敗，請查看詳細訊息",
        });
      } finally {
        state.isSiteListTableLoading = false;
      }
    };

    /** 關閉欄位顯示設定Panel時 */
    const onColDisplayCtrlPanelHidded = async () => {
      await API.patchTableDisplayPreference(tableType, { displayMap: state.colDisplayCtrl });
    };

    const onSiteOptionListShown = async () => {
      try {
        state.isSiteOptionLoading = true;

        const siteOptionsResponse = await API.getSiteOptions();
        if(siteOptionsResponse.data.result !== APIResultCode.success) {
          throw new Error(siteOptionsResponse.data.msg);
        }

        state.siteOptions = siteOptionsResponse.data.content;
      } catch (error) {
        console.log(error);
      } finally {
        state.isSiteOptionLoading = false;
      }
    }
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

    /** 更新顯示資料 */
    const refreshSiteList = async () => {
      /* 若 開發時 等待 1 秒 */
      if (process.env.VUE_APP_RUN_ENV === "DEV") await delay(1000);

      await getAllSitesAsync();
      updateDisplayList();
    };

    /** 過濾顯示結果 */
    function updateDisplayList() {
      const filterdData = state.allSiteList.filter((x) => {
        let FilterResult = false;
        const keyword = state.searchText.toUpperCase();
        if (x.name) FilterResult ||= x.name.toUpperCase().includes(keyword);
        if (x.remark) FilterResult ||= x.remark.toUpperCase().includes(keyword);
        if (x.factories.length > 0) FilterResult ||= x.factories.filter((x) => x.name.toUpperCase().includes(keyword)).length > 0;
        return FilterResult;
      });

      state.filterdListLength = filterdData.length;
      state.displaySiteList = filterdData.slice((state.currentPage - 1) * state.pageSize, state.currentPage * state.pageSize);
    }

    /* 清除檢查 */
    const clearValidator = (Form: any) => {
      Form.value?.clearValidate();
    };

    /** 呼叫API, 取得 廠區 清單 */
    async function getAllSitesAsync() {
      /* 呼叫API getSites */
      const getAllSiteResponse = await API.getAllSites();

      /** 查看 resultCode */
      if (getAllSiteResponse.data.result !== APIResultCode.success) {
        throw new Error(getAllSiteResponse.data.msg);
      }

      /* 把 資料 丟到 SiteList */
      state.allSiteList = getAllSiteResponse.data.content;
    }

    /** 呼叫API, 刪除 廠區 */
    async function deleteSiteAsync(siteId: number) {
      const response = await API.deleteSite(siteId);

      /** 查看 resultCode */
      if (response.data.result !== APIResultCode.success) {
        throw new Error(t(response.data.msg));
      }
    }
    //#endregion

    return {
      ...toRefs(state),
      paginatorSetup,
      userInfoStore,
      t,

      /* Form */
      addSiteFormRef,
      addSiteFormRules,
      modifySiteFormRef,
      modifySiteFormRules,

      /* icon */
      Filter,

      /* UI Events */
      onAddSiteClicked,
      onSubmitNewSiteClicked,
      onModifySiteClicked,
      onSubmitModifySiteClicked,
      onDeleteSiteClicked,
      onFilterInputed,
      onPagesizeChanged,
      onCurrentPageChanged,
      onSortChanged,
      onTabSelected,
      onColDisplayCtrlPanelHidded,
      onSiteOptionListShown
    };
  },
});
</script>

<style scoped></style>
