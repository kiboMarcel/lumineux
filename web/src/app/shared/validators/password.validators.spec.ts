import { describe, expect, it } from 'vitest';
import { FormControl, FormGroup } from '@angular/forms';
import { mustDifferValidator, mustMatchValidator, passwordPolicyValidator } from './password.validators';

describe('passwordPolicyValidator', () => {
  const validate = (value: string) => passwordPolicyValidator()(new FormControl(value));

  it('accepte un mot de passe conforme', () => {
    expect(validate('Passw0rd')).toBeNull();
  });

  it('rejette un mot de passe trop court', () => {
    expect(validate('Ab1')?.['minlength']).toBeTruthy();
  });

  it('rejette un mot de passe sans chiffre', () => {
    expect(validate('Password')?.['digit']).toBe(true);
  });

  it('rejette un mot de passe sans lettre', () => {
    expect(validate('12345678')?.['letter']).toBe(true);
  });

  it('laisse la valeur vide au validateur required', () => {
    expect(validate('')).toBeNull();
  });
});

describe('validateurs de groupe', () => {
  const group = (a: string, b: string) =>
    new FormGroup({ newPassword: new FormControl(a), other: new FormControl(b) });

  it('mustDiffer signale des valeurs identiques', () => {
    expect(mustDifferValidator('newPassword', 'other')(group('Passw0rd', 'Passw0rd'))).toEqual({ mustDiffer: true });
    expect(mustDifferValidator('newPassword', 'other')(group('Passw0rd', 'Autre123'))).toBeNull();
  });

  it('mustMatch signale une confirmation différente', () => {
    expect(mustMatchValidator('newPassword', 'other')(group('Passw0rd', 'Nope1234'))).toEqual({ mismatch: true });
    expect(mustMatchValidator('newPassword', 'other')(group('Passw0rd', 'Passw0rd'))).toBeNull();
  });
});
