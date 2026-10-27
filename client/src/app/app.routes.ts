import { Routes } from '@angular/router';

import { Dashboard } from './components/dashboard/dashboard';
import { RequestsList } from './components/requests-list/requests-list';

export const routes: Routes = [
  { path: '', component: Dashboard },
  { path: 'requests', component: RequestsList },
  { path: '**', redirectTo: '' }
];
