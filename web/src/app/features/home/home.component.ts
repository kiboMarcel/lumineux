import { Component, inject } from '@angular/core';
import { SessionStore } from '../../core/session/session-store';

/** Accueil de la console après connexion (feature 008). */
@Component({
  selector: 'app-home',
  imports: [],
  template: `
    <div class="lx-page-head">
      <div>
        <h1 class="lx-title">Bienvenue, {{ session.currentUser()?.displayName }}</h1>
        <p class="lx-subtitle">Console Lumineux — les modules disponibles dépendent de vos droits.</p>
      </div>
    </div>

    <div class="lx-card">
      <h2 style="font-size:1rem; margin:0 0 12px;">Vos droits</h2>
      @if (session.permissions().length === 0) {
        <p class="lx-empty">Aucun droit de gestion n'est associé à votre compte.</p>
      } @else {
        <div class="lx-tags">
          @for (p of session.permissions(); track p) {
            <span class="lx-pill lx-pill-info lx-pill-plain">{{ p }}</span>
          }
        </div>
      }
    </div>
  `,
})
export class HomeComponent {
  readonly session = inject(SessionStore);
}
