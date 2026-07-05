import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { NotificationService } from '../../shared/notifications/notification.service';
import { SessionStore } from '../session/session-store';
import { messageForError } from './error-messages';

/**
 * Gestion centralisée des erreurs (FR-007/008). Un **401 sur une session active** (jeton en mémoire)
 * signifie une session expirée/invalide → purge + retour connexion en conservant l'URL visée
 * (`returnUrl`), quel que soit le corps (le challenge JWT a un corps vide : on se base sur le statut).
 * Les erreurs réseau/serveur sont notifiées ; les autres erreurs sont **relayées** aux composants qui
 * les affichent en contexte (messages génériques anti-énumération préservés).
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const session = inject(SessionStore);
  const router = inject(Router);
  const notifier = inject(NotificationService);

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status === 401 && session.accessToken() !== null) {
        const returnUrl = router.url;
        session.clear();
        notifier.info('Votre session a expiré. Veuillez vous reconnecter.');
        void router.navigate(['/login'], { queryParams: { returnUrl } });
      } else if (err.status === 0 || err.status >= 500) {
        notifier.error(messageForError(err));
      }
      return throwError(() => err);
    }),
  );
};
