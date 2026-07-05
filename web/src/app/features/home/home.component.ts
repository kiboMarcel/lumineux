import { Component, inject } from '@angular/core';
import { SessionStore } from '../../core/session/session-store';

/** Accueil de la console après connexion (feature 008). */
@Component({
  selector: 'app-home',
  imports: [],
  template: `
    <div class="lx-card">
      <h1 class="lx-title">Bienvenue, {{ session.currentUser()?.displayName }}</h1>
      <p class="lx-muted">
        Vous êtes connecté à la console Lumineux. Les modules disponibles dépendent de vos droits.
      </p>
      @if (session.permissions().length === 0) {
        <p class="lx-muted">Aucun droit de gestion n'est associé à votre compte.</p>
      } @else {
        <ul>
          @for (p of session.permissions(); track p) {
            <li>{{ p }}</li>
          }
        </ul>
      }
    </div>
  `,
})
export class HomeComponent {
  readonly session = inject(SessionStore);
}
