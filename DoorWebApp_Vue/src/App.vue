<template>
  <el-config-provider :locale="userInfoStore.el_local">
    <transition>
      <router-view />
    </transition>
  </el-config-provider>
</template>

<script lang="ts">
import { defineComponent, onMounted } from "vue";
import { useRouter } from "vue-router";
import { useUserInfoStore } from "./stores/UserInfoStore";
import { usePaginatorSetup } from "@/stores/PaginatorStore";
import API from "@/apis/TPSAPI";
import { APIResultCode } from "@/models/enums/APIResultCode";

export default defineComponent({
  setup() {
    const router = useRouter();
    const userInfoStore = useUserInfoStore();
    const paginatorSetup = usePaginatorSetup();

    //註冊resize事件
    window.addEventListener("resize", onResized);

    onMounted(async () => {
      console.log(`App onMounted`);

      //設置分頁RWD
      paginatorSetup.update(window.innerWidth);
      
    });

    /** Resize事件 (全域) */
    function onResized() {
      paginatorSetup.update(window.innerWidth);
    }

    return {
      userInfoStore,
    };
  },
});
</script>

<style>

:root {
  --card-width: 110px;
  --card-aspect-ratio: 1.45;
  --card-hover-scale:1.05;
  --el-font-size-base:14px !important;
}

html,
body {
  height: 100%;
  margin: 0px;
  padding: 0px;
  /* min-width: 1300px; */
}

#app {
  font-family: "Helvetica Neue", Helvetica, "Microsoft JhengHei", "微軟正黑體", Arial, sans-serif;
  --overlay-z-index: 5;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  text-align: center;
  color: #2c3e50;
  height: 100%;
  margin: 0px;
  padding: 0px;
  /* overflow: hidden; */
}
.header {
  --el-header-padding: 0px 10px;
  position: fixed;
  background-color: #ffffff;
  width: 100%;
  z-index: 4;
  box-shadow: 0px 4px 13px rgba(0, 0, 0, 0.05);
}
nav {
  padding: 30px;
}

nav a {
  font-weight: bold;
  color: #2c3e50;
}

nav a.router-link-exact-active {
  color: #EEBE77;
}

.icon-wrap {
  display: flex;
  flex-direction: row;
  justify-content: center;
  align-items: center;
}
.icon-btn {
  cursor: pointer;
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
}

.icon-btn:hover {
  cursor: pointer;
  color: #79bbff;
}

ion-icon {
  pointer-events: none;
}

.content-title {
  color: #606266;
}

.v-enter-active,
.v-leave-active {
  transition: opacity 0.5s;
}

.v-enter-from,
.v-leave-to {
  opacity: 0;
}

.v-enter-to,
.v-leave-from {
  opacity: 1;
}

.project-list-dialog {
  width: 100vw;
  max-width: 100% !important;
}
.kaizen-list-dialog {
  width: 100vw;
  max-width: 100%;
}

.dialog {
  width: 100%;
}

.dialog .el-checkbox{
  margin-right: 20px ;
}

.top-pn-modify-dialog {
  width: 100%;
}

@media (min-width: 576px) {
  .project-list-dialog {
    width: 75vw;
    max-width: 100% !important;
  }

  .kaizen-list-dialog {
    width: 75vw;
    max-width: 700px;
  }

  .dialog {
    max-width: 550px;
    min-width: 300px;
  }

  .top-pn-modify-dialog {
    max-width: 850px;
  }
}

.el-loading-mask {
  background: rgba(0, 0, 0, 0.2);
}

.fc .fc-timegrid-slot{
  height: 2.25em !important;
}
</style>
