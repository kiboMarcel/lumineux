import { Injectable, signal } from '@angular/core';

export type NotificationKind = 'error' | 'success' | 'info';

export interface Notification {
  kind: NotificationKind;
  message: string;
}

/** File d'affichage des messages transverses (erreurs mappées, confirmations). */
@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly _current = signal<Notification | null>(null);
  readonly current = this._current.asReadonly();

  error(message: string): void { this._current.set({ kind: 'error', message }); }
  success(message: string): void { this._current.set({ kind: 'success', message }); }
  info(message: string): void { this._current.set({ kind: 'info', message }); }
  clear(): void { this._current.set(null); }
}
