import { Routes } from '@angular/router';
import { authGuard, guestOnly, permissionGuard, setupGuard } from './core/guards/guards';
import { ActivateComponent } from './features/activate/activate.component';
import { ChangePasswordComponent } from './features/change-password/change-password.component';
import { ForgotPasswordComponent } from './features/forgot-password/forgot-password.component';
import { HomeComponent } from './features/home/home.component';
import { LoginComponent } from './features/login/login.component';
import { MemberDetailComponent } from './features/members/member-detail/member-detail.component';
import { MemberFormComponent } from './features/members/member-form/member-form.component';
import { MemberListComponent } from './features/members/member-list/member-list.component';
import { MemberProfilesComponent } from './features/bureau-profiles/member-profiles/member-profiles.component';
import { ProfileDetailComponent } from './features/bureau-profiles/profile-detail/profile-detail.component';
import { ProfileFormComponent } from './features/bureau-profiles/profile-form/profile-form.component';
import { ProfileListComponent } from './features/bureau-profiles/profile-list/profile-list.component';
import { ResetPasswordComponent } from './features/reset-password/reset-password.component';
import { SetupComponent } from './features/setup/setup.component';
import { SessionStartComponent } from './features/attendance/session-start/session-start.component';
import { SessionRunComponent } from './features/attendance/session-run/session-run.component';
import { ShellComponent } from './shell/shell.component';

const manageMembers = { permission: 'manage_members' };
const manageBureauProfiles = { permission: 'manage_bureau_profiles' };
// Lecture élargie (feature 011) : administration des profils OU gestion des membres.
const profilesReadAccess = { anyPermissions: ['manage_bureau_profiles', 'manage_members'] };
const manageAttendance = { permission: 'manage_attendance' };

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

      // Module Membres (feature 009) — droit manage_members requis
      { path: 'members', component: MemberListComponent, canActivate: [permissionGuard], data: manageMembers },
      { path: 'members/new', component: MemberFormComponent, canActivate: [permissionGuard], data: manageMembers },
      { path: 'members/:id', component: MemberDetailComponent, canActivate: [permissionGuard], data: manageMembers },
      { path: 'members/:id/edit', component: MemberFormComponent, canActivate: [permissionGuard], data: manageMembers },

      // Module Profils du bureau (feature 011) — lecture élargie / écriture réservée admin
      { path: 'bureau-profiles', component: ProfileListComponent, canActivate: [permissionGuard], data: profilesReadAccess },
      { path: 'bureau-profiles/new', component: ProfileFormComponent, canActivate: [permissionGuard], data: manageBureauProfiles },
      { path: 'bureau-profiles/:id', component: ProfileDetailComponent, canActivate: [permissionGuard], data: profilesReadAccess },
      { path: 'bureau-profiles/:id/edit', component: ProfileFormComponent, canActivate: [permissionGuard], data: manageBureauProfiles },

      // Profils & droits d'un membre (feature 011) — lecture élargie
      { path: 'members/:id/profiles', component: MemberProfilesComponent, canActivate: [permissionGuard], data: profilesReadAccess },

      // Module Présences (feature 014) — droit manage_attendance requis
      { path: 'attendance', component: SessionStartComponent, canActivate: [permissionGuard], data: manageAttendance },
      { path: 'attendance/sessions/:id', component: SessionRunComponent, canActivate: [permissionGuard], data: manageAttendance },
    ],
  },

  { path: '**', redirectTo: '' },
];
