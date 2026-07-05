import { HttpErrorResponse } from '@angular/common/http';
import { ProblemDetails } from '../api/models';

/**
 * Traduit une erreur HTTP de l'API en message **exploitable** et non technique (FR-008, SC-007).
 * S'appuie sur le format ProblemDetails (RFC 7807) et l'extension `code` (codes métier).
 */
export function messageForError(err: HttpErrorResponse): string {
  const problem = (err.error ?? {}) as ProblemDetails;

  if (err.status === 0) {
    return "Service indisponible. Vérifiez votre connexion et réessayez.";
  }

  switch (err.status) {
    case 400:
      return problem.detail || 'Requête invalide. Vérifiez les informations saisies.';
    case 401:
      return "Non authentifié. Veuillez vous connecter.";
    case 403:
      if (problem.code === 'password_change_required') {
        return 'Un changement de mot de passe est requis pour ce compte.';
      }
      return "Vous n'avez pas les droits nécessaires pour cette action.";
    case 404:
      return 'Ressource introuvable.';
    case 409:
      return problem.detail || 'Conflit : opération impossible dans l\'état actuel.';
    case 410:
      return 'Ce lien a expiré.';
    default:
      return 'Une erreur inattendue est survenue. Veuillez réessayer.';
  }
}

/** Indique si l'erreur signale une obligation de changement de mot de passe (première connexion). */
export function isPasswordChangeRequired(err: HttpErrorResponse): boolean {
  return err.status === 403 && (err.error as ProblemDetails | null)?.code === 'password_change_required';
}
