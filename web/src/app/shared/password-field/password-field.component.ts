import { Component, forwardRef, input, signal } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

/**
 * Champ mot de passe réutilisable avec bascule **afficher/masquer** (accessible),
 * compatible ReactiveForms (`formControlName`). Le libellé reste porté par le
 * `.lx-field` parent. Style : classe globale `.lx-password` (voir styles.css).
 */
@Component({
  selector: 'app-password-field',
  standalone: true,
  imports: [],
  template: `
    <div class="lx-password">
      <input
        [id]="id()"
        [type]="show() ? 'text' : 'password'"
        [attr.autocomplete]="autocomplete()"
        [value]="value"
        [disabled]="disabled"
        (input)="onInput($event)"
        (blur)="onTouched()"
      />
      <button
        type="button"
        class="lx-password-toggle"
        (click)="show.set(!show())"
        [attr.aria-label]="show() ? 'Masquer le mot de passe' : 'Afficher le mot de passe'"
        [attr.aria-pressed]="show()"
      >
        @if (show()) {
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.9" stroke-linecap="round" stroke-linejoin="round"><path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"/><path d="M1 1l22 22"/></svg>
        } @else {
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.9" stroke-linecap="round" stroke-linejoin="round"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/></svg>
        }
      </button>
    </div>
  `,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => PasswordFieldComponent),
      multi: true,
    },
  ],
})
export class PasswordFieldComponent implements ControlValueAccessor {
  /** `id` de l'input (pour l'association avec le `<label for>` parent). */
  readonly id = input('');
  readonly autocomplete = input('current-password');

  /** Bascule d'affichage (masqué par défaut). */
  readonly show = signal(false);

  value = '';
  disabled = false;

  private onChange: (value: string) => void = () => {};
  onTouched: () => void = () => {};

  writeValue(value: string): void {
    this.value = value ?? '';
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }

  onInput(event: Event): void {
    this.value = (event.target as HTMLInputElement).value;
    this.onChange(this.value);
  }
}
