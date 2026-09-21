import { provideNzIcons } from 'ng-zorro-antd/icon';
import {
  CoffeeOutline,
  LockOutline,
  LogoutOutline,
  MailOutline,
  MenuFoldOutline,
  MenuUnfoldOutline,
  SearchOutline,
  UserOutline,
} from '@ant-design/icons-angular/icons';

/**
 * One icon set, matching the component library. Register the icons a feature needs
 * on that feature's route providers rather than growing this list.
 */
export const provideAppIcons = () =>
  provideNzIcons([
    CoffeeOutline,
    LockOutline,
    LogoutOutline,
    MailOutline,
    MenuFoldOutline,
    MenuUnfoldOutline,
    // The header search, which is core chrome rather than a feature (D-0019).
    SearchOutline,
    UserOutline,
  ]);
