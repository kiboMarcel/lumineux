import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { PasswordFieldComponent } from './password-field.component';

describe('PasswordFieldComponent', () => {
  beforeEach(() => TestBed.configureTestingModule({}));

  it('masqué par défaut, bascule à la demande', () => {
    const comp = TestBed.createComponent(PasswordFieldComponent).componentInstance;
    expect(comp.show()).toBe(false);
    comp.show.set(true);
    expect(comp.show()).toBe(true);
  });

  it('propage la valeur saisie via ControlValueAccessor', () => {
    const comp = TestBed.createComponent(PasswordFieldComponent).componentInstance;
    const changed = vi.fn();
    comp.registerOnChange(changed);
    comp.onInput({ target: { value: 'S3cret' } } as unknown as Event);
    expect(comp.value).toBe('S3cret');
    expect(changed).toHaveBeenCalledWith('S3cret');
  });

  it('writeValue reflète la valeur du contrôle', () => {
    const comp = TestBed.createComponent(PasswordFieldComponent).componentInstance;
    comp.writeValue('abc');
    expect(comp.value).toBe('abc');
    comp.writeValue(null as unknown as string);
    expect(comp.value).toBe('');
  });

  it('setDisabledState reflète l\'état désactivé', () => {
    const comp = TestBed.createComponent(PasswordFieldComponent).componentInstance;
    comp.setDisabledState(true);
    expect(comp.disabled).toBe(true);
  });
});
