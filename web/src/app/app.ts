import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NotificationService } from './shared/notifications/notification.service';

/**
 * Racine de l'application : bandeau de notifications transverses (erreurs mappées, messages de
 * session) + zone de routage. Aucun secret n'y est affiché.
 */
@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  template: `
    @if (notifier.current(); as n) {
      <div class="lx-alert lx-alert-{{ n.kind }}" role="status" style="margin: 0.5rem; text-align: center;">
        {{ n.message }}
        <button type="button" class="lx-btn lx-btn-link" (click)="notifier.clear()" aria-label="Fermer">×</button>
      </div>
    }
    <router-outlet />
  `,
})
export class App {
  readonly notifier = inject(NotificationService);
}
