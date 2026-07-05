import { Component, computed, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { NotificationService } from '../shared/notifications/notification.service';
import { SessionStore } from '../core/session/session-store';

interface NavItem {
  label: string;
  permission: string;
  route?: string;
}

/**
 * Coquille de la console (feature 008, US1). Affiche la navigation **adaptée aux droits** de la
 * session (masquage) et l'action de déconnexion. L'API reste l'autorité sur les autorisations.
 */
@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="lx-shell">
      <header class="lx-topbar">
        <strong>Lumineux</strong>
        <nav class="lx-nav" aria-label="Navigation principale">
          <a routerLink="/" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }">Accueil</a>
          @for (item of visibleModules(); track item.permission) {
            @if (item.route) {
              <a [routerLink]="item.route" routerLinkActive="active" [attr.data-permission]="item.permission">{{ item.label }}</a>
            } @else {
              <button type="button" class="lx-nav-item" (click)="comingSoon()" [attr.data-permission]="item.permission">
                {{ item.label }}
              </button>
            }
          }
          <a routerLink="/account/change-password" routerLinkActive="active">Mot de passe</a>
        </nav>
        <div class="lx-user">
          <span class="lx-muted">{{ session.currentUser()?.displayName }}</span>
          <button type="button" class="lx-btn lx-btn-ghost" (click)="logout()">Se déconnecter</button>
        </div>
      </header>
      <main class="lx-content">
        <router-outlet />
      </main>
    </div>
  `,
  styles: [
    `.lx-nav-item { background: none; border: none; padding: 0.4rem 0.7rem; cursor: pointer; color: inherit; font: inherit; }`,
  ],
})
export class ShellComponent {
  readonly session = inject(SessionStore);
  private readonly router = inject(Router);
  private readonly notifier = inject(NotificationService);

  private readonly modules: NavItem[] = [
    { label: 'Membres', permission: 'manage_members', route: '/members' },
    { label: 'Profils du bureau', permission: 'manage_bureau_profiles' },
    { label: 'Présences', permission: 'manage_attendance' },
  ];

  /** Modules visibles = ceux pour lesquels la session détient le droit (RBAC d'affichage, FR-005). */
  readonly visibleModules = computed(() =>
    this.modules.filter((m) => this.session.hasPermission(m.permission)),
  );

  comingSoon(): void {
    this.notifier.info('Module à venir dans un prochain incrément.');
  }

  logout(): void {
    this.session.clear();
    void this.router.navigate(['/login']);
  }
}
