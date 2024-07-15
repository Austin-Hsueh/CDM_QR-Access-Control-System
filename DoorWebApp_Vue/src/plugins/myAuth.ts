import { App, Plugin } from 'vue';
import { useUserInfoStore } from './../stores/UserInfoStore';

export const MyAuthPlugin: Plugin = {
  install: (app:App, options:any) => {
    app.directive("auth-id", (el:HTMLElement, binding:any, vnode:any) => {
      const userInfo = useUserInfoStore();
      const authId = parseInt(binding.arg);
      console.log('my auth check');
      console.log(`auth Id : ${authId}`);
      console.log(`userInfo.permissions : ${JSON.stringify(userInfo.permissions)}`);
      if(userInfo.permissions.includes(authId)) {
        console.log('includes');
        
        el.style.color = "green";
      } else {
        console.log('not includes');
        el.style.color = "red";
      }
    });
  }
}