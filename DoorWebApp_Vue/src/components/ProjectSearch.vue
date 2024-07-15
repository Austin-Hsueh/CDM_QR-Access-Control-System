<template>
  <!-- Search Form -->
  <el-form @submit.prevent class="mt-2" :label-position="'right'" :label-width="'auto'">
    <div class="row row-cols-1 row-cols-sm-2 row-cols-md-2 row-cols-lg-4 row-cols-xl-4">
      <!-- 專案代碼 -->
      <div class="col">
        <el-form-item class="w-100" :label="t('PJCode') + ':'">
          <el-input v-model.trim="searchCriteria.pjCode" @keyup.enter="onInputFieldEnterKeyUp" clearable :placeholder="t('placeholder.input')" />
        </el-form-item>
      </div>

      <!-- 建立者 -->
      <div class="col">
        <el-form-item class="w-100" :label="t('creator') + ':'">
          <el-input v-model="searchCriteria.creatorKeyword" @keyup.enter="onInputFieldEnterKeyUp" clearable :placeholder="t('placeholder.input')" />
        </el-form-item>
      </div>

      <!-- 廠區 -->
      <div class="col">
        <el-form-item class="w-100" :label="t('site') + ':'">
          <el-select
            :placeholder="t('placeholder.select')"
            :loading="isSiteLoading"
            :loading-text="t('loading')"
            @visible-change="onSiteListShown"
            v-model="searchCriteria.site"
            value-key="id"
            class="w-100"
            @change="searchCriteria.factory = undefined"
            clearable
          >
            <el-option v-for="site in sites" :key="site.id" :label="site.name" :value="site" />
          </el-select>
        </el-form-item>
      </div>

      <!-- 工廠 -->
      <div class="col">
        <el-form-item class="w-100" :label="t('factory') + ':'">
          <el-select :placeholder="t('placeholder.select')" v-model="searchCriteria.factory" value-key="id" class="w-100" clearable>
            <el-option v-for="factory in searchCriteria.site?.factories" :key="factory.id" :label="factory.name" :value="factory" />
          </el-select>
        </el-form-item>
      </div>

      <!-- 起始時間 -->
      <div class="col">
        <el-form-item class="w-100" :label="t('startDate') + ':'">
          <el-date-picker
            v-model="searchCriteria.startDate"
            value-format="YYYY-MM-DD"
            type="month"
            :placeholder="t('placeholder.date')"
            style="width: 100%"
            clearable
          />
        </el-form-item>
      </div>

      <!-- 結束時間 -->
      <div class="col">
        <el-form-item class="w-100" :label="t('endDate') + ':'">
          <el-date-picker
            v-model="searchCriteria.endDate"
            value-format="YYYY-MM-DD"
            type="month"
            :placeholder="t('placeholder.date')"
            style="width: 100%"
            clearable
          />
        </el-form-item>
      </div>
    </div>
  </el-form>

  <!-- Btn: 清除 搜尋 -->
  <div class="d-flex flex-row justify-content-end">
    <el-button type="" @click="onClearSearchCriteriaClicked()">{{ t("clear") }}</el-button>
    <el-button type="primary" @click="onSearchProjectClicked()">{{ t("search") }}</el-button>
  </div>
  <hr />

  <!-- table -->
  <div class="d-flex flex-column">
    <div class="align-self-end">
      <el-popover placement="bottom" @hide="onColDisplayCtrlPanelHidded" :Title="t('column')" trigger="click">
        <template #reference>
          <el-button link>
            <el-icon :size="20"><Operation /></el-icon>
          </el-button>
        </template>
        <div>
          <el-checkbox v-model="colDisplayCtrl[0]">{{ t("PJCode") }}</el-checkbox>
          <el-checkbox v-model="colDisplayCtrl[1]">{{ t("period") }}</el-checkbox>
          <el-checkbox v-model="colDisplayCtrl[2]">{{ t("startMonth") }}</el-checkbox>
          <el-checkbox v-model="colDisplayCtrl[3]">{{ t("endMonth") }}</el-checkbox>
          <el-checkbox v-model="colDisplayCtrl[4]">{{ t("factories") }}</el-checkbox>
          <el-checkbox v-model="colDisplayCtrl[5]">{{ t("factorys") }}</el-checkbox>
          <el-checkbox v-model="colDisplayCtrl[6]">{{ t("team") }}</el-checkbox>
          <el-checkbox v-model="colDisplayCtrl[7]">{{ t("payRate") }}</el-checkbox>
          <el-checkbox v-model="colDisplayCtrl[8]">{{ t("creator") }}</el-checkbox>
          <el-checkbox v-model="colDisplayCtrl[9]">{{ t("createDate") }}</el-checkbox>
        </div>
      </el-popover>
    </div>
    <el-table
      ref="projectTableRef"
      class="w-100"
      type="selection"
      v-loading.fullscreen.lock="isTableLoading"
      :max-height="450"
      :data="displayProjects"
      :stripe="true"
      highlight-current-row
      @current-change="onProjectSelectChanged"
      @sort-change="onSortChanged"
    >
      <el-table-column width="70" prop="radio" fixed="left" align="center" :label="t('select')">
        <template #default="scope">
          <el-radio v-model="tableRadio" :label="scope.row.id"><i></i></el-radio>
        </template>
      </el-table-column>
      <el-table-column min-width="140" v-if="colDisplayCtrl[0]" sortable="custom" :label="t('PJCode')" prop="pjCode" />
      <el-table-column min-width="80" v-if="colDisplayCtrl[1]" sortable="custom" :label="t('period')" prop="periodNum" />
      <el-table-column min-width="120" v-if="colDisplayCtrl[2]" sortable="custom" :label="t('startMonth')" prop="startMonth" />
      <el-table-column min-width="120" v-if="colDisplayCtrl[3]" sortable="custom" :label="t('endMonth')" prop="endMonth" />
      <el-table-column min-width="120" v-if="colDisplayCtrl[4]" sortable="custom" :label="t('factories')" prop="siteName" />
      <el-table-column min-width="120" v-if="colDisplayCtrl[5]" sortable="custom" :label="t('factorys')" prop="factoryName" />
      <el-table-column min-width="120" v-if="colDisplayCtrl[6]" sortable="custom" :label="t('team')" prop="teamNum" />
      <el-table-column min-width="120" v-if="colDisplayCtrl[7]" sortable="custom" :label="t('payRate')" prop="workRate" />
      <el-table-column min-width="120" v-if="colDisplayCtrl[8]" sortable="custom" :label="t('creator')" prop="creatorDisplayName" />
      <el-table-column min-width="120" v-if="colDisplayCtrl[9]" sortable="custom" :label="t('createDate')" prop="createTime" />
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
          :total="filteredProjects.length"
          @size-change="onPagesizeChanged"
          @current-change="onCurrentPageChanged"
          justify="end"
        />
      </div>
    </el-col>
  </el-row>
  <!-- /pagination -->
