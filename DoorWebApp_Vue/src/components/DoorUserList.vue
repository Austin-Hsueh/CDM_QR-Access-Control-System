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
        <el-table-column label="門禁權限" prop="groupNames"/>
        <el-table-column label="通行時間" prop="phone"/>
      </el-table>
    </el-col>
  </el-row>
  <!-- /table -->

  <!-- pagination -->
  <el-row justify="end" class="mt-3">
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
import { M_IUsers } from "@/models/M_IUser";
import type { ComponentSize, FormInstance, FormRules, ElMessage } from 'element-plus';

const { t } = useI18n();
const userInfo = ref<M_IUsers[]>([]); // Specify the type of the array
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
//#endregion

//#region Hook functions
// onActivated(() => {
  
// });
onMounted(() => {
  getUsers();
  console.log(`Received door ID: ${props.doorId}`);
});

//#endregion

//#region Private Functions
async function getUsers() {
  try {
    const getUsersResult = await API.getAllUsers();
    if (getUsersResult.data.result != 1) throw new Error(getUsersResult.data.msg);
    userInfo.value = getUsersResult.data.content;

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
