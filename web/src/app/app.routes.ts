import { Routes } from '@angular/router';
import { authGuard, guestOnly, setupGuard } from './core/guards/guards';
import { ActivateComponent } from './features/activate/activate.component';
import { ChangePasswordComponent } from './features/change-password/change-password.component';
import { ForgotPasswordComponent } from './features/forgot-password/forgot-password.component';
import { HomeComponent } from './features/home/home.component';
import { LoginComponent } from './features/login/login.component';
import { ResetPasswordComponent } from './features/reset-password/reset-password.component';
import { SetupComponent } from './features/setup/setup.component';
import { ShellComponent } from './shell/shell.component';

export const routes: Routes = [
  // Routes publiques
  { path: 'login', component: LoginComponent, canActivate: [guestOnly] },
  { path: 'auth/activate', component: ActivateComponent },
  { path: 'auth/forgot-password', component: ForgotPasswordComponent },
  { path: 'auth/reset-password', component: ResetPasswordComponent },
  { path: 'setup/first-admin', component: SetupComponent, canActivate: [setupGuard] },

  // Console protégée
  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      { path: '', component: HomeComponent },
      { path: 'account/change-password', component: ChangePasswordComponent },
    ],
  },

  { path: '**', redirectTo: '' },
];
