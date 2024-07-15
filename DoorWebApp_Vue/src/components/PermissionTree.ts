import i18n from "@/locale";
import { useI18n } from "vue-i18n";

//const { t } = useI18n();
const t = i18n.global.t;

export interface Tree {
  id: number;
  label:  any;
  children?: Tree[];
}

const PermissionTree_v2: Tree[] = [
  {
    id: 1,
    label: 'TPS_Performance_Tracking', //'效益追蹤表'
    children: [
      {
        id: 110,
        label: "view"
      },
      {
        id: 120,
        label: "export"
      }
    ]
  },
  {
    id: 2,
    label: 'search_for_kaizen_strategy', //'改善對策清單'
    children: [
      {
        id: 210,
        label: 'view' //查詢
      },
      {
        id: 220,
        label: "export" //匯出
      }
    ]
  },
  {
    id: 3,
    label: 'database_maintain', //資料庫維護
    children: [
      {
        id: 310,
        label: 'view' //查詢
      },
      {
        id: 320,
        label: "edit" //編輯
      },
      // {
      //   id: 331,
      //   label: "modify" //修改(僅限自己)
      // },
      // {
      //   id: 332,
      //   label: "modify" //修改(不限使用者)
      // },
      // {
      //   id: 341,
      //   label: "delete" //刪除(僅限自己)
      // },
      // {
      //   id: 342,
      //   label: "delete" //刪除(不限使用者)
      // }
    ]
  },
  {
    id: 4,
    label: 'System_Settings', //下拉選單項目
    children: [
      {
        id: 410,
        label: "view"
      },
      {
        id: 420,
        label: "edit"
      },
      // {
      //   id: 430,
      //   label: "modify"
      // },
      // {
      //   id: 440,
      //   label: "delete"
      // }
    ]
  }
]

export default PermissionTree_v2;