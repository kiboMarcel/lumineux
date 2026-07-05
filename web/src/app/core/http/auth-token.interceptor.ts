import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { SessionStore } from '../session/session-store';

/**
 * Ajoute l'en-tête `Authorization: Bearer <jeton>` à chaque requête lorsqu'une session est active
 * (FR-002). Sans jeton en mémoire, la requête part telle quelle (les ressources protégées répondront
 * alors 401, géré par l'intercepteur d'erreurs).
 */
export const authTokenInterceptor: HttpInterceptorFn = (req, next) => {
  const token = inject(SessionStore).accessToken();
  if (!token) {
    return next(req);
  }
  return next(req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
};
