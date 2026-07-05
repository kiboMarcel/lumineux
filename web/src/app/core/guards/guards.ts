import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { NotificationService } from '../../shared/notifications/notification.service';
import { SessionStore } from '../session/session-store';

/** Exige une session active ; sinon redirige vers /login en conservant l'URL visée (FR-006). */
export const authGuard: CanActivateFn = (_route, state) => {
  const session = inject(SessionStore);
  const router = inject(Router);
  if (session.isAuthenticated()) {
    return true;
  }
  return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};

/**
 * Contrôle d'affichage basé sur les droits. Autorise si :
 * - `route.data.permission` (droit **unique** requis) est détenu, OU
 * - `route.data.anyPermissions` (liste) contient **au moins un** droit détenu (any-of, feature 011).
 * Sans contrainte déclarée, l'accès est autorisé. L'API reste l'autorité (403 géré).
 */
export const permissionGuard: CanActivateFn = (route) => {
  const session = inject(SessionStore);
  const router = inject(Router);
  const notifier = inject(NotificationService);

  if (!session.isAuthenticated()) {
    return router.createUrlTree(['/login']);
  }

  const required = route.data?.['permission'] as string | undefined;
  const anyOf = route.data?.['anyPermissions'] as string[] | undefined;

  const singleOk = required ? session.hasPermission(required) : true;
  const anyOk = anyOf && anyOf.length > 0 ? anyOf.some((p) => session.hasPermission(p)) : true;

  if ((!required && !anyOf) || (required && singleOk) || (anyOf && anyOk)) {
    return true;
  }

  notifier.error("Vous n'avez pas les droits nécessaires pour accéder à cette page.");
  return router.createUrlTree(['/']);
};

/** Empêche d'afficher les écrans publics (connexion) à un utilisateur déjà authentifié. */
export const guestOnly: CanActivateFn = () => {
  const session = inject(SessionStore);
  const router = inject(Router);
  return session.isAuthenticated() ? router.createUrlTree(['/']) : true;
};

/**
 * N'autorise l'écran d'installation que pour un visiteur non authentifié. Une instance déjà amorcée
 * est traitée par l'API (409), géré par le composant (FR-016).
 */
export const setupGuard: CanActivateFn = () => {
  const session = inject(SessionStore);
  const router = inject(Router);
  return session.isAuthenticated() ? router.createUrlTree(['/']) : true;
};
