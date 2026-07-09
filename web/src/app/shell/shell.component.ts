import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter, map } from 'rxjs';
import { NotificationService } from '../shared/notifications/notification.service';
import { SessionStore } from '../core/session/session-store';

interface NavItem {
  label: string;
  route?: string;
  /** Droit unique requis pour voir l'entrée. */
  permission?: string;
  /** OU l'un de ces droits (any-of, feature 011). */
  anyPermissions?: string[];
}

/**
 * Coquille de la console (feature 008, US1). Refonte « Tandem » : sidebar de marque + topbar.
 * Affiche la navigation **adaptée aux droits** de la session (masquage) et la déconnexion.
 * L'API reste l'autorité sur les autorisations.
 */
@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="lx-shell">
      <!-- ============ SIDEBAR (marque) ============ -->
      <aside class="lx-sidebar">
        <div class="lx-brand">
          <span class="lx-brand-mark">
            <img src="logo-icon.svg" alt="" aria-hidden="true" />
          </span>
          <span>
            <span class="lx-brand-name">Lumineux</span>
            <span class="lx-brand-tag">Console bureau</span>
          </span>
        </div>

        <div class="lx-nav-heading">Navigation</div>
        <nav class="lx-nav" aria-label="Navigation principale">
          <a routerLink="/" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }">Accueil</a>
          @for (item of visibleModules(); track item.label) {
            @if (item.route) {
              <a [routerLink]="item.route" routerLinkActive="active">{{ item.label }}</a>
            } @else {
              <button type="button" class="lx-nav-item" (click)="comingSoon()">{{ item.label }}</button>
            }
          }
          <a routerLink="/account/change-password" routerLinkActive="active">Mot de passe</a>
        </nav>

        <div class="lx-sidebar-user">
          <span class="lx-avatar" aria-hidden="true">{{ initials() }}</span>
          <span class="lx-user-meta">
            <span class="lx-user-name">{{ session.currentUser()?.displayName }}</span>
            <span class="lx-user-role">{{ roleLabel() }}</span>
          </span>
        </div>
      </aside>

      <!-- ============ ZONE PRINCIPALE ============ -->
      <div class="lx-main">
        <header class="lx-topbar">
          <span class="lx-page-title">{{ pageTitle() }}</span>
          <div class="lx-user">
            <button type="button" class="lx-btn lx-btn-ghost" (click)="logout()">Se déconnecter</button>
          </div>
        </header>
        <main class="lx-content">
          <router-outlet />
        </main>
      </div>
    </div>
  `,
})
export class ShellComponent {
  readonly session = inject(SessionStore);
  private readonly router = inject(Router);
  private readonly notifier = inject(NotificationService);

  private readonly modules: NavItem[] = [
    { label: 'Membres', permission: 'manage_members', route: '/members' },
    { label: 'Profils du bureau', route: '/bureau-profiles', anyPermissions: ['manage_bureau_profiles', 'manage_members'] },
    { label: 'Présences', permission: 'manage_attendance', route: '/attendance' },
    { label: 'Antennes', permission: 'manage_referentials', route: '/antennas' },
    { label: 'Rapports', permission: 'manage_attendance', route: '/reports' },
  ];

  /** Modules visibles selon les droits de la session (RBAC d'affichage ; l'API reste l'autorité). */
  readonly visibleModules = computed(() =>
    this.modules.filter((m) => this.canSee(m)),
  );

  /** Titre de la page courante, dérivé de l'URL (affiché dans la topbar). */
  readonly pageTitle = toSignal(
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      map((e) => this.titleFor(e.urlAfterRedirects)),
    ),
    { initialValue: this.titleFor(this.router.url) },
  );

  /** Initiales de l'utilisateur pour l'avatar. */
  readonly initials = computed(() => {
    const name = this.session.currentUser()?.displayName ?? '';
    const parts = name.trim().split(/\s+/).filter(Boolean);
    if (parts.length === 0) return '·';
    if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
    return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
  });

  /** Rôle affiché sous le nom (administration des profils vs bureau). */
  readonly roleLabel = computed(() =>
    this.session.hasPermission('manage_bureau_profiles') ? 'Administration' : 'Bureau',
  );

  private canSee(item: NavItem): boolean {
    if (item.anyPermissions?.length) {
      return item.anyPermissions.some((p) => this.session.hasPermission(p));
    }
    return item.permission ? this.session.hasPermission(item.permission) : true;
  }

  private titleFor(url: string): string {
    const u = url.split('?')[0].split('#')[0];
    if (u === '/' || u === '') return 'Accueil';
    if (u.startsWith('/members')) return 'Membres';
    if (u.startsWith('/bureau-profiles')) return 'Profils du bureau';
    if (u.startsWith('/attendance')) return 'Présences';
    if (u.startsWith('/antennas')) return 'Antennes';
    if (u.startsWith('/reports')) return 'Rapports';
    if (u.startsWith('/account/change-password')) return 'Mot de passe';
    return 'Lumineux';
  }

  comingSoon(): void {
    this.notifier.info('Module à venir dans un prochain incrément.');
  }

  logout(): void {
    this.session.clear();
    void this.router.navigate(['/login']);
  }
}
