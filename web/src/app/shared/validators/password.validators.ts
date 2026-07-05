import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';
import { environment } from '../../../environments/environment';

/**
 * Politique de mot de passe côté client (FR-017) — **guidage uniquement**, l'API tranche : longueur
 * minimale (paramètre d'environnement, défaut 8), au moins une lettre et un chiffre.
 */
export function passwordPolicyValidator(): ValidatorFn {
  const min = environment.passwordMinLength;
  return (control: AbstractControl): ValidationErrors | null => {
    const value = (control.value ?? '') as string;
    if (!value) {
      return null; // `required` gère le cas vide
    }
    const errors: ValidationErrors = {};
    if (value.length < min) {
      errors['minlength'] = { requiredLength: min, actualLength: value.length };
    }
    if (!/[A-Za-z]/.test(value)) {
      errors['letter'] = true;
    }
    if (!/[0-9]/.test(value)) {
      errors['digit'] = true;
    }
    return Object.keys(errors).length ? errors : null;
  };
}

/** Message unique décrivant la politique (affiché sous les champs de nouveau mot de passe). */
export function passwordPolicyHint(): string {
  return `Au moins ${environment.passwordMinLength} caractères, dont une lettre et un chiffre.`;
}

/**
 * Vérifie que deux champs d'un formulaire diffèrent (ex. nouveau ≠ ancien/temporaire). À appliquer au
 * niveau du groupe. Renvoie l'erreur `mustDiffer` sur le groupe si les valeurs sont identiques.
 */
export function mustDifferValidator(firstField: string, secondField: string): ValidatorFn {
  return (group: AbstractControl): ValidationErrors | null => {
    const a = group.get(firstField)?.value;
    const b = group.get(secondField)?.value;
    if (a && b && a === b) {
      return { mustDiffer: true };
    }
    return null;
  };
}

/** Vérifie l'égalité de deux champs (mot de passe / confirmation). Erreur `mismatch` sur le groupe. */
export function mustMatchValidator(field: string, confirmField: string): ValidatorFn {
  return (group: AbstractControl): ValidationErrors | null => {
    const a = group.get(field)?.value;
    const b = group.get(confirmField)?.value;
    if (a && b && a !== b) {
      return { mismatch: true };
    }
    return null;
  };
}
