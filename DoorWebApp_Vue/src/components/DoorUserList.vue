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
  </div>

  <!-- table -->
  <el-row>
    <el-col :span="24">
      <el-table name="userInfoTable" style="width: 100%" height="400" :data="userInfo">
        <el-table-column :label="t('username')"  prop="username"/>
        <el-table-column :label="t('displayName')" prop="displayName" />
        <el-table-column :label="t('Permissions')" prop="groupIds">
          <template v-slot="scope">
            <div v-for="groupId in scope.row.groupIds" :key="groupId">
              <span v-if="groupId === 1">大門</span>
              <span v-else-if="groupId === 2">Car教室</span>
              <span v-else-if="groupId === 3">Sunny教室</span>
              <span v-else-if="groupId === 4">儲藏室</span>
            </div>
          </template>
        </el-table-column>
      </el-table>
    </el-col>
  </el-row>
  <!-- /table -->

  <!-- pagination -->
   <!-- 20240731 上線測試, 暫時排除未完分頁功能 by Austin -->
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
</template>

<script setup lang="ts">

import { ref, onMounted, onActivated, reactive, defineProps } from "vue";
import { useI18n } from "vue-i18n";
import API from '@/apis/TPSAPI';
import { M_IUsersDoor } from "@/models/M_IUsersDoor";
import type { ComponentSize, FormInstance, FormRules, ElMessage } from 'element-plus';

const { t } = useI18n();
const userInfo = ref<M_IUsersDoor[]>([]); // Specify the type of the array
const currentPage4 = ref(4)
const pageSize4 = ref(100)
const size = ref<ComponentSize>('default')
const searchText = ref('')

// 定义 props
const props = defineProps<{
  doorId: number
}>();

const handleSizeChange = (val: number) => {
  console.log(`${val} items per page`)
}
const handleCurrentChange = (val: number) => {
  console.log(`current page: ${val}`)
}

//#region UI Events
const onFilterInputed = () => {
  console.log("Search Function");
  if(!searchText.value || searchText.value.trim() === ''){
    getUsersDoor();
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
//#endregion

//#region Hook functions
// onActivated(() => {
  
// });
onMounted(() => {
  getUsersDoor();
  console.log(`Received door ID: ${props.doorId}`);
});

//#endregion

//#region Private Functions
async function getUsersDoor() {
  try {
    const getUsersResult = await API.getUsersDoor(props.doorId);
    if (getUsersResult.data.result != 1) throw new Error(getUsersResult.data.msg);
    userInfo.value = getUsersResult.data.content;

   userInfo.value = getUsersResult.data.content.filter(item => item.groupIds.includes(props.doorId));

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
