<template>
  <el-container>
    <div v-if="isOpenMenu" class="overlay" @click.self="isOpenMenu = false"></div>
    <el-header class="header"> <Header @on-hamburger-clicked="isOpenMenu = true" /> </el-header>
    <el-container>
      <el-aside class="sidebar" v-bind:class="{'open': isOpenMenu}">
        <Aside @on-menu-item-select="isOpenMenu = false"/>
      </el-aside>
      <el-main class="page-content has-sidebar">
        <router-view v-slot="{ Component }">
          <keep-alive>
            <component :is="Component" />
          </keep-alive> </router-view
      ></el-main>
    </el-container>
  </el-container>
</template>

<script lang="ts">
import { defineComponent, toRefs, reactive, onMounted } from "vue";
import Header from "@/components/Header.vue";
import Aside from "@/components/Aside_v1.vue";
export default defineComponent({
  components: {
    Header,
    Aside,
  },
  setup() {
    const state = reactive({
      isOpenMenu: false
    });

    return {
      ...toRefs(state),
    };
  },
});
</script>

<style>
:root {
  --sidebar-width: 200px;
  --sidebar-z-index: 5;
  --header-height: 65px;
  --el-header-padding-sm: 0px 20px;
  --el-header-padding: 0px 5px;
}

.el-menu {
  border-right: 0px !important;
}

.sidebar {
  position: fixed;
  top: var(--header-height);
  bottom: 0;
  left: 0;
  z-index: var(--sidebar-z-index);
  width: var(--sidebar-width);
  padding: 0px;
  overflow-y: auto;
  transform: translate(0);
  transition: background-color var(--el-transition-duration-fast), opacity 0.25s, transform 0.5s cubic-bezier(0.19, 1, 0.22, 1);
}

.sidebar.open {
  transform: translate(0);
}

.overlay {
  position: fixed;
  top: 0;
  right: 0;
  bottom: 0;
  left: 0;
  background: rgba(0, 0, 0, 0.6);
  transition: opacity 0.5s;
  z-index: var(--overlay-z-index);
}

.page-content {
  /* padding-top: var(--header-height); */
  padding-left: calc(var(--sidebar-width) + 10px);
  margin-top: 60px;
}

.header {
  --el-header-padding: 0px 10px;
}
.hamburger {
  display: none;
}

/** Mobile */
@media screen and (max-width: 767px) {
  .sidebar {
    transform: translate(-100%);
    top: 0;
  }
  .hamburger {
    display: flex;
  }

  .page-content {
    padding-left: 10px;
  }
}


</style>
