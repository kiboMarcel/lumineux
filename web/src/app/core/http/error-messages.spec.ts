import { HttpErrorResponse } from '@angular/common/http';
import { describe, expect, it } from 'vitest';
import { isPasswordChangeRequired, messageForError } from './error-messages';

const err = (status: number, body: unknown = null) =>
  new HttpErrorResponse({ status, error: body });

describe('messageForError', () => {
  it('produit un message distinct par type (SC-007)', () => {
    expect(messageForError(err(0))).toContain('indisponible');
    expect(messageForError(err(400, { detail: 'Champ requis' }))).toBe('Champ requis');
    expect(messageForError(err(401))).toContain('Non authentifié');
    expect(messageForError(err(403))).toContain('droits');
    expect(messageForError(err(404))).toContain('introuvable');
    expect(messageForError(err(409, { detail: 'Déjà installé' }))).toBe('Déjà installé');
    expect(messageForError(err(410))).toContain('expiré');
    expect(messageForError(err(500))).toContain('inattendue');
  });

  it('reconnaît le code métier password_change_required', () => {
    const e = err(403, { code: 'password_change_required' });
    expect(isPasswordChangeRequired(e)).toBe(true);
    expect(messageForError(e)).toContain('changement de mot de passe');
  });

  it('ne confond pas un 401 avec une obligation de changement', () => {
    expect(isPasswordChangeRequired(err(401))).toBe(false);
  });
});
