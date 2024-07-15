import { library } from "@fortawesome/fontawesome-svg-core";
import { FontAwesomeIcon } from "@fortawesome/vue-fontawesome";

/**  在此加入所需的icon (solid類) */
import {
  faPhone,
  faUser,
  faFlag,
  faEdit,
  faReply,
  faSave,
  faChevronDown,
  faSignOutAlt,
  faUserCircle,
  faMobileAlt,
  faAddressCard,
  faBan,
  faGripVertical,
  faEquals,
  faGlobe
} from "@fortawesome/free-solid-svg-icons";

/** 在此加入所需的icon (regular類) */
import {
  faCheckCircle,
  
} from "@fortawesome/free-regular-svg-icons";


/* add icons to the library */
library.add(faPhone);
library.add(faEquals);
library.add(faUser);
library.add(faFlag);
library.add(faEdit);
library.add(faReply);
library.add(faSave);
library.add(faChevronDown);
library.add(faSignOutAlt);
library.add(faUserCircle);
library.add(faMobileAlt);
library.add(faAddressCard);
library.add(faCheckCircle);
library.add(faBan);
library.add(faGripVertical);
library.add(faGlobe);



export default FontAwesomeIcon;