</template>
<script lang="ts">
import { defineComponent, ref, reactive, toRefs } from "vue";
import { useI18n } from "vue-i18n";
import API from "@/apis/TPSAPI";
import { APIResultCode } from "@/models/enums/APIResultCode";

// element-plus
import { ElMessage } from "element-plus";
// element-plus icons
import { Search, EditPen, Delete, Calendar } from "@element-plus/icons-vue";

/** Utils */
import { usePaginatorSetup } from "@/stores/PaginatorStore";
import IReqProjectSearchCriteriaDTO from "@/models/dto/IReqProjectSearchCriteriaDTO";
import IResProjectInfoDTO from "@/models/dto/IResProjectInfoDTO";
import ProjectSearchCriteria from "@/models/ProjectSearchCriteria";
import ElTable from "element-plus/lib/components/table";
import IResSiteInfoDTO from "@/models/dto/IResSiteInfoDTO";
import { compare, dateFormat, delay } from "@/plugins/utility";
import { DataTableType } from "@/models/enums/DataTableType";

export default defineComponent({
  emits: ["ProjectSelect"],
  setup(prpos, context) {
    const { t } = useI18n();
    const paginatorSetup = usePaginatorSetup();
    const tableType = DataTableType.projectSearchTable;

    const columns: any = [
      { prop: "pjCode", visiable: true, label: t("PJCode") },
      { prop: "siteName", visiable: true, label: t("factories") },
      { prop: "factoryName", visiable: true, label: t("factorys") },
    ];
    const state = reactive({
      isTableLoading: false,
      searchCriteria: new ProjectSearchCriteria(),
      filteredProjects: [] as IResProjectInfoDTO[],
      displayProjects: [] as IResProjectInfoDTO[],
      isSiteLoading: false,
      tableRadio: undefined as number | undefined,

      // pagination
      currentPage: 1,
      pageSize: 20,

      // 廠區/工廠選擇清單
      sites: [] as IResSiteInfoDTO[],

      colDisplayCtrl: new Array(50).fill(true) as boolean[],
      columns: columns, //試驗中
    });

    // #region 建立表單ref與Validator
    const projectTableRef = ref<InstanceType<typeof ElTable>>();
    // #endregion

    // #region Hook functions
    // #endregion

    //#region UI Events
    /** 按下搜尋 */
    const onSearchProjectClicked = async () => {
      try {
        state.isTableLoading = true;

        //清除table排序
        projectTableRef.value?.clearSort();
        await filterProjectListAsync();
      } catch (error) {
        console.log(error);
        ElMessage({
          type: "error",
          message: "處理失敗，請查看詳細訊息",
        });
      } finally {
        state.isTableLoading = false;
      }
    };
    /** 按下並放開Enter鍵 */
    const onInputFieldEnterKeyUp = async () => {
      await onSearchProjectClicked();
    };

    /** search 清除 */
    const onClearSearchCriteriaClicked = () => {
      state.searchCriteria.clear();
    };

    /** 顯示廠區下拉選單時 */
    const onSiteListShown = async () => {
      try {
        state.isSiteLoading = true;
        await updateSite();
      } catch (error) {
        console.log(error);
      } finally {
        state.isSiteLoading = false;
      }
    };
    let a = t("hello");

    /** 當專案被選擇時 */
    const onProjectSelectChanged = async (project: IResProjectInfoDTO) => {
      if (!project) return;
      console.log(`onProjectSelectChanged.  ${project.id}, ${project.pjCode}`);
      state.tableRadio = project.id;
      context.emit("ProjectSelect", project);
    };

    /** 改變排序 */
    const onSortChanged = (sort: any) => {
      console.log(`onSortChanged: ${sort.column}, ${sort.prop}, ${sort.order}`);
      try {
        state.isTableLoading = true;
        state.filteredProjects.sort(compare(sort.prop, sort.order));
        updateDisplayList();
      } catch (error) {
        console.log(error);
      } finally {
        state.isTableLoading = false;
      }
    };

    /** 分頁輸入 */
    const onPagesizeChanged = () => {
      updateDisplayList();
    };

    const onCurrentPageChanged = () => {
      updateDisplayList();
    };

    /** 關閉欄位顯示設定Panel時 */
    const onColDisplayCtrlPanelHidded = async () => {
      await API.patchTableDisplayPreference(tableType, { displayMap: state.colDisplayCtrl });
    };

    //#endregion

    //#region Private Functions
    /** 供外部呼叫 : 當顯示dialog的時候會呼叫此方法，用以重置搜尋結果 */
    const resetSearch = async () => {
      try {
        state.isTableLoading = true;
        await updateUserPreference();
        await updateSite();
        await filterProjectListAsync();
        state.tableRadio = undefined;
      } catch (error) {
        console.log(error);
        ElMessage({
          type: "error",
          message: "處理失敗，請查看詳細訊息",
        });
      } finally {
        state.isTableLoading = false;
      }
    };

    /** 更新使用者設置偏好 */
    const updateUserPreference = async () => {
      const getPrefResponse = await API.getTableDisplayPreference(tableType);
      if (getPrefResponse.data.result !== 1) {
        console.log(`updateUserPreference : ${getPrefResponse.data.msg}`);
      }
      state.colDisplayCtrl = getPrefResponse.data.content;
    };

    /** 呼叫API, 更新廠區清單 */
    const updateSite = async () => {
      try {
        const getSiteResult = await API.getAllSites();

        if (getSiteResult.data.result !== APIResultCode.success) {
          throw new Error(getSiteResult.data.msg);
        }

        state.sites = getSiteResult.data.content;
      } catch (error) {
        console.log(error);
      }
    };

    /** 更新搜尋結果 */
    const filterProjectListAsync = async () => {
      if (process.env.VUE_APP_RUN_ENV === "DEV") await delay(1000);

      let searchCriteriaDTO = state.searchCriteria.ToReqDTO();
      searchCriteriaDTO.startDate = undefined;
      searchCriteriaDTO.endDate = undefined;
      searchCriteriaDTO.includeDate = new Date();

      await getFilteredProjects(searchCriteriaDTO);

      updateDisplayList();
    };

    /** 呼叫API, 取得搜尋結果 */
    const getFilteredProjects = async (searchCriteria: IReqProjectSearchCriteriaDTO) => {
      /* 若 開發時 等待 1 秒 */
      if (process.env.VUE_APP_RUN_ENV === "DEV") await delay(1000);

      const projectSearchResult = await API.searchProject(searchCriteria);
      if (projectSearchResult.data.result !== APIResultCode.success) {
        throw new Error(projectSearchResult.data.msg);
      }
      state.filteredProjects = projectSearchResult.data.content;
    };

    /** 更新顯示清單 */
    const updateDisplayList = () => {
      state.displayProjects = state.filteredProjects.slice((state.currentPage - 1) * state.pageSize, state.currentPage * state.pageSize);
    };
    //#endregion

    return {
      ...toRefs(state),
      paginatorSetup,
      t,

      // icons
      Search,
      EditPen,
      Delete,
      Calendar,

      // function
      getFilteredProjects,
      updateDisplayList,

      onSearchProjectClicked,
      onClearSearchCriteriaClicked,
      onPagesizeChanged,
      onCurrentPageChanged,
      onSortChanged,
      onSiteListShown,
      onProjectSelectChanged,
      onInputFieldEnterKeyUp,
      onColDisplayCtrlPanelHidded,
      resetSearch,

      // date
      dateFormat,
    };
  },
});
</script>
<style scoped></style>
