import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MembersApi } from '../../../core/api/members-api';
import { MemberResponse } from '../member.models';

/** Fiche d'un membre (feature 009, US1, FR-004/005). Aucun secret exposé. */
@Component({
  selector: 'app-member-detail',
  imports: [RouterLink],
  template: `
    <div class="lx-card">
      @if (notFound()) {
        <div class="lx-alert lx-alert-error" role="alert">Membre introuvable.</div>
        <a routerLink="/members">Retour à la liste</a>
      } @else if (member(); as m) {
        <div class="lx-page-head">
          <h1 class="lx-title" style="margin:0;">{{ m.firstName }} {{ m.lastName }}</h1>
          <div class="lx-links" style="margin:0;">
            <a class="lx-btn lx-btn-ghost" [routerLink]="['/members', m.id, 'profiles']">Profils & droits</a>
            <a class="lx-btn lx-btn-ghost" [routerLink]="['/members', m.id, 'edit']">Modifier</a>
          </div>
        </div>
        <dl style="display:grid; grid-template-columns:auto 1fr; gap:0.4rem 1rem;">
          <dt class="lx-muted">Référence</dt><dd>{{ m.reference }}</dd>
          <dt class="lx-muted">Sexe</dt><dd>{{ m.gender }}</dd>
          <dt class="lx-muted">Mobile</dt><dd>{{ m.mobile || '—' }}</dd>
          <dt class="lx-muted">E-mail</dt><dd>{{ m.email || '—' }}</dd>
          <dt class="lx-muted">Profession</dt><dd>{{ m.profession || '—' }}</dd>
          <dt class="lx-muted">Statut</dt><dd>{{ m.status }}</dd>
          <dt class="lx-muted">Activation du compte</dt><dd>{{ m.accountActivationState }}</dd>
        </dl>
        <div class="lx-links"><a routerLink="/members">Retour à la liste</a></div>
      } @else {
        <p class="lx-muted">Chargement…</p>
      }
    </div>
  `,
})
export class MemberDetailComponent {
  private readonly api = inject(MembersApi);
  private readonly route = inject(ActivatedRoute);

  readonly member = signal<MemberResponse | null>(null);
  readonly notFound = signal(false);

  constructor() {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.api.get(id).subscribe({
      next: (m) => this.member.set(m),
      error: (err: HttpErrorResponse) => {
        if (err.status === 404) {
          this.notFound.set(true);
        }
      },
    });
  }
}
